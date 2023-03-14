// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    public static partial class Base64
    {
        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64Text">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <returns>true if <paramref name="base64Text"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<char> base64Text) =>
            IsValid<char, Base64CharValidatable>(base64Text, out _);

        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64Text">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <param name="decodedLength">The maximum length (in bytes) if you were to decode the base 64 encoded text <paramref name="base64Text"/> within a byte span.</param>
        /// <returns>true if <paramref name="base64Text"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<char> base64Text, out int decodedLength) =>
            IsValid<char, Base64CharValidatable>(base64Text, out decodedLength);

        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64TextUtf8">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <returns>true if <paramref name="base64TextUtf8"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8) =>
            IsValid<byte, Base64ByteValidatable>(base64TextUtf8, out _);

        /// <summary>
        /// Validates the span of UTF-8 encoded text represented as base64 into binary data.
        /// </summary>
        /// <param name="base64TextUtf8">The input span which contains UTF-8 encoded text in base64 that needs to be validated.</param>
        /// <param name="decodedLength">The maximum length (in bytes) if you were to decode the base 64 encoded text <paramref name="base64TextUtf8"/> within a byte span.</param>
        /// <returns>true if <paramref name="base64TextUtf8"/> is decodable; otherwise, false.</returns>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8, out int decodedLength) =>
            IsValid<byte, Base64ByteValidatable>(base64TextUtf8, out decodedLength);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValid<T, TBase64Validatable>(ReadOnlySpan<T> base64Text, out int decodedLength)
            where TBase64Validatable : IBase64Validatable<T>
        {
            if (base64Text.IsEmpty)
            {
                decodedLength = 0;
                return true;
            }

            int length = base64Text.Length;
            int paddingCount = 0;

            int indexOfPaddingInvalidOrWhitespace = TBase64Validatable.IndexOfAnyExcept(base64Text);
            if (indexOfPaddingInvalidOrWhitespace >= 0)
            {
                while (indexOfPaddingInvalidOrWhitespace >= 0)
                {
                    T charToValidate = base64Text[indexOfPaddingInvalidOrWhitespace];
                    if (TBase64Validatable.IsWhitespace(charToValidate))
                    {
                        // Chars to be ignored (e,g, whitespace...) should not count towards the length.
                        length--;
                    }
                    else if (TBase64Validatable.IsEncodingPad(charToValidate))
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
                        ReadOnlySpan<T> slicedSpan = base64Text.Slice(indexOfPaddingInvalidOrWhitespace + 1);
                        int nextIndexOfPaddingInvalidOrWhitespace = TBase64Validatable.IndexOfAnyExcept(slicedSpan);
                        if (nextIndexOfPaddingInvalidOrWhitespace == -1)
                        {
                            // No more invalid chars found.
                            break;
                        }
                        indexOfPaddingInvalidOrWhitespace = nextIndexOfPaddingInvalidOrWhitespace + indexOfPaddingInvalidOrWhitespace + 1; // Add current index offset.
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

        internal interface IBase64Validatable<T>
        {
            static abstract int IndexOfAnyExcept(ReadOnlySpan<T> span);
            static abstract bool IsWhitespace(T value);
            static abstract bool IsEncodingPad(T value);
        }

        internal readonly struct Base64CharValidatable : IBase64Validatable<char>
        {
            private static readonly IndexOfAnyValues<char> s_validBase64Chars = IndexOfAnyValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

            public static int IndexOfAnyExcept(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(s_validBase64Chars);
            public static bool IsWhitespace(char value) => Base64.IsWhitespace(value);
            public static bool IsEncodingPad(char value) => value == Base64.EncodingPad;
        }

        internal readonly struct Base64ByteValidatable : IBase64Validatable<byte>
        {
            private static readonly IndexOfAnyValues<byte> s_validBase64Chars = IndexOfAnyValues.Create(Base64.EncodingMap);

            public static int IndexOfAnyExcept(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(s_validBase64Chars);
            public static bool IsWhitespace(byte value) => Base64.IsWhitespace(value);
            public static bool IsEncodingPad(byte value) => value == Base64.EncodingPad;
        }
    }
}
