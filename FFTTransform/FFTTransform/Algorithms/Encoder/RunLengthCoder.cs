using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms.Encoder
{
    public class RunLengthCoder
    {

        public class ChannelDcAcResult
        {
            public ChannelDcAcResult()
            {
                ACs = new List<JpegTriplet>();
                DCs = new List<JpegTriplet>();
            }
            public List<JpegTriplet> ACs { get; set; }
            public List<JpegTriplet> DCs { get; set; }

            public void AddZigZag(List<JpegTriplet> zigzag)
            {
                ACs.Add(zigzag[0]);
                DCs.AddRange(zigzag.Skip(1));
            }
        }
        
        /// <summary>
        /// Given the (8x8) matrix, processes run-length encoding via zig-zag traverse.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static List<JpegTriplet> ZigZagCode(int[,] matrix)
        {
            int direction = -1;
            int i = 0, j = 0;
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);

            Func<int, int, bool> inBounds = (i, j) => i >= 0 && j >= 0 && i < rows && j < cols;

            List<int> zigZagTraverse = new List<int>();

            while(!(i == rows-1 && j == cols-1))
            {
                while(inBounds(i, j))
                {
                    zigZagTraverse.Add(matrix[i, j]);
                    i += direction;
                    j -= direction;
                }

                if(i >= rows) // second half, going down
                {
                    i --;
                    j += 2;
                }
                else if (j >= cols) // second half, going up
                {
                    j--;
                    i += 2;
                }
                if (i < 0) // first half, exceeded top
                    i = 0;
                else if (j < 0) // first half, exceeded left
                    j = 0;

                direction *= -1;
            }
            foreach (var x in zigZagTraverse)
                Console.Write($"{x} ");

            List<JpegTriplet> rlEncoded = new();
            int count0 = 0;
            foreach(var x in zigZagTraverse)
            {
                if (x == 0)
                    count0++;
                else
                {
                    rlEncoded.Add(new JpegTriplet(count0, (short)Math.Log(x, 2) + 1, (short)x));
                    count0 = 0;
                }
            }
            rlEncoded.Add(JpegTriplet.EOB());
            return rlEncoded;
        }

        
    }
}
