// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

[SkipOnMono("This test suite tests CoreCLR and Crossgen2/NativeAOT-specific layout rules.")]
public unsafe class ManagedSequential
{
    [StructLayout(LayoutKind.Sequential)]
    class LayoutClassObjectBase
    {
        public byte b1;
        public long l1;
    }
    class AutoClassLayoutBase : LayoutClassObjectBase
    {
        public byte b2;
        public long l2;
    }

    class AutoClassObjectBase
    {
        public byte b1;
        public long l1;
    }

    [StructLayout(LayoutKind.Sequential)]
    class LayoutClassLayoutBase : LayoutClassObjectBase
    {
        public byte b2;
        public long l2;
    }

    [StructLayout(LayoutKind.Sequential)]
    class EmptySequentialClassObjectBase
    {
    }

    class AutoClassDerived : EmptySequentialClassObjectBase
    {
        public int i;
    }

    [StructLayout(LayoutKind.Sequential)]
    class ManagedSequentialDisqualifiedClassDerived : EmptySequentialClassObjectBase
    {
        public AutoClassObjectBase o;
        public byte b;
    }

    class RawData
    {
        public byte data;
    }

    struct OneByte
    {
        public byte Value;
    }

    struct Empty
    {
    }

    class Int32Base
    {
        public int BaseValue;
    }

    class AutoClassWithGap<TSmall, TLarge> : Int32Base
    {
        public ushort X;
        public TSmall Small;
        public ushort Y;
        public TLarge Large;
    }

    class AutoClassWithGapAndExtraField : Int32Base
    {
        public ushort X;
        public OneByte Small;
        public ushort Y;
        public ushort Z;
        public object Large;
    }

    [Fact]
    public static void AutoClassGapFillingWithOneByteStruct()
    {
        VerifyAutoClassGapFilling(new OneByte { Value = 0x5A }, new object());
    }

    [Fact]
    public static void AutoClassGapFillingWithEmptyStruct()
    {
        VerifyAutoClassGapFilling(new Empty(), new object());
    }

    [Fact]
    public static void AutoClassGapFillingWithInt64()
    {
        VerifyAutoClassGapFilling(new OneByte { Value = 0x5A }, 0x1122334455667788L);
    }

    private static void VerifyAutoClassGapFilling<TSmall, TLarge>(TSmall small, TLarge large)
    {
        var value = new AutoClassWithGap<TSmall, TLarge>
        {
            BaseValue = 0x11223344,
            X = 0x1234,
            Small = small,
            Y = 0xFFFF,
            Large = large
        };

        Assert.Equal(0x11223344, Read(ref value.BaseValue));
        Assert.Equal((ushort)0x1234, Read(ref value.X));
        Assert.Equal(small, Read(ref value.Small));
        Assert.Equal(ushort.MaxValue, Read(ref value.Y));
        Assert.Equal(large, Read(ref value.Large));
    }

    [Fact]
    public static void AutoClassGapFillingDoesNotOverlapStruct()
    {
        var large = new object();
        var value = new AutoClassWithGapAndExtraField
        {
            BaseValue = 0x11223344,
            X = 0x1234,
            Small = new OneByte { Value = 0x5A },
            Y = 0x5678,
            Z = 0x9ABC,
            Large = large
        };

        Assert.Equal(0x11223344, Read(ref value.BaseValue));
        Assert.Equal((ushort)0x1234, Read(ref value.X));
        Assert.Equal((byte)0x5A, Read(ref value.Small.Value));
        Assert.Equal((ushort)0x5678, Read(ref value.Y));
        Assert.Equal((ushort)0x9ABC, Read(ref value.Z));
        Assert.Same(large, Read(ref value.Large));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T Read<T>(ref T value) => value;

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void LayoutClassObjectBaseIsManagedSequential()
    {
        var o = new LayoutClassObjectBase();
        // Validate that the long member is placed after the byte member, as is done with sequential layout in this case.
        Assert.Equal(8, (int)Unsafe.ByteOffset(ref o.b1, ref Unsafe.As<long, byte>(ref o.l1)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void LayoutClassLayoutBaseIsManagedSequential()
    {
        var o = new LayoutClassLayoutBase();
        // Validate that the long member is placed after the byte member, as is done with sequential layout in this case.
        Assert.Equal(8, (int)Unsafe.ByteOffset(ref o.b2, ref Unsafe.As<long, byte>(ref o.l2)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void AutoClassLayoutBaseIsManagedSequential()
    {
        var o = new AutoClassLayoutBase();
        // Validate that the long member is placed before the byte member, as is done with auto layout in this case.
        Assert.Equal(-8, (int)Unsafe.ByteOffset(ref o.b2, ref Unsafe.As<long, byte>(ref o.l2)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void AutoClassObjectBaseIsManagedSequential()
    {
        var o = new AutoClassObjectBase();
        // Validate that the long member is placed before the byte member, as is done with auto layout in this case.
        Assert.Equal(-8, (int)Unsafe.ByteOffset(ref o.b1, ref Unsafe.As<long, byte>(ref o.l1)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void AutoClassDerivedFromManagedSequentialEmpty()
    {
        var o = new AutoClassDerived();
        // Validate that the int member is placed at the 0 offset.
        Assert.Equal(0, (int)Unsafe.ByteOffset(ref Unsafe.As<RawData>(o).data, ref Unsafe.As<int, byte>(ref o.i)));
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void ManagedSequentialDisqualifiedClassDerivedFromManagedSequentialEmpty()
    {
        var o = new ManagedSequentialDisqualifiedClassDerived();
        // Validate that the object member is placed at the 0 offset.
        Assert.Equal(0, (int)Unsafe.ByteOffset(ref Unsafe.As<RawData>(o).data, ref Unsafe.As<AutoClassObjectBase, byte>(ref o.o)));
        // Validate that the byte member is placed immediately after the object member.
        Assert.Equal(sizeof(nint), (int)Unsafe.ByteOffset(ref Unsafe.As<AutoClassObjectBase, byte>(ref o.o), ref o.b));
    }
}
