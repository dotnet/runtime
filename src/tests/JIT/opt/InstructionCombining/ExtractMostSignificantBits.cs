// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace TestExtractMostSignificantBits
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            bool fail = false;

            Vector128<ushort> utf16Data = Vector128.Create(
                (ushort)0x0000, (ushort)0x0800, (ushort)0x07FF, (ushort)0x8000,
                (ushort)0xD7FF, (ushort)0xD800, (ushort)0x0001, (ushort)0xFFFF);

            if (LessThanUInt16Mask(utf16Data, 0x0800) != 0x45)
            {
                fail = true;
            }

            if (GreaterThanOrEqualUInt16Mask(utf16Data, 0x0800) != 0xBA)
            {
                fail = true;
            }

            if (!AnyLessThanUInt16(utf16Data, 0x0800))
            {
                fail = true;
            }

            if (NoneLessThanUInt16(Vector128.Create((ushort)0x0800), 0x0800) != true)
            {
                fail = true;
            }

            if (CountGreaterThanOrEqualUInt16(utf16Data, 0x0800) != 5)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualUInt16(utf16Data, 0x0800) != 1)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualUInt16(Vector128<ushort>.Zero, 0x0800) != 32)
            {
                fail = true;
            }

            Vector128<int> intData = Vector128.Create(-2, 0, 7, 8);

            if (LessThanInt32Mask(intData, 7) != 0x3)
            {
                fail = true;
            }

            if (!AnyLessThanInt32(intData, 7))
            {
                fail = true;
            }

            if (NoneLessThanInt32(Vector128.Create(7), 7) != true)
            {
                fail = true;
            }

            if (CountLessThanInt32(intData, 7) != 2)
            {
                fail = true;
            }

            if (IndexOfFirstLessThanInt32(intData, 7) != 0)
            {
                fail = true;
            }

            if (IndexOfFirstLessThanInt32(Vector128.Create(7), 7) != 32)
            {
                fail = true;
            }

            Vector128<byte> byteData = Vector128.Create(
                (byte)0x00, (byte)0x80, (byte)0x7F, (byte)0xFF,
                (byte)0x01, (byte)0x81, (byte)0x40, (byte)0xC0,
                (byte)0x02, (byte)0x82, (byte)0x20, (byte)0xA0,
                (byte)0x04, (byte)0x84, (byte)0x10, (byte)0x90);

            if (GreaterThanOrEqualByteMask(byteData, 0x80) != 0xAAAA)
            {
                fail = true;
            }

            if (!AnyGreaterThanOrEqualByte(byteData, 0x80))
            {
                fail = true;
            }

            if (NoneGreaterThanOrEqualByte(Vector128<byte>.Zero, 0x80) != true)
            {
                fail = true;
            }

            if (CountGreaterThanOrEqualByte(byteData, 0x80) != 8)
            {
                fail = true;
            }

            if (CountGreaterThanOrEqualByteViaLocal(byteData, 0x80) != 8)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualByte(byteData, 0x80) != 1)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualByte(Vector128<byte>.Zero, 0x80) != 32)
            {
                fail = true;
            }

            Vector64<ushort> utf16Data64 = Vector64.Create(
                (ushort)0x0000, (ushort)0x0800, (ushort)0x07FF, (ushort)0xFFFF);

            if (LessThanUInt16Mask64(utf16Data64, 0x0800) != 0x5)
            {
                fail = true;
            }

            if (!AnyLessThanUInt1664(utf16Data64, 0x0800))
            {
                fail = true;
            }

            if (NoneLessThanUInt1664(Vector64.Create((ushort)0x0800), 0x0800) != true)
            {
                fail = true;
            }

            if (CountGreaterThanOrEqualUInt1664(utf16Data64, 0x0800) != 2)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualUInt1664(utf16Data64, 0x0800) != 1)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualUInt1664(Vector64<ushort>.Zero, 0x0800) != 32)
            {
                fail = true;
            }

            Vector64<int> intData64 = Vector64.Create(-2, 8);

            if (LessThanInt32Mask64(intData64, 7) != 0x1)
            {
                fail = true;
            }

            if (!AnyLessThanInt3264(intData64, 7))
            {
                fail = true;
            }

            if (NoneLessThanInt3264(Vector64.Create(7), 7) != true)
            {
                fail = true;
            }

            if (CountLessThanInt3264(intData64, 7) != 1)
            {
                fail = true;
            }

            if (IndexOfFirstLessThanInt3264(intData64, 7) != 0)
            {
                fail = true;
            }

            if (IndexOfFirstLessThanInt3264(Vector64.Create(7), 7) != 32)
            {
                fail = true;
            }

            Vector64<byte> byteData64 = Vector64.Create(
                (byte)0x00, (byte)0x80, (byte)0x7F, (byte)0xFF,
                (byte)0x01, (byte)0x81, (byte)0x40, (byte)0xC0);

            if (GreaterThanOrEqualByteMask64(byteData64, 0x80) != 0xAA)
            {
                fail = true;
            }

            if (!AnyGreaterThanOrEqualByte64(byteData64, 0x80))
            {
                fail = true;
            }

            if (NoneGreaterThanOrEqualByte64(Vector64<byte>.Zero, 0x80) != true)
            {
                fail = true;
            }

            if (CountGreaterThanOrEqualByte64(byteData64, 0x80) != 4)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualByte64(byteData64, 0x80) != 1)
            {
                fail = true;
            }

            if (IndexOfFirstGreaterThanOrEqualByte64(Vector64<byte>.Zero, 0x80) != 32)
            {
                fail = true;
            }

            return fail ? 101 : 100;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint LessThanUInt16Mask(Vector128<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhi {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: and {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: addv {{h[0-9]+}}, {{v[0-9]+}}.8h
            return Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint GreaterThanOrEqualUInt16Mask(Vector128<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: and {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: addv {{h[0-9]+}}, {{v[0-9]+}}.8h
            return Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyLessThanUInt16(Vector128<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhi {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umaxv {{h[0-9]+}}, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits() != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneLessThanUInt16(Vector128<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhi {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umaxv {{h[0-9]+}}, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits() == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountGreaterThanOrEqualUInt16(Vector128<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, #15
            // ARM64-FULL-LINE: addv {{h[0-9]+}}, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            return BitOperations.PopCount(Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfFirstGreaterThanOrEqualUInt16(Vector128<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.8h, {{v[0-9]+}}.8h, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: uminv {{h[0-9]+}}, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, #1
            return BitOperations.TrailingZeroCount(Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint LessThanInt32Mask(Vector128<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: and {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: addv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: smov {{x[0-9]+}}, {{v[0-9]+}}.s[0]
            return Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyLessThanInt32(Vector128<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: umaxv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits() != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneLessThanInt32(Vector128<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: umaxv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits() == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountLessThanInt32(Vector128<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, #31
            // ARM64-FULL-LINE: addv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            return BitOperations.PopCount(Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfFirstLessThanInt32(Vector128<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.4s, {{v[0-9]+}}.4s, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: uminv {{s[0-9]+}}, {{v[0-9]+}}.4s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, #1
            return BitOperations.TrailingZeroCount(Vector128.LessThan(value, Vector128.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint GreaterThanOrEqualByteMask(Vector128<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: and {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: addv {{h[0-9]+}}, {{v[0-9]+}}.8h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            return Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyGreaterThanOrEqualByte(Vector128<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umaxv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits() != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneGreaterThanOrEqualByte(Vector128<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umaxv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits() == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountGreaterThanOrEqualByte(Vector128<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, #7
            // ARM64-FULL-LINE: addv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            return BitOperations.PopCount(Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountGreaterThanOrEqualByteViaLocal(Vector128<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, #7
            // ARM64-FULL-LINE: addv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            Vector128<byte> mask = Vector128.GreaterThanOrEqual(value, Vector128.Create(limit));
            return BitOperations.PopCount(mask.ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfFirstGreaterThanOrEqualByte(Vector128<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.16b, {{v[0-9]+}}.16b, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: uminv {{b[0-9]+}}, {{v[0-9]+}}.16b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, #1
            return BitOperations.TrailingZeroCount(Vector128.GreaterThanOrEqual(value, Vector128.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint LessThanUInt16Mask64(Vector64<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhi {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: and {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            return Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyLessThanUInt1664(Vector64<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhi {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: umaxv {{h[0-9]+}}, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits() != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneLessThanUInt1664(Vector64<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhi {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: umaxv {{h[0-9]+}}, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits() == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountGreaterThanOrEqualUInt1664(Vector64<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, #15
            // ARM64-FULL-LINE: addv {{h[0-9]+}}, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            return BitOperations.PopCount(Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfFirstGreaterThanOrEqualUInt1664(Vector64<ushort> value, ushort limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.4h, {{v[0-9]+}}.4h, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: uminv {{h[0-9]+}}, {{v[0-9]+}}.4h
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.h[0]
            // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, #1
            return BitOperations.TrailingZeroCount(Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint LessThanInt32Mask64(Vector64<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: and {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            return Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyLessThanInt3264(Vector64<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: umaxp {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits() != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneLessThanInt3264(Vector64<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: umaxp {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits() == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountLessThanInt3264(Vector64<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, #31
            // ARM64-FULL-LINE: addp {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            return BitOperations.PopCount(Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfFirstLessThanInt3264(Vector64<int> value, int limit)
        {
            // ARM64-FULL-LINE: cmgt {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: uminp {{v[0-9]+}}.2s, {{v[0-9]+}}.2s, {{v[0-9]+}}.2s
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.s[0]
            // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, #1
            return BitOperations.TrailingZeroCount(Vector64.LessThan(value, Vector64.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static uint GreaterThanOrEqualByteMask64(Vector64<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: and {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            return Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool AnyGreaterThanOrEqualByte64(Vector64<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: umaxv {{b[0-9]+}}, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, ne
            return Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits() != 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool NoneGreaterThanOrEqualByte64(Vector64<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: umaxv {{b[0-9]+}}, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            // ARM64-FULL-LINE: cset {{[wx][0-9]+}}, eq
            return Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits() == 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int CountGreaterThanOrEqualByte64(Vector64<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: ushr {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, #7
            // ARM64-FULL-LINE: addv {{b[0-9]+}}, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            return BitOperations.PopCount(Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int IndexOfFirstGreaterThanOrEqualByte64(Vector64<byte> value, byte limit)
        {
            // ARM64-FULL-LINE: cmhs {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: bsl {{v[0-9]+}}.8b, {{v[0-9]+}}.8b, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: uminv {{b[0-9]+}}, {{v[0-9]+}}.8b
            // ARM64-FULL-LINE: umov {{w[0-9]+}}, {{v[0-9]+}}.b[0]
            // ARM64-FULL-LINE: sub {{w[0-9]+}}, {{w[0-9]+}}, #1
            return BitOperations.TrailingZeroCount(Vector64.GreaterThanOrEqual(value, Vector64.Create(limit)).ExtractMostSignificantBits());
        }
    }
}
