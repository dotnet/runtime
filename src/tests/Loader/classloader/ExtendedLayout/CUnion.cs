// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

namespace ExtendedLayoutTests;

public static class CUnionTests
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void BlittablePrimitiveFieldsLayout()
    {
        CUnionBlittablePrimitiveFields c = default;

        // All fields should be at offset 0, size should be max field size (4 bytes for int/float)
        Assert.Equal(4, Unsafe.SizeOf<CUnionBlittablePrimitiveFields>());
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<int, byte>(ref c.a)));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<float, byte>(ref c.b)));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionBlittablePrimitiveFields, byte>(ref c), ref c.c));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NonBlittableUnmanagedPrimitiveFields_TreatedAsBlittable()
    {
        CUnionNonBlittablePrimitiveFields c = default;
        Assert.Equal(Unsafe.SizeOf<CUnionNonBlittablePrimitiveFields>(), Marshal.SizeOf<CUnionNonBlittablePrimitiveFields>());
        // Size should be 2 (char is 2 bytes, bool is 1 byte)
        Assert.Equal(2, Unsafe.SizeOf<CUnionNonBlittablePrimitiveFields>());
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionNonBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<bool, byte>(ref c.b)));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionNonBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<char, byte>(ref c.c)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ReferenceFields_ThrowTypeLoadException()
    {
        Assert.Throws<TypeLoadException>(() => typeof(CUnionWithReferenceFields));

        Assert.Throws<TypeLoadException>(() => typeof(CUnionWithMixedFields));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NestedCUnion()
    {
        CUnionCustomCUnionField nested = default;

        // Size should be the size of NestedCUnionType (8 bytes for int64)
        Assert.Equal(8, Unsafe.SizeOf<CUnionCustomCUnionField>());
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NestedNonCUnionNonAuto()
    {
        CUnionCustomSeqStructField nested = default;

        Assert.Equal(4, Unsafe.SizeOf<CUnionCustomSeqStructField>());
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NestedAutoLayout_ThrowTypeLoadException()
    {
        Assert.Throws<TypeLoadException>(() => typeof(CUnionCustomAutoStructField));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void EmptyUnion()
    {
        Assert.Throws<TypeLoadException>(() => typeof(EmptyCUnion));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExplicitOffsets_Ignored()
    {
        CUnionWithOffsets c = default;

        // Offset should be 0 regardless of what's specified
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionWithOffsets, byte>(ref c), ref Unsafe.As<int, byte>(ref c.a)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExplicitSize_Ignored()
    {
        // Size should be 4 (int size), not 12
        Assert.Equal(4, Unsafe.SizeOf<CUnionWithSize>());
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void Pack_Ignored()
    {
        // Size should be 4 (max of int8=1 and int32=4), aligned to 4
        Assert.Equal(4, Unsafe.SizeOf<CUnionWithPack>());

        CUnionWithPack c = default;

        // Both fields should be at offset 0
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionWithPack, byte>(ref c), ref c.a));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionWithPack, byte>(ref c), ref Unsafe.As<int, byte>(ref c.b)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void MixedSizes_SizeIsLargestField()
    {
        CUnionMixedSizes c = default;

        // Size should be 8 (int64 is the largest field)
        Assert.Equal(8, Unsafe.SizeOf<CUnionMixedSizes>());

        // All fields should be at offset 0
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionMixedSizes, byte>(ref c), ref c.byteField));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionMixedSizes, byte>(ref c), ref Unsafe.As<short, byte>(ref c.shortField)));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionMixedSizes, byte>(ref c), ref Unsafe.As<int, byte>(ref c.intField)));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionMixedSizes, byte>(ref c), ref Unsafe.As<long, byte>(ref c.longField)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void TwoInts_ShareSameMemory()
    {
        CUnionTwoInts c = default;

        // Size should be 4 (both fields are int32)
        Assert.Equal(4, Unsafe.SizeOf<CUnionTwoInts>());

        // Both fields should be at offset 0
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionTwoInts, byte>(ref c), ref Unsafe.As<int, byte>(ref c.first)));
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CUnionTwoInts, byte>(ref c), ref Unsafe.As<int, byte>(ref c.second)));

        // Writing to one field should affect the other
        c.first = 0x55556666;
        Assert.Equal(0x55556666, c.second);

        c.second = 0x77778888;
        Assert.Equal(0x77778888, c.first);
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ByRefLike_ThrowTypeLoadException()
    {
        Assert.Throws<TypeLoadException>(() => typeof(CUnionByRefLike));
    }
}

// CUnion type definitions

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionBlittablePrimitiveFields
{
    public int a;
    public float b;
    public byte c;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionNonBlittablePrimitiveFields
{
    public bool b;
    public char c;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionWithReferenceFields
{
    public string a;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionWithMixedFields
{
    public int a;
    public string b;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct NestedCUnionType
{
    public int x;
    public long y;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionCustomCUnionField
{
    public NestedCUnionType x;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionCustomSeqStructField
{
    public NestedSequentialType y;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionCustomAutoStructField
{
    public NestedAutoLayoutType y;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct EmptyCUnion
{
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionMixedSizes
{
    public byte byteField;
    public short shortField;
    public int intField;
    public long longField;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public struct CUnionTwoInts
{
    public int first;
    public int second;
}

[ExtendedLayout(ExtendedLayoutKind.CUnion)]
public ref struct CUnionByRefLike
{
    public int a;
}
