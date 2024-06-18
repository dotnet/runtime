// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64UrlText">A span of text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Base64Url.DecodeFromChars(ReadOnlySpan{char})"/> and
        /// <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> would successfully decode (in the case
        /// of <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> assuming sufficient output space).
        /// Any amount of whitespace is allowed anywhere in the input, where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64UrlText) => IsValid(base64UrlText, out _);

        /// <summary>Validates that the specified span of text is comprised of valid base-64 encoded data.</summary>
        /// <param name="base64UrlText">A span of text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input text.</param>
        /// <returns><see langword="true"/> if <paramref name="base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// If the method returns <see langword="true"/>, the same text passed to <see cref="Base64Url.DecodeFromChars(ReadOnlySpan{char})"/> and
        /// <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> would successfully decode (in the case
        /// of <see cref="Base64Url.TryDecodeFromChars(ReadOnlySpan{char}, Span{byte}, out int)"/> assuming sufficient output space).
        /// Any amount of whitespace is allowed anywhere in the input, where whitespace is defined as the characters ' ', '\t', '\r', or '\n'.
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<char> base64UrlText, out int decodedLength)
        {
            if (!base64UrlText.IsEmpty)
            {
                int length = 0, paddingCount = 0;
                for (int i = 0; i < base64UrlText.Length; i++)
                {
                    char charToValidate = base64UrlText[i];
                    if (charToValidate > byte.MaxValue)
                    {
                        // Invalid char was found.
                        goto Fail;
                    }

                    int index = DecodingMap[charToValidate];
                    if (index >= 0)
                    {
                        length++;
                        continue;
                    }

                    if (IsWhiteSpace(charToValidate))
                    {
                        continue;
                    }

                    if (!IsValidPadding(charToValidate))
                    {
                        // Invalid char was found.
                        goto Fail;
                    }

                    // Encoding pad found. Determine if padding is valid, then stop processing.
                    paddingCount = 1;
                    for (i++; i < base64UrlText.Length; i++)
                    {
                        char charToValidateInPadding = base64UrlText[i];

                        if (IsValidPadding(charToValidateInPadding))
                        {
                            // There can be at most 2 padding chars.
                            if (paddingCount >= 2)
                            {
                                goto Fail;
                            }

                            paddingCount++;
                        }
                        else if (!IsWhiteSpace(charToValidateInPadding))
                        {
                            // Invalid char was found.
                            goto Fail;
                        }
                    }

                    length += paddingCount;
                    break;
                }

                if (!ValidateAndDecodeLength(length, paddingCount, out decodedLength))
                {
                    goto Fail;
                }

                return true;
            }

            decodedLength = 0;
            return true;

        Fail:
            decodedLength = 0;
            return false;
        }


        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="utf8Base64UrlText">A span of UTF-8 text to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="utf8Base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> utf8Base64UrlText) => IsValid(utf8Base64UrlText, out _);

        /// <summary>Validates that the specified span of UTF-8 text is comprised of valid base-64 encoded data.</summary>
        /// <param name="utf8Base64UrlText">A span of UTF-8 text to validate.</param>
        /// <param name="decodedLength">If the method returns true, the number of decoded bytes that will result from decoding the input UTF-8 text.</param>
        /// <returns><see langword="true"/> if <paramref name="utf8Base64UrlText"/> contains a valid, decodable sequence of base-64 encoded data; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// where whitespace is defined as the characters ' ', '\t', '\r', or '\n' (as bytes).
        /// </remarks>
        public static bool IsValid(ReadOnlySpan<byte> utf8Base64UrlText, out int decodedLength)
        {
            if (!utf8Base64UrlText.IsEmpty)
            {
                int length = 0, paddingCount = 0;
                for (int i = 0; i < utf8Base64UrlText.Length; i++)
                {
                    byte byteToValidate = utf8Base64UrlText[i];

                    int index = DecodingMap[byteToValidate];
                    if (index >= 0)
                    {
                        length++;
                        continue;
                    }

                    if (IsWhiteSpace(byteToValidate))
                    {
                        continue;
                    }

                    if (!IsValidPadding(byteToValidate))
                    {
                        // Invalid char was found.
                        goto Fail;
                    }

                    // Encoding pad found. Determine if padding is valid, then stop processing.
                    paddingCount = 1;
                    for (i++; i < utf8Base64UrlText.Length; i++)
                    {
                        byte charToValidateInPadding = utf8Base64UrlText[i];
                        if (IsValidPadding(charToValidateInPadding))
                        {
                            // There can be at most 2 padding chars.
                            if (paddingCount >= 2)
                            {
                                goto Fail;
                            }

                            paddingCount++;
                        }
                        else if (!IsWhiteSpace(charToValidateInPadding))
                        {
                            // Invalid char was found.
                            goto Fail;
                        }
                    }

                    length += paddingCount;
                    break;
                }

                if (!ValidateAndDecodeLength(length, paddingCount, out decodedLength))
                {
                    goto Fail;
                }

                return true;
            }

            decodedLength = 0;
            return true;

        Fail:
            decodedLength = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ValidateAndDecodeLength(int length, int paddingCount, out int decodedLength)
        {
            // Padding is optional for Base64Url, so need to account remainder. If remainder is 1, then it's invalid.
            int remainder = (int)((uint)length % 4);
            if (remainder == 1 || (remainder > 1 && (remainder - paddingCount == 1 || paddingCount == remainder)))
            {
                decodedLength = 0;
                return false;
            }

            decodedLength = (length >> 2) * 3 + (remainder > 0 ? remainder - 1 : 0) - paddingCount;
            return true;
        }
    }
}
