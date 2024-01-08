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

            public ChannelDcAcResult(List<JpegTriplet> aCs, List<JpegTriplet> dCs)
            {
                ACs = aCs;
                DCs = dCs;
            }

            public List<JpegTriplet> ACs { get; set; }
            public List<JpegTriplet> DCs { get; set; }

            public void AddZigZag(List<JpegTriplet> zigzag)
            {
                if (zigzag[0].ZerosBefore == 0)
                {
                    ACs.Add(zigzag[0]);
                    if(zigzag.Count > 1)
                        DCs.AddRange(zigzag.Skip(1));
                    else 
                        DCs.AddRange(zigzag);
                }
                else
                {
                    ACs.Add(new JpegTriplet(0, 0, 0));
                    zigzag[0].ZerosBefore--;
                    DCs.AddRange(zigzag);
                }
            }

            private int indexAtAc = 0;
            private int indexAtDc = 0;

            public List<JpegTriplet> PopZigZag()
            {
                List<JpegTriplet> elements = new();
                elements.Add(ACs[indexAtAc++]);
                //ACs.RemoveAt(0);
                elements.AddRange(DCs.Skip(indexAtDc).TakeWhile((triplet) => triplet != JpegTriplet.EOB()));
                indexAtDc += elements.Count;
                //DCs = DCs.Skip(elements.Count).ToList();
                return elements;
            }
        }
        
        /// <summary>
        /// Given the (8x8) matrix, processes run-length encoding via zig-zag traverse.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static List<JpegTriplet> ZigZagCode(int[,] matrix)
        {
            /*int direction = -1;
            int i = 0, j = 0;
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);

            Func<int, int, bool> inBounds = (i, j) => i >= 0 && j >= 0 && i < rows && j < cols;*/

            List<int> zigZagTraverse = new List<int>();
            ZigZagTraverse(matrix, (i, j) => zigZagTraverse.Add(matrix[i, j]));
/*
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
            }*/
            
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

        public static int[,] ZigZagDecode(List<JpegTriplet> zigZag, int mcuUnit)
        {
            int[,] section = new int[8, 8];
            List<int> decompressedZigzag = new();
            int i;
            for(i = 0; i < zigZag.Count; i++)
            {
                while (zigZag[i].ZerosBefore != (char)0)
                {
                    decompressedZigzag.Add(0);
                    zigZag[i].ZerosBefore--;
                }
                decompressedZigzag.Add(zigZag[i].Coeff);
            }

            while (decompressedZigzag.Count < mcuUnit * mcuUnit)
                decompressedZigzag.Add(0);

            //if(decompressedZigzag.Count != mcuUnit*mcuUnit)
             //   throw new InvalidOperationException("Decompression not reached expected number of elements (mcuUnit * mcuUnit).");
            int k = 0;
            ZigZagTraverse(section, (i, j) => { section[i, j] = decompressedZigzag[k++]; });

            return section;
        }

        private static void ZigZagTraverse(int[,] matrix, Action<int, int> operation)
        {
            int direction = -1;
            int i = 0, j = 0;
            int rows = matrix.GetLength(0), cols = matrix.GetLength(1);
            Func<int, int, bool> inBounds = (i, j) => i >= 0 && j >= 0 && i < rows && j < cols;
            while (!(i == rows - 1 && j == cols - 1))
            {
                while (inBounds(i, j))
                {
                    operation(i, j);
                    i += direction;
                    j -= direction;
                }

                if (i >= rows) // second half, going down
                {
                    i--;
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
        }

        
    }
}
