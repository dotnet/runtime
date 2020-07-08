// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Unicode
{
    internal static partial class Utf8Utility
    {
        /// <summary>
        /// Returns the index in <paramref name="utf8Data"/> where the first non-whitespace character
        /// appears, or the input length if the data contains only whitespace characters.
        /// </summary>
        public static int GetIndexOfFirstNonWhiteSpaceChar(ReadOnlySpan<byte> utf8Data)
        {
            // This method is optimized for the case where the input data is ASCII, and if the
            // data does need to be trimmed it's likely that only a relatively small number of
            // bytes will be trimmed.

            int i = 0;
            int length = utf8Data.Length;

            while (i < length)
            {
                // Very quick check: see if the byte is in the range [ 21 .. 7F ].
                // If so, we can skip the more expensive logic later in this method.

                if ((sbyte)utf8Data[i] > (sbyte)0x20)
                {
                    break;
                }

                uint possibleAsciiByte = utf8Data[i];
                if (UnicodeUtility.IsAsciiCodePoint(possibleAsciiByte))
                {
                    // The simple comparison failed. Let's read the actual byte value,
                    // and if it's ASCII we can delegate to Rune's inlined method
                    // implementation.

                    if (Rune.IsWhiteSpace(new Rune(possibleAsciiByte)))
                    {
                        i++;
                        continue;
                    }
                }
                else
                {
                    // Not ASCII data. Go back to the slower "decode the entire scalar"
                    // code path, then compare it against our Unicode tables.

                    Rune.DecodeFromUtf8(utf8Data.Slice(i), out Rune decodedRune, out int bytesConsumed);
                    if (Rune.IsWhiteSpace(decodedRune))
                    {
                        i += bytesConsumed;
                        continue;
                    }
                }

                break; // If we got here, we saw a non-whitespace subsequence.
            }

            return i;
        }

        /// <summary>
        /// Returns the index in <paramref name="utf8Data"/> where the trailing whitespace sequence
        /// begins, or 0 if the data contains only whitespace characters, or the span length if the
        /// data does not end with any whitespace characters.
        /// </summary>
        public static int GetIndexOfTrailingWhiteSpaceSequence(ReadOnlySpan<byte> utf8Data)
        {
            // This method is optimized for the case where the input data is ASCII, and if the
            // data does need to be trimmed it's likely that only a relatively small number of
            // bytes will be trimmed.

            int length = utf8Data.Length;

            while (length > 0)
            {
                // Very quick check: see if the byte is in the range [ 21 .. 7F ].
                // If so, we can skip the more expensive logic later in this method.

                if ((sbyte)utf8Data[length - 1] > (sbyte)0x20)
                {
                    break;
                }

                uint possibleAsciiByte = utf8Data[length - 1];
                if (UnicodeUtility.IsAsciiCodePoint(possibleAsciiByte))
                {
                    // The simple comparison failed. Let's read the actual byte value,
                    // and if it's ASCII we can delegate to Rune's inlined method
                    // implementation.

                    if (Rune.IsWhiteSpace(new Rune(possibleAsciiByte)))
                    {
                        length--;
                        continue;
                    }
                }
                else
                {
                    // Not ASCII data. Go back to the slower "decode the entire scalar"
                    // code path, then compare it against our Unicode tables.

                    Rune.DecodeLastFromUtf8(utf8Data.Slice(0, length), out Rune decodedRune, out int bytesConsumed);
                    if (Rune.IsWhiteSpace(decodedRune))
                    {
                        length -= bytesConsumed;
                        continue;
                    }
                }

                break; // If we got here, we saw a non-whitespace subsequence.
            }

            return length;
        }
    }
}
