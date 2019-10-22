// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;

#if BUILDING_INBOX_LIBRARY
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

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
        private static ReadOnlySpan<byte> AllowList => new byte[byte.MaxValue + 1]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0000..U+000F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0010..U+001F
            1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, // U+0020..U+002F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, // U+0030..U+003F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0040..U+004F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, // U+0050..U+005F
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0060..U+006F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // U+0070..U+007F

            // Also include the ranges from U+0080 to U+00FF for performance to avoid UTF8 code from checking boundary.
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+00F0..U+00FF
        };

#if BUILDING_INBOX_LIBRARY
        private const string HexFormatString = "X4";
#endif

        private static readonly StandardFormat s_hexStandardFormat = new StandardFormat('X', 4);

        private static bool NeedsEscaping(byte value) => AllowList[value] == 0;

        private static bool NeedsEscapingNoBoundsCheck(char value) => AllowList[value] == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool NeedsEscaping(char value) => value > LastAsciiCharacter || AllowList[value] == 0;

#if BUILDING_INBOX_LIBRARY
        private static readonly Vector128<short> s_mask_UInt16_0x20 = Vector128.Create((short)0x20); // Space ' '

        private static readonly Vector128<short> s_mask_UInt16_0x22 = Vector128.Create((short)0x22); // Quotation Mark '"'
        private static readonly Vector128<short> s_mask_UInt16_0x26 = Vector128.Create((short)0x26); // Ampersand '&'
        private static readonly Vector128<short> s_mask_UInt16_0x27 = Vector128.Create((short)0x27); // Apostrophe '''
        private static readonly Vector128<short> s_mask_UInt16_0x2B = Vector128.Create((short)0x2B); // Plus sign '+'
        private static readonly Vector128<short> s_mask_UInt16_0x3C = Vector128.Create((short)0x3C); // Less Than Sign '<'
        private static readonly Vector128<short> s_mask_UInt16_0x3E = Vector128.Create((short)0x3E); // Greater Than Sign '>'
        private static readonly Vector128<short> s_mask_UInt16_0x5C = Vector128.Create((short)0x5C); // Reverse Solidus '\'
        private static readonly Vector128<short> s_mask_UInt16_0x60 = Vector128.Create((short)0x60); // Grave Access '`'

        private static readonly Vector128<short> s_mask_UInt16_0x7E = Vector128.Create((short)0x7E); // Tilde '~'

        private static readonly Vector128<sbyte> s_mask_SByte_0x20 = Vector128.Create((sbyte)0x20); // Space ' '

        private static readonly Vector128<sbyte> s_mask_SByte_0x22 = Vector128.Create((sbyte)0x22); // Quotation Mark '"'
        private static readonly Vector128<sbyte> s_mask_SByte_0x26 = Vector128.Create((sbyte)0x26); // Ampersand '&'
        private static readonly Vector128<sbyte> s_mask_SByte_0x27 = Vector128.Create((sbyte)0x27); // Apostrophe '''
        private static readonly Vector128<sbyte> s_mask_SByte_0x2B = Vector128.Create((sbyte)0x2B); // Plus sign '+'
        private static readonly Vector128<sbyte> s_mask_SByte_0x3C = Vector128.Create((sbyte)0x3C); // Less Than Sign '<'
        private static readonly Vector128<sbyte> s_mask_SByte_0x3E = Vector128.Create((sbyte)0x3E); // Greater Than Sign '>'
        private static readonly Vector128<sbyte> s_mask_SByte_0x5C = Vector128.Create((sbyte)0x5C); // Reverse Solidus '\'
        private static readonly Vector128<sbyte> s_mask_SByte_0x60 = Vector128.Create((sbyte)0x60); // Grave Access '`'

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<short> CreateEscapingMask(Vector128<short> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<short> mask = Sse2.CompareLessThan(sourceValue, s_mask_UInt16_0x20); // Space ' ', anything in the control characters range

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x22)); // Quotation Mark '"'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x26)); // Ampersand '&'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x27)); // Apostrophe '''
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x2B)); // Plus sign '+'

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x3C)); // Less Than Sign '<'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x3E)); // Greater Than Sign '>'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x5C)); // Reverse Solidus '\'
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_UInt16_0x60)); // Grave Access '`'

            mask = Sse2.Or(mask, Sse2.CompareGreaterThan(sourceValue, s_mask_UInt16_0x7E)); // Tilde '~', anything above the ASCII range

            return mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector128<sbyte> CreateEscapingMask(Vector128<sbyte> sourceValue)
        {
            Debug.Assert(Sse2.IsSupported);

            Vector128<sbyte> mask = Sse2.CompareLessThan(sourceValue, s_mask_SByte_0x20); // Control characters, and anything above 0x7E since sbyte.MaxValue is 0x7E

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x22)); // Quotation Mark "
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x26)); // Ampersand &
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x27)); // Apostrophe '
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x2B)); // Plus sign +

            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x3C)); // Less Than Sign <
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x3E)); // Greater Than Sign >
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x5C)); // Reverse Solidus \
            mask = Sse2.Or(mask, Sse2.CompareEqual(sourceValue, s_mask_SByte_0x60)); // Grave Access `

            return mask;
        }
