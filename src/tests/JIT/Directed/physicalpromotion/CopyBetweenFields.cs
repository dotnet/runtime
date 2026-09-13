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
        [FieldOffset(4)] public int High;
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
            Assert.Equal(wide, NarrowWithLiveRemainder(wide));
            Assert.Equal(wide, NarrowSplitSource(wide));
            Assert.Equal(3 * (long)(short)wide, NarrowShortAtOffset(wide));
            Assert.Equal(3 * (int)(sbyte)wide, NarrowByte(wide));
            Assert.Equal(3 * (int)(ushort)value, NarrowIntToUShort(value));
            ModifiedAfterCopy(wide);
            CopyArgumentIsolation(wide);
            CopyArgumentAcrossException(wide);
        }
        foreach (long value in new[] { long.MinValue, long.MaxValue, -1L, 0x1234567887654321L })
        {
            Assert.Equal(value, NarrowWithLiveRemainder(value));
            Assert.Equal(value, NarrowSplitSource(value));
            Assert.Equal(3 * (long)(short)value, NarrowShortAtOffset(value));
            Assert.Equal(3 * (int)(sbyte)value, NarrowByte(value));
        }
        ReturnBufferExceptions();
        ReturnBufferAfterRead();
        PartialReturnBuffer();
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
        // The first call defines the non-GC return buffer; no prolog zeroing is needed.
        // X64-WINDOWS-NOT: xor
        // X64-WINDOWS: call {{.*}}CopyBetweenFields:Create
        // Build the outgoing argument from the replacement, without synchronizing
        // source storage and immediately loading across that narrow store.
        // X64-WINDOWS: mov [[VALUE:r[a-z0-9]+]], qword ptr [rsp+[[SOURCE:0x[0-9A-Fa-f]+]]]
        // X64-WINDOWS-NOT: mov qword ptr [rsp+[[SOURCE]]], [[VALUE]]
        // X64-WINDOWS: call {{.*}}CopyBetweenFields:Consume
        // X64-WINDOWS-NOT: mov {{e[a-z0-9]+}}, dword ptr [rsp
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        Consume(src); // The outgoing copy can take Wide directly from its replacement.
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
        // X64-WINDOWS-NOT: xor
        // X64-WINDOWS: call {{.*}}CopyBetweenFields:Create
        // Allow the first write-back, but reject another store to the same slot.
        // X64-WINDOWS: mov [[VALUE:r[a-z0-9]+]], qword ptr [rsp+[[OFFSET:0x[0-9A-Fa-f]+]]]
        // X64-WINDOWS: mov qword ptr [rsp+[[OFFSET]]], [[VALUE]]
        // X64-WINDOWS-NOT: mov qword ptr [rsp+[[OFFSET]]], [[VALUE]]
        // X64-WINDOWS-NOT: mov {{e[a-z0-9]+}}, dword ptr [rsp
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
        // X64-WINDOWS-NOT: mov {{e[a-z0-9]+}}, dword ptr [rsp
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
    private static long NarrowWithLiveRemainder(long value)
    {
        S src = Create(value);
        src.Wide = value;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        S dst = src;
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        // The source's upper bytes must also reach the destination storage.
        CheckFields(dst, value, value + 1, value + 2);
        return dst.Wide;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long NarrowSplitSource(long value)
    {
        S src = Create(value);
        src.Wide = value;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        S dst = src;
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.High);
        Observe(dst.High);
        Observe(dst.High);
        return ((long)dst.High << 32) | (uint)dst.Narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long NarrowShortAtOffset(long value)
    {
        S src = Create(value);
        src.Other = value;
        Observe(src.Other);
        Observe(src.Other);
        Observe(src.Other);
        S dst = src;
        return Observe(dst.OffsetSignedShort) + (long)Observe(dst.OffsetSignedShort) + Observe(dst.OffsetSignedShort);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NarrowByte(long value)
    {
        S src = Create(value);
        src.Wide = value;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        S dst = src;
        return Observe(dst.SignedByte) + Observe(dst.SignedByte) + Observe(dst.SignedByte);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NarrowIntToUShort(int value)
    {
        S src = Create(value);
        src.Narrow = value;
        Observe(src.Narrow);
        Observe(src.Narrow);
        Observe(src.Narrow);
        S dst = src;
        return Observe(dst.UnsignedShort) + Observe(dst.UnsignedShort) + Observe(dst.UnsignedShort);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S ThrowBeforeReturning() => throw new System.InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReturnBufferExceptions()
    {
        S value = default;
        try
        {
            value = ThrowBeforeReturning();
        }
        catch (System.InvalidOperationException)
        {
            CheckFields(value, 0, 0, 0);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReturnBufferAfterRead()
    {
        S value = default;
        CheckFields(value, 0, 0, 0);
        value = Create(123);
        CheckFields(value, 123, 124, 125);
    }

    private struct Outer
    {
        public S Value;
        public long Other;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckOuter(Outer value)
    {
        CheckFields(value.Value, 123, 124, 125);
        Assert.Equal(0, value.Other);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PartialReturnBuffer()
    {
        Outer value = default;
        value.Value = Create(123);
        CheckOuter(value);
    }

    private static long s_consumed;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeAndModify(S value)
    {
        s_consumed = value.Wide;
        value.Wide = ~value.Wide;
        value.Other = -7;
        CheckFields(value, ~s_consumed, -7, s_consumed + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyArgumentIsolation(long value)
    {
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        ConsumeAndModify(src);
        Assert.Equal(value + 1, s_consumed);
        CheckFields(src, value + 1, value + 1, value + 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeThenThrow(S value)
    {
        ConsumeAndModify(value);
        throw new System.InvalidOperationException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyArgumentAcrossException(long value)
    {
        S src = Create(value);
        try
        {
            src.Wide++;
            Observe(src.Wide);
            Observe(src.Wide);
            Observe(src.Wide);
            ConsumeThenThrow(src);
        }
        catch (System.InvalidOperationException)
        {
            CheckFields(src, value + 1, value + 1, value + 2);
        }
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
