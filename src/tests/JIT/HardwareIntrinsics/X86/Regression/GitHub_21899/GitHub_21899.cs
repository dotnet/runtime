using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.CompilerServices;
using Xunit;

namespace GitHub_21899
{
    public class GitHub_21899
    {
        [Fact]
        public static int TestEntryPoint()
        {
            bool pass = true;
            pass = test1() && test2() && test3() && test4();
            return pass ? 100 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ulong MultiplyNoFlags1(ulong a, ulong b)
        {
            ulong r;
            Bmi2.X64.MultiplyNoFlags(a, b, &r);
            return r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe ulong MultiplyNoFlags2(ulong a, ulong b)
        {
            ulong r;
            r = Bmi2.X64.MultiplyNoFlags(a, b, &r);
            return r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe uint MultiplyNoFlags3(uint a, uint b)
        {
            uint r;
            Bmi2.MultiplyNoFlags(a, b, &r);
            return r;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static unsafe uint MultiplyNoFlags4(uint a, uint b)
        {
            uint r;
            r = Bmi2.MultiplyNoFlags(a, b, &r);
            return r;
        }

        static bool test1()
        {
            return !Bmi2.X64.IsSupported || (MultiplyNoFlags1(1111111111111UL, 1111111111111UL) == 1107357235536201905UL);
        }

        static bool test2()
        {
            return !Bmi2.X64.IsSupported || (MultiplyNoFlags2(1111111111111UL, 1111111111111UL) == 66926UL);
        }

        static bool test3()
        {
            return !Bmi2.IsSupported || (MultiplyNoFlags3(1111111U, 1111111U) == 1912040369U);
        }

        static bool test4()
        {
            return !Bmi2.IsSupported || (MultiplyNoFlags4(1111111U, 1111111U) == 287U);
        }
    }
}
