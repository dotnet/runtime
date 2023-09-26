// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// There was an issue with Sse41.BlendVariable where we might reuse XMM0
// for targetReg.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class TestClass_91798
{
    Vector128<uint> v128_uint_75 = Vector128.Create((uint)2, 1, 0, 1);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Vector128<uint> Method0()
    {
        return Sse41.BlendVariable(v128_uint_75, Vector128<uint>.One, v128_uint_75);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        if (Sse41.IsSupported)
        {
            TestClass_91798 obj = new TestClass_91798();
            obj.Method0();
        }
    }
}
/*
Environment:

set DOTNET_EnableSSE42=0

Assert failure(PID 8884 [0x000022b4], Thread: 14588 [0x38fc]): Assertion failed '(targetReg != REG_XMM0)' in 'TestClass_91798:Method0():System.Runtime.Intrinsics.Vector128`1[uint]:this' during 'Generate code' (IL size 23; hash 0x557a6266; FullOpts)

    File: D:\git\runtime2\src\coreclr\jit\emitxarch.cpp Line: 8612
    Image: d:\git\runtime2\artifacts\tests\coreclr\windows.x64.Checked\tests\Core_Root\corerun.exe
*/

