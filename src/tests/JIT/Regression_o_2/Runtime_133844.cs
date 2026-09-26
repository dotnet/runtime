// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_133844;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133844
{
    private int _negativeOne = -1;
    private int _minValue = int.MinValue;
    private Vector<int> _value = Vector.Create(1);
    private Vector<int> _sink;
    private int _zero;

    [Fact]
    public static void DivideByZeroIsPreserved()
    {
        Assert.Throws<DivideByZeroException>(new Runtime_133844().DivideByZero);
    }

    [Fact]
    public static void OverflowIsPreserved()
    {
        Assert.Throws<OverflowException>(new Runtime_133844().Overflow);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void DivideByZero()
    {
        _sink = (_value ^ _value) * Vector.CreateHarmonicSequence(_zero, _zero);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private void Overflow()
    {
        _sink = (_value ^ _value) * (Vector.Create(_minValue) / Vector.Create(_negativeOne));
    }
}
