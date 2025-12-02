// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Buffers.Text
{
    internal static partial class Base64Helper
    {
        internal static bool IsValid<T, TBase64Validatable>(TBase64Validatable validatable, ReadOnlySpan<T> base64Text, out int decodedLength)
            where TBase64Validatable : IBase64Validatable<T>
            where T : struct
        {
            int length = 0, paddingCount = 0;
            T lastChar = default;

            if (!base64Text.IsEmpty)
            {
#if NET
                while (!base64Text.IsEmpty)
                {
                    int index = validatable.IndexOfAnyExcept(base64Text);
                    if ((uint)index >= (uint)base64Text.Length)
                    {
                        length += base64Text.Length;
                        lastChar = base64Text[base64Text.Length - 1];
                        break;
                    }

                    length += index;
                    if (index != 0)
                    {
                        lastChar = base64Text[index - 1];
                    }

                    T charToValidate = base64Text[index];
                    base64Text = base64Text.Slice(index + 1);

                    if (validatable.IsWhiteSpace(charToValidate))
                    {
                        // It's common if there's whitespace for there to be multiple whitespace characters in a row,
                        // e.g. \r\n.  Optimize for that case by looping here.
                        while (!base64Text.IsEmpty && validatable.IsWhiteSpace(base64Text[0]))
                        {
                            base64Text = base64Text.Slice(1);
                        }
                        continue;
                    }

                    if (!validatable.IsEncodingPad(charToValidate))
                    {
                        // Invalid char was found.
                        goto Fail;
                    }

                    // Encoding pad found. Determine if padding is valid, then stop processing.
                    paddingCount = 1;
                    foreach (T charToValidateInPadding in base64Text)
                    {
#else
                for (int i = 0; i < base64Text.Length; i++)
                {
                    T charToValidate = base64Text[i];
                    int value = validatable.DecodeValue(charToValidate);
                    if (value == -2)
                    {
                        // Not an Ascii char
                        goto Fail;
                    }

                    if (value >= 0) // valid char
                    {
                        length++;
                        lastChar = charToValidate;
                        continue;
                    }
                    if (validatable.IsWhiteSpace(charToValidate))
                    {
                        continue;
                    }

                    if (!validatable.IsEncodingPad(charToValidate))
                    {
                        // Invalid char was found.
                        goto Fail;
                    }

                    // Encoding pad found. Determine if padding is valid, then stop processing.
                    paddingCount = 1;
                    for (i++; i < base64Text.Length; i++)
                    {
                        T charToValidateInPadding = base64Text[i];
#endif
                        if (validatable.IsEncodingPad(charToValidateInPadding))
                        {
                            // There can be at most 2 padding chars.
                            if (paddingCount >= 2)
                            {
                                goto Fail;
                            }

                            paddingCount++;
                        }
                        else if (!validatable.IsWhiteSpace(charToValidateInPadding))
                        {
                            // Invalid char was found.
                            goto Fail;
                        }
                    }

                    length += paddingCount;
                    break;
                }

                if (!validatable.ValidateAndDecodeLength(lastChar, length, paddingCount, out decodedLength))
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

        internal interface IBase64Validatable<T>
        {
#if NET
            int IndexOfAnyExcept(ReadOnlySpan<T> span);
#else
            int DecodeValue(T value);
#endif
            bool IsWhiteSpace(T value);
            bool IsEncodingPad(T value);
            bool ValidateAndDecodeLength(T lastChar, int length, int paddingCount, out int decodedLength);
        }

        internal readonly struct Base64CharValidatable : IBase64Validatable<char>
        {
#if NET
            private static readonly SearchValues<char> s_validBase64Chars = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/");

            public int IndexOfAnyExcept(ReadOnlySpan<char> span) => span.IndexOfAnyExcept(s_validBase64Chars);
#else
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int DecodeValue(char value)
            {
                if (value > byte.MaxValue)
                {
                    // Invalid char was found.
                    return -2;
                }

                return default(Base64DecoderByte).DecodingMap[value];
            }
#endif
            public bool IsWhiteSpace(char value) => Base64Helper.IsWhiteSpace(value);
            public bool IsEncodingPad(char value) => value == EncodingPad;
            public bool ValidateAndDecodeLength(char lastChar, int length, int paddingCount, out int decodedLength) =>
                default(Base64ByteValidatable).ValidateAndDecodeLength((byte)lastChar, length, paddingCount, out decodedLength);
        }

        internal readonly struct Base64ByteValidatable : IBase64Validatable<byte>
        {
#if NET
            private static readonly SearchValues<byte> s_validBase64Chars = SearchValues.Create(default(Base64EncoderByte).EncodingMap);

            public int IndexOfAnyExcept(ReadOnlySpan<byte> span) => span.IndexOfAnyExcept(s_validBase64Chars);
#else
            public int DecodeValue(byte value) => default(Base64DecoderByte).DecodingMap[value];
#endif
            public bool IsWhiteSpace(byte value) => Base64Helper.IsWhiteSpace(value);
            public bool IsEncodingPad(byte value) => value == EncodingPad;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool ValidateAndDecodeLength(byte lastChar, int length, int paddingCount, out int decodedLength)
            {
                if (length % 4 == 0)
                {
                    int decoded = default(Base64DecoderByte).DecodingMap[lastChar];
                    if ((paddingCount == 1 && (decoded & 0x03) != 0) ||
                        (paddingCount == 2 && (decoded & 0x0F) != 0))
                    {
                        // unused lower bits are not 0, reject input
                        decodedLength = 0;
                        return false;
                    }

                    // Remove padding to get exact length.
                    decodedLength = (int)((uint)length / 4 * 3) - paddingCount;
                    return true;
                }

                decodedLength = 0;
                return false;
            }
        }
    }
}