#endif

        public static unsafe int NeedsEscaping(ReadOnlySpan<byte> value, JavaScriptEncoder encoder)
        {
            fixed (byte* ptr = value)
            {
                int idx = 0;

                if (encoder != null)
                {
                    idx = encoder.FindFirstCharacterToEncodeUtf8(value);
                    goto Return;
                }

#if BUILDING_INBOX_LIBRARY
                if (Sse2.IsSupported)
                {
                    sbyte* startingAddress = (sbyte*)ptr;
                    while (value.Length - 16 >= idx)
                    {
                        Debug.Assert(startingAddress >= ptr && startingAddress <= (ptr + value.Length - 16));

                        // Load the next 16 bytes.
                        Vector128<sbyte> sourceValue = Sse2.LoadVector128(startingAddress);

                        // Check if any of the 16 bytes need to be escaped.
                        Vector128<sbyte> mask = CreateEscapingMask(sourceValue);

                        int index = Sse2.MoveMask(mask.AsByte());
                        // If index == 0, that means none of the 16 bytes needed to be escaped.
                        // TrailingZeroCount is relatively expensive, avoid it if possible.
                        if (index != 0)
                        {
                            // Found at least one byte that needs to be escaped, figure out the index of
                            // the first one found that needed to be escaped within the 16 bytes.
                            Debug.Assert(index > 0 && index <= 65_535);
                            int tzc = BitOperations.TrailingZeroCount(index);
                            Debug.Assert(tzc >= 0 && tzc <= 16);
                            idx += tzc;
                            goto Return;
                        }
                        idx += 16;
                        startingAddress += 16;
                    }

                    // Process the remaining characters.
                    Debug.Assert(value.Length - idx < 16);
                }
#endif

                for (; idx < value.Length; idx++)
                {
                    Debug.Assert((ptr + idx) <= (ptr + value.Length));
                    if (NeedsEscaping(*(ptr + idx)))
                    {
                        goto Return;
                    }
                }

                idx = -1; // all characters allowed

            Return:
                return idx;
            }
        }

        public static unsafe int NeedsEscaping(ReadOnlySpan<char> value, JavaScriptEncoder encoder)
        {
            fixed (char* ptr = value)
            {
                int idx = 0;

                // Some implementations of JavascriptEncoder.FindFirstCharacterToEncode may not accept
                // null pointers and gaurd against that. Hence, check up-front and fall down to return -1.
                if (encoder != null && !value.IsEmpty)
                {
                    idx = encoder.FindFirstCharacterToEncode(ptr, value.Length);
                    goto Return;
                }

#if BUILDING_INBOX_LIBRARY
                if (Sse2.IsSupported)
                {
                    short* startingAddress = (short*)ptr;
                    while (value.Length - 8 >= idx)
                    {
                        Debug.Assert(startingAddress >= ptr && startingAddress <= (ptr + value.Length - 8));

                        // Load the next 8 characters.
                        Vector128<short> sourceValue = Sse2.LoadVector128(startingAddress);

                        // Check if any of the 8 characters need to be escaped.
                        Vector128<short> mask = CreateEscapingMask(sourceValue);

                        int index = Sse2.MoveMask(mask.AsByte());
                        // If index == 0, that means none of the 8 characters needed to be escaped.
                        // TrailingZeroCount is relatively expensive, avoid it if possible.
                        if (index != 0)
                        {
                            // Found at least one character that needs to be escaped, figure out the index of
                            // the first one found that needed to be escaped within the 8 characters.
                            Debug.Assert(index > 0 && index <= 65_535);
                            int tzc = BitOperations.TrailingZeroCount(index);
                            Debug.Assert(tzc % 2 == 0 && tzc >= 0 && tzc <= 16);
                            idx += tzc >> 1;
                            goto Return;
                        }
                        idx += 8;
                        startingAddress += 8;
                    }

                    // Process the remaining characters.
                    Debug.Assert(value.Length - idx < 8);
                }
#endif

                for (; idx < value.Length; idx++)
                {
                    Debug.Assert((ptr + idx) <= (ptr + value.Length));
                    if (NeedsEscaping(*(ptr + idx)))
                    {
                        goto Return;
                    }
                }

                idx = -1; // All characters are allowed.

            Return:
                return idx;
            }
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

            if (result != OperationStatus.Done)
            {
                ThrowHelper.ThrowArgumentException_InvalidUTF8(value.Slice(encoderBytesWritten));
            }

            Debug.Assert(encoderBytesConsumed == value.Length);

            written += encoderBytesWritten;
        }

        public static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder encoder, out int written)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            written = indexOfFirstByteToEscape;

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
                while (indexOfFirstByteToEscape < value.Length)
                {
                    byte val = value[indexOfFirstByteToEscape];
                    if (IsAsciiValue(val))
                    {
                        if (NeedsEscaping(val))
                        {
                            EscapeNextBytes(val, destination, ref written);
                            indexOfFirstByteToEscape++;
                        }
                        else
                        {
                            destination[written] = val;
                            written++;
                            indexOfFirstByteToEscape++;
                        }
                    }
                    else
                    {
                        // Fall back to default encoder.
                        destination = destination.Slice(written);
                        value = value.Slice(indexOfFirstByteToEscape);
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

        private static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, JavaScriptEncoder encoder, ref int written)
        {
            Debug.Assert(encoder != null);

            OperationStatus result = encoder.Encode(value, destination, out int encoderBytesConsumed, out int encoderCharsWritten);

            Debug.Assert(result != OperationStatus.DestinationTooSmall);
            Debug.Assert(result != OperationStatus.NeedMoreData);

            if (result != OperationStatus.Done)
            {
                ThrowHelper.ThrowArgumentException_InvalidUTF16(value[encoderCharsWritten]);
            }

            Debug.Assert(encoderBytesConsumed == value.Length);

            written += encoderCharsWritten;
        }

        public static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder encoder, out int written)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            written = indexOfFirstByteToEscape;

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
                while (indexOfFirstByteToEscape < value.Length)
                {
                    char val = value[indexOfFirstByteToEscape];
                    if (IsAsciiValue(val))
                    {
                        if (NeedsEscapingNoBoundsCheck(val))
                        {
                            EscapeNextChars(val, destination, ref written);
                            indexOfFirstByteToEscape++;
                        }
                        else
                        {
                            destination[written] = val;
                            written++;
                            indexOfFirstByteToEscape++;
                        }
                    }
                    else
                    {
                        // Fall back to default encoder.
                        destination = destination.Slice(written);
                        value = value.Slice(indexOfFirstByteToEscape);
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
        /// Converts a number 0 - 15 to its associated hex character '0' - 'F' as byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte Int32LsbToHexDigit(int value)
        {
            Debug.Assert(value < 16);
            return (byte)((value < 10) ? ('0' + value) : ('A' + (value - 10)));
        }
#endif
    }
}
