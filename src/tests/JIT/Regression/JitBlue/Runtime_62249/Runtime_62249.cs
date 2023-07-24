// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_62249
{
    public struct CanBeReinterpretedAsDouble
    {
        public double _0;
    }

    // Note that all VFP registers are occupied by d0-d7 arguments, hence the last argument is passed on the stack.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Callee(double d0, double d1, double d2, double d3, double d4, double d5, double d6, double d7, CanBeReinterpretedAsDouble onStack)
    {
        return onStack._0 == 62249 ? 100 : 0;
    }

    public static int Caller(ref CanBeReinterpretedAsDouble byRef)
    {
        // Since the last parameter
        //   1. Is passed by value
        //   2. Has size of power of 2
        //   3. Has a single field
        // morph transforms OBJ(struct<CanBeReinterpretedAsDouble, 8>, byRef) to IND(double, byRef).
        // However, lower does not expect such transformation and asserts.
        return Callee(0, 0, 0, 6, 2, 2, 4, 9, byRef);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var val = new CanBeReinterpretedAsDouble();
        val._0 = 62249;

        return Caller(ref val);
    }
}
