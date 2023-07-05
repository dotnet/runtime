// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace TestCase_recurse_cs
{
    public class Test
    {
        private static int Fact1(ref int arg, ref int result)
        {
            if (arg > 1)
            {
                result *= arg;
                arg--;
                return Fact1(ref arg, ref result);
            }
            return 0x12345;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            int arg = 6;
            int result = 1;
            if (Fact1(ref arg, ref result) != 0x12345)
            {
                Console.WriteLine("FAILED");
                return 1;
            }
            if (result != 720)
            {
                Console.WriteLine("FAILED");
                return 2;
            }
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
