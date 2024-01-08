using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFTTransform.Algorithms.Encoder
{
    public class JpegTriplet : IEquatable<JpegTriplet>
    {
        public JpegTriplet()
        {
            ZerosBefore = (char)0;
            NmbBitsForCoeff = (char)0;
            Coeff = 0;
        }

        public JpegTriplet(int z, int nmbB, short coefficient)
        {
            ZerosBefore = (char)z;
            NmbBitsForCoeff = (char)nmbB;
            Coeff = coefficient;
        }

        public JpegTriplet(JpegTriplet jpegTriplet)
        {
            ZerosBefore = jpegTriplet.ZerosBefore;
            NmbBitsForCoeff = jpegTriplet.NmbBitsForCoeff;
            Coeff = jpegTriplet.Coeff;
        }

        public char ZerosBefore { get; set; }
        public char NmbBitsForCoeff { get; set; }
        public short Coeff { get; set; }

        public static JpegTriplet EOB()
        {
            return new JpegTriplet();
        }

        public static bool operator==(JpegTriplet obj1, JpegTriplet obj2)
        {
            if (ReferenceEquals(obj1, obj2))
                return true;

            if (ReferenceEquals(obj1, null) || ReferenceEquals(obj2, null))
                return false;

            return obj1.Equals(obj2);
        }

        public static bool operator !=(JpegTriplet obj1, JpegTriplet obj2)
        {
            return !(obj1 == obj2);
        }


        public bool Equals(JpegTriplet? other)
        {
            if (ReferenceEquals(other, null))
                return false;

            return ZerosBefore == other.ZerosBefore && NmbBitsForCoeff == other.NmbBitsForCoeff && Coeff == other.Coeff;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ZerosBefore, NmbBitsForCoeff, Coeff);
        }

    }
}
