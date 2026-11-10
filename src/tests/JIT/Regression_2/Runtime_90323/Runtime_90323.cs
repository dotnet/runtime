// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_90323
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float ConvertToSingle(long value) => (float)value;

    // 32-bit currently performs a 2-step conversion which causes a different result to be produced
    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.Is64BitProcess))]
    public static int TestEntryPoint()
    {
        bool passed = true;

        long value = 0x4000_0040_0000_0001L;

        if (ConvertToSingle(value) != (float)(value))
        {
            Console.WriteLine($"Mismatch between codegen and constant folding: {ConvertToSingle(value)} != {(float)(value)}");
            passed = false;
        }

        if (BitConverter.SingleToUInt32Bits((float)(value)) != 0x5E80_0001) // 4.6116866E+18f
        {
            Console.WriteLine($"Mismatch between constant folding and expected value: {(float)(value)} != 4.6116866E+18f; 0x{BitConverter.SingleToUInt32Bits((float)(value)):X8} != 0x5E800001");
            passed = false;
        }

        return passed ? 100 : 0;
    }
}
