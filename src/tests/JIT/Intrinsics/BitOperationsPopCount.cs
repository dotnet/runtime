// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace BitOperationsPopCountTest
{
    public class Program
    {
        private static int _errorCode = 100;

        [Fact]
        public static int TestEntryPoint()
        {
            // PopCount calls with a constant argument are folded

            Test(0U, BitOperations.PopCount(0U), 0);
            Test(1U, BitOperations.PopCount(1U), 1);
            Test(2U, BitOperations.PopCount(2U), 1);
            Test(1111U, BitOperations.PopCount(1111U), 6);
            Test(unchecked((uint)-101), BitOperations.PopCount(unchecked((uint)-101)), 29);
            Test(4294967294U, BitOperations.PopCount(4294967294U), 31);
            Test(4294967295U, BitOperations.PopCount(4294967295U), 32);

            Test(0UL, BitOperations.PopCount(0UL), 0);
            Test(1UL, BitOperations.PopCount(1UL), 1);
            Test(2UL, BitOperations.PopCount(2UL), 1);
            Test(1111UL, BitOperations.PopCount(1111UL), 6);
            Test(4294967294UL, BitOperations.PopCount(4294967294UL), 31);
            Test(4294967295UL, BitOperations.PopCount(4294967295UL), 32);
            Test(unchecked((ulong)-101), BitOperations.PopCount(unchecked((ulong)-101)), 61);
            Test(18446744073709551614UL, BitOperations.PopCount(18446744073709551614UL), 63);
            Test(18446744073709551615UL, BitOperations.PopCount(18446744073709551615UL), 64);

            return _errorCode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Test(ulong input, int output, int expected)
        {
            if (output != expected)
            {
                Console.WriteLine($"BitOperations.PopCount with a constant argument failed.");
                Console.WriteLine($"Input:    {input}");
                Console.WriteLine($"Output:   {output}");
                Console.WriteLine($"Expected: {expected}");

                _errorCode++;
            }
        }
    }
}
