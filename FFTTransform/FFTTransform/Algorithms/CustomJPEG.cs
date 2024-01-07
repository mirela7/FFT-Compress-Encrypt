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

            FileReaderWriter.WriteJpeg(path, Image.Width, Image.Height, dcY, dcC, acY, acC);

            //FileReaderWriter.Save(path, data, Transform);
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

        public (ZigzagResult, ZigzagResult, ZigzagResult) ProcessMCU()
        {

            ImageArrayData<object>[,,] MCUArrays = new ImageArrayData<object>[Image.Rows/MCU_Unit, Image.Cols/MCU_Unit, 3];

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

        public byte[,] SampleCbMCU(int tli, int tlj)
        {
            byte[,] bytes = new byte[MCU_Unit, MCU_Unit];
            for (int i = tli/2; i < tli/2 + MCU_Unit; i++)
                for (int j = tlj/2; j < tlj/2 + MCU_Unit; j++)
                    bytes[i-tli/2, j-tlj/2] = DownsampledCbCr.Data[i, j, 1];
            return bytes;
        }

        public byte[,] SampleCrMCU(int tli, int tlj)
        {
            byte[,] bytes = new byte[MCU_Unit, MCU_Unit];
            for (int i = tli/2; i < tli/2 + MCU_Unit; i++)
                for (int j = tlj/2; j < tlj/2 + MCU_Unit; j++)
                    bytes[i - tli / 2, j - tlj / 2] = DownsampledCbCr.Data[i, j, 2];
            return bytes;
        }
    }
}
