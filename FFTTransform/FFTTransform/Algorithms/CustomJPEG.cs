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

    public class CustomJPEG
    {
        const short MCU_Unit = 8;

        public Image<Ycc, byte> Image { get; set; }
        public Image<Ycc, byte> DownsampledCbCr { get; set; }


        //public FourierRelatedTransform<T, object> Transform { get; set; }
        public DCT Transform { get; set; }

        public CustomJPEG()
        {
            Transform = new DCT();
        }

        public void InitImage(string path)
        {
            Mat inputImage = CvInvoke.Imread(path);
            if (inputImage == null || inputImage.IsEmpty)
                throw new ArgumentException("Invalid path.");
            
            Console.WriteLine("Is it grayscale? Defaults to 'no'");
            string response = Console.ReadLine();
            bool isGrayscale = false;
            if (response != null && response.Length > 0 && response[0] == 'y')
                isGrayscale = true;

            if (!isGrayscale)
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
            InitImage(path);
            Console.WriteLine("Downsampling...");
            ChromaDownsample();
            ZigzagResult y, cb, cr;
            Console.WriteLine("Processing MCUs...");
            (y, cb, cr) = ProcessMCU();

            HuffmanCoder dcY = new(), dcC = new(), acY = new(), acC = new();

            dcY.AddRange(y.DCs);
            acY.AddRange(y.ACs);

            dcC.AddRange(cb.DCs);
            dcC.AddRange(cr.DCs);

            acC.AddRange(cb.ACs);
            acC.AddRange(cr.ACs);

            Console.WriteLine("Hufman encoding dcY MCUs...");
            dcY.Encode();
            Console.WriteLine("Hufman encoding dcC MCUs...");
            dcC.Encode();
            Console.WriteLine("Hufman encoding acY MCUs...");
            acY.Encode();
            Console.WriteLine("Hufman encoding acC MCUs...");
            acC.Encode();

            string outputPath = $"{PathUtil.GetPathWithoutFileExtension(path)}_dct_compressed.bin";
            Console.WriteLine("Writing to file...");
            FileReaderWriter.WriteJpeg(outputPath, Image.Width, Image.Height, dcY, dcC, acY, acC);
        }

        public void Decode(string path)
        {
            int imageWidth, imageHeight;
            HuffmanCoder dcY, dcC, acY, acC;

            Console.WriteLine("Reading data...");
            FileReaderWriter.ReadJpeg(path, out imageWidth, out imageHeight, out dcY, out dcC, out acY, out acC);

            Console.WriteLine("Rebuilding MCUs....");
            RebuildMCU(imageWidth, imageHeight, dcY, dcC, acY, acC);
            Console.WriteLine("Upsampling...");
            ChromaUpsample();

            string outputPath = $"{PathUtil.GetPathWithoutFileExtension(path)}_decompressed.bmp";

            Image<Bgr, byte> transformed = Image.Convert<Bgr, byte>();
            CvInvoke.Imwrite(outputPath, transformed);
            CvInvoke.Imshow("Image", transformed);
            //CvInvoke.WaitKey();
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
            for(int i = 0; i < Image.Height; i ++)
                for(int j = 0; j < Image.Width; j++)
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
            DownsampledCbCr = new Image<Ycc, byte>(Image.Width / 2, Image.Height / 2);


            for (int i = 0; i < imageWidth; i+=MCU_Unit)
            {
                for (int j = 0; j < imageHeight; j += MCU_Unit)
                {
                    List<JpegTriplet> unitYList = yMcu.PopZigZag();
                    int[,] YChannelBefore = RunLengthCoder.ZigZagDecode(unitYList, MCU_Unit);

                    double[,] YDequan = Quantization.Dequantize(YChannelBefore, Quantization.QuantizationType.YQUANTIZATION);
                    byte[,] YChannel = DCT.ConvertDoubleMatrixToByteMatrix(Transform.Transform(YDequan, inverse:true));


                    PutYMCU(i, j, YChannel);

                    if (i % (2 * MCU_Unit) == 0 && j % (2 * MCU_Unit) == 0)
                    {
                        List<JpegTriplet> unitCbList = cMcu.PopZigZag();
                        List<JpegTriplet> unitCrList = cMcu.PopZigZag();

                        int[,] CbChannelBefore = RunLengthCoder.ZigZagDecode(unitCbList, MCU_Unit);
                        int[,] CrChannelBefore = RunLengthCoder.ZigZagDecode(unitCrList, MCU_Unit);

                        double[,] CbDequan = Quantization.Dequantize(CbChannelBefore, Quantization.QuantizationType.CQUANTIZATION);
                        double[,] CrDequan = Quantization.Dequantize(CrChannelBefore, Quantization.QuantizationType.CQUANTIZATION);


                        //CbChannelBefore = Quantization.Dequantize(CbChannelBefore, Type.Y);
                        //CrChannelBefore = Quantization.Dequantize(CrChannelBefore, Type.Y);

                        byte[,] CbChannel = DCT.ConvertDoubleMatrixToByteMatrix(Transform.Transform(CbDequan, inverse:true));
                        byte[,] CrChannel = DCT.ConvertDoubleMatrixToByteMatrix(Transform.Transform(CrDequan, inverse:true));
                        
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

                    double[,] yPrepared = DCT.ConvertByteMatrixToDoubleMatrix(YChannel);
                    int[,] YTransformed = Quantization.Quantize(Transform.Transform(yPrepared, inverse:false), Quantization.QuantizationType.YQUANTIZATION);
                    
                    List<JpegTriplet> YCompressed = RunLengthCoder.ZigZagCode(YTransformed);


                    yMcu.AddZigZag(YCompressed);

                    if(i % (2*MCU_Unit) == 0 && j % (2*MCU_Unit) == 0)
                    {
                        byte[,] CbChannel = SampleCbMCU(i, j);
                        byte[,] CrChannel = SampleCrMCU(i, j);
                        
                        double[,] cbPrepared = DCT.ConvertByteMatrixToDoubleMatrix(CbChannel);
                        double[,] crPrepared = DCT.ConvertByteMatrixToDoubleMatrix(CrChannel);


                        int[,] CbTransformed = Quantization.Quantize(Transform.Transform(cbPrepared, inverse: false), Quantization.QuantizationType.CQUANTIZATION);
                        int[,] CrTransformed = Quantization.Quantize(Transform.Transform(crPrepared, inverse: false), Quantization.QuantizationType.CQUANTIZATION);

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
