// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

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

    [Fact]
    public static void LayoutClassObjectBaseIsManagedSequential()
    {
        var o = new LayoutClassObjectBase();
        // Validate that the long member is placed after the byte member, as is done with sequential layout in this case.
        Assert.Equal(8, (int)Unsafe.ByteOffset(ref o.b1, ref Unsafe.As<long, byte>(ref o.l1)));
    }

    [Fact]
    public static void LayoutClassLayoutBaseIsManagedSequential()
    {
        var o = new LayoutClassLayoutBase();
        // Validate that the long member is placed after the byte member, as is done with sequential layout in this case.
        Assert.Equal(8, (int)Unsafe.ByteOffset(ref o.b2, ref Unsafe.As<long, byte>(ref o.l2)));
    }

    [Fact]
    public static void AutoClassLayoutBaseIsManagedSequential()
    {
        var o = new AutoClassLayoutBase();
        // Validate that the long member is placed before the byte member, as is done with auto layout in this case.
        Assert.Equal(-8, (int)Unsafe.ByteOffset(ref o.b2, ref Unsafe.As<long, byte>(ref o.l2)));
    }

    [Fact]
    public static void AutoClassObjectBaseIsManagedSequential()
    {
        var o = new AutoClassObjectBase();
        // Validate that the long member is placed before the byte member, as is done with auto layout in this case.
        Assert.Equal(-8, (int)Unsafe.ByteOffset(ref o.b1, ref Unsafe.As<long, byte>(ref o.l1)));
    }

    [Fact]
    public static void AutoClassDerivedFromManagedSequentialEmpty()
    {
        var o = new AutoClassDerived();
        // Validate that the int member is placed at the 0 offset.
        Assert.Equal(0, (int)Unsafe.ByteOffset(ref Unsafe.As<RawData>(o).data, ref Unsafe.As<int, byte>(ref o.i)));
    }

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
