// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace System.Text.Json
{
    // TODO: Replace the escaping logic with publicly shipping APIs from https://github.com/dotnet/corefx/issues/33509
    internal static partial class JsonWriterHelper
    {
        // Only allow ASCII characters between ' ' (0x20) and '~' (0x7E), inclusively,
        // but exclude characters that need to be escaped as hex: '"', '\'', '&', '+', '<', '>', '`'
        // and exclude characters that need to be escaped by adding a backslash: '\n', '\r', '\t', '\\', '\b', '\f'
        //
        // non-zero = allowed, 0 = disallowed
        public const int LastAsciiCharacter = 0x7F;
        private static ReadOnlySpan<byte> AllowList => new byte[LastAsciiCharacter + 1] {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0000..U+000F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0010..U+001F
            1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, // U+0020..U+002F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, // U+0030..U+003F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0040..U+004F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, // U+0050..U+005F
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0060..U+006F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // U+0070..U+007F
        };

        private const string HexFormatString = "X4";
        private static readonly StandardFormat s_hexStandardFormat = new StandardFormat('X', 4);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscapingNoBoundsCheck(byte value)
        {
            Debug.Assert(value <= LastAsciiCharacter);
            return AllowList[value] == 0;
        }

        private static bool NeedsEscapingNoBoundsCheck(char value)
        {
            Debug.Assert(value <= LastAsciiCharacter);
            return AllowList[value] == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscaping(byte value) => value > LastAsciiCharacter || AllowList[value] == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscaping(char value) => value > LastAsciiCharacter || AllowList[value] == 0;

        public static int NeedsEscaping(ReadOnlySpan<byte> value, JavaScriptEncoder encoder)
        {
            if (encoder != null)
            {
                return encoder.FindFirstCharacterToEncodeUtf8(value);
            }

            int idx;
            for (idx = 0; idx < value.Length; idx++)
            {
                if (NeedsEscaping(value[idx]))
                {
                    goto Return;
                }
            }

            idx = -1; // all characters allowed

        Return:
            return idx;
        }

        public static int NeedsEscaping(ReadOnlySpan<char> value, JavaScriptEncoder encoder)
        {
            if (encoder != null)
            {
                return encoder.FindFirstCharacterToEncodeUtf8(MemoryMarshal.Cast<char, byte>(value));
            }

            int idx;
            for (idx = 0; idx < value.Length; idx++)
            {
                if (NeedsEscaping(value[idx]))
                {
                    goto Return;
                }
            }

            idx = -1; // all characters allowed

        Return:
            return idx;
        }

        public static int GetMaxEscapedLength(int textLength, int firstIndexToEscape)
        {
            Debug.Assert(textLength > 0);
            Debug.Assert(firstIndexToEscape >= 0 && firstIndexToEscape < textLength);
            return firstIndexToEscape + JsonConstants.MaxExpansionFactorWhileEscaping * (textLength - firstIndexToEscape);
        }

        private static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, JavaScriptEncoder encoder, ref int written)
        {
            Debug.Assert(encoder != null);

            OperationStatus result = encoder.EncodeUtf8(value, destination, out int encoderBytesConsumed, out int encoderBytesWritten);

            Debug.Assert(result != OperationStatus.DestinationTooSmall);
            Debug.Assert(result != OperationStatus.NeedMoreData);
            Debug.Assert(encoderBytesConsumed == value.Length);

            if (result != OperationStatus.Done)
            {
                ThrowHelper.ThrowArgumentException_InvalidUTF8(value.Slice(encoderBytesWritten));
            }

            written += encoderBytesWritten;
        }

        public static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder encoder, out int written)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            written = indexOfFirstByteToEscape;
            int consumed = indexOfFirstByteToEscape;

            if (encoder != null)
            {
                destination = destination.Slice(indexOfFirstByteToEscape);
                value = value.Slice(indexOfFirstByteToEscape);
                EscapeString(value, destination, encoder, ref written);
            }
            else
            {
                // For performance when no encoder is specified, perform escaping here for Ascii and on the
                // first occurrence of a non-Ascii character, then call into the default encoder.
                while (consumed < value.Length)
                {
                    byte val = value[consumed];
                    if (IsAsciiValue(val))
                    {
                        if (NeedsEscapingNoBoundsCheck(val))
                        {
                            EscapeNextBytes(val, destination, ref written);
                            consumed++;
                        }
                        else
                        {
                            destination[written] = val;
                            written++;
                            consumed++;
                        }
                    }
                    else
                    {
                        // Fall back to default encoder.
                        destination = destination.Slice(written);
                        value = value.Slice(consumed);
                        EscapeString(value, destination, JavaScriptEncoder.Default, ref written);
                        break;
                    }
                }
            }
        }

        private static void EscapeNextBytes(byte value, Span<byte> destination, ref int written)
        {
            destination[written++] = (byte)'\\';
            switch (value)
            {
                case JsonConstants.Quote:
                    // Optimize for the common quote case.
                    destination[written++] = (byte)'u';
                    destination[written++] = (byte)'0';
                    destination[written++] = (byte)'0';
                    destination[written++] = (byte)'2';
                    destination[written++] = (byte)'2';
                    break;
                case JsonConstants.LineFeed:
                    destination[written++] = (byte)'n';
                    break;
                case JsonConstants.CarriageReturn:
                    destination[written++] = (byte)'r';
                    break;
                case JsonConstants.Tab:
                    destination[written++] = (byte)'t';
                    break;
                case JsonConstants.BackSlash:
                    destination[written++] = (byte)'\\';
                    break;
                case JsonConstants.BackSpace:
                    destination[written++] = (byte)'b';
                    break;
                case JsonConstants.FormFeed:
                    destination[written++] = (byte)'f';
                    break;
                default:
                    destination[written++] = (byte)'u';

                    bool result = Utf8Formatter.TryFormat(value, destination.Slice(written), out int bytesWritten, format: s_hexStandardFormat);
                    Debug.Assert(result);
                    Debug.Assert(bytesWritten == 4);
                    written += bytesWritten;
                    break;
            }
        }

        private static bool IsAsciiValue(byte value) => value <= LastAsciiCharacter;

        private static bool IsAsciiValue(char value) => value <= LastAsciiCharacter;

        /// <summary>
        /// Returns <see langword="true"/> if <paramref name="value"/> is a UTF-8 continuation byte.
        /// A UTF-8 continuation byte is a byte whose value is in the range 0x80-0xBF, inclusive.
        /// </summary>
        private static bool IsUtf8ContinuationByte(byte value) => (value & 0xC0) == 0x80;

        /// <summary>
        /// Returns <see langword="true"/> if the low word of <paramref name="char"/> is a UTF-16 surrogate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLowWordSurrogate(uint @char)
            => (@char & 0xF800U) == 0xD800U;

        // We can't use the type Rune since it is not available on netstandard2.0
        // To avoid extensive ifdefs and for simplicity, just using an int to reprepsent the scalar value, instead.
        public static SequenceValidity PeekFirstSequence(ReadOnlySpan<byte> data, out int numBytesConsumed, out int rune)
        {
            // This method is implemented to match the behavior of System.Text.Encoding.UTF8 in terms of
            // how many bytes it consumes when reporting invalid sequences. The behavior is as follows:
            //
            // - Some bytes are *always* invalid (ranges [ C0..C1 ] and [ F5..FF ]), and when these
            //   are encountered it's an invalid sequence of length 1.
            //
            // - Multi-byte sequences which are overlong are reported as an invalid sequence of length 2,
            //   since per the Unicode Standard Table 3-7 it's always possible to tell these by the second byte.
            //   Exception: Sequences which begin with [ C0..C1 ] are covered by the above case, thus length 1.
            //
            // - Multi-byte sequences which are improperly terminated (no continuation byte when one is
            //   expected) are reported as invalid sequences up to and including the last seen continuation byte.

            Debug.Assert(JsonHelpers.IsValidUnicodeScalar(ReplacementChar));
            rune = ReplacementChar;

            if (data.IsEmpty)
            {
                // No data to peek at
                numBytesConsumed = 0;
                return SequenceValidity.Empty;
            }

            byte firstByte = data[0];

            if (IsAsciiValue(firstByte))
            {
                // ASCII byte = well-formed one-byte sequence.
                Debug.Assert(JsonHelpers.IsValidUnicodeScalar(firstByte));
                rune = firstByte;
                numBytesConsumed = 1;
                return SequenceValidity.WellFormed;
            }

            if (!JsonHelpers.IsInRangeInclusive(firstByte, (byte)0xC2U, (byte)0xF4U))
            {
                // Standalone continuation byte or "always invalid" byte = ill-formed one-byte sequence.
                goto InvalidOneByteSequence;
            }

            // At this point, we know we're working with a multi-byte sequence,
            // and we know that at least the first byte is potentially valid.

            if (data.Length < 2)
            {
                // One byte of an incomplete multi-byte sequence.
                goto OneByteOfIncompleteMultiByteSequence;
            }

            byte secondByte = data[1];

            if (!IsUtf8ContinuationByte(secondByte))
            {
                // One byte of an improperly terminated multi-byte sequence.
                goto InvalidOneByteSequence;
            }

            if (firstByte < (byte)0xE0U)
            {
                // Well-formed two-byte sequence.
                uint scalar = (((uint)firstByte & 0x1FU) << 6) | ((uint)secondByte & 0x3FU);
                Debug.Assert(JsonHelpers.IsValidUnicodeScalar(scalar));
                rune = (int)scalar;
                numBytesConsumed = 2;
                return SequenceValidity.WellFormed;
            }

            if (firstByte < (byte)0xF0U)
            {
                // Start of a three-byte sequence.
                // Need to check for overlong or surrogate sequences.

                uint scalar = (((uint)firstByte & 0x0FU) << 12) | (((uint)secondByte & 0x3FU) << 6);
                if (scalar < 0x800U || IsLowWordSurrogate(scalar))
                {
                    goto OverlongOutOfRangeOrSurrogateSequence;
                }

                // At this point, we have a valid two-byte start of a three-byte sequence.

                if (data.Length < 3)
                {
                    // Two bytes of an incomplete three-byte sequence.
                    goto TwoBytesOfIncompleteMultiByteSequence;
                }
                else
                {
                    byte thirdByte = data[2];
                    if (IsUtf8ContinuationByte(thirdByte))
                    {
                        // Well-formed three-byte sequence.
                        scalar |= (uint)thirdByte & 0x3FU;
                        Debug.Assert(JsonHelpers.IsValidUnicodeScalar(scalar));
                        rune = (int)scalar;
                        numBytesConsumed = 3;
                        return SequenceValidity.WellFormed;
                    }
                    else
                    {
                        // Two bytes of improperly terminated multi-byte sequence.
                        goto InvalidTwoByteSequence;
                    }
                }
            }

            {
                // Start of four-byte sequence.
                // Need to check for overlong or out-of-range sequences.

                uint scalar = (((uint)firstByte & 0x07U) << 18) | (((uint)secondByte & 0x3FU) << 12);
                Debug.Assert(JsonHelpers.IsValidUnicodeScalar(scalar));
                if (!JsonHelpers.IsInRangeInclusive(scalar, 0x10000U, 0x10FFFFU))
                {
                    goto OverlongOutOfRangeOrSurrogateSequence;
                }

                // At this point, we have a valid two-byte start of a four-byte sequence.

                if (data.Length < 3)
                {
                    // Two bytes of an incomplete four-byte sequence.
                    goto TwoBytesOfIncompleteMultiByteSequence;
                }
                else
                {
                    byte thirdByte = data[2];
                    if (IsUtf8ContinuationByte(thirdByte))
                    {
                        // Valid three-byte start of a four-byte sequence.

                        if (data.Length < 4)
                        {
                            // Three bytes of an incomplete four-byte sequence.
                            goto ThreeBytesOfIncompleteMultiByteSequence;
                        }
                        else
                        {
                            byte fourthByte = data[3];
                            if (IsUtf8ContinuationByte(fourthByte))
                            {
                                // Well-formed four-byte sequence.
                                scalar |= (((uint)thirdByte & 0x3FU) << 6) | ((uint)fourthByte & 0x3FU);
                                Debug.Assert(JsonHelpers.IsValidUnicodeScalar(scalar));
                                rune = (int)scalar;
                                numBytesConsumed = 4;
                                return SequenceValidity.WellFormed;
                            }
                            else
                            {
                                // Three bytes of an improperly terminated multi-byte sequence.
                                goto InvalidThreeByteSequence;
                            }
                        }
                    }
                    else
                    {
                        // Two bytes of improperly terminated multi-byte sequence.
                        goto InvalidTwoByteSequence;
                    }
                }
            }

        // Everything below here is error handling.

        InvalidOneByteSequence:
            numBytesConsumed = 1;
            return SequenceValidity.Invalid;

        InvalidTwoByteSequence:
        OverlongOutOfRangeOrSurrogateSequence:
            numBytesConsumed = 2;
            return SequenceValidity.Invalid;

        InvalidThreeByteSequence:
            numBytesConsumed = 3;
            return SequenceValidity.Invalid;

        OneByteOfIncompleteMultiByteSequence:
            numBytesConsumed = 1;
            return SequenceValidity.Incomplete;

        TwoBytesOfIncompleteMultiByteSequence:
            numBytesConsumed = 2;
            return SequenceValidity.Incomplete;

        ThreeBytesOfIncompleteMultiByteSequence:
            numBytesConsumed = 3;
            return SequenceValidity.Incomplete;
        }

        private static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, JavaScriptEncoder encoder, ref int written)
        {
            // todo: issue #39523: add an Encode(ReadOnlySpan<char>) decode API to System.Text.Encodings.Web.TextEncoding to avoid utf16->utf8->utf16 conversion.

            Debug.Assert(encoder != null);

            // Convert char to byte.
            byte[] utf8DestinationArray = null;
            Span<byte> utf8Destination;
            int length = checked((value.Length) * JsonConstants.MaxExpansionFactorWhileTranscoding);
            if (length > JsonConstants.StackallocThreshold)
            {
                utf8DestinationArray = ArrayPool<byte>.Shared.Rent(length);
                utf8Destination = utf8DestinationArray;
            }
            else
            {
                unsafe
                {
                    byte* ptr = stackalloc byte[JsonConstants.StackallocThreshold];
                    utf8Destination = new Span<byte>(ptr, JsonConstants.StackallocThreshold);
                }
            }

            ReadOnlySpan<byte> utf16Value = MemoryMarshal.AsBytes(value);
            OperationStatus toUtf8Status = ToUtf8(utf16Value, utf8Destination, out int bytesConsumed, out int bytesWritten);

            Debug.Assert(toUtf8Status != OperationStatus.DestinationTooSmall);
            Debug.Assert(toUtf8Status != OperationStatus.NeedMoreData);

            if (toUtf8Status != OperationStatus.Done)
            {
                if (utf8DestinationArray != null)
                {
                    utf8Destination.Slice(0, bytesWritten).Clear();
                    ArrayPool<byte>.Shared.Return(utf8DestinationArray);
                }

                ThrowHelper.ThrowArgumentException_InvalidUTF8(utf16Value.Slice(bytesWritten));
            }

            Debug.Assert(toUtf8Status == OperationStatus.Done);
            Debug.Assert(bytesConsumed == utf16Value.Length);

            // Escape the bytes.
            byte[] utf8ConvertedDestinationArray = null;
            Span<byte> utf8ConvertedDestination;
            length = checked(bytesWritten * JsonConstants.MaxExpansionFactorWhileEscaping);
            if (length > JsonConstants.StackallocThreshold)
            {
                utf8ConvertedDestinationArray = ArrayPool<byte>.Shared.Rent(length);
                utf8ConvertedDestination = utf8ConvertedDestinationArray;
            }
            else
            {
                unsafe
                {
                    byte* ptr = stackalloc byte[JsonConstants.StackallocThreshold];
                    utf8ConvertedDestination = new Span<byte>(ptr, JsonConstants.StackallocThreshold);
                }
            }

            EscapeString(utf8Destination.Slice(0, bytesWritten), utf8ConvertedDestination, indexOfFirstByteToEscape: 0, encoder, out int convertedBytesWritten);

            if (utf8DestinationArray != null)
            {
                utf8Destination.Slice(0, bytesWritten).Clear();
                ArrayPool<byte>.Shared.Return(utf8DestinationArray);
            }

            // Convert byte to char.
#if BUILDING_INBOX_LIBRARY
            OperationStatus toUtf16Status = Utf8.ToUtf16(utf8ConvertedDestination.Slice(0, convertedBytesWritten), destination, out int bytesRead, out int charsWritten);
            Debug.Assert(toUtf16Status == OperationStatus.Done);
            Debug.Assert(bytesRead == convertedBytesWritten);
#else
            string utf16 = JsonReaderHelper.GetTextFromUtf8(utf8ConvertedDestination.Slice(0, convertedBytesWritten));
            utf16.AsSpan().CopyTo(destination);
            int charsWritten = utf16.Length;
#endif
            written += charsWritten;

            if (utf8ConvertedDestinationArray != null)
            {
                utf8ConvertedDestination.Slice(0, written).Clear();
                ArrayPool<byte>.Shared.Return(utf8ConvertedDestinationArray);
            }
        }

        public static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder encoder, out int written)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            written = indexOfFirstByteToEscape;
            int consumed = indexOfFirstByteToEscape;

            if (encoder != null)
            {
                destination = destination.Slice(indexOfFirstByteToEscape);
                value = value.Slice(indexOfFirstByteToEscape);
                EscapeString(value, destination, encoder, ref written);
            }
            else
            {
                // For performance when no encoder is specified, perform escaping here for Ascii and on the
                // first occurrence of a non-Ascii character, then call into the default encoder.
                while (consumed < value.Length)
                {
                    char val = value[consumed];
                    if (IsAsciiValue(val))
                    {
                        if (NeedsEscapingNoBoundsCheck(val))
                        {
                            EscapeNextChars(val, destination, ref written);
                            consumed++;
                        }
                        else
                        {
                            destination[written] = val;
                            written++;
                            consumed++;
                        }
                    }
                    else
                    {
                        // Fall back to default encoder.
                        destination = destination.Slice(written);
                        value = value.Slice(consumed);
                        EscapeString(value, destination, JavaScriptEncoder.Default, ref written);
                        break;
                    }
                }
            }
        }

        private static void EscapeNextChars(char value, Span<char> destination, ref int written)
        {
            Debug.Assert(IsAsciiValue(value));

            destination[written++] = '\\';
            switch ((byte)value)
            {
                case JsonConstants.Quote:
                    // Optimize for the common quote case.
                    destination[written++] = 'u';
                    destination[written++] = '0';
                    destination[written++] = '0';
                    destination[written++] = '2';
                    destination[written++] = '2';
                    break;
                case JsonConstants.LineFeed:
                    destination[written++] = 'n';
                    break;
                case JsonConstants.CarriageReturn:
                    destination[written++] = 'r';
                    break;
                case JsonConstants.Tab:
                    destination[written++] = 't';
                    break;
                case JsonConstants.BackSlash:
                    destination[written++] = '\\';
                    break;
                case JsonConstants.BackSpace:
                    destination[written++] = 'b';
                    break;
                case JsonConstants.FormFeed:
                    destination[written++] = 'f';
                    break;
                default:
                    destination[written++] = 'u';
#if BUILDING_INBOX_LIBRARY
                    int intChar = value;
                    intChar.TryFormat(destination.Slice(written), out int charsWritten, HexFormatString);
                    Debug.Assert(charsWritten == 4);
                    written += charsWritten;
#else
                    written = WriteHex(value, destination, written);
#endif
                    break;
            }
        }

        /// <summary>
        /// A scalar that represents the Unicode replacement character U+FFFD.
        /// </summary>
        private const int ReplacementChar = 0xFFFD;

#if !BUILDING_INBOX_LIBRARY
        private static int WriteHex(int value, Span<char> destination, int written)
        {
            destination[written++] = (char)Int32LsbToHexDigit(value >> 12);
            destination[written++] = (char)Int32LsbToHexDigit((int)((value >> 8) & 0xFU));
            destination[written++] = (char)Int32LsbToHexDigit((int)((value >> 4) & 0xFU));
            destination[written++] = (char)Int32LsbToHexDigit((int)(value & 0xFU));
            return written;
        }

        /// <summary>
        /// Converts a number 0 - 15 to its associated hex character '0' - 'f' as byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Int32LsbToHexDigit(int value)
        {
            Debug.Assert(value < 16);
            return (byte)((value < 10) ? ('0' + value) : ('a' + (value - 10)));
        }
#endif
    }
}
