// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System
{
    internal static class IriHelper
    {
        //
        // Checks if provided non surrogate char lies in iri range
        // This method implements the ABNF checks per https://tools.ietf.org/html/rfc3987#section-2.2
        //
        internal static bool CheckIriUnicodeRange(char unicode, bool isQuery)
        {
            return ((unicode >= '\u00A0' && unicode <= '\uD7FF') ||
               (unicode >= '\uF900' && unicode <= '\uFDCF') ||
               (unicode >= '\uFDF0' && unicode <= '\uFFEF') ||
               (isQuery && unicode >= '\uE000' && unicode <= '\uF8FF'));
        }

        //
        // Check if highSurr and lowSurr are a surrogate pair then
        // it checks if the combined char is in the range
        // Takes in isQuery because iri restrictions for query are different
        // This method implements the ABNF checks per https://tools.ietf.org/html/rfc3987#section-2.2
        //
        internal static bool CheckIriUnicodeRange(char highSurr, char lowSurr, out bool isSurrogatePair, bool isQuery)
        {
            Debug.Assert(char.IsHighSurrogate(highSurr));

            if (Rune.TryCreate(highSurr, lowSurr, out Rune rune))
            {
                isSurrogatePair = true;

                // U+xxFFFE..U+xxFFFF is always private use for all planes, so we exclude it.
                // U+E0000..U+E0FFF is disallowed per the 'ucschar' definition in the ABNF.
                // U+F0000 and above are only allowed for 'iprivate' per the ABNF (isQuery = true).

                return ((rune.Value & 0xFFFF) < 0xFFFE)
                    && ((uint)(rune.Value - 0xE0000) >= (0xE1000 - 0xE0000))
                    && (isQuery || rune.Value < 0xF0000);
            }

            isSurrogatePair = false;
            return false;
        }

        //
        // Check reserved chars according to RFC 3987 in a specific component
        //
        internal static bool CheckIsReserved(char ch, UriComponents component)
        {
            if ((UriComponents.AbsoluteUri & component) == 0)
            {
                return component == 0 && UriHelper.IsGenDelim(ch);
            }

            return UriHelper.RFC3986ReservedMarks.Contains(ch);
        }

        //
        // IRI normalization for strings containing characters that are not allowed or
        // escaped characters that should be unescaped in the context of the specified Uri component.
        //
        internal static unsafe string EscapeUnescapeIri(char* pInput, int start, int end, UriComponents component)
        {
            int size = end - start;
            ValueStringBuilder dest = new ValueStringBuilder(size);
            byte[]? bytes = null;

            int next = start;
            char ch;

            Span<byte> maxUtf8EncodedSpan = stackalloc byte[4];

            for (; next < end; ++next)
            {
                if ((ch = pInput[next]) == '%')
                {
                    if (next + 2 < end)
                    {
                        ch = UriHelper.DecodeHexChars(pInput[next + 1], pInput[next + 2]);

                        // Do not unescape a reserved char
                        if (ch == Uri.c_DummyChar || ch == '%' || CheckIsReserved(ch, component) || UriHelper.IsNotSafeForUnescape(ch))
                        {
                            // keep as is
                            dest.Append(pInput[next++]);
                            dest.Append(pInput[next++]);
                            dest.Append(pInput[next]);
                            continue;
                        }
                        else if (ch <= '\x7F')
                        {
                            Debug.Assert(ch < 0xFF, "Expecting ASCII character.");
                            //ASCII
                            dest.Append(ch);
                            next += 2;
                            continue;
                        }
                        else
                        {
                            // possibly utf8 encoded sequence of unicode

                            // check if safe to unescape according to Iri rules

                            Debug.Assert(ch < 0xFF, "Expecting ASCII character.");

                            int startSeq = next;
                            int byteCount = 1;
                            // lazy initialization of max size, will reuse the array for next sequences
                            if (bytes is null)
                                bytes = new byte[end - next];

                            bytes[0] = (byte)ch;
                            next += 3;
                            while (next < end)
                            {
                                // Check on exit criterion
                                if ((ch = pInput[next]) != '%' || next + 2 >= end)
                                    break;

                                // already made sure we have 3 characters in str
                                ch = UriHelper.DecodeHexChars(pInput[next + 1], pInput[next + 2]);

                                //invalid hex sequence ?
                                if (ch == Uri.c_DummyChar)
                                    break;
                                // character is not part of a UTF-8 sequence ?
                                else if (ch < '\x80')
                                    break;
                                else
                                {
                                    //a UTF-8 sequence
                                    bytes[byteCount++] = (byte)ch;
                                    next += 3;
                                }

                                Debug.Assert(ch < 0xFF, "Expecting ASCII character.");
                            }
                            next--; // for loop will increment


                            // Using encoder with no replacement fall-back will skip all invalid UTF-8 sequences.
                            Encoding noFallbackCharUTF8 = Encoding.GetEncoding(
                                                                                Encoding.UTF8.CodePage,
                                                                                new EncoderReplacementFallback(""),
                                                                                new DecoderReplacementFallback(""));

                            char[] unescapedChars = new char[bytes.Length];
                            int charCount = noFallbackCharUTF8.GetChars(bytes, 0, byteCount, unescapedChars, 0);


                            if (charCount != 0)
                            {
                                // If invalid sequences were present in the original escaped string, we need to
                                // copy the escaped versions of those sequences.
                                // Decoded Unicode values will be kept only when they are allowed by the URI/IRI RFC
                                // rules.
                                UriHelper.MatchUTF8Sequence(ref dest, unescapedChars, charCount, bytes,
                                    byteCount, component == UriComponents.Query, true);
                            }
                            else
                            {
                                // copy escaped sequence as is
                                for (int i = startSeq; i <= next; ++i)
                                {
                                    dest.Append(pInput[i]);
                                }
                            }
                        }
                    }
                    else
                    {
                        dest.Append(pInput[next]);
                    }
                }
                else if (ch > '\x7f')
                {
                    // unicode

                    bool isInIriUnicodeRange;
                    bool surrogatePair = false;

                    char ch2 = '\0';

                    if ((char.IsHighSurrogate(ch)) && (next + 1 < end))
                    {
                        ch2 = pInput[next + 1];
                        isInIriUnicodeRange = CheckIriUnicodeRange(ch, ch2, out surrogatePair, component == UriComponents.Query);
                    }
                    else
                    {
                        isInIriUnicodeRange = CheckIriUnicodeRange(ch, component == UriComponents.Query);
                    }

                    if (isInIriUnicodeRange)
                    {
                        dest.Append(ch);
                        if (surrogatePair)
                        {
                            dest.Append(ch2);
                        }
                    }
                    else
                    {
                        Rune rune;
                        if (surrogatePair)
                        {
                            rune = new Rune(ch, ch2);
                        }
                        else if (!Rune.TryCreate(ch, out rune))
                        {
                            rune = Rune.ReplacementChar;
                        }

                        int bytesWritten = rune.EncodeToUtf8(maxUtf8EncodedSpan);
                        Span<byte> encodedBytes = maxUtf8EncodedSpan.Slice(0, bytesWritten);

                        foreach (byte b in encodedBytes)
                        {
                            UriHelper.EscapeAsciiChar(b, ref dest);
                        }
                    }

                    if (surrogatePair)
                    {
                        next++;
                    }
                }
                else
                {
                    // just copy the character
                    dest.Append(pInput[next]);
                }
            }

            string result = dest.ToString();
            return result;
        }
    }
}
