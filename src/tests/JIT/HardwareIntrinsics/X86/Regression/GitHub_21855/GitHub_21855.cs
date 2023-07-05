using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub_21855
{
    public class GitHub_21855
    {

        [Fact]
        public static void Test()
        {
            bool pass =true;
            if (Sse2.IsSupported)
            {
                Vector128<byte> src = Vector128.Create((byte)1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16);
                Vector128<uint> srcAsUInt32 = src.AsUInt32();
                Vector128<uint> result = Sse2.Shuffle(srcAsUInt32, _MM_SHUFFLE(0, 1, 2, 3));
                pass = result.Equals(Sse2.Shuffle(srcAsUInt32, (byte)(0 << 6 | 1 << 4 | 2 << 2 | 3)));
            }
            Assert.True(pass);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static byte _MM_SHUFFLE(int a, int b, int c, int d)
        {
            return (byte)(a << 6 | b << 4 | c << 2 | d);
        }
    }
}
