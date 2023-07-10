// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Program
{
    // RyuJIT codegen, VC++, clang and gcc may produce different results for casting uint64 to 
    // double.
    //    1) (double)0x84595161401484A0UL --> 43e08b2a2c280290  (RyuJIT codegen or VC++)
    //    2) (double)0x84595161401484A0UL --> 43e08b2a2c280291  (clang or gcc)
    // Constant folding in RyuJIT simply does (double)0x84595161401484A0UL in its C++ implementation.
    // If it is compiled by clang or gcc, the example unsigned value and cast tree node are folded into 
    // 43e08b2a2c280291, which is different from what RyuJIT codegen or VC++ produces.
    // 
    // We don't have a good way to tell if the CLR is compiled by clang or VC++, so we simply allow
    // both answers.

    [Fact]
    public static int TestEntryPoint()
    {
        ulong u64 = 0x84595161401484A0UL;
        double f64 = (double)u64;        
        long h64 = BitConverter.DoubleToInt64Bits(f64);            
        long expected_h64_1 = 0x43e08b2a2c280291L;
        long expected_h64_2 = 0x43e08b2a2c280290L;
        if ((h64 != expected_h64_1) && (h64 != expected_h64_2)) {
            Console.WriteLine(String.Format("Expected: 0x{0:x} or 0x{1:x}\nActual: 0x{2:x}", expected_h64_1, expected_h64_2, h64));
            return -1;
        }
        return 100;
    }
}
