// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64UrlText">A span of text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>TODO : Update remarks
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Convert.FromBase64String(string)"/> and
        /// <see cref="Convert.TryFromBase64Chars"/> would successfully decode (in the case
        /// of <see cref="Convert.TryFromBase64Chars"/> assuming sufficient output space). Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64UrlText) =>
            Base64.IsValid<char, Base64UrlCharValidatable>(base64UrlText, out _);

        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64UrlText">A span of text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input text.</param>
        /// <returns><see langword="true"/> if <paramref name="base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>TODO : Update remarks
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Convert.FromBase64String(string)"/> and
        /// <see cref="Convert.TryFromBase64Chars"/> would successfully decode (in the case
        /// of <see cref="Convert.TryFromBase64Chars"/> assuming sufficient output space). Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64UrlText, out int decodedLength) =>
            Base64.IsValid<char, Base64UrlCharValidatable>(base64UrlText, out decodedLength);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="utf8Base64UrlText">A span of UTF-8 text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="utf8Base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks> TODO : Update remarks
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> utf8Base64UrlText) =>
            Base64.IsValid<byte, Base64UrlByteValidatable>(utf8Base64UrlText, out _);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="utf8Base64UrlText">A span of UTF-8 text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input UTF-8 text.</param>
        /// <returns><see langword="true"/> if <paramref name="utf8Base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks> TODO : Update remarks
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> utf8Base64UrlText, out int decodedLength) =>
            Base64.IsValid<byte, Base64UrlByteValidatable>(utf8Base64UrlText, out decodedLength);

        private static bool ValidateAndDecodeLength(int length, int paddingCount, out int decodedLength)
        {
            if (length == 1)
            {
                decodedLength = 0;
                return false;
            }

            // Padding is optional for Base64Url, so need to account remainder.
            int remainder = length % 4;
            decodedLength = (int)((uint)length / 4 * 3) + (remainder > 0 ? remainder - 1 : 0) - paddingCount;
            return true;
        }

        private const uint UrlEncodingPad = '%'; // url padding

        private readonly struct Base64UrlCharValidatable : Base64.IBase64Validatable<char>
        {
            private static readonly SearchValues<char> s_validBase64UrlChars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

            public static int IndexOfAnyExcept(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(s_validBase64UrlChars);
            public static bool IsWhiteSpace(char value) => Base64.IsWhiteSpace(value);
            public static bool IsEncodingPad(char value) => value == Base64.EncodingPad || value == UrlEncodingPad;
            public static bool ValidateAndDecodeLength(int length, int paddingCount, out int decodedLength) =>
                Base64Url.ValidateAndDecodeLength(length, paddingCount, out decodedLength);
        }

        private readonly struct Base64UrlByteValidatable : Base64.IBase64Validatable<byte>
        {
            private static readonly SearchValues<byte> s_validBase64UrlChars = SearchValues.Create(Base64UrlEncoderByte.EncodingMap);

            public static int IndexOfAnyExcept(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(s_validBase64UrlChars);
            public static bool IsWhiteSpace(byte value) => Base64.IsWhiteSpace(value);
            public static bool IsEncodingPad(byte value) => value == Base64.EncodingPad || value == UrlEncodingPad;
            public static bool ValidateAndDecodeLength(int length, int paddingCount, out int decodedLength) =>
                Base64Url.ValidateAndDecodeLength(length, paddingCount, out decodedLength);
        }
    }
}
