// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class LocalAddressContainment
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Fields
    {
        public int First;
        public int Second;
        public int Third;
        public int Fourth;
    }

    [StructLayout(LayoutKind.Explicit, Size = 65536)]
    private struct LargeLocal
    {
        [FieldOffset(65528)]
        public long Last;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(BitConverter.IsLittleEndian ? 0x0000000300000002L : 0x0000000200000003L, ReadAcrossFields());
        WriteAcrossFields();
        Assert.Equal(42, ReadAtOffsetLimit());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadAcrossFields()
    {
        Fields value = new Fields { First = 1, Second = 2, Third = 3, Fourth = 4 };
        return Unsafe.ReadUnaligned<long>(ref Unsafe.As<int, byte>(ref value.Second));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void WriteAcrossFields()
    {
        Fields value = new Fields { First = 1, Second = 2, Third = 3, Fourth = 4 };
        Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref value.Second), 0x0000000600000005L);
        Assert.Equal(1, value.First);
        Assert.Equal(BitConverter.IsLittleEndian ? 5 : 6, value.Second);
        Assert.Equal(BitConverter.IsLittleEndian ? 6 : 5, value.Third);
        Assert.Equal(4, value.Fourth);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadAtOffsetLimit()
    {
        LargeLocal value = default;
        Initialize(ref value);
        return Unsafe.ReadUnaligned<long>(ref Unsafe.AddByteOffset(ref Unsafe.As<LargeLocal, byte>(ref value), 65528));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Initialize(ref LargeLocal value)
    {
        value.Last = 42;
    }
}
