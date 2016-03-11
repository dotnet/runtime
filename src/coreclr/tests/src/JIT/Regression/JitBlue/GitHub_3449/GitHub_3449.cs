// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System;

public class Program
{
    // RyuJIT codegen and clang (or gcc) may produce different results for casting uint64 to 
    // double, and the clang result is more accurate. For example,
    //    1) (double)0x84595161401484A0UL --> 43e08b2a2c280290  (RyuJIT codegen or VC++)
    //    2) (double)0x84595161401484A0UL --> 43e08b2a2c280291  (clang or gcc)
    // Constant folding in RyuJIT simply does (double)0x84595161401484A0UL in its C++ implementation.
    // If it is compiled by clang, the example unsigned value and cast tree node are folded into 
    // 43e08b2a2c280291, which is different from what the codegen produces. To fix this inconsistency,
    // the constant folding is forced to have the same behavior as the codegen, and the result
    // must be always 43e08b2a2c280290.
    public static int Main(string[] args)
    {
        //Check if the test is being executed on ARMARCH
        bool isProcessorArmArch = false;        
        string processorArchEnvVar = null;
        processorArchEnvVar = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
        
        if ((processorArchEnvVar != null)
            && (processorArchEnvVar.Equals("ARM", StringComparison.CurrentCultureIgnoreCase)
                || processorArchEnvVar.Equals("ARM64", StringComparison.CurrentCultureIgnoreCase)))
        {
            isProcessorArmArch = true;
        }        
        
        ulong u64 = 0x84595161401484A0UL;
        double f64 = (double)u64;        
        long h64 = BitConverter.DoubleToInt64Bits(f64);            
        long expected_h64 = isProcessorArmArch ? 0x43e08b2a2c280291L : 0x43e08b2a2c280290L;
        if (h64 != expected_h64) {
            Console.WriteLine(String.Format("Expected: 0x{0:x}\nActual: 0x{1:x}", expected_h64, h64));
            return -1;
        }
        return 100;
    }
}
