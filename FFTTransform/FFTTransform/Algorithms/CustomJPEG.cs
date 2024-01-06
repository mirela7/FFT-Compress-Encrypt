using Emgu.CV;
using Emgu.CV.Structure;
using FFTTransform.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    public class CustomJPEG<T1, T2>
    {
        const short MCU_Unit = 8;

        public Image<Ycc, byte> Image { get; set; }
        public Image<Ycc, byte> DownsampledCbCr { get; set; }


        //public FourierRelatedTransform<T, object> Transform { get; set; }
        public FourierRelatedTransform Transform { get; set; }

        public CustomJPEG(Image<Gray, byte> image) 
        {
            Transform = FourierRelatedTransform.Factory(FourierRelatedTransform.TransformType.FFT);
            Image = ConvertImage(image);
        }

        public Image<Ycc, byte> ConvertImage(Image<Gray, byte> image)
        {
            return image.Convert<Ycc, byte>();
        }

        public void Encode(string path)
        {
            ChromaDownsample();
            ImageArrayData<object>[,,] data = ProcessMCU();
            FileReaderWriter.Save(path, data, Transform);
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

        public ImageArrayData<object>[,,] ProcessMCU()
        {
            ImageArrayData<object>[,,] MCUArrays = new ImageArrayData<object>[Image.Rows/MCU_Unit, Image.Cols/MCU_Unit, 3];
            for (int i = 0; i < Image.Rows; i+= MCU_Unit)
            {
                for(int j = 0; j < Image.Cols; j+= MCU_Unit)
                {
                    byte[,] YChannel = SampleYMCU(i, j);
                    object[,] YTransformed = Transform.Transform(YChannel);
                    ImageArrayData<object> YCompressed = Transform.WriteToArrayData(YTransformed);
                    MCUArrays[i/MCU_Unit, j/MCU_Unit, 0] = YCompressed;

                    if(i % (2*MCU_Unit) == 0 && j % (2*MCU_Unit) == 0)
                    {
                        byte[,] CbChannel = SampleCbMCU(i, j);
                        byte[,] CrChannel = SampleCrMCU(i, j);

                        object[,] CbTransformed = Transform.Transform(CbChannel);
                        object[,] CrTransformed = Transform.Transform(CrChannel);

                        ImageArrayData<object> CbCompressed = Transform.WriteToArrayData(CbTransformed);
                        ImageArrayData<object> CrCompressed = Transform.WriteToArrayData(CrTransformed);

                        MCUArrays[i / MCU_Unit, j / MCU_Unit, 1] = CbCompressed;
                        MCUArrays[i / MCU_Unit, j / MCU_Unit, 2] = CrCompressed;
                    }
                }
            }
            return MCUArrays;
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
