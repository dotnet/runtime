// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;

public class AddressExposedLoads
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeSpan(ref ReadOnlySpan<byte> span) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearSpan(ref ReadOnlySpan<byte> span) => span = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeInt(ref int value) { }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct Header
    {
        public byte Kind, Flags, Count, Version;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeHeader(ref Header header) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadPackedHeader(byte kind)
    {
        Header header = new() { Kind = kind, Flags = 1, Count = 2, Version = 3 };
        ExposeHeader(ref header);
        if (header.Kind != 0)
            return -1;

        // Read all four bytes starting at the same field the guard read.
        return BinaryPrimitives.ReadInt32LittleEndian(MemoryMarshal.CreateReadOnlySpan(ref header.Kind, 4));
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct ShortHeader
    {
        public short Kind;
        public ushort Flags;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeHeader(ref ShortHeader header) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReadPackedShortHeader(short kind)
    {
        ShortHeader header = new() { Kind = kind, Flags = 0x1234 };
        ExposeHeader(ref header);
        if (header.Kind != -32767)
            return -1;

        return BinaryPrimitives.ReadInt32LittleEndian(
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<short, byte>(ref header.Kind), 4));
    }

    [Fact]
    public static void WidenedShortRead()
    {
        Assert.Equal(0x12348001, ReadPackedShortHeader(-32767));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VolatileReads(int value)
    {
        // ARM64: {{ldr|ldar|ldapr}} w{{[0-9]+}},
        // ARM64: {{ldr|ldar|ldapr}} w{{[0-9]+}},
        ExposeInt(ref value);
        return Volatile.Read(ref value) + Volatile.Read(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GuardExposedSpan(ReadOnlySpan<byte> span, int count)
    {
        // ARM64-NOT: ThrowArgumentOutOfRangeException
        // X64-NOT: ThrowArgumentOutOfRangeException
        ExposeSpan(ref span);
        if (count < 0 || count > span.Length)
            return -1;
        return span.Slice(0, count).Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SliceAfterWrite(ReadOnlySpan<byte> span, int count, bool throughCall)
    {
        ExposeSpan(ref span);
        if (count < 0 || count > span.Length)
            return -1;
        if (throughCall)
            ClearSpan(ref span);
        else
            span = default;
        return span.Slice(0, count).Length;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(6, VolatileReads(3));
        Assert.Equal(0x03020100, ReadPackedHeader(0));
        byte[] array = new byte[8];
        foreach (int count in new[] { int.MinValue, -1, 0, 1, 8, 9, int.MaxValue })
            Assert.Equal(count >= 0 && count <= 8 ? count : -1, GuardExposedSpan(array, count));
        foreach (bool throughCall in new[] { false, true })
        {
            Assert.Equal(0, SliceAfterWrite(array, 0, throughCall));
            Assert.Throws<ArgumentOutOfRangeException>(() => SliceAfterWrite(array, 1, throughCall));
        }
    }
}
