// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public class IntCast
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long(short value)
        {
            // X64-NOT: cdqe
            return (long)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long Cast_Short_To_Long_Add(short value1, short value2)
        {
            // X64:     movsx
            // X64-NOT: cdqe
            // X64:     movsx
            // X64-NOT: movsxd

            return (long)value1 + (long)value2;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            foreach (int value in new[] { int.MinValue, -65537, -32769, -129, -128, -1, 0, 127, 128, 32767, 32768, 65535, int.MaxValue })
            {
                Assert.Equal((int)(sbyte)value, NarrowByte(value));
                Assert.Equal((int)(short)value, NarrowShort(value));
                Assert.Equal((int)(short)value, NarrowReturnedShort(value));
                Assert.Equal((long)(short)value, NarrowReturnedShortToLong(value));
                Assert.Equal((long)(sbyte)value, NarrowByteToLong(value));
                Assert.Equal((long)(short)value, NarrowShortToLong(value));
                Assert.Equal((long)(uint)(sbyte)value, NarrowByteToUIntThenLong(value));
                Assert.Equal((long)(uint)(short)value, NarrowShortToUIntThenLong(value));
                Assert.Equal((int)(sbyte)value, NarrowByteFromMemory(new[] { value }));
                Assert.Equal((int)(short)value, NarrowShortFromMemory(new[] { value }));
                Assert.Equal(unchecked(value + (sbyte)value), ExtendByteWithLiveSource(value));
                Assert.Equal(unchecked(value + (byte)value), ExtendUnsignedByteWithLiveSource(value));
                Assert.Equal(unchecked(value + (byte)value), ExtendByteArray(new[] { (byte)value }, value));
                s_byte = (byte)value;
                Assert.Equal(unchecked(value + (byte)value), ExtendStaticByte(value));
                Assert.Equal(unchecked(value + (sbyte)(value + 1)), ExtendPreservedByte(value, value + 1));
                Assert.Equal((ulong)(uint)(sbyte)value, NativeThenInt(value));
                Assert.Equal((long)(sbyte)value, IntThenNative(value));
                Assert.Equal((ulong)(uint)(sbyte)value, LoadByteAsUInt(new[] { (sbyte)value }));
                Assert.Equal((ulong)(uint)(short)value, LoadShortAsUInt(new[] { (short)value }));
                Assert.Equal((long)(sbyte)value, LoadByteAsLong(new[] { (sbyte)value }));
                Assert.Equal((long)(short)value, LoadShortAsLong(new[] { (short)value }));
                Assert.Equal((ulong)(uint)(short)value, LoadShortWithMixedUses(new[] { (short)value }));
            }
            if (Cast_Short_To_Long(Int16.MaxValue) != 32767)
                return 0;

            if (Cast_Short_To_Long_Add(Int16.MaxValue, Int16.MaxValue) != 65534)
                return 0;

            return 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowByte(int value)
        {
            // X64-FULL-LINE: movsx eax, {{[a-z0-9]+}}
            return (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowShort(int value)
        {
            // X64-FULL-LINE: movsx eax, {{[a-z0-9]+}}
            return (short)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ReturnInt(int value) => value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowReturnedShort(int value)
        {
            // A call result forces the source into EAX, allowing cwde for an int result.
            // X64: call
            // X64: cwde
            return (short)ReturnInt(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowReturnedShortToLong(int value)
        {
            // cwde would incorrectly clear bits 32-63 for a negative short.
            // X64: call
            // X64-NOT: cwde
            // X64: movsx rax, ax
            return (short)ReturnInt(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowByteToLong(int value)
        {
            // X64-FULL-LINE: movsx rax, {{[a-z0-9]+}}
            return (sbyte)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowShortToLong(int value)
        {
            // X64-FULL-LINE: movsx rax, {{[a-z0-9]+}}
            return (short)value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowByteToUIntThenLong(int value) => (uint)(sbyte)value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long NarrowShortToUIntThenLong(int value) => (uint)(short)value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowByteFromMemory(int[] value) => (sbyte)value[0];

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int NarrowShortFromMemory(int[] value) => (short)value[0];

        private static int s_observed;
        private static long s_observedLong;
        private static byte s_byte;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Observe(int value) => s_observed = value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtendByteWithLiveSource(int value)
        {
            // The body has one 3-byte extension, a move, a call and a lea.
            // Checking its size detects a redundant prefix invisible in the mnemonic.
            // X64-WINDOWS: movsx {{e[a-z0-9]+}}, {{[a-z0-9]+}}
            // X64-WINDOWS: ;; size=14
            int narrowed = (sbyte)value;
            Observe(narrowed);
            return value + narrowed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtendUnsignedByteWithLiveSource(int value)
        {
            // X64-WINDOWS: movzx {{e[a-z0-9]+}}, {{[a-z0-9]+}}
            // X64-WINDOWS: ;; size=14
            int narrowed = (byte)value;
            Observe(narrowed);
            return value + narrowed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtendByteArray(byte[] values, int value)
        {
            int narrowed = values[0];
            Observe(narrowed);
            return value + narrowed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtendStaticByte(int value)
        {
            int narrowed = s_byte;
            Observe(narrowed);
            return value + narrowed;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ExtendPreservedByte(int first, int second)
        {
            Observe(first);
            Observe(second);
            return first + (sbyte)second;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong NativeThenInt(int value)
        {
            // The second extension must clear the upper 32 bits for negative values.
            // X64: movsx rax,
            // X64: movsx eax,
            long extended = (sbyte)value;
            s_observedLong = extended;
            return (uint)(sbyte)extended;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long IntThenNative(int value)
        {
            // A 32-bit extension does not establish the sign of the upper 32 bits.
            // X64: movsx eax,
            // X64: cdqe
            int extended = (sbyte)value;
            s_observed = extended;
            return (sbyte)extended;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong LoadByteAsUInt(sbyte[] values)
        {
            // X64: movsx eax, byte ptr [
            return (uint)values[0];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong LoadShortAsUInt(short[] values)
        {
            // X64: movsx eax, word ptr [
            return (uint)values[0];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LoadByteAsLong(sbyte[] values)
        {
            // X64: movsx rax, byte ptr [
            // X64-NOT: movsxd
            return values[0];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long LoadShortAsLong(short[] values)
        {
            // X64: movsx rax, word ptr [
            // X64-NOT: movsxd
            return values[0];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ulong LoadShortWithMixedUses(short[] values)
        {
            // The signed use must not leave sign bits in the unsigned result.
            // X64: movsx rax, word ptr [
            // X64: mov qword ptr [{{.*}}], {{r[a-z0-9]+}}
            // X64: mov eax, eax
            int value = values[0];
            s_observedLong = value;
            return (uint)value;
        }
    }
}
