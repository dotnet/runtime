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
        public static unsafe bool IsValid(ReadOnlySpan<char> base64Text, out int decodedLength)
        {
            if (base64Text.IsEmpty)
            {
                decodedLength = 0;
                return true;
            }

            // Check for invalid chars
            int indexOfFirstNonBase64 = base64Text.IndexOfAnyExcept(ValidBase64CharsSortedAsc);
            if (indexOfFirstNonBase64 > -1)
            {
                decodedLength = 0;
                return false;
            }

            int length = base64Text.Length;
            int paddingCount = 0;

            // Check if there are chars that need to be ignored while determining the length
            if (base64Text.IndexOfAny(CharsToIgnore) > -1)
            {
                fixed (char* srcChars = &MemoryMarshal.GetReference(base64Text))
                {
                    int numberOfCharsToIgnore = 0;
                    char* src = srcChars;

                    for (int i = 0; i < base64Text.Length; i++)
                    {
                        char charToValidate = *src++;
                        if (IsCharToBeIgnored(charToValidate))
                        {
                            numberOfCharsToIgnore++;
                        }
                        // 61 = '=' (padding)
                        else if (charToValidate == 61)
                        {
                            paddingCount++;
                        }
                    }

                    length -= numberOfCharsToIgnore;
                }
            }

            if (length % 4 != 0)
            {
                decodedLength = 0;
                return false;
            }

            // Remove padding to get exact length
            decodedLength = (length / 4 * 3) - paddingCount;
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
        public static unsafe bool IsValid(ReadOnlySpan<byte> base64TextUtf8, out int decodedLength)
        {
            if (base64TextUtf8.IsEmpty)
            {
                decodedLength = 0;
                return true;
            }

            // Check for invalid chars
            int indexOfFirstNonBase64 = base64TextUtf8.IndexOfAnyExcept(ValidBase64BytesSortedAsc);
            if (indexOfFirstNonBase64 > -1)
            {
                decodedLength = 0;
                return false;
            }

            int length = base64TextUtf8.Length;
            int paddingCount = 0;

            // Check if there are chars that need to be ignored while determining the length
            if (base64TextUtf8.IndexOfAny(BytesToIgnore) > -1)
            {
                fixed (byte* srcBytes = &MemoryMarshal.GetReference(base64TextUtf8))
                {
                    int numberOfBytesToIgnore = 0;
                    byte* src = srcBytes;

                    for (int i = 0; i < base64TextUtf8.Length; i++)
                    {
                        byte byteToValidate = *src++;
                        if (IsByteToBeIgnored(byteToValidate))
                        {
                            numberOfBytesToIgnore++;
                        }
                        // 61 = '=' (padding)
                        else if (byteToValidate == 61)
                        {
                            paddingCount++;
                        }
                    }

                    length -= numberOfBytesToIgnore;
                }
            }

            if (length % 4 != 0)
            {
                decodedLength = 0;
                return false;
            }

            // Remove padding to get exact length
            decodedLength = (length / 4 * 3) - paddingCount;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsCharToBeIgnored(char aChar)
        {
            switch (aChar)
            {
                case '\n': // Line feed
                case '\t': // Horizontal tab
                case '\r': // Carriage return
                case ' ':  // Space
                    return true;
                default:
                    return false;
            }
        }

        private static ReadOnlySpan<byte> ValidBase64BytesSortedAsc => new byte[] {
            9, 10, 13, 32, 43, 47,                   //Line feed, Horizontal tab, Carriage return, Space, +, /
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57,  //0..9,
            61,                                      //=
            65, 66, 67, 68, 69, 70, 71, 72,          //A..H
            73, 74, 75, 76, 77, 78, 79, 80,          //I..P
            81, 82, 83, 84, 85, 86, 87, 88,          //Q..X
            89, 90, 97, 98, 99, 100, 101, 102,       //Y..Z, a..f
            103, 104, 105, 106, 107, 108, 109, 110,  //g..n
            111, 112, 113, 114, 115, 116, 117, 118,  //o..v
            119, 120, 121, 122,                      //w..z
        };

        private static ReadOnlySpan<char> ValidBase64CharsSortedAsc => new char[] {
            '\n', '\t', '\r', ' ', '+', '/',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            '=',
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
            'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f',
            'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n',
            'o', 'p', 'q', 'r', 's', 't', 'u', 'v',
            'w', 'x', 'y', 'z',
        };

        private static ReadOnlySpan<char> CharsToIgnore => new char[] { '\n', '\t', '\r', ' ' };
    }
}
