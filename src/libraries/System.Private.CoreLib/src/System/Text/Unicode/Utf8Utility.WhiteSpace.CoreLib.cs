// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

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
            return (int)GetIndexOfFirstNonWhiteSpaceChar(ref MemoryMarshal.GetReference(utf8Data), (uint)utf8Data.Length);
        }

        private static nuint GetIndexOfFirstNonWhiteSpaceChar(ref byte utf8Data, nuint length)
        {
            // This method is optimized for the case where the input data is ASCII, and if the
            // data does need to be trimmed it's likely that only a relatively small number of
            // bytes will be trimmed.

            nuint i = 0;

            while (i < length)
            {
                // Very quick check: see if the byte is in the range [ 21 .. 7F ].
                // If so, we can skip the more expensive logic later in this method.

                if ((sbyte)Unsafe.AddByteOffset(ref utf8Data, i) > (sbyte)0x20)
                {
                    break;
                }

                uint possibleAsciiByte = Unsafe.AddByteOffset(ref utf8Data, i);
                if (UnicodeUtility.IsAsciiCodePoint(possibleAsciiByte))
                {
                    // The simple comparison failed. Let's read the actual byte value,
                    // and if it's ASCII we can delegate to Rune's inlined method
                    // implementation.

                    if (Rune.IsWhiteSpace(Rune.UnsafeCreate(possibleAsciiByte)))
                    {
                        i++;
                        continue;
                    }
                }
                else
                {
                    // Not ASCII data. Go back to the slower "decode the entire scalar"
                    // code path, then compare it against our Unicode tables.

                    Rune.DecodeFromUtf8(new ReadOnlySpan<byte>(ref utf8Data, (int)length).Slice((int)i), out Rune decodedRune, out int bytesConsumed);
                    if (Rune.IsWhiteSpace(decodedRune))
                    {
                        i += (uint)bytesConsumed;
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
            return (int)GetIndexOfTrailingWhiteSpaceSequence(ref MemoryMarshal.GetReference(utf8Data), (uint)utf8Data.Length);
        }

        private static nuint GetIndexOfTrailingWhiteSpaceSequence(ref byte utf8Data, nuint length)
        {
            // This method is optimized for the case where the input data is ASCII, and if the
            // data does need to be trimmed it's likely that only a relatively small number of
            // bytes will be trimmed.

            while (length > 0)
            {
                // Very quick check: see if the byte is in the range [ 21 .. 7F ].
                // If so, we can skip the more expensive logic later in this method.

                if ((sbyte)Unsafe.Add(ref Unsafe.AddByteOffset(ref utf8Data, length), -1) > (sbyte)0x20)
                {
                    break;
                }

                uint possibleAsciiByte = Unsafe.Add(ref Unsafe.AddByteOffset(ref utf8Data, length), -1);
                if (UnicodeUtility.IsAsciiCodePoint(possibleAsciiByte))
                {
                    // The simple comparison failed. Let's read the actual byte value,
                    // and if it's ASCII we can delegate to Rune's inlined method
                    // implementation.

                    if (Rune.IsWhiteSpace(Rune.UnsafeCreate(possibleAsciiByte)))
                    {
                        length--;
                        continue;
                    }
                }
                else
                {
                    // Not ASCII data. Go back to the slower "decode the entire scalar"
                    // code path, then compare it against our Unicode tables.

                    Rune.DecodeLastFromUtf8(new ReadOnlySpan<byte>(ref utf8Data, (int)length), out Rune decodedRune, out int bytesConsumed);
                    if (Rune.IsWhiteSpace(decodedRune))
                    {
                        length -= (uint)bytesConsumed;
                        continue;
                    }
                }

                break; // If we got here, we saw a non-whitespace subsequence.
            }

            return length;
        }
    }
}
