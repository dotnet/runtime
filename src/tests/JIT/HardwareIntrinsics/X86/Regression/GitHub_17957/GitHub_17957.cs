using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using Xunit;

namespace GitHub_17957
{
    public class Program
    {
        const int Pass = 100;
        const int Fail = 0;

        [Fact]
        public static void Test()
        {
            Assert.True(Test128());
            Assert.True(Test256());
        }

        public static bool Test128()
        {
            Vector128<short> vs = Vector128<short>.Zero;
            vs = vs.WithElement(0, (short)(-1));
            if (vs.GetElement(0) != -1)
            {
                return false;
            }

            vs = vs.WithElement(3, (short)(-1));
            if (vs.GetElement(3) != -1)
            {
                return false;
            }

            vs = vs.WithElement(7, (short)(-1));
            if (vs.GetElement(7) != -1)
            {
                return false;
            }


            Vector128<ushort> vus = Vector128<ushort>.Zero;
            vus = vus.WithElement(0, ushort.MaxValue);
            if (vus.GetElement(0) != ushort.MaxValue)
            {
                return false;
            }

            vus = vus.WithElement(3, ushort.MaxValue);
            if (vus.GetElement(3) != ushort.MaxValue)
            {
                return false;
            }

            vus = vus.WithElement(7, ushort.MaxValue);
            if (vus.GetElement(7) != ushort.MaxValue)
            {
                return false;
            }


            Vector128<sbyte> vsb = Vector128<sbyte>.Zero;
            vsb = vsb.WithElement(0, (sbyte)(-1));
            if (vsb.GetElement(0) != -1)
            {
                return false;
            }

            vsb = vsb.WithElement(7, (sbyte)(-1));
            if (vsb.GetElement(7) != -1)
            {
                return false;
            }

            vsb = vsb.WithElement(15, (sbyte)(-1));
            if (vsb.GetElement(15) != -1)
            {
                return false;
            }

            Vector128<byte> vb = Vector128<byte>.Zero;
            vb = vb.WithElement(0, byte.MaxValue);
            if (vb.GetElement(0) != byte.MaxValue)
            {
                return false;
            }

            vb = vb.WithElement(7, byte.MaxValue);
            if (vb.GetElement(7) != byte.MaxValue)
            {
                return false;
            }

            vb = vb.WithElement(15, byte.MaxValue);
            if (vb.GetElement(15) != byte.MaxValue)
            {
                return false;
            }

            Vector128<float> vf = Vector128<float>.Zero;
            vf = vf.WithElement(0, -1.0f);
            if (vf.GetElement(0) != -1.0f)
            {
                return false;
            }

            vf = vf.WithElement(1, -1f);
            if (vf.GetElement(1) != -1.0f)
            {
                return false;
            }

            vf = vf.WithElement(2, -1f);
            if (vf.GetElement(2) != -1.0f)
            {
                return false;
            }

            vf = vf.WithElement(3, -1.0f);
            if (vf.GetElement(3) != -1.0f)
            {
                return false;
            }

            return true;
        }

        public static bool Test256()
        {
            Vector256<short> vs = Vector256<short>.Zero;
            vs = vs.WithElement(0, (short)(-1));
            if (vs.GetElement(0) != -1)
            {
                return false;
            }

            vs = vs.WithElement(3, (short)(-1));
            if (vs.GetElement(3) != -1)
            {
                return false;
            }

            vs = vs.WithElement(9, (short)(-1));
            if (vs.GetElement(9) != -1)
            {
                return false;
            }


            Vector256<ushort> vus = Vector256<ushort>.Zero;
            vus = vus.WithElement(0, ushort.MaxValue);
            if (vus.GetElement(0) != ushort.MaxValue)
            {
                return false;
            }

            vus = vus.WithElement(3, ushort.MaxValue);
            if (vus.GetElement(3) != ushort.MaxValue)
            {
                return false;
            }

            vus = vus.WithElement(8, ushort.MaxValue);
            if (vus.GetElement(8) != ushort.MaxValue)
            {
                return false;
            }


            Vector256<sbyte> vsb = Vector256<sbyte>.Zero;
            vsb = vsb.WithElement(0, (sbyte)(-1));
            if (vsb.GetElement(0) != -1)
            {
                return false;
            }

            vsb = vsb.WithElement(7, (sbyte)(-1));
            if (vsb.GetElement(7) != -1)
            {
                return false;
            }

            vsb = vsb.WithElement(16, (sbyte)(-1));
            if (vsb.GetElement(16) != -1)
            {
                return false;
            }

            Vector256<byte> vb = Vector256<byte>.Zero;
            vb = vb.WithElement(0, byte.MaxValue);
            if (vb.GetElement(0) != byte.MaxValue)
            {
                return false;
            }

            vb = vb.WithElement(7, byte.MaxValue);
            if (vb.GetElement(7) != byte.MaxValue)
            {
                return false;
            }

            vb = vb.WithElement(17, byte.MaxValue);
            if (vb.GetElement(17) != byte.MaxValue)
            {
                return false;
            }

            return true;
        }
    }
}
