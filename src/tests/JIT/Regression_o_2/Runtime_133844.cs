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

    [Fact]
    public static void UnsignedDivisionPreservesExceptions()
    {
        Vector<uint> dividend = Vector.Create(0x80000000u);
        Assert.Throws<DivideByZeroException>(() => UnsignedDivision(dividend, Vector<uint>.Zero));
        Assert.Equal(Vector<uint>.Zero, UnsignedDivision(dividend, Vector.Create(uint.MaxValue)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(2)]
    public static void DivisionWithConstantOperands(int value)
    {
        Vector<int> vector = Vector.Create(value);
        Assert.Equal(Vector<int>.Zero, ConstantDivisor(vector));
        Assert.Equal(Vector.Create(42 / value), ConstantDividend(vector));
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

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector<int> ConstantDivisor(Vector<int> dividend)
    {
        return (dividend ^ dividend) * (dividend / Vector.Create(2));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector<int> ConstantDividend(Vector<int> divisor)
    {
        return Vector.Create(42) / divisor;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector<uint> UnsignedDivision(Vector<uint> dividend, Vector<uint> divisor)
    {
        return (dividend ^ dividend) * (dividend / divisor);
    }
}
