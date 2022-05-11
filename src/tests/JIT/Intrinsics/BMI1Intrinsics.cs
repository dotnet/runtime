using System;
using System.Runtime.CompilerServices;

namespace BMI1Intrinsics
{
    internal class Program
    {
        private static int _errorCode = 100;

        static int Main(string[] args)
        {
            // bmi1 expression are folded to to hwintrinsics that return identical results

            var values = new (uint input1, uint input2, uint andnExpected, uint blsiExpected, uint blsrExpected, uint blmskExpected)[] {
                (0, 0, 0, 0 ,0 ,0),
                (1, 0, 1, 1 ,0 ,0xfffffffe),
                (uint.MaxValue / 2, 0, 0x7fffffff, 0x1 ,0x7ffffffe ,0xfffffffe),
                ((uint.MaxValue / 2) - 1,  0, 0x7FFFFFFE, 2 ,0x7FFFFFFC ,0xFFFFFFFC),
                ((uint.MaxValue / 2) + 1, 0, 0x80000000, 0x80000000 ,0 ,0),
                (uint.MaxValue - 1, 0, 0xFFFFFFFE, 2 ,0xFFFFFFFC ,0xFFFFFFFC),
                (uint.MaxValue , 0, 0xFFFFFFFF, 1 ,0xFFFFFFFE ,0xFFFFFFFE),
                (0xAAAAAAAA,0xAAAAAAAA,0,2,0xAAAAAAA8,0xFFFFFFFC),
                (0xAAAAAAAA,0x55555555,0xAAAAAAAA,2,0xAAAAAAA8,0xFFFFFFFC),
            };

            foreach (var value in values)
            {
                Test(value.input1, AndNot_32bit(value.input1, value.input2), value.andnExpected, nameof(AndNot_32bit));
                Test(value.input1, ExtractLowestSetIsolatedBit_32bit(value.input1), value.blsiExpected, nameof(ExtractLowestSetIsolatedBit_32bit));
                Test(value.input1, ResetLowestSetBit_32bit(value.input1), value.blsrExpected, nameof(ResetLowestSetBit_32bit));
                Test(value.input1, GetMaskUpToLowestSetBit_32bit(value.input1), value.blmskExpected, nameof(GetMaskUpToLowestSetBit_32bit));
            }


            var values2 = new (ulong input1, ulong input2, ulong andnExpected, ulong blsiExpected, ulong blsrExpected, ulong blmskExpected)[] {
                (0,                                    0,                  0,                  0,                  0,                  0),
                (1,                                    0,                  1,                  1,                  0,0xFFFFFFFF_FFFFFFFE),
                (ulong.MaxValue / 2,                   0,0x7FFFFFFF_FFFFFFFF,                  1,0x7FFFFFFF_FFFFFFFE,0xFFFFFFFF_FFFFFFFE),
                ((ulong.MaxValue / 2) - 1,             0,0x7FFFFFFF_FFFFFFFE,                  2,0x7FFFFFFF_FFFFFFFC,0xFFFFFFFF_FFFFFFFC),
                ((ulong.MaxValue / 2) + 1,             0,0x80000000_00000000,0x80000000_00000000,                  0,                  0),
                (ulong.MaxValue - 1,                   0,0xFFFFFFFF_FFFFFFFE,                  2,0xFFFFFFFF_FFFFFFFC,0xFFFFFFFF_FFFFFFFC),
                (ulong.MaxValue,                       0,0xFFFFFFFF_FFFFFFFF,                  1,0xFFFFFFFF_FFFFFFFE,0xFFFFFFFF_FFFFFFFE),
                (0xAAAAAAAA_AAAAAAAA,0xAAAAAAAA_AAAAAAAA,                  0,                  2,0xAAAAAAAA_AAAAAAA8,0xFFFFFFFF_FFFFFFFC),
                (0xAAAAAAAA_AAAAAAAA,0x55555555_55555555,0xAAAAAAAA_AAAAAAAA,                  2,0xAAAAAAAA_AAAAAAA8,0xFFFFFFFF_FFFFFFFC),
            };

            foreach (var value in values2)
            {
                Test(value.input1, AndNot_64bit(value.input1, value.input2), value.andnExpected, nameof(AndNot_64bit));
                Test(value.input1, ExtractLowestSetIsolatedBit_64bit(value.input1), value.blsiExpected, nameof(ExtractLowestSetIsolatedBit_64bit));
                Test(value.input1, ResetLowestSetBit_64bit(value.input1), value.blsrExpected, nameof(ResetLowestSetBit_64bit));
                Test(value.input1, GetMaskUpToLowestSetBit_64bit(value.input1), value.blmskExpected, nameof(GetMaskUpToLowestSetBit_64bit));
            }

            return _errorCode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint AndNot_32bit(uint x, uint y) => x & (~y); // bmi1 andn

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong AndNot_64bit(ulong x, ulong y) => x & (~y); // bmi1 andn

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ExtractLowestSetIsolatedBit_32bit(uint x) => (uint)(x & (-x)); // bmi1 blsi

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ExtractLowestSetIsolatedBit_64bit(ulong x) => x & (ulong)(-(long)x); // bmi1 blsi

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ResetLowestSetBit_32bit(uint x) => x & (x - 1); // bmi1 blsr

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong ResetLowestSetBit_64bit(ulong x) => x & (x - 1); // bmi1 blsr

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint GetMaskUpToLowestSetBit_32bit(uint x) => (uint)(x ^ (-x)); // bmi1 blmsk

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ulong GetMaskUpToLowestSetBit_64bit(ulong x) => x ^ (ulong)(-(long)x); // bmi1 blmsk

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Test(uint input, uint output, uint expected, string callerName)
        {
            if (output != expected)
            {
                Console.WriteLine($"{callerName} failed.");
                Console.WriteLine($"Input:    {input:X}");
                Console.WriteLine($"Output:   {output:X}");
                Console.WriteLine($"Expected: {expected:X}");

                _errorCode++;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Test(ulong input, ulong output, ulong expected, string callerName)
        {
            if (output != expected)
            {
                Console.WriteLine($"{callerName} failed.");
                Console.WriteLine($"Input:    {input:X}");
                Console.WriteLine($"Output:   {output:X}");
                Console.WriteLine($"Expected: {expected:X}");

                _errorCode++;
            }
        }
    }
}
