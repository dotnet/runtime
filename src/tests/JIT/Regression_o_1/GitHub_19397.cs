// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;

// This is a regression test for a bug that was exposed with jitStressRegs=8 in
// System.Text.ConsoleEncoding::GetMaxByteCount, due to the lack of handling
// of a register-allocator-added COPY of a multi-reg multiply.

namespace GitHub_19397
{
    public class Program
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static long getValue()
        {
            return(0x0101010101010101L);
        }
        [Fact]
        public static int TestEntryPoint()
        {
            long value = getValue();
            Console.WriteLine($"Result is {value}");
            if (value == 0x0101010101010101L)
            {
                Console.WriteLine("PASS");
                return 100;
            }
            else
            {
                Console.WriteLine("FAIL");
                return -1;
            }
        }

    }
}
