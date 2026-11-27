// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public struct StructJustDecimal128
{
    public StructJustDecimal128(Decimal128 val) { value = val; }
    public Decimal128 value;
}

public struct StructWithDecimal128
{
    public StructWithDecimal128(Decimal128 val) { value = val; messUpPadding = 0x10; }
    public byte messUpPadding;
    public Decimal128 value;
}

unsafe partial class Decimal128Native
{
    [DllImport(nameof(Decimal128Native))]
    public static extern Decimal128 GetDecimal128(ulong upper, ulong lower);

    [DllImport(nameof(Decimal128Native))]
    public static extern void GetDecimal128Out(ulong upper, ulong lower, Decimal128* value);

    [DllImport(nameof(Decimal128Native))]
    public static extern void GetDecimal128Out(ulong upper, ulong lower, out Decimal128 value);

    [DllImport(nameof(Decimal128Native))]
    public static extern void GetDecimal128Out(ulong upper, ulong lower, out StructJustDecimal128 value);

    [DllImport(nameof(Decimal128Native))]
    public static extern ulong GetDecimal128Lower_S(StructJustDecimal128 value);

    [DllImport(nameof(Decimal128Native))]
    public static extern ulong GetDecimal128Lower(Decimal128 value);

    [DllImport(nameof(Decimal128Native))]
    public static extern void AddStructWithDecimal128_ByRef(ref StructWithDecimal128 lhs, ref StructWithDecimal128 rhs);
}

unsafe partial class Decimal32Native
{
    [DllImport("Decimal128Native")]
    public static extern uint GetDecimal32Bits(Decimal32 value);
}

unsafe partial class Decimal64Native
{
    [DllImport("Decimal128Native")]
    public static extern ulong GetDecimal64Bits(Decimal64 value);
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe partial class Decimal128Native
{
    // Decimal128 shares the raw 128-bit little/big-endian layout of UInt128, so we can build and
    // inspect known bit patterns by reinterpreting a UInt128 without relying on decimal semantics.
    private static Decimal128 FromBits(ulong upper, ulong lower) => Unsafe.BitCast<UInt128, Decimal128>(new UInt128(upper, lower));

    private static UInt128 ToBits(Decimal128 value) => Unsafe.BitCast<Decimal128, UInt128>(value);

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69399", TestRuntimes.Mono)]
    public static void TestDecimal128FieldLayout()
    {
        // Validates that the ABI-required alignment of Decimal128 within a struct matches the native compiler (16-byte on most targets; 8-byte on ARM32).
        StructWithDecimal128 lhs = new StructWithDecimal128(FromBits(11, 12));
        StructWithDecimal128 rhs = new StructWithDecimal128(FromBits(13, 14));

        AddStructWithDecimal128_ByRef(ref lhs, ref rhs);
        Assert.Equal(new UInt128(24, 26), ToBits(lhs.value));
        Assert.Equal((byte)0x10, lhs.messUpPadding);

        Decimal128 value2;
        GetDecimal128Out(3, 4, &value2);
        Assert.Equal(new UInt128(3, 4), ToBits(value2));

        GetDecimal128Out(5, 6, out Decimal128 value3);
        Assert.Equal(new UInt128(5, 6), ToBits(value3));

        GetDecimal128Out(7, 8, out StructJustDecimal128 value4);
        Assert.Equal(new UInt128(7, 8), ToBits(value4.value));

        // Until the decimal by-value ABI is implemented, validate that we don't marshal by value.

        // Checking return value
        Assert.Throws<MarshalDirectiveException>(() => GetDecimal128(0, 1));

        // Checking input value as Decimal128 itself
        Assert.Throws<MarshalDirectiveException>(() => GetDecimal128Lower(default(Decimal128)));

        // Checking input value as structure wrapping Decimal128
        Assert.Throws<MarshalDirectiveException>(() => GetDecimal128Lower_S(default(StructJustDecimal128)));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/69399", TestRuntimes.Mono)]
    public static void TestDecimal32And64MarshalRestriction()
    {
        // Decimal32/Decimal64 have no native calling convention either, so by-value marshaling is blocked.
        Assert.Throws<MarshalDirectiveException>(() => Decimal32Native.GetDecimal32Bits(default(Decimal32)));
        Assert.Throws<MarshalDirectiveException>(() => Decimal64Native.GetDecimal64Bits(default(Decimal64)));
    }
}
