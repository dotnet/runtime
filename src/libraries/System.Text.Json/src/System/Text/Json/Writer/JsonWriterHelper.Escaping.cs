// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Text.Encodings.Web;

#if !NETCOREAPP
using System.Runtime.CompilerServices;
#endif

namespace System.Text.Json
{
    // TODO: Replace the escaping logic with publicly shipping APIs from https://github.com/dotnet/runtime/issues/27919
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

#if NETCOREAPP
        private const string HexFormatString = "X4";
#endif

        private static readonly StandardFormat s_hexStandardFormat = new StandardFormat('X', 4);

        private static bool NeedsEscaping(byte value) => AllowList[value] == 0;

        private static bool NeedsEscapingNoBoundsCheck(char value) => AllowList[value] == 0;

        public static int NeedsEscaping(ReadOnlySpan<byte> value, JavaScriptEncoder? encoder)
        {
            return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncodeUtf8(value);
        }

        public static unsafe int NeedsEscaping(ReadOnlySpan<char> value, JavaScriptEncoder? encoder)
        {
            // Some implementations of JavaScriptEncoder.FindFirstCharacterToEncode may not accept
            // null pointers and guard against that. Hence, check up-front to return -1.
            if (value.IsEmpty)
            {
                return -1;
            }

            fixed (char* ptr = value)
            {
                return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncode(ptr, value.Length);
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

        public static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int written)
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

        public static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int written)
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
#if NETCOREAPP
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

#if !NETCOREAPP
        private static int WriteHex(int value, Span<char> destination, int written)
        {
            destination[written++] = HexConverter.ToCharUpper(value >> 12);
            destination[written++] = HexConverter.ToCharUpper(value >> 8);
            destination[written++] = HexConverter.ToCharUpper(value >> 4);
            destination[written++] = HexConverter.ToCharUpper(value);
            return written;
        }
#endif
    }
}
