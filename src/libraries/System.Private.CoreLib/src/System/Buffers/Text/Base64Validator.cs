// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Base64
    {
        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64Text">A span of text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="base64Text"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Convert.FromBase64String(string)"/> and
        /// <see cref="Convert.TryFromBase64Chars"/> would successfully decode (in the case
        /// of <see cref="Convert.TryFromBase64Chars"/> assuming sufficient output space). Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64Text) =>
            IsValid<char, Base64CharValidatable>(base64Text, out _);

        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64Text">A span of text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input text.</param>
        /// <returns><see langword="true"/> if <paramref name="base64Text"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Convert.FromBase64String(string)"/> and
        /// <see cref="Convert.TryFromBase64Chars"/> would successfully decode (in the case
        /// of <see cref="Convert.TryFromBase64Chars"/> assuming sufficient output space). Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64Text, out int decodedLength) =>
            IsValid<char, Base64CharValidatable>(base64Text, out decodedLength);

        /// <summary>Validates that the specified span of UTF8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64TextUtf8">A span of UTF8 text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="base64TextUtf8"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="DecodeFromUtf8"/> and
        /// <see cref="DecodeFromUtf8InPlace"/> would successfully decode. Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8) =>
            IsValid<byte, Base64ByteValidatable>(base64TextUtf8, out _);

        /// <summary>Validates that the specified span of UTF8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64TextUtf8">A span of UTF8 text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input UTF8 text.</param>
        /// <returns><see langword="true"/> if <paramref name="base64TextUtf8"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="DecodeFromUtf8"/> and
        /// <see cref="DecodeFromUtf8InPlace"/> would successfully decode. Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8, out int decodedLength) =>
            IsValid<byte, Base64ByteValidatable>(base64TextUtf8, out decodedLength);

        private static bool IsValid<T, TBase64Validatable>(ReadOnlySpan<T> base64Text, out int decodedLength)
            where TBase64Validatable : IBase64Validatable<T>
        {
            int length = 0, paddingCount = 0;

            if (!base64Text.IsEmpty)
            {
                while (true)
                {
                    int index = TBase64Validatable.IndexOfAnyExcept(base64Text);
                    if ((uint)index >= (uint)base64Text.Length)
                    {
                        length += base64Text.Length;
                        break;
                    }

                    length += index;

                    T charToValidate = base64Text[index];
                    base64Text = base64Text.Slice(index + 1);

                    if (TBase64Validatable.IsWhiteSpace(charToValidate))
                    {
                        // It's common if there's whitespace for there to be multiple whitespace characters in a row,
                        // e.g. \r\n.  Optimize for that case by looping here.
                        while (!base64Text.IsEmpty && TBase64Validatable.IsWhiteSpace(base64Text[0]))
                        {
                            base64Text = base64Text.Slice(1);
                        }
                        continue;
                    }

                    if (!TBase64Validatable.IsEncodingPad(charToValidate))
                    {
                        // Invalid char was found.
                        goto Fail;
                    }

                    // Encoding pad found. Determine if padding is valid, then stop processing.
                    paddingCount = 1;
                    foreach (T charToValidateInPadding in base64Text)
                    {
                        if (TBase64Validatable.IsEncodingPad(charToValidateInPadding))
                        {
                            // There can be at most 2 padding chars.
                            if (paddingCount >= 2)
                            {
                                goto Fail;
                            }

                            paddingCount++;
                        }
                        else if (!TBase64Validatable.IsWhiteSpace(charToValidateInPadding))
                        {
                            // Invalid char was found.
                            goto Fail;
                        }
                    }

                    length += paddingCount;
                    break;
                }

                if (length % 4 != 0)
                {
                    goto Fail;
                }
            }

            // Remove padding to get exact length.
            decodedLength = (int)((uint)length / 4 * 3) - paddingCount;
            return true;

            Fail:
            decodedLength = 0;
            return false;
        }

        private interface IBase64Validatable<T>
        {
            static abstract int IndexOfAnyExcept(ReadOnlySpan<T> span);
            static abstract bool IsWhiteSpace(T value);
            static abstract bool IsEncodingPad(T value);
        }

        private readonly struct Base64CharValidatable : IBase64Validatable<char>
        {
            private static readonly SearchValues<char> s_validBase64Chars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

            public static int IndexOfAnyExcept(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(s_validBase64Chars);
            public static bool IsWhiteSpace(char value) => Base64.IsWhiteSpace(value);
            public static bool IsEncodingPad(char value) => value == EncodingPad;
        }

        private readonly struct Base64ByteValidatable : IBase64Validatable<byte>
        {
            private static readonly SearchValues<byte> s_validBase64Chars = SearchValues.Create(EncodingMap);

            public static int IndexOfAnyExcept(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(s_validBase64Chars);
            public static bool IsWhiteSpace(byte value) => Base64.IsWhiteSpace(value);
            public static bool IsEncodingPad(byte value) => value == EncodingPad;
        }
    }
}
