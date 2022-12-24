// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    public static partial class Base64
    {
        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64Text">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <returns>true if <paramref name="base64Text"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<char> base64Text) => IsValid(base64Text, out int _);

        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64Text">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <param name="decodedLength">The maximum length (in bytes) if you were to decode the base 64 encoded text <paramref name="base64Text"/> within a byte span.</param>
        /// <returns>true if <paramref name="base64Text"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<char> base64Text, out int decodedLength)
        {
            if (base64Text.IsEmpty)
            {
                decodedLength = 0;
                return true;
            }

            int length = base64Text.Length;
            int paddingCount = 0;

            int indexOfPaddingInvalidOrWhitespace = base64Text.IndexOfAnyExcept(validBase64Chars);
            if (indexOfPaddingInvalidOrWhitespace >= 0)
            {
                while (indexOfPaddingInvalidOrWhitespace >= 0)
                {
                    char charToValidate = base64Text[indexOfPaddingInvalidOrWhitespace];
                    if (IsWhitespace(charToValidate))
                    {
                        // Chars to be ignored (e,g, whitespace...) should not count towards the length.
                        length--;
                    }
                    else if (charToValidate == EncodingPad)
                    {
                        // There can be at most 2 padding chars.
                        if (paddingCount == 2)
                        {
                            decodedLength = 0;
                            return false;
                        }

                        paddingCount++;
                    }
                    else
                    {
                        // An invalid char was encountered.
                        decodedLength = 0;
                        return false;
                    }

                    if (indexOfPaddingInvalidOrWhitespace == base64Text.Length - 1)
                    {
                        // The end of the input has been reached.
                        break;
                    }

                    // If no padding is found, slice and use IndexOfAnyExcept to look for the next invalid char.
                    if (paddingCount == 0)
                    {
                        indexOfPaddingInvalidOrWhitespace = base64Text
                            .Slice(indexOfPaddingInvalidOrWhitespace + 1, base64Text.Length - indexOfPaddingInvalidOrWhitespace - 1)
                            .IndexOfAnyExcept(validBase64Chars)
                            + indexOfPaddingInvalidOrWhitespace + 1; // Add current index offset.
                    }
                    // If padding is already found, simply increment, as the common case might have 2 sequential padding chars.
                    else
                    {
                        indexOfPaddingInvalidOrWhitespace++;
                    }
                }

                // If the invalid chars all consisted of whitespace, the input will be empty.
                if (length == 0)
                {
                    decodedLength = 0;
                    return true;
                }
            }

            if (length % 4 != 0)
            {
                decodedLength = 0;
                return false;
            }

            // Remove padding to get exact length
            decodedLength = (int)((uint)length / 4 * 3) - paddingCount;
            return true;
        }

        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64TextUtf8">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <returns>true if <paramref name="base64TextUtf8"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8) => IsValid(base64TextUtf8, out int _);

        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64TextUtf8">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <param name="decodedLength">The maximum length (in bytes) if you were to decode the base 64 encoded text <paramref name="base64TextUtf8"/> within a byte span.</param>
        /// <returns>true if <paramref name="base64TextUtf8"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8, out int decodedLength)
        {
            if (base64TextUtf8.IsEmpty)
            {
                decodedLength = 0;
                return true;
            }

            int length = base64TextUtf8.Length;
            int paddingCount = 0;

            int indexOfPaddingInvalidOrWhitespace = base64TextUtf8.IndexOfAnyExcept(validBase64Bytes);
            if (indexOfPaddingInvalidOrWhitespace >= 0)
            {
                while (indexOfPaddingInvalidOrWhitespace >= 0)
                {
                    byte byteToValidate = base64TextUtf8[indexOfPaddingInvalidOrWhitespace];
                    if (IsWhitespace(byteToValidate))
                    {
                        // Bytes to be ignored (e,g, whitespace...) should not count towards the length.
                        length--;
                    }
                    else if (byteToValidate == EncodingPad)
                    {
                        // There can be at most 2 padding chars.
                        if (paddingCount == 2)
                        {
                            decodedLength = 0;
                            return false;
                        }

                        paddingCount++;
                    }
                    else
                    {
                        // An invalid char was encountered.
                        decodedLength = 0;
                        return false;
                    }

                    if (indexOfPaddingInvalidOrWhitespace == base64TextUtf8.Length - 1)
                    {
                        // The end of the input has been reached.
                        break;
                    }

                    // If no padding is found, slice and use IndexOfAnyExcept to look for the next invalid char.
                    if (paddingCount == 0)
                    {
                        indexOfPaddingInvalidOrWhitespace = base64TextUtf8
                            .Slice(indexOfPaddingInvalidOrWhitespace + 1, base64TextUtf8.Length - indexOfPaddingInvalidOrWhitespace - 1)
                            .IndexOfAnyExcept(validBase64Bytes)
                            + indexOfPaddingInvalidOrWhitespace + 1; // Add current index offset.
                    }
                    // If padding is already found, simply increment, as the common case might have 2 sequential padding chars.
                    else
                    {
                        indexOfPaddingInvalidOrWhitespace++;
                    }
                }

                // If the invalid chars all consisted of whitespace, the input will be empty.
                if (length == 0)
                {
                    decodedLength = 0;
                    return true;
                }
            }

            if (length % 4 != 0)
            {
                decodedLength = 0;
                return false;
            }

            // Remove padding to get exact length
            decodedLength = (int)((uint)length / 4 * 3) - paddingCount;
            return true;
        }

        private static readonly IndexOfAnyValues<byte> validBase64Bytes = IndexOfAnyValues.Create(EncodingMap);

        private static readonly IndexOfAnyValues<char> validBase64Chars = IndexOfAnyValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");
    }
}
