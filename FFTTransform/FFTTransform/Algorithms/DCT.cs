using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms
{
    public class DCT
    {
        public DCT() { }

        public int[,] Transform(byte[,] matrix)
        {
            return new int[,] { { 0 } };
        }

        internal byte[,] InverseTransform(int[,] yChannelBefore)
        {
            throw new NotImplementedException();
        }
    }
}
