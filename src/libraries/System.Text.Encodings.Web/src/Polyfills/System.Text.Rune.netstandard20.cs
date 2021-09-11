// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Diagnostics.CodeAnalysis;

// Contains a polyfill implementation of System.Text.Rune that works on netstandard2.0.
// Implementation copied from:
// https://github.com/dotnet/runtime/blob/177d6f1a0bfdc853ae9ffeef4be99ff984c4f5dd/src/libraries/System.Private.CoreLib/src/System/Text/Rune.cs

namespace System.Text
{
    internal readonly struct Rune
    {
        private const int MaxUtf16CharsPerRune = 2; // supplementary plane code points are encoded as 2 UTF-16 code units

        private const char HighSurrogateStart = '\ud800';
        private const char LowSurrogateStart = '\udc00';
        private const int HighSurrogateRange = 0x3FF;

        private readonly uint _value;

        /// <summary>
        /// Creates a <see cref="Rune"/> from the provided Unicode scalar value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="value"/> does not represent a value Unicode scalar value.
        /// </exception>
        public Rune(uint value)
        {
            if (!UnicodeUtility.IsValidUnicodeScalar(value))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value);
            }
            _value = value;
        }

        /// <summary>
        /// Creates a <see cref="Rune"/> from the provided Unicode scalar value.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="value"/> does not represent a value Unicode scalar value.
        /// </exception>
        public Rune(int value)
            : this((uint)value)
        {
        }

        // non-validating ctor
        private Rune(uint scalarValue, bool unused)
        {
            UnicodeDebug.AssertIsValidScalar(scalarValue);
            _value = scalarValue;
        }

        /// <summary>
        /// Returns true if and only if this scalar value is ASCII ([ U+0000..U+007F ])
        /// and therefore representable by a single UTF-8 code unit.
        /// </summary>
        public bool IsAscii => UnicodeUtility.IsAsciiCodePoint(_value);

        /// <summary>
        /// Returns true if and only if this scalar value is within the BMP ([ U+0000..U+FFFF ])
        /// and therefore representable by a single UTF-16 code unit.
        /// </summary>
        public bool IsBmp => UnicodeUtility.IsBmpCodePoint(_value);

        public static bool operator ==(Rune left, Rune right) => left._value == right._value;

        public static bool operator !=(Rune left, Rune right) => left._value != right._value;

        public static bool IsControl(Rune value)
        {
            // Per the Unicode stability policy, the set of control characters
            // is forever fixed at [ U+0000..U+001F ], [ U+007F..U+009F ]. No
            // characters will ever be added to or removed from the "control characters"
            // group. See https://www.unicode.org/policies/stability_policy.html.

            // Logic below depends on Rune.Value never being -1 (since Rune is a validating type)
            // 00..1F (+1) => 01..20 (&~80) => 01..20
            // 7F..9F (+1) => 80..A0 (&~80) => 00..20

            return ((value._value + 1) & ~0x80u) <= 0x20u;
        }

        /// <summary>
        /// A <see cref="Rune"/> instance that represents the Unicode replacement character U+FFFD.
        /// </summary>
        public static Rune ReplacementChar => UnsafeCreate(UnicodeUtility.ReplacementChar);

        /// <summary>
        /// Returns the length in code units (<see cref="char"/>) of the
        /// UTF-16 sequence required to represent this scalar value.
        /// </summary>
        /// <remarks>
        /// The return value will be 1 or 2.
        /// </remarks>
        public int Utf16SequenceLength
        {
            get
            {
                int codeUnitCount = UnicodeUtility.GetUtf16SequenceLength(_value);
                Debug.Assert(codeUnitCount > 0 && codeUnitCount <= MaxUtf16CharsPerRune);
                return codeUnitCount;
            }
        }

        /// <summary>
        /// Returns the Unicode scalar value as an integer.
        /// </summary>
        public int Value => (int)_value;

        /// <summary>
        /// Decodes the <see cref="Rune"/> at the beginning of the provided UTF-16 source buffer.
        /// </summary>
        /// <returns>
        /// <para>
        /// If the source buffer begins with a valid UTF-16 encoded scalar value, returns <see cref="OperationStatus.Done"/>,
        /// and outs via <paramref name="result"/> the decoded <see cref="Rune"/> and via <paramref name="charsConsumed"/> the
        /// number of <see langword="char"/>s used in the input buffer to encode the <see cref="Rune"/>.
        /// </para>
        /// <para>
        /// If the source buffer is empty or contains only a standalone UTF-16 high surrogate character, returns <see cref="OperationStatus.NeedMoreData"/>,
        /// and outs via <paramref name="result"/> <see cref="ReplacementChar"/> and via <paramref name="charsConsumed"/> the length of the input buffer.
        /// </para>
        /// <para>
        /// If the source buffer begins with an ill-formed UTF-16 encoded scalar value, returns <see cref="OperationStatus.InvalidData"/>,
        /// and outs via <paramref name="result"/> <see cref="ReplacementChar"/> and via <paramref name="charsConsumed"/> the number of
        /// <see langword="char"/>s used in the input buffer to encode the ill-formed sequence.
        /// </para>
        /// </returns>
        /// <remarks>
        /// The general calling convention is to call this method in a loop, slicing the <paramref name="source"/> buffer by
        /// <paramref name="charsConsumed"/> elements on each iteration of the loop. On each iteration of the loop <paramref name="result"/>
        /// will contain the real scalar value if successfully decoded, or it will contain <see cref="ReplacementChar"/> if
        /// the data could not be successfully decoded. This pattern provides convenient automatic U+FFFD substitution of
        /// invalid sequences while iterating through the loop.
        /// </remarks>
        public static OperationStatus DecodeFromUtf16(ReadOnlySpan<char> source, out Rune result, out int charsConsumed)
        {
            if (!source.IsEmpty)
            {
                // First, check for the common case of a BMP scalar value.
                // If this is correct, return immediately.

                char firstChar = source[0];
                if (TryCreate(firstChar, out result))
                {
                    charsConsumed = 1;
                    return OperationStatus.Done;
                }

                // First thing we saw was a UTF-16 surrogate code point.
                // Let's optimistically assume for now it's a high surrogate and hope
                // that combining it with the next char yields useful results.

                if (1 < (uint)source.Length)
                {
                    char secondChar = source[1];
                    if (TryCreate(firstChar, secondChar, out result))
                    {
                        // Success! Formed a supplementary scalar value.
                        charsConsumed = 2;
                        return OperationStatus.Done;
                    }
                    else
                    {
                        // Either the first character was a low surrogate, or the second
                        // character was not a low surrogate. This is an error.
                        goto InvalidData;
                    }
                }
                else if (!char.IsHighSurrogate(firstChar))
                {
                    // Quick check to make sure we're not going to report NeedMoreData for
                    // a single-element buffer where the data is a standalone low surrogate
                    // character. Since no additional data will ever make this valid, we'll
                    // report an error immediately.
                    goto InvalidData;
                }
            }

            // If we got to this point, the input buffer was empty, or the buffer
            // was a single element in length and that element was a high surrogate char.

            charsConsumed = source.Length;
            result = ReplacementChar;
            return OperationStatus.NeedMoreData;

        InvalidData:

            charsConsumed = 1; // maximal invalid subsequence for UTF-16 is always a single code unit in length
            result = ReplacementChar;
            return OperationStatus.InvalidData;
        }

        /// <summary>
        /// Decodes the <see cref="Rune"/> at the beginning of the provided UTF-8 source buffer.
        /// </summary>
        /// <returns>
        /// <para>
        /// If the source buffer begins with a valid UTF-8 encoded scalar value, returns <see cref="OperationStatus.Done"/>,
        /// and outs via <paramref name="result"/> the decoded <see cref="Rune"/> and via <paramref name="bytesConsumed"/> the
        /// number of <see langword="byte"/>s used in the input buffer to encode the <see cref="Rune"/>.
        /// </para>
        /// <para>
        /// If the source buffer is empty or contains only a partial UTF-8 subsequence, returns <see cref="OperationStatus.NeedMoreData"/>,
        /// and outs via <paramref name="result"/> <see cref="ReplacementChar"/> and via <paramref name="bytesConsumed"/> the length of the input buffer.
        /// </para>
        /// <para>
        /// If the source buffer begins with an ill-formed UTF-8 encoded scalar value, returns <see cref="OperationStatus.InvalidData"/>,
        /// and outs via <paramref name="result"/> <see cref="ReplacementChar"/> and via <paramref name="bytesConsumed"/> the number of
        /// <see langword="char"/>s used in the input buffer to encode the ill-formed sequence.
        /// </para>
        /// </returns>
        /// <remarks>
        /// The general calling convention is to call this method in a loop, slicing the <paramref name="source"/> buffer by
        /// <paramref name="bytesConsumed"/> elements on each iteration of the loop. On each iteration of the loop <paramref name="result"/>
        /// will contain the real scalar value if successfully decoded, or it will contain <see cref="ReplacementChar"/> if
        /// the data could not be successfully decoded. This pattern provides convenient automatic U+FFFD substitution of
        /// invalid sequences while iterating through the loop.
        /// </remarks>
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> source, out Rune result, out int bytesConsumed)
        {
            // This method follows the Unicode Standard's recommendation for detecting
            // the maximal subpart of an ill-formed subsequence. See The Unicode Standard,
            // Ch. 3.9 for more details. In summary, when reporting an invalid subsequence,
            // it tries to consume as many code units as possible as long as those code
            // units constitute the beginning of a longer well-formed subsequence per Table 3-7.

            int index = 0;

            // Try reading input[0].

            if ((uint)index >= (uint)source.Length)
            {
                goto NeedsMoreData;
            }

            uint tempValue = source[index];
            if (!UnicodeUtility.IsAsciiCodePoint(tempValue))
            {
                goto NotAscii;
            }

        Finish:

            bytesConsumed = index + 1;
            Debug.Assert(1 <= bytesConsumed && bytesConsumed <= 4); // Valid subsequences are always length [1..4]
            result = UnsafeCreate(tempValue);
            return OperationStatus.Done;

        NotAscii:

            // Per Table 3-7, the beginning of a multibyte sequence must be a code unit in
            // the range [C2..F4]. If it's outside of that range, it's either a standalone
            // continuation byte, or it's an overlong two-byte sequence, or it's an out-of-range
            // four-byte sequence.

            if (!UnicodeUtility.IsInRangeInclusive(tempValue, 0xC2, 0xF4))
            {
                goto FirstByteInvalid;
            }

            tempValue = (tempValue - 0xC2) << 6;

            // Try reading input[1].

            index++;
            if ((uint)index >= (uint)source.Length)
            {
                goto NeedsMoreData;
            }

            // Continuation bytes are of the form [10xxxxxx], which means that their two's
            // complement representation is in the range [-65..-128]. This allows us to
            // perform a single comparison to see if a byte is a continuation byte.

            int thisByteSignExtended = (sbyte)source[index];
            if (thisByteSignExtended >= -64)
            {
                goto Invalid;
            }

            tempValue += (uint)thisByteSignExtended;
            tempValue += 0x80; // remove the continuation byte marker
            tempValue += (0xC2 - 0xC0) << 6; // remove the leading byte marker

            if (tempValue < 0x0800)
            {
                Debug.Assert(UnicodeUtility.IsInRangeInclusive(tempValue, 0x0080, 0x07FF));
                goto Finish; // this is a valid 2-byte sequence
            }

            // This appears to be a 3- or 4-byte sequence. Since per Table 3-7 we now have
            // enough information (from just two code units) to detect overlong or surrogate
            // sequences, we need to perform these checks now.

            if (!UnicodeUtility.IsInRangeInclusive(tempValue, ((0xE0 - 0xC0) << 6) + (0xA0 - 0x80), ((0xF4 - 0xC0) << 6) + (0x8F - 0x80)))
            {
                // The first two bytes were not in the range [[E0 A0]..[F4 8F]].
                // This is an overlong 3-byte sequence or an out-of-range 4-byte sequence.
                goto Invalid;
            }

            if (UnicodeUtility.IsInRangeInclusive(tempValue, ((0xED - 0xC0) << 6) + (0xA0 - 0x80), ((0xED - 0xC0) << 6) + (0xBF - 0x80)))
            {
                // This is a UTF-16 surrogate code point, which is invalid in UTF-8.
                goto Invalid;
            }

            if (UnicodeUtility.IsInRangeInclusive(tempValue, ((0xF0 - 0xC0) << 6) + (0x80 - 0x80), ((0xF0 - 0xC0) << 6) + (0x8F - 0x80)))
            {
                // This is an overlong 4-byte sequence.
                goto Invalid;
            }

            // The first two bytes were just fine. We don't need to perform any other checks
            // on the remaining bytes other than to see that they're valid continuation bytes.

            // Try reading input[2].

            index++;
            if ((uint)index >= (uint)source.Length)
            {
                goto NeedsMoreData;
            }

            thisByteSignExtended = (sbyte)source[index];
            if (thisByteSignExtended >= -64)
            {
                goto Invalid; // this byte is not a UTF-8 continuation byte
            }

            tempValue <<= 6;
            tempValue += (uint)thisByteSignExtended;
            tempValue += 0x80; // remove the continuation byte marker
            tempValue -= (0xE0 - 0xC0) << 12; // remove the leading byte marker

            if (tempValue <= 0xFFFF)
            {
                Debug.Assert(UnicodeUtility.IsInRangeInclusive(tempValue, 0x0800, 0xFFFF));
                goto Finish; // this is a valid 3-byte sequence
            }

            // Try reading input[3].

            index++;
            if ((uint)index >= (uint)source.Length)
            {
                goto NeedsMoreData;
            }

            thisByteSignExtended = (sbyte)source[index];
            if (thisByteSignExtended >= -64)
            {
                goto Invalid; // this byte is not a UTF-8 continuation byte
            }

            tempValue <<= 6;
            tempValue += (uint)thisByteSignExtended;
            tempValue += 0x80; // remove the continuation byte marker
            tempValue -= (0xF0 - 0xE0) << 18; // remove the leading byte marker

            UnicodeDebug.AssertIsValidSupplementaryPlaneScalar(tempValue);
            goto Finish; // this is a valid 4-byte sequence

        FirstByteInvalid:

            index = 1; // Invalid subsequences are always at least length 1.

        Invalid:

            Debug.Assert(1 <= index && index <= 3); // Invalid subsequences are always length 1..3
            bytesConsumed = index;
            result = ReplacementChar;
            return OperationStatus.InvalidData;

        NeedsMoreData:

            Debug.Assert(0 <= index && index <= 3); // Incomplete subsequences are always length 0..3
            bytesConsumed = index;
            result = ReplacementChar;
            return OperationStatus.NeedMoreData;
        }

        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Rune other) && Equals(other);

        public bool Equals(Rune other) => this == other;

        public override int GetHashCode() => Value;

        /// <summary>
        /// Attempts to create a <see cref="Rune"/> from the provided input value.
        /// </summary>
        public static bool TryCreate(char ch, out Rune result)
        {
            uint extendedValue = ch;
            if (!UnicodeUtility.IsSurrogateCodePoint(extendedValue))
            {
                result = UnsafeCreate(extendedValue);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to create a <see cref="Rune"/> from the provided UTF-16 surrogate pair.
        /// Returns <see langword="false"/> if the input values don't represent a well-formed UTF-16surrogate pair.
        /// </summary>
        public static bool TryCreate(char highSurrogate, char lowSurrogate, out Rune result)
        {
            // First, extend both to 32 bits, then calculate the offset of
            // each candidate surrogate char from the start of its range.

            uint highSurrogateOffset = (uint)highSurrogate - HighSurrogateStart;
            uint lowSurrogateOffset = (uint)lowSurrogate - LowSurrogateStart;

            // This is a single comparison which allows us to check both for validity at once since
            // both the high surrogate range and the low surrogate range are the same length.
            // If the comparison fails, we call to a helper method to throw the correct exception message.

            if ((highSurrogateOffset | lowSurrogateOffset) <= HighSurrogateRange)
            {
                // The 0x40u << 10 below is to account for uuuuu = wwww + 1 in the surrogate encoding.
                result = UnsafeCreate((highSurrogateOffset << 10) + ((uint)lowSurrogate - LowSurrogateStart) + (0x40u << 10));
                return true;
            }
            else
            {
                // Didn't have a high surrogate followed by a low surrogate.
                result = default;
                return false;
            }
        }

        /// <summary>
        /// Encodes this <see cref="Rune"/> to a UTF-16 destination buffer.
        /// </summary>
        /// <param name="destination">The buffer to which to write this value as UTF-16.</param>
        /// <param name="charsWritten">
        /// The number of <see cref="char"/>s written to <paramref name="destination"/>,
        /// or 0 if the destination buffer is not large enough to contain the output.</param>
        /// <returns>True if the value was written to the buffer; otherwise, false.</returns>
        public bool TryEncodeToUtf16(Span<char> destination, out int charsWritten)
        {
            if (destination.Length >= 1)
            {
                if (IsBmp)
                {
                    destination[0] = (char)_value;
                    charsWritten = 1;
                    return true;
                }
                else if (destination.Length >= 2)
                {
                    UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(_value, out destination[0], out destination[1]);
                    charsWritten = 2;
                    return true;
                }
            }

            // Destination buffer not large enough

            charsWritten = default;
            return false;
        }

        /// <summary>
        /// Encodes this <see cref="Rune"/> to a destination buffer as UTF-8 bytes.
        /// </summary>
        /// <param name="destination">The buffer to which to write this value as UTF-8.</param>
        /// <param name="bytesWritten">
        /// The number of <see cref="byte"/>s written to <paramref name="destination"/>,
        /// or 0 if the destination buffer is not large enough to contain the output.</param>
        /// <returns>True if the value was written to the buffer; otherwise, false.</returns>
        public bool TryEncodeToUtf8(Span<byte> destination, out int bytesWritten)
        {
            // The bit patterns below come from the Unicode Standard, Table 3-6.

            if (destination.Length >= 1)
            {
                if (IsAscii)
                {
                    destination[0] = (byte)_value;
                    bytesWritten = 1;
                    return true;
                }

                if (destination.Length >= 2)
                {
                    if (_value <= 0x7FFu)
                    {
                        // Scalar 00000yyy yyxxxxxx -> bytes [ 110yyyyy 10xxxxxx ]
                        destination[0] = (byte)((_value + (0b110u << 11)) >> 6);
                        destination[1] = (byte)((_value & 0x3Fu) + 0x80u);
                        bytesWritten = 2;
                        return true;
                    }

                    if (destination.Length >= 3)
                    {
                        if (_value <= 0xFFFFu)
                        {
                            // Scalar zzzzyyyy yyxxxxxx -> bytes [ 1110zzzz 10yyyyyy 10xxxxxx ]
                            destination[0] = (byte)((_value + (0b1110 << 16)) >> 12);
                            destination[1] = (byte)(((_value & (0x3Fu << 6)) >> 6) + 0x80u);
                            destination[2] = (byte)((_value & 0x3Fu) + 0x80u);
                            bytesWritten = 3;
                            return true;
                        }

                        if (destination.Length >= 4)
                        {
                            // Scalar 000uuuuu zzzzyyyy yyxxxxxx -> bytes [ 11110uuu 10uuzzzz 10yyyyyy 10xxxxxx ]
                            destination[0] = (byte)((_value + (0b11110 << 21)) >> 18);
                            destination[1] = (byte)(((_value & (0x3Fu << 12)) >> 12) + 0x80u);
                            destination[2] = (byte)(((_value & (0x3Fu << 6)) >> 6) + 0x80u);
                            destination[3] = (byte)((_value & 0x3Fu) + 0x80u);
                            bytesWritten = 4;
                            return true;
                        }
                    }
                }
            }

            // Destination buffer not large enough

            bytesWritten = default;
            return false;
        }

        /// <summary>
        /// Creates a <see cref="Rune"/> without performing validation on the input.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Rune UnsafeCreate(uint scalarValue) => new Rune(scalarValue, false);
    }
}
