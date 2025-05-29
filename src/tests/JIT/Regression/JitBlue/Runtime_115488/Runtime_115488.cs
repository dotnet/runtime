// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

public class TestClass
{
    static Vector512<double> s_v512_double_51 = Vector512.Create(5.033707865168539);
    Vector512<double> v512_double_113 = Vector512.Create(-1.8461538461538463, 0, -4.942857142857143, 3.0576923076923075, 0.06382978723404255, -1.9444444444444444, 3.1363636363636362, 5.2105263157894735);
    private void Method0()
    {
        if (Avx512F.VL.IsSupported)
        {
            byte byte_221 = 0;
            s_v512_double_51 = Avx512F.TernaryLogic(v512_double_113, v512_double_113, v512_double_113, byte_221);
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

set DOTNET_TieredCompilation=0

JIT assert failed:
Assertion failed '!(checkUnusedValues && def->IsUnusedValue()) && "operands should never be marked as unused values"' in 'TestClass:Method0():this' during 'Lowering nodeinfo' (IL size 379; hash 0x46e9aa75; FullOpts)

    File: D:\a\_work\1\s\src\coreclr\jit\lir.cpp Line: 1649

*/
