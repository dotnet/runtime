// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 20.86 KB to 1.7 KB.
// JIT assert failed:
// Assertion failed 'unreached' in 'Runtime_107587:Method0():this' during 'Lowering nodeinfo' (IL size 133; hash 0x46e9aa75; FullOpts)

    // File: D:\a\_work\1\s\src\coreclr\jit\lowerxarch.cpp Line: 11752

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class Runtime_107587
{
    static Vector512<sbyte> s_v512_sbyte_42 = Vector512.Create((sbyte)92);
    public Vector512<sbyte> Method0()
    {
        byte byte_152 = 3;
        return Avx512F.TernaryLogic(s_v512_sbyte_42, s_v512_sbyte_42, s_v512_sbyte_42, byte_152);
    }
    
    [Fact]   
    public static void TestEntryPoint()
    {
        if (Avx512F.IsSupported)
        {
            new Runtime_107587().Method0();
        }
    }
}
