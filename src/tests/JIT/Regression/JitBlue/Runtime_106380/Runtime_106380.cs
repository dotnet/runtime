// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 24.98 KB to 779 B.
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_106380
{
    static Vector64<uint> s_v64_uint_23 = Vector64<uint>.AllBitsSet;
    Vector64<uint> v64_uint_74 = Vector64.Create((uint)2, 2);
    private void Method0()
    {
        unchecked
        {
            AdvSimd.MaxPairwise(v64_uint_74 += Vector64.ConditionalSelect(Vector64<uint>.Zero, s_v64_uint_23, s_v64_uint_23) & v64_uint_74, v64_uint_74 += (s_v64_uint_23 ^ v64_uint_74)& (s_v64_uint_23 = v64_uint_74));
            return;
        }
    }
    [Fact]
    public static void TestEntryPoint()
    {
        new Runtime_106380().Method0();
    }
}

/*
Environment:

set DOTNET_TieredCompilation=0

Assert failure(PID 13404 [0x0000345c], Thread: 5136 [0x1410]): Assertion failed 'type != TYP_VOID' in 'TestClass:Method0():this' during 'Optimize Valnum CSEs' (IL size 111; hash 0x46e9aa75; FullOpts)
    File: C:\wk\runtime\src\coreclr\jit\gentree.cpp:8388
    Image: C:\aman\Core_Root\corerun.exe
*/
