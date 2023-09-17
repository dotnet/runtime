// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Numerics;
using Xunit;

public class TestClass
{
    static ulong s_ulong_18 = 5;
    static Vector128<ulong> s_v128_ulong_36 = Vector128.Create((ulong)5, 0);
    Vector128<ulong> v128_ulong_87 = Vector128.CreateScalar((ulong)5);
    private void Method0()
    {
        unchecked
        {
            s_ulong_18 = Vector128.Dot(v128_ulong_87 += s_v128_ulong_36 *= v128_ulong_87| (s_v128_ulong_36 = v128_ulong_87), v128_ulong_87 = (s_v128_ulong_36 *= Vector128<ulong>.Zero)* (v128_ulong_87 *= s_v128_ulong_36));
            return;
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        new TestClass().Method0();
    }
}
/*
Environment:

set COMPlus_TieredCompilation=0
set COMPlus_JitStress=2

Assert failure(PID 6932 [0x00001b14], Thread: 5892 [0x1704]): Assertion failed '!m_VariableLiveRanges->back().m_EndEmitLocation.Valid()' in 'TestClass:Method0():this' during 'Generate code' (IL size 130; hash 0x46e9aa75; FullOpts)
    File: D:\a\_work\1\s\src\coreclr\jit\codegencommon.cpp Line: 8729
    Image: e:\kpathak\CORE_ROOT\corerun.exe
*/
