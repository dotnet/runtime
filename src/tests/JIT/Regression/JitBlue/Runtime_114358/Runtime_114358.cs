// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 206.63 KB to 1.9 KB.


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class TestClass_114358
{
    public struct S1
    {
        public int int_1;
    }
    static byte s_byte_4 = 1;
    static Vector64<short> s_v64_short_20 = Vector64.Create(94, -2, 3, 3);
    static Vector128<byte> s_v128_byte_28 = Vector128.Create((byte)2);
    static Vector128<ushort> s_v128_ushort_31 = Vector128.Create((ushort)32766);
    Vector64<short> v64_short_70 = Vector64<short>.AllBitsSet;
    Vector128<byte> v128_byte_78 = Vector128.CreateScalar((byte)0);
    Vector128<short> v128_short_80 = Vector128.Create(-2, 0, 2, 94, 3, 0, 3, 0);
    Vector128<ushort> v128_ushort_81 = Vector128<ushort>.AllBitsSet;
    private static List<string> toPrint = new List<string>();
    internal void Method0()
    {
        unchecked
        {
            S1 s1_172 = new S1();
            s_v128_ushort_31 = Vector128.LessThan(s_v128_ushort_31 -= Vector128<ushort>.Zero | v128_ushort_81, AdvSimd.AddWideningUpper(v128_ushort_81 & v128_ushort_81, s_v128_byte_28 = v128_byte_78));
            v128_short_80 = AdvSimd.ExtractVector128(AdvSimd.MultiplyByScalar(v128_short_80 - v128_short_80, AdvSimd.MultiplySubtractByScalar(v64_short_70, s_v64_short_20, v64_short_70)), v128_short_80 - v128_short_80, s_byte_4);
            s_v64_short_20 = AdvSimd.ShiftRightLogicalRoundedAdd(v64_short_70 -= v64_short_70 += v64_short_70, v64_short_70 + Vector64<short>.AllBitsSet + v64_short_70 + Vector64<short>.AllBitsSet & v64_short_70, s_byte_4 >>= s1_172.int_1 <<= 15 + 4);
            return;
        }
    }

    [Fact]
    public static void Repro()
    {
        if (AdvSimd.IsSupported)
        {
            new TestClass_114358().Method0();
        }
    }
}
/*
Environment:

set DOTNET_AltJit=Method0
set DOTNET_AltJitName=clrjit_universal_arm64_x64.dll
set DOTNET_EnableWriteXorExecute=0
set DOTNET_JitDisasm=Method0
set DOTNET_JitStressRegs=2
set DOTNET_TieredCompilation=0

Debug: 1639727076

Release: 0
JIT assert failed:
Assertion failed '(targetReg == op1Reg) || (targetReg != op3Reg)' in 'TestClass:Method0():this' during 'Generate code' (IL size 298; hash 0x46e9aa75; FullOpts)

    File: /Users/runner/work/1/s/src/coreclr/jit/hwintrinsiccodegenarm64.cpp Line: 416


*/
