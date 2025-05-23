// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Text.Encodings.Web
{
    internal sealed partial class OptimizedInboxTextEncoder
    {
        /// <summary>
        /// A bitmap which represents the 64-bit pre-escaped form of the ASCII code points.
        /// A pre-escaped code point has the form [ WW 00 FF EE DD CC BB AA ],
        /// where AA - FF are the six-ASCII-byte escaped representation of the
        /// code point, zero-padded at the end. The high byte of the pre-escaped form
        /// is the number of non-zero bytes which make up the pre-escaped data.
        ///
        /// Example: If the escaped form of "@" is "%40", the pre-escaped form will be:
        /// 0x30_00_00_00_00_30_34_25. Iterate over the least significant bytes one-by-one
        /// to reconstruct the escaped representation, stopping when you hit a null byte.
        /// </summary>
        private unsafe struct AsciiPreescapedData
        {
            private fixed ulong Data[128];

            internal void PopulatePreescapedData(in AllowedBmpCodePointsBitmap allowedCodePointsBmp, ScalarEscaperBase innerEncoder)
            {
                this = default; // clear all existing data

                Span<char> tempBuffer = stackalloc char[8] { '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0' };
                for (int i = 0; i < 128; i++)
                {
                    ulong thisPreescapedData;
                    int encodedCharCount;

                    Rune rune = new Rune(i); // guaranteed to succeed
                    if (!Rune.IsControl(rune) && allowedCodePointsBmp.IsCharAllowed((char)i))
                    {
                        thisPreescapedData = (uint)i; // char maps to itself
                        encodedCharCount = 1;
                    }
                    else
                    {
                        encodedCharCount = innerEncoder.EncodeUtf16(rune, tempBuffer.Slice(0, 6));
                        Debug.Assert(encodedCharCount > 0 && encodedCharCount <= 6, "Inner encoder returned bad length.");

                        thisPreescapedData = 0;
                        tempBuffer.Slice(encodedCharCount).Clear();
                        for (int j = encodedCharCount - 1; j >= 0; j--)
                        {
                            uint thisChar = tempBuffer[j];
                            Debug.Assert(thisChar <= 0x7F, "Inner encoder returned non-ASCII data.");
                            thisPreescapedData = (thisPreescapedData << 8) | thisChar;
                        }
                    }

                    Data[i] = thisPreescapedData | ((ulong)(uint)encodedCharCount << 56);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal readonly bool TryGetPreescapedData(uint codePoint, out ulong preescapedData)
            {
                if (codePoint <= 0x7F)
                {
                    preescapedData = Data[codePoint];
                    return true;
                }
                else
                {
                    preescapedData = default;
                    return false;
                }
            }
        }
    }
}
