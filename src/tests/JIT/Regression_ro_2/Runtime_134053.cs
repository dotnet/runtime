// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_134053
{
    [ConditionalTheory(typeof(AdvSimd.Arm64), nameof(AdvSimd.Arm64.IsSupported))]
    [InlineData(0x4008000000000000L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArmIntegerNegation(long value)
    {
        // Integer negation changes the encoding of 3.0 to -1.5, not -3.0.
        Vector64<double> one = Vector64.Create(1.0);
        Assert.Equal(-0.5, AdvSimd.FusedMultiplyAddScalar(Vector64.Create(-value).AsDouble(), one, one).ToScalar());
    }

    [ConditionalTheory(typeof(AdvSimd.Arm64), nameof(AdvSimd.Arm64.IsSupported))]
    [InlineData(0x4008000040000000L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ArmReinterpretedDoubleNegation(long bits)
    {
        // Double negation leaves the low 32 bits (2.0f) unchanged.
        double value = BitConverter.Int64BitsToDouble(bits);
        Vector64<float> one = Vector64.Create(1.0f);
        Assert.Equal(3.0f, AdvSimd.FusedMultiplyAddScalar(one, Vector64.CreateScalarUnsafe(-value).AsSingle(), one).ToScalar());
    }

    [ConditionalTheory(typeof(Fma), nameof(Fma.IsSupported))]
    [InlineData(0x4008000040000000L)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void XarchReinterpretedDoubleNegation(long bits)
    {
        double value = BitConverter.Int64BitsToDouble(bits);
        Vector128<float> one = Vector128.Create(1.0f);
        Assert.Equal(3.0f, Fma.MultiplyAddScalar(one, Vector128.CreateScalarUnsafe(-value).AsSingle(), one).ToScalar());
    }

    [Theory]
    [InlineData(2.0, 3.0, 4.0, 0x4000000000000000L)]
    [InlineData(0.0, -1.0, 0.0, long.MinValue)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SameTypeNegation(double x, double y, double z, long expectedBits)
    {
        double expected = BitConverter.Int64BitsToDouble(expectedBits);
        Assert.Equal(expectedBits, BitConverter.DoubleToInt64Bits(Math.FusedMultiplyAdd(-x, -y, -z)));
        Assert.Equal(BitConverter.SingleToInt32Bits((float)expected), BitConverter.SingleToInt32Bits(MathF.FusedMultiplyAdd(-(float)x, -(float)y, -(float)z)));
    }
}
