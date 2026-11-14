// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class CopyBetweenFields
{
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct S
    {
        [FieldOffset(0)] public long Wide;
        [FieldOffset(0)] public int Narrow;
        [FieldOffset(0)] public sbyte SignedByte;
        [FieldOffset(0)] public byte UnsignedByte;
        [FieldOffset(0)] public short SignedShort;
        [FieldOffset(0)] public ushort UnsignedShort;
        [FieldOffset(8)] public long Other;
        [FieldOffset(8)] public short OffsetSignedShort;
        [FieldOffset(8)] public ushort OffsetUnsignedShort;
        [FieldOffset(16)] public long Last;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        foreach (int value in new[] { 0, 1, -1, 127, 128, 255, 256, 32767, 32768, 65535, 65536, int.MinValue, int.MaxValue })
        {
            Assert.Equal(3 * (int)(sbyte)value + 3 * (byte)value, SignedByteToUnsigned(value));
            Assert.Equal(3 * (int)(byte)value + 3 * (sbyte)value, UnsignedByteToSigned(value));
            Assert.Equal(3 * (int)(short)value + 3 * (ushort)value, SignedShortToUnsigned(value));
            Assert.Equal(3 * (int)(ushort)value + 3 * (short)value, UnsignedShortToSigned(value));
            Assert.Equal(3 * (int)(short)value + 3 * (ushort)value, SignedShortToUnsignedAtOffset(value));
            DyingDestination(value);

            long wide = (1L << 40) + value;
            Assert.Equal(wide + 1 + (int)(wide + 1), CleanSource(wide));
            Assert.Equal(wide + 1 + (int)(wide + 1), DirtySource(wide));
            Assert.Equal(wide + (int)wide, ReadBackSource(wide));
            ModifiedAfterCopy(wide);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S Create(long value) => new S { Wide = value, Other = value + 1, Last = value + 2 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(S value)
    {
        Assert.Equal(value.Other + 1, value.Last);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Observe(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(long value) { }

    // Multiple primitive uses encourage physical promotion without requiring stress.
    // The copy must preserve the low bits while changing their extension in the destination.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedByteToUnsigned(int value)
    {
        // X64-FULL-LINE: movzx {{[a-z0-9]+}}, {{[a-z0-9]+}}
        S src = Create(value);
        src.SignedByte = (sbyte)value;
        int sum = Observe(src.SignedByte) + Observe(src.SignedByte) + Observe(src.SignedByte);
        S dst = src;
        return sum + Observe(dst.UnsignedByte) + Observe(dst.UnsignedByte) + Observe(dst.UnsignedByte);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UnsignedByteToSigned(int value)
    {
        // X64-FULL-LINE: movsx {{[a-z0-9]+}}, {{[a-z0-9]+}}
        S src = Create(value);
        src.UnsignedByte = (byte)value;
        int sum = Observe(src.UnsignedByte) + Observe(src.UnsignedByte) + Observe(src.UnsignedByte);
        S dst = src;
        return sum + Observe(dst.SignedByte) + Observe(dst.SignedByte) + Observe(dst.SignedByte);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedShortToUnsigned(int value)
    {
        // X64-FULL-LINE: movzx {{[a-z0-9]+}}, {{[a-z0-9]+}}
        S src = Create(value);
        src.SignedShort = (short)value;
        int sum = Observe(src.SignedShort) + Observe(src.SignedShort) + Observe(src.SignedShort);
        S dst = src;
        return sum + Observe(dst.UnsignedShort) + Observe(dst.UnsignedShort) + Observe(dst.UnsignedShort);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int UnsignedShortToSigned(int value)
    {
        // X64-FULL-LINE: movsx {{[a-z0-9]+}}, {{[a-z0-9]+}}
        S src = Create(value);
        src.UnsignedShort = (ushort)value;
        int sum = Observe(src.UnsignedShort) + Observe(src.UnsignedShort) + Observe(src.UnsignedShort);
        S dst = src;
        return sum + Observe(dst.SignedShort) + Observe(dst.SignedShort) + Observe(dst.SignedShort);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CleanSource(long value)
    {
        // Once Consume has synchronized the struct, no further write-back is needed.
        // X64-WINDOWS: call {{.*}}CopyBetweenFields:Consume
        // X64-WINDOWS-NOT: mov qword ptr [rsp{{[^]]*}}], {{r[a-z0-9]+}}
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        Consume(src); // The struct and promoted field now hold the same value.
        S dst = src;
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Consume(src);
        return src.Wide + dst.Narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long DirtySource(long value)
    {
        // Allow the first write-back, but reject another store to the same slot.
        // X64-WINDOWS: mov [[VALUE:r[a-z0-9]+]], qword ptr [rsp+[[OFFSET:0x[0-9A-Fa-f]+]]]
        // X64-WINDOWS: mov qword ptr [rsp+[[OFFSET]]], [[VALUE]]
        // X64-WINDOWS-NOT: mov qword ptr [rsp+[[OFFSET]]], [[VALUE]]
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        S dst = src; // Requires one write-back before reading the narrower destination field.
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Consume(src); // Must not write the same value back again.
        return src.Wide + dst.Narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ReadBackSource(long value)
    {
        // This method only reads the return buffer; there should be no scalar stack store.
        // X64-WINDOWS-NOT: mov qword ptr [rsp{{[^]]*}}], {{r[a-z0-9]+}}
        S src = Create(value);
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        S dst = src; // The field was read from the return buffer and does not need writing back.
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Consume(src);
        return src.Wide + dst.Narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SignedShortToUnsignedAtOffset(int value)
    {
        // X64-FULL-LINE: movzx {{[a-z0-9]+}}, {{[a-z0-9]+}}
        S src = Create(value);
        src.OffsetSignedShort = (short)value;
        int sum = Observe(src.OffsetSignedShort) + Observe(src.OffsetSignedShort) + Observe(src.OffsetSignedShort);
        S dst = src;
        return sum + Observe(dst.OffsetUnsignedShort) + Observe(dst.OffsetUnsignedShort) + Observe(dst.OffsetUnsignedShort);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DyingDestination(int value)
    {
        S dst = Create(value);
        Observe(dst.UnsignedByte);
        Observe(dst.UnsignedByte);
        Observe(dst.UnsignedByte);
        S src = Create(value);
        src.SignedByte = (sbyte)value;
        Observe(src.SignedByte);
        Observe(src.SignedByte);
        Observe(src.SignedByte);
        dst = src;
        // The copied destination replacement dies here, but the other fields remain live.
        dst.UnsignedByte = 42;
        Assert.Equal(42, Observe(dst.UnsignedByte));
        Assert.Equal(value + 1L, dst.Other);
        Assert.Equal(value + 2L, dst.Last);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ModifiedAfterCopy(long value)
    {
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        S dst = src;
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        src.Wide += 13; // A later modification must make the source need writing back again.
        CheckFields(src, value + 14, value + 1, value + 2);
        CheckFields(dst, value + 1, value + 1, value + 2);
        Assert.Equal((int)(value + 1), dst.Narrow);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckFields(S value, long wide, long other, long last)
    {
        Assert.Equal(wide, value.Wide);
        Assert.Equal(other, value.Other);
        Assert.Equal(last, value.Last);
    }
}
