// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen
// Reduced from 313.92 KB to 1.05 KB.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Numerics;
using Xunit;

public class Runtime_115487
{
    static int s_int_13 = 0;
    static Vector128<ushort> s_v128_ushort_25 = Vector128.Create((ushort)5, 1, 1, 1, 5, 4, 53, 2);
    ushort ushort_81 = 1;
    Vector128<ushort> v128_ushort_88 = Vector128.Create((ushort)2, 53, 5, 5, 0, 1, 1, 4);
    private static List<string> toPrint = new List<string>();

    private void Method0()
    {
        unchecked
        {
            s_v128_ushort_25 = Vector128.WithElement(s_v128_ushort_25 * v128_ushort_88 ^ s_v128_ushort_25 & Vector128.MaxNative(s_v128_ushort_25, s_v128_ushort_25), s_int_13 >> s_int_13 & 3, ushort_81);
            return;
        }
    }

    [Fact]
    public static void Problem()
    {
        Assert.Equal(Vector128.Create((ushort)5, 1, 1, 1, 5, 4, 53, 2), s_v128_ushort_25);
        _ = Antigen();
        Assert.Equal(Vector128.Create((ushort)1, 52, 4, 4, 5, 0, 0, 10), s_v128_ushort_25);
    }

    private static int Antigen()
    {
        new Runtime_115487().Method0();
        return string.Join(Environment.NewLine, toPrint).GetHashCode();
    }
}
/*
Environment:

set DOTNET_TieredCompilation=0

JIT assert failed:
Assertion failed '(insCodesMR[ins] != BAD_CODE)' in 'TestClass:Method0():this' during 'Generate code' (IL size 83; hash 0x46e9aa75; MinOpts)

    File: D:\a\_work\1\s\src\coreclr\jit\emitxarch.cpp Line: 4203

*/
