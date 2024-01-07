using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Utils
{
    internal static class BinaryReaderWriterExtension
    {
        public static void WriteCompact(this BinaryWriter bw, BitArray bitArray)
        {
            int length = bitArray.Count;
            byte[] data = new byte[length / 8 + 1];

            byte pow = 7;
            int index = 0;
            byte dataByte = 0;
            foreach (bool boolBit in bitArray)
            {
                byte bit = boolBit == true ? (byte)1 : (byte)0;
                dataByte = (byte)(dataByte | (bit << pow));
                if (pow == 0)
                {
                    data[index++] = dataByte;
                    dataByte = 0;
                    pow = 7;
                }
                else
                    pow--;
            }
            if (index < data.Length && dataByte != 0)
                data[index++] = dataByte;

            bw.Write(length);
            bw.Write(data);
        }

        public static BitArray ReadCompact(this BinaryReader br)
        {
            int length = br.ReadInt32();

            BitArray arr = new BitArray(length);

            byte[] data = br.ReadBytes(length / 8 + 1);

            int index = 0;
            foreach (byte byt in data)
            {
                byte pow = 7;
                while(pow >= 0 && index < length)
                {
                    arr.Set(index, (byt & (1<<pow)) != 0);
                    index++;
                    if (pow == 0)
                        break;
                    pow--;
                }
                if (index >= length)
                    break;
            }
            return arr;
        }
    }
}
