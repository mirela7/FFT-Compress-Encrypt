using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using FFTTransform.Algorithms.Encoder;
using FFTTransform.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    using static System.Net.Mime.MediaTypeNames;
    using ZigzagResult = RunLengthCoder.ChannelDcAcResult;

    public class CustomJPEG<T1, T2>
    {
        const short MCU_Unit = 8;

        public Image<Ycc, byte> Image { get; set; }
        public Image<Ycc, byte> DownsampledCbCr { get; set; }


        //public FourierRelatedTransform<T, object> Transform { get; set; }
        public DCT Transform { get; set; }

        public CustomJPEG(string path)
        {
            Transform = new DCT();

            Mat inputImage = CvInvoke.Imread(path);
            if (inputImage == null || inputImage.IsEmpty)
                throw new ArgumentException("Invalid path.");
            if (inputImage.Depth == DepthType.Cv8U && inputImage.NumberOfChannels == 3)
            {
                Image<Bgr, byte> colorImage = new(inputImage);
                Image = colorImage.Convert<Ycc, byte>();
            }
            else
            {
                Image<Gray, byte> colorImage = new(inputImage);
                Image = colorImage.Convert<Ycc, byte>();
            }
        }

        public void Encode(string path)
        {
            ChromaDownsample();
            ZigzagResult y, cb, cr;
            (y, cb, cr) = ProcessMCU();

            HuffmanCoder dcY = new(), dcC = new(), acY = new(), acC = new();

            dcY.AddRange(y.DCs);
            acY.AddRange(y.ACs);

            dcC.AddRange(cb.DCs);
            dcC.AddRange(cr.DCs);

            acC.AddRange(cb.ACs);
            acC.AddRange(cr.ACs);

            dcY.Encode();
            dcC.Encode();
            acY.Encode();
            acC.Encode();

            string outputPath = $"{PathUtil.GetPathWithoutFileExtension(path)}_decompressed.bin";

            FileReaderWriter.WriteJpeg(outputPath, Image.Width, Image.Height, dcY, dcC, acY, acC);
        }

        public void Decode(string path)
        {
            int imageWidth, imageHeight;
            HuffmanCoder dcY, dcC, acY, acC;

            FileReaderWriter.ReadJpeg(path, out imageWidth, out imageHeight, out dcY, out dcC, out acY, out acC);

            RebuildMCU(imageWidth, imageHeight, dcY, dcC, acY, acC);
            ChromaUpsample();

            string outputPath = $"{PathUtil.GetPathWithoutFileExtension(path)}_decompressed.bmp";

            CvInvoke.Imwrite(outputPath, Image);
            CvInvoke.Imshow("Image", Image);
            CvInvoke.WaitKey();
        }

        public void ChromaDownsample()
        {
            DownsampledCbCr = new Image<Ycc, byte>(Image.Width/2, Image.Height/2);
            for(int i = 0; i < DownsampledCbCr.Height; i++)
            {
                for(int j = 0; j < DownsampledCbCr.Width; j++)
                {
                    DownsampledCbCr.Data[i, j, 1] = Image.Data[i * 2, j * 2, 1];
                    DownsampledCbCr.Data[i, j, 2] = Image.Data[i * 2, j * 2, 2];
                }
            }

        }

        public void ChromaUpsample()
        {
            for(int i = 0; i <= Image.Height; i ++)
                for(int j = 0; j <= Image.Width; j++)
                {
                    Image.Data[i, j, 1] = DownsampledCbCr.Data[i / 2, j / 2, 1];
                    Image.Data[i, j, 2] = DownsampledCbCr.Data[i / 2, j / 2, 2];
                }
        }

        public void RebuildMCU(int imageWidth, int imageHeight, HuffmanCoder dcY, HuffmanCoder dcC, HuffmanCoder acY, HuffmanCoder acC)
        {

            ZigzagResult yMcu = new(acY.Triplets, dcY.Triplets);
            ZigzagResult cMcu = new(acC.Triplets, dcC.Triplets);
            Image = new Image<Ycc, byte>(imageWidth, imageHeight);

            for (int i = 0; i < imageWidth; i+=MCU_Unit)
            {
                for(int j = 0; j < imageHeight; j += MCU_Unit)
                {
                    List<JpegTriplet> unitYList = yMcu.PopZigZag();
                    int[,] YChannelBefore = RunLengthCoder.ZigZagDecode(unitYList, MCU_Unit);

                    //YChannelBefore = Quantization.Dequantize(YChannelBefore, Type.Y);
                    byte[,] YChannel = Transform.InverseTransform(YChannelBefore);

                    PutYMCU(i, j, YChannel);

                    if (i % (2 * MCU_Unit) == 0 && j % (2 * MCU_Unit) == 0)
                    {
                        List<JpegTriplet> unitCbList = cMcu.PopZigZag();
                        List<JpegTriplet> unitCrList = cMcu.PopZigZag();

                        int[,] CbChannelBefore = RunLengthCoder.ZigZagDecode(unitCbList, MCU_Unit);
                        int[,] CrChannelBefore = RunLengthCoder.ZigZagDecode(unitCrList, MCU_Unit);

                        //CbChannelBefore = Quantization.Dequantize(CbChannelBefore, Type.Y);
                        //CrChannelBefore = Quantization.Dequantize(CrChannelBefore, Type.Y);

                        byte[,] CbChannel = Transform.InverseTransform(CbChannelBefore);
                        byte[,] CrChannel = Transform.InverseTransform(CrChannelBefore);
                        
                        PutCbMCU(i, j, CbChannel);
                        PutCrMCU(i, j, CrChannel);
                    }
                }
            }
        }

        public (ZigzagResult, ZigzagResult, ZigzagResult) ProcessMCU()
        {
            ZigzagResult yMcu = new();
            ZigzagResult cbMcu = new();
            ZigzagResult crMcu = new();

            for (int i = 0; i < Image.Rows; i+= MCU_Unit)
            {
                for(int j = 0; j < Image.Cols; j+= MCU_Unit)
                {
                    byte[,] YChannel = SampleYMCU(i, j);

                    int[,] YTransformed = Transform.Transform(YChannel);
                    List<JpegTriplet> YCompressed = RunLengthCoder.ZigZagCode(YTransformed);
                    
                    yMcu.AddZigZag(YCompressed);

                    if(i % (2*MCU_Unit) == 0 && j % (2*MCU_Unit) == 0)
                    {
                        byte[,] CbChannel = SampleCbMCU(i, j);
                        byte[,] CrChannel = SampleCrMCU(i, j);

                        int[,] CbTransformed = Transform.Transform(CbChannel);
                        int[,] CrTransformed = Transform.Transform(CrChannel);

                        List<JpegTriplet> CbCompressed = RunLengthCoder.ZigZagCode(CbTransformed);
                        List<JpegTriplet> CrCompressed = RunLengthCoder.ZigZagCode(CrTransformed);

                        cbMcu.AddZigZag(CbCompressed);
                        crMcu.AddZigZag(CrCompressed);
                    }
                }
            }
            return (yMcu, cbMcu, crMcu);
        }

        public byte[,] SampleYMCU(int tli, int tlj)
        {
            byte[,] bytes = new byte[MCU_Unit, MCU_Unit];
            for (int i = tli; i < tli + MCU_Unit; i++)
                for (int j = tlj; j < tlj + MCU_Unit; j++)
                    bytes[i - tli, j - tlj] = Image.Data[i, j, 0];
            return bytes;
        }

        public void PutYMCU(int tli, int tlj, byte[,] data)
        {
            for (int i = tli; i < tli + MCU_Unit; i++)
                for (int j = tlj; j < tlj + MCU_Unit; j++)
                    Image.Data[i, j, 0] = data[i-tli, j- tlj];
        }

        public byte[,] SampleCbMCU(int tli, int tlj)
        {
            byte[,] bytes = new byte[MCU_Unit, MCU_Unit];
            for (int i = tli/2; i < tli/2 + MCU_Unit; i++)
                for (int j = tlj/2; j < tlj/2 + MCU_Unit; j++)
                    bytes[i-tli/2, j-tlj/2] = DownsampledCbCr.Data[i, j, 1];
            return bytes;
        }

        public void PutCbMCU(int tli, int tlj, byte[,] data)
        {
            for (int i = tli / 2; i < tli / 2 + MCU_Unit; i++)
                for (int j = tlj / 2; j < tlj / 2 + MCU_Unit; j++)
                    DownsampledCbCr.Data[i, j, 1] = data[i - tli / 2, j - tlj / 2];
        }

        public byte[,] SampleCrMCU(int tli, int tlj)
        {
            byte[,] bytes = new byte[MCU_Unit, MCU_Unit];
            for (int i = tli/2; i < tli/2 + MCU_Unit; i++)
                for (int j = tlj/2; j < tlj/2 + MCU_Unit; j++)
                    bytes[i - tli / 2, j - tlj / 2] = DownsampledCbCr.Data[i, j, 2];
            return bytes;
        }

        public void PutCrMCU(int tli, int tlj, byte[,] data)
        {
            for (int i = tli / 2; i < tli / 2 + MCU_Unit; i++)
                for (int j = tlj / 2; j < tlj / 2 + MCU_Unit; j++)
                    DownsampledCbCr.Data[i, j, 2] = data[i - tli / 2, j - tlj / 2];
        }
    }
}
