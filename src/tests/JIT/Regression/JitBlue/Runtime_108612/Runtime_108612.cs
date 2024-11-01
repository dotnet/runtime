// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 18.05 KB to 1.2 KB.
// JIT assert failed:
// Assertion failed 'm_store->TypeGet() == m_src->TypeGet()' in 'TestClass:Method0():this' during 'Assertion prop' (IL size 91; hash 0x46e9aa75; FullOpts)
//
//    File: /Users/runner/work/1/s/src/coreclr/jit/morphblock.cpp Line: 665


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class Runtime_108612
{
    static Vector128<byte> s_v128_byte_27 = Vector128.CreateScalar((byte)0);
    static Vector128<int> s_v128_int_31 = Vector128<int>.Zero;
    static Vector128<uint> s_v128_uint_32 = Vector128.CreateScalar((uint)5);
    Vector64<byte> v64_byte_67 = Vector64.CreateScalar((byte)5);
    Vector128<int> v128_int_81 = Vector128.Create(-1, 6, 2, 1);
    private static List<string> toPrint = new List<string>();
    private void Method0()
    {
        unchecked
        {
            v128_int_81 = AdvSimd.NegateSaturate(s_v128_int_31 *= 15|4);
            s_v128_uint_32 = AdvSimd.ShiftRightLogicalRounded(s_v128_uint_32, Vector128.Sum(s_v128_byte_27 | s_v128_byte_27));
            v64_byte_67 = AdvSimd.Arm64.AddAcross(s_v128_byte_27 | s_v128_byte_27);
            return;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (AdvSimd.IsSupported)
        {
            Antigen();
        }
    }

    private static int Antigen()
    {
        try
        {
            new Runtime_108612().Method0();
        }
        catch (Exception e) { }
        return string.Join(Environment.NewLine, toPrint).GetHashCode();
    }
}