// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
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
            return IsInInclusiveRange(unicode, '\u00A0', '\uD7FF')
                || IsInInclusiveRange(unicode, '\uF900', '\uFDCF')
                || IsInInclusiveRange(unicode, '\uFDF0', '\uFFEF')
                || (isQuery && IsInInclusiveRange(unicode, '\uE000', '\uF8FF'));
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

        internal static bool CheckIriUnicodeRange(uint value, bool isQuery)
        {
            if (value <= 0xFFFF)
            {
                return IsInInclusiveRange(value, '\u00A0', '\uD7FF')
                    || IsInInclusiveRange(value, '\uF900', '\uFDCF')
                    || IsInInclusiveRange(value, '\uFDF0', '\uFFEF')
                    || (isQuery && IsInInclusiveRange(value, '\uE000', '\uF8FF'));
            }
            else
            {
                return ((value & 0xFFFF) < 0xFFFE)
                    && !IsInInclusiveRange(value, 0xE0000, 0xE0FFF)
                    && (isQuery || value < 0xF0000);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInInclusiveRange(uint value, uint min, uint max)
            => (value - min) <= (max - min);

        // IRI normalization for strings containing characters that are not allowed or
        // escaped characters that should be unescaped in the context of the specified Uri component.
        public static void EscapeUnescapeIri(ref ValueStringBuilder dest, scoped ReadOnlySpan<char> span, bool isQuery)
        {
            Span<byte> maxUtf8EncodedSpan = stackalloc byte[4];

            for (int i = 0; (uint)i < (uint)span.Length; i++)
            {
                char ch = span[i];

                if (ch == '%')
                {
                    if ((uint)(i + 2) < (uint)span.Length)
                    {
                        ch = UriHelper.DecodeHexChars(span[i + 1], span[i + 2]);

                        // Do not unescape a reserved char
                        if (ch == Uri.c_DummyChar || UriHelper.IsNotSafeForUnescape(ch))
                        {
                            // keep as is
                            dest.Append(span[i]);
                            dest.Append(span[i + 1]);
                            dest.Append(span[i + 2]);
                            i += 2;
                        }
                        else if (ch <= '\x7F')
                        {
                            // ASCII
                            dest.Append(ch);
                            i += 2;
                        }
                        else
                        {
                            // possibly utf8 encoded sequence of unicode
                            int charactersRead = PercentEncodingHelper.UnescapePercentEncodedUTF8Sequence(
                                span.Slice(i),
                                ref dest,
                                isQuery,
                                iriParsing: true);

                            Debug.Assert(charactersRead > 0);
                            i += charactersRead - 1; // -1 as i will be incremented in the loop
                        }

                        continue;
                    }
                }
                else if (ch > '\x7f')
                {
                    // unicode

                    bool isInIriUnicodeRange;
                    bool surrogatePair = false;

                    char ch2 = '\0';

                    if (char.IsHighSurrogate(ch) && (uint)(i + 1) < (uint)span.Length)
                    {
                        ch2 = span[i + 1];
                        isInIriUnicodeRange = CheckIriUnicodeRange(ch, ch2, out surrogatePair, isQuery);
                    }
                    else
                    {
                        isInIriUnicodeRange = CheckIriUnicodeRange(ch, isQuery);
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
                        ReadOnlySpan<byte> encodedBytes = maxUtf8EncodedSpan.Slice(0, bytesWritten);

                        foreach (byte b in encodedBytes)
                        {
                            UriHelper.PercentEncodeByte(b, ref dest);
                        }
                    }

                    if (surrogatePair)
                    {
                        i++;
                    }

                    continue;
                }

                // ASCII, just copy the character
                dest.Append(ch);
            }
        }
    }
}
