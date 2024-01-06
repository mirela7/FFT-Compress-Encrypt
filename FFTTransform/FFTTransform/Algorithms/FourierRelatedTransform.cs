using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    

    public abstract class FourierRelatedTransform
    {
        public enum TransformType
        {
            FFT,
            DCT
        }
        public abstract object[,] Transform(byte[,] channel);

        public abstract ImageArrayData<object> WriteToArrayData(object[,] channelData);
        public abstract byte[,] InverseTransform(object[,] data);

        public abstract object[,] ReadFromArrayData(ImageArrayData<object> arrayData);

        public abstract void SerializeObject<T>(BinaryWriter bw, T obj);

        public abstract object DeserializeObject(BinaryReader br);

        public static FourierRelatedTransform Factory(TransformType type)
        {
            switch(type)
            {
                case TransformType.FFT:
                    return new FFT();
                default: throw new InvalidOperationException("Invalid type specified.");
            }

        }
    }
}