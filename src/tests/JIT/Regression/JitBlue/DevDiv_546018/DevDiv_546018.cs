// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This tests an interop call (resulting in RETURNTRAP in its epilog)
// when there are live 256 bit values at the callsite.
// The bug was that codegen for RETURNTRAP node requested a single temp register
// and asserted that there was only one. In this case the set of temp registers
// includes floating-point registers that may be needed for saving/restoring the upper
// part of 256-bit registers. The fix was for codegen to request a single temp int register
// and assert that there was only one.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    static Random random;

    [Fact]
    public static int TestEntryPoint()
    {
        random = new Random ();
        VectorSingle_op_Division_VectorSingle_VectorSingle (5);
        return 100;
    }

    [MethodImpl (MethodImplOptions.NoInlining)]
    internal static void VectorSingle_op_Division_VectorSingle_VectorSingle (long iterations)
    {
        Vector<float> dividend = CreateRandomVector ();
        Vector<float> divisor = CreateRandomVector ();

        Vector<float> result = dividend / divisor;
        GC.Collect ();
        for (long iteration = 0L; iteration < iterations; iteration++) {
            result = dividend / divisor;
        }
        GC.KeepAlive (new object [1] { result });
    }

    [MethodImpl (MethodImplOptions.NoInlining)]
    static Vector<float> CreateRandomVector ()
    {
        return new Vector<float> ((float)(random.NextDouble () + 1.0));
    }
}

