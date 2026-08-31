// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static System.Buffers.Text.Base64Helper;

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
            Base64Helper.IsValid(default(Base64CharValidatable), base64Text, out _);

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
            Base64Helper.IsValid(default(Base64CharValidatable), base64Text, out decodedLength);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64TextUtf8">A span of UTF-8 text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="base64TextUtf8"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="DecodeFromUtf8"/> and
        /// <see cref="DecodeFromUtf8InPlace"/> would successfully decode. Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8) =>
            Base64Helper.IsValid(default(Base64ByteValidatable), base64TextUtf8, out _);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64TextUtf8">A span of UTF-8 text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input UTF-8 text.</param>
        /// <returns><see langword="true"/> if <paramref name="base64TextUtf8"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="DecodeFromUtf8"/> and
        /// <see cref="DecodeFromUtf8InPlace"/> would successfully decode. Any amount of whitespace is allowed anywhere in the input,
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> base64TextUtf8, out int decodedLength) =>
            Base64Helper.IsValid(default(Base64ByteValidatable), base64TextUtf8, out decodedLength);

    }
}
