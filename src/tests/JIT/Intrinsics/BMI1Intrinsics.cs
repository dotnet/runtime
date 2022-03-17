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
                Test(value.input1, AndNot(value.input1, value.input2), value.andnExpected, nameof(AndNot));
                Test(value.input1, ExtractLowestSetIsolatedBit(value.input1), value.blsiExpected, nameof(ExtractLowestSetIsolatedBit));
                Test(value.input1, ResetLowestSetBit(value.input1), value.blsrExpected, nameof(ResetLowestSetBit));
                Test(value.input1, GetMaskUpToLowestSetBit(value.input1), value.blmskExpected, nameof(GetMaskUpToLowestSetBit));
            }

            return _errorCode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint AndNot(uint x, uint y) => x & (~y); // bmi1 andn

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ExtractLowestSetIsolatedBit(uint x) => (uint)(x & (-x)); // bmi1 blsi

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint ResetLowestSetBit(uint x) => x & (x - 1); // bmi1 blsr

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint GetMaskUpToLowestSetBit(uint x) => (uint)(x ^ (-x)); // bmi1 blmsk

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Test(uint input, uint output, uint expected,string callerName)
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
