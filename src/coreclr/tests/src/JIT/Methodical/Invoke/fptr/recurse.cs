// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace TestCase
{
    internal class Test
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

        private static int Main()
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
