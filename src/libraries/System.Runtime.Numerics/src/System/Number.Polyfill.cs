// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System
{
    // Polyfill CoreLib internal interfaces and methods
    // Define necessary members only

    internal interface IUtfChar<TSelf> :
        IEquatable<TSelf>
        where TSelf : unmanaged, IUtfChar<TSelf>
    {
        public static abstract TSelf CastFrom(byte value);

        public static abstract TSelf CastFrom(char value);

        public static abstract TSelf CastFrom(int value);

        public static abstract TSelf CastFrom(uint value);

        public static abstract TSelf CastFrom(ulong value);

        public static abstract uint CastToUInt32(TSelf value);
    }

#pragma warning disable CA1067 // Polyfill only type
    internal readonly struct Utf16Char(char ch) : IUtfChar<Utf16Char>
#pragma warning restore CA1067
    {
        private readonly char value = ch;

        public static Utf16Char CastFrom(byte value) => new((char)value);
        public static Utf16Char CastFrom(char value) => new(value);
        public static Utf16Char CastFrom(int value) => new((char)value);
        public static Utf16Char CastFrom(uint value) => new((char)value);
        public static Utf16Char CastFrom(ulong value) => new((char)value);
        public static uint CastToUInt32(Utf16Char value) => value.value;
        public bool Equals(Utf16Char other) => value == other.value;
    }

#pragma warning disable CA1067 // Polyfill only type
    internal readonly struct Utf8Char(byte ch) : IUtfChar<Utf8Char>
#pragma warning restore CA1067
    {
        private readonly byte value = ch;

        public static Utf8Char CastFrom(byte value) => new(value);
        public static Utf8Char CastFrom(char value) => new((byte)value);
        public static Utf8Char CastFrom(int value) => new((byte)value);
        public static Utf8Char CastFrom(uint value) => new((byte)value);
        public static Utf8Char CastFrom(ulong value) => new((byte)value);
        public static uint CastToUInt32(Utf8Char value) => value.value;
        public bool Equals(Utf8Char other) => value == other.value;
    }

    internal static partial class Number
    {
        internal static bool AllowHyphenDuringParsing(this NumberFormatInfo info)
        {
            string negativeSign = info.NegativeSign;
            return negativeSign.Length == 1 &&
                   negativeSign[0] switch
                   {
                       '\u2012' or         // Figure Dash
                       '\u207B' or         // Superscript Minus
                       '\u208B' or         // Subscript Minus
                       '\u2212' or         // Minus Sign
                       '\u2796' or         // Heavy Minus Sign
                       '\uFE63' or         // Small Hyphen-Minus
                       '\uFF0D' => true,   // Fullwidth Hyphen-Minus
                       _ => false
                   };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsWhiteSpace<TChar>(this ReadOnlySpan<TChar> span)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            int elemsConsumed;

            for (int i = 0; i < span.Length; i += elemsConsumed)
            {
                if (DecodeFromUtfChar(span, out Rune rune, out elemsConsumed) != OperationStatus.Done)
                {
                    return false;
                }

                if (!Rune.IsWhiteSpace(rune))
                {
                    return false;
                }
            }

            return true;
        }

        internal static OperationStatus DecodeFromUtfChar<TChar>(ReadOnlySpan<TChar> span, out Rune result, out int elemsConsumed)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            return (typeof(TChar) == typeof(Utf8Char))
                 ? Rune.DecodeFromUtf8(MemoryMarshal.Cast<TChar, byte>(span), out result, out elemsConsumed)
                 : Rune.DecodeFromUtf16(MemoryMarshal.Cast<TChar, char>(span), out result, out elemsConsumed);
        }

        internal static OperationStatus FromHexString(ReadOnlySpan<byte> utf8Text, Span<byte> destination, out int charsConsumed, out int bytesWritten)
        {
            (int quotient, int remainder) = Math.DivRem(utf8Text.Length, 2);

            if (quotient == 0)
            {
                charsConsumed = 0;
                bytesWritten = 0;

                return remainder == 1 ? OperationStatus.NeedMoreData : OperationStatus.Done;
            }

            OperationStatus result;

            if (destination.Length < quotient)
            {
                utf8Text = utf8Text.Slice(0, destination.Length * 2);
                quotient = destination.Length;
                result = OperationStatus.DestinationTooSmall;
            }
            else
            {
                if (remainder == 1)
                {
                    utf8Text = utf8Text.Slice(0, utf8Text.Length - 1);
                    result = OperationStatus.NeedMoreData;
                }
                else
                {
                    result = OperationStatus.Done;
                }

                destination = destination.Slice(0, quotient);
            }

            if (!TryDecodeHexStringFromUtf8(utf8Text, destination, out charsConsumed))
            {
                bytesWritten = charsConsumed / 2;
                return OperationStatus.InvalidData;
            }

            bytesWritten = quotient;
            charsConsumed = utf8Text.Length;
            return result;
        }

        internal static bool TryDecodeHexStringFromUtf8(ReadOnlySpan<byte> utf8Text, Span<byte> destination, out int bytesProcessed)
        {
            if (BitConverter.IsLittleEndian && (Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported) &&
                (utf8Text.Length >= Vector128<byte>.Count))
            {
                return TryDecodeFromUtf16_Vector128(utf8Text, destination, out bytesProcessed);
            }

            return TryDecodeHexStringFromUtf8_Scalar(utf8Text, destination, out bytesProcessed);
        }

        internal static bool TryDecodeFromUtf16_Vector128(ReadOnlySpan<byte> utf8Text, Span<byte> destination, out int bytesProcessed)
        {
            Debug.Assert(Ssse3.IsSupported || AdvSimd.Arm64.IsSupported || PackedSimd.IsSupported);
            Debug.Assert(utf8Text.Length <= destination.Length);
            Debug.Assert((utf8Text.Length % 2) == 0);
            Debug.Assert(utf8Text.Length >= Vector128<byte>.Count);

            nuint offset = 0;
            nuint lengthSubOneVector128 = (uint)(utf8Text.Length - Vector128<byte>.Count);

            ref byte srcRef = ref MemoryMarshal.GetReference(utf8Text);
            ref byte destRef = ref MemoryMarshal.GetReference(destination);

            do
            {
                // The algorithm is UTF8 so we'll be loading one UTF-8 vector
                Vector128<byte> vec = Vector128.LoadUnsafe(ref srcRef, offset);

                // Based on "Algorithm #3" https://github.com/WojciechMula/toys/blob/master/simd-parse-hex/geoff_algorithm.cpp
                // by Geoff Langdale and Wojciech Mula
                // Move digits '0'..'9' into range 0xf6..0xff.
                Vector128<byte> t1 = vec + Vector128.Create((byte)(0xFF - '9'));
                // And then correct the range to 0xf0..0xf9.
                // All other bytes become less than 0xf0.
                Vector128<byte> t2 = Vector128.SubtractSaturate(t1, Vector128.Create((byte)6));
                // Convert into uppercase 'a'..'f' => 'A'..'F' and
                // move hex letter 'A'..'F' into range 0..5.
                Vector128<byte> t3 = (vec & Vector128.Create((byte)0xDF)) - Vector128.Create((byte)'A');
                // And correct the range into 10..15.
                // The non-hex letters bytes become greater than 0x0f.
                Vector128<byte> t4 = Vector128.AddSaturate(t3, Vector128.Create((byte)10));
                // Convert '0'..'9' into nibbles 0..9. Non-digit bytes become
                // greater than 0x0f. Finally choose the result: either valid nibble (0..9/10..15)
                // or some byte greater than 0x0f.
                Vector128<byte> nibbles = Vector128.Min(t2 - Vector128.Create((byte)0xF0), t4);

                // Any high bit is a sign that input is not a valid hex data
                if (!AllCharsInVectorAreAscii(vec) ||
                    Vector128.AddSaturate(nibbles, Vector128.Create((byte)(127 - 15))).ExtractMostSignificantBits() != 0)
                {
                    // Input is either non-ASCII or invalid hex data
                    break;
                }
                Vector128<byte> output;
                if (Ssse3.IsSupported)
                {
                    output = Ssse3.MultiplyAddAdjacent(nibbles,
                        Vector128.Create((short)0x0110).AsSByte()).AsByte();
                }
                else if (AdvSimd.Arm64.IsSupported)
                {
                    // Workaround for missing MultiplyAddAdjacent on ARM
                    Vector128<short> even = AdvSimd.Arm64.TransposeEven(nibbles, Vector128<byte>.Zero).AsInt16();
                    Vector128<short> odd = AdvSimd.Arm64.TransposeOdd(nibbles, Vector128<byte>.Zero).AsInt16();
                    even = AdvSimd.ShiftLeftLogical(even, 4).AsInt16();
                    output = AdvSimd.AddSaturate(even, odd).AsByte();
                }
                else if (PackedSimd.IsSupported)
                {
                    Vector128<byte> shiftedNibbles = PackedSimd.ShiftLeft(nibbles, 4);
                    Vector128<byte> zipped = PackedSimd.BitwiseSelect(nibbles, shiftedNibbles, Vector128.Create((ushort)0xFF00).AsByte());
                    output = PackedSimd.AddPairwiseWidening(zipped).AsByte();
                }
                else
                {
                    // We explicitly recheck each IsSupported query to ensure that the trimmer can see which paths are live/dead
                    ThrowHelper.ThrowUnreachableException();
                    output = default;
                }
                // Accumulate output in lower INT64 half and take care about endianness
                output = Vector128.Shuffle(output, Vector128.Create((byte)0, 2, 4, 6, 8, 10, 12, 14, 0, 0, 0, 0, 0, 0, 0, 0));
                // Store 8 bytes in dest by given offset
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref destRef, offset / 2), output.AsUInt64().ToScalar());

                offset += (uint)Vector128<byte>.Count;
                if (offset == (nuint)utf8Text.Length)
                {
                    bytesProcessed = utf8Text.Length;
                    return true;
                }
                // Overlap with the current chunk for trailing elements
                if (offset > lengthSubOneVector128)
                {
                    offset = lengthSubOneVector128;
                }
            }
            while (true);

            // Fall back to the scalar routine in case of invalid input.
            bool fallbackResult = TryDecodeHexStringFromUtf8_Scalar(utf8Text.Slice((int)offset), destination.Slice((int)(offset / 2)), out int fallbackProcessed);
            bytesProcessed = (int)offset + fallbackProcessed;
            return fallbackResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllCharsInVectorAreAscii(Vector128<byte> vec)
        {
            return (vec & Vector128.Create(unchecked((byte)~0x7F))).Equals(Vector128<byte>.Zero);
        }

        internal static bool TryDecodeHexStringFromUtf8_Scalar(ReadOnlySpan<byte> utf8Text, Span<byte> destination, out int bytesProcessed)
        {
            Debug.Assert(utf8Text.Length % 2 == 0, "Un-even number of characters provided");
            Debug.Assert(utf8Text.Length / 2 == destination.Length, "Target buffer not right-sized for provided characters");

            int i = 0;
            int j = 0;
            int byteLo = 0;
            int byteHi = 0;
            while (j < destination.Length)
            {
                byteLo = FromChar(utf8Text[i + 1]);
                byteHi = FromChar(utf8Text[i]);

                // byteHi hasn't been shifted to the high half yet, so the only way the bitwise or produces this pattern
                // is if either byteHi or byteLo was not a hex character.
                if ((byteLo | byteHi) == 0xFF)
                    break;

                destination[j++] = (byte)((byteHi << 4) | byteLo);
                i += 2;
            }

            if (byteLo == 0xFF)
                i++;

            bytesProcessed = i;
            return (byteLo | byteHi) != 0xFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FromChar(int c)
        {
            return (c >= CharToHexLookup.Length) ? 0xFF : CharToHexLookup[c];
        }

        internal static ReadOnlySpan<byte> CharToHexLookup =>
        [
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
            0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 63
            0xFF, 0xA,  0xB,  0xC,  0xD,  0xE,  0xF,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 79
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 95
            0xFF, 0xa,  0xb,  0xc,  0xd,  0xe,  0xf,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 111
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 127
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 143
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 159
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 175
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 191
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 207
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 223
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 239
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 255
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> FromString<TChar>(string value)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            if (typeof(TChar) == typeof(Utf8Char))
            {
                return Unsafe.BitCast<ReadOnlySpan<byte>, ReadOnlySpan<TChar>>(Encoding.UTF8.GetBytes(value));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(Utf16Char));
                return Unsafe.BitCast<ReadOnlySpan<char>, ReadOnlySpan<TChar>>(value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PositiveSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.PositiveSign);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NegativeSignTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.NegativeSign);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencySymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.CurrencySymbol);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.PercentSymbol);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PerMilleSymbolTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.PerMilleSymbol);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencyDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.CurrencyDecimalSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> CurrencyGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.CurrencyGroupSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NumberDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.NumberDecimalSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> NumberGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.NumberGroupSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentDecimalSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.PercentDecimalSeparator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> PercentGroupSeparatorTChar<TChar>(this NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar> => FromString<TChar>(info.PercentGroupSeparator);
    }
}
