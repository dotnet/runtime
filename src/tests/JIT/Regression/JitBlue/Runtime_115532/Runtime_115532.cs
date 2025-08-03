// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// set DOTNET_TieredCompilation=0

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;
using Xunit;

public class Runtime_115532
{
    static decimal s_decimal_6 = 27.023809523809523809523809524m;
    static int s_int_9 = 1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private double LeafMethod3()
    {
        unchecked
        {
            Vector128<double> vdec = Vector128<double>.Zero
                .WithElement(0, (double)s_decimal_6)
                .WithElement(1, (double)s_int_9);

            try
            {
                vdec = vdec.WithElement(2, 0.0)
                           .WithElement(3, 0.0);
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            return vdec.GetElement(0);
        }
    }

    [Fact]
    public static void Problem()
    {
        Assert.Equal((double)s_decimal_6, new Runtime_115532().LeafMethod3());
    }
}


/*Debug: 0
JIT assert failed:
Assertion failed '0 <= imm8 && imm8 < count' in 'TestClass:LeafMethod3():System.Decimal:this' during 'Lowering nodeinfo' (IL size 256; hash 0xbbebf3e2; MinOpts)

    File: Q:\git\runtime3\src\coreclr\jit\lowerxarch.cpp Line: 5670

Release: 0
JIT assert failed:
Assertion failed '0 <= imm8 && imm8 < count' in 'TestClass:LeafMethod3():System.Decimal:this' during 'Lowering nodeinfo' (IL size 238; hash 0xbbebf3e2; FullOpts)

    File: Q:\git\runtime3\src\coreclr\jit\lowerxarch.cpp Line: 5670

*/
