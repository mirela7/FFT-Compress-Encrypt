using Emgu.CV.Features2D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Utils
{
    internal class BitArrayComparer : IEqualityComparer<BitArray>
    {
        public bool Equals(BitArray? x, BitArray? y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;
            if(x.Count != y.Count) return false;

            for(int i = 0; i < x.Count; i++)
                if (x[i] != y[i]) 
                    return false;
            return true;
        }

        public int GetHashCode(BitArray? obj)
        {
            if (obj is null)
                return 0;
            int hashCode = HashCode.Combine(obj.Count);
            foreach (bool c in obj)
                hashCode = HashCode.Combine(hashCode, c == true ? 1 : 0);
            return hashCode;
        }
    }
}
