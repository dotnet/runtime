// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

namespace ExtendedLayoutTests;

public static class CStructTests
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void BlittablePrimitiveFieldsLayout()
    {
        var c = default(CStructBlittablePrimitiveFields);

        Assert.Equal(12, Unsafe.SizeOf<CStructBlittablePrimitiveFields>());
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CStructBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<int, byte>(ref c.a)));
        Assert.Equal(4, Unsafe.ByteOffset(ref Unsafe.As<CStructBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<float, byte>(ref c.b)));
        Assert.Equal(8, Unsafe.ByteOffset(ref Unsafe.As<CStructBlittablePrimitiveFields, byte>(ref c), ref c.c));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NonBlittableUnmanagedPrimitiveFields_TreatedAsBlittable()
    {
        var c = default(CStructNonBlittablePrimitiveFields);
        Assert.Equal(Unsafe.SizeOf<CStructNonBlittablePrimitiveFields>(), Marshal.SizeOf<CStructNonBlittablePrimitiveFields>());
        Assert.Equal(4, Unsafe.SizeOf<CStructNonBlittablePrimitiveFields>());
        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CStructNonBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<bool, byte>(ref c.b)));
        Assert.Equal(2, Unsafe.ByteOffset(ref Unsafe.As<CStructNonBlittablePrimitiveFields, byte>(ref c), ref Unsafe.As<char, byte>(ref c.c)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ReferenceFields_ThrowTypeLoadException()
    {
        Assert.Throws<TypeLoadException>(() => typeof(CStructWithReferenceFields));

        Assert.Throws<TypeLoadException>(() => typeof(CStructWithMixedFields));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NestedCStruct()
    {
        var nested = new CStructCustomCStructField
        {
            y = new NestedCStructType
            {
                x = 42
            }
        };

        Assert.Equal(4, Unsafe.SizeOf<CStructCustomCStructField>());
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NestedNonCStructNonAuto()
    {
        var nested = new CStructCustomSeqStructField
        {
            y = new NestedSequentialType
            {
                x = 42
            }
        };

        Assert.Equal(4, Unsafe.SizeOf<CStructCustomSeqStructField>());
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void NestedAutoLayout_ThrowTypeLoadException()
    {
        Assert.Throws<TypeLoadException>(() => typeof(CStructCustomAutoStructField));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void EmptyStruct()
    {
        Assert.Throws<TypeLoadException>(() => typeof(EmptyCStruct));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExplicitOffsets_Ignored()
    {
        CStructWithOffsets c = default;

        Assert.Equal(0, Unsafe.ByteOffset(ref Unsafe.As<CStructWithOffsets, byte>(ref c), ref Unsafe.As<int, byte>(ref c.a)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ExplicitSize_Ignored()
    {
        Assert.Equal(4, Unsafe.SizeOf<CStructWithSize>());
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void Pack_Ignored()
    {
        Assert.Equal(8, Unsafe.SizeOf<CStructWithPack>());

        CStructWithPack c = default;

        Assert.Equal(4, Unsafe.ByteOffset(ref Unsafe.As<CStructWithPack, byte>(ref c), ref Unsafe.As<int, byte>(ref c.b)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ByRefLike_ThrowTypeLoadException()
    {
        Assert.Throws<TypeLoadException>(() => typeof(CStructByRefLike));
    }
}

// CStruct type definitions

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructBlittablePrimitiveFields
{
    public int a;
    public float b;
    public byte c;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructNonBlittablePrimitiveFields
{
    public bool b;
    public char c;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructWithReferenceFields
{
    public string a;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructWithMixedFields
{
    public int a;
    public string b;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct NestedCStructType
{
    public int x;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructCustomCStructField
{
    public NestedCStructType y;
}

[StructLayout(LayoutKind.Sequential)]
public struct NestedSequentialType
{
    public int x;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructCustomSeqStructField
{
    public NestedSequentialType y;
}

[StructLayout(LayoutKind.Auto)]
public struct NestedAutoLayoutType
{
    public int x;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct CStructCustomAutoStructField
{
    public NestedAutoLayoutType y;
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public struct EmptyCStruct
{
}

[ExtendedLayout(ExtendedLayoutKind.CStruct)]
public ref struct CStructByRefLike
{
    public int a;
}
