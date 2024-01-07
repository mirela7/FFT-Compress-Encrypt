using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Utils
{
    internal static class BinaryWriterExtension
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

            bw.Write(length);
            bw.Write(data);
        }
    }
}
