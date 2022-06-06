// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    internal static class UriHelper
    {
        // http://host/Path/Path/File?Query is the base of
        //      - http://host/Path/Path/File/ ...    (those "File" words may be different in semantic but anyway)
        //      - http://host/Path/Path/#Fragment
        //      - http://host/Path/Path/?Query
        //      - http://host/Path/Path/MoreDir/ ...
        //      - http://host/Path/Path/OtherFile?Query
        //      - http://host/Path/Path/Fl
        //      - http://host/Path/Path/
        //
        //  It is not a base for
        //      - http://host/Path/Path         (that last "Path" is not considered as a directory)
        //      - http://host/Path/Path?Query
        //      - http://host/Path/Path#Fragment
        //      - http://host/Path/Path2/
        //      - http://host/Path/Path2/MoreDir
        //      - http://host/Path/File
        //
        // ASSUMES that strings like http://host/Path/Path/MoreDir/../../  have been canonicalized before going to this method.
        // ASSUMES that back slashes already have been converted if applicable.
        //
        internal static unsafe bool TestForSubPath(char* selfPtr, int selfLength, char* otherPtr, int otherLength,
            bool ignoreCase)
        {
            int i = 0;
            char chSelf;
            char chOther;

            bool AllSameBeforeSlash = true;

            for (; i < selfLength && i < otherLength; ++i)
            {
                chSelf = *(selfPtr + i);
                chOther = *(otherPtr + i);

                if (chSelf == '?' || chSelf == '#')
                {
                    // survived so far and selfPtr does not have any more path segments
                    return true;
                }

                // If selfPtr terminates a path segment, so must otherPtr
                if (chSelf == '/')
                {
                    if (chOther != '/')
                    {
                        // comparison has failed
                        return false;
                    }
                    // plus the segments must be the same
                    if (!AllSameBeforeSlash)
                    {
                        // comparison has failed
                        return false;
                    }
                    //so far so good
                    AllSameBeforeSlash = true;
                    continue;
                }

                // if otherPtr terminates then selfPtr must not have any more path segments
                if (chOther == '?' || chOther == '#')
                {
                    break;
                }

                if (!ignoreCase)
                {
                    if (chSelf != chOther)
                    {
                        AllSameBeforeSlash = false;
                    }
                }
                else
                {
                    if (char.ToLowerInvariant(chSelf) != char.ToLowerInvariant(chOther))
                    {
                        AllSameBeforeSlash = false;
                    }
                }
            }

            // If self is longer then it must not have any more path segments
            for (; i < selfLength; ++i)
            {
                if ((chSelf = *(selfPtr + i)) == '?' || chSelf == '#')
                {
                    return true;
                }
                if (chSelf == '/')
                {
                    return false;
                }
            }
            //survived by getting to the end of selfPtr
            return true;
        }

        internal static string EscapeString(
            string stringToEscape, // same name as public API
            bool checkExistingEscaped, ReadOnlySpan<bool> unreserved, char forceEscape1 = '\0', char forceEscape2 = '\0')
        {
            ArgumentNullException.ThrowIfNull(stringToEscape);

            if (stringToEscape.Length == 0)
            {
                return string.Empty;
            }

            // Get the table of characters that do not need to be escaped.
            Debug.Assert(unreserved.Length == 0x80);
            ReadOnlySpan<bool> noEscape = stackalloc bool[0];
            if ((forceEscape1 | forceEscape2) == 0)
            {
                noEscape = unreserved;
            }
            else
            {
                Span<bool> tmp = stackalloc bool[0x80];
                unreserved.CopyTo(tmp);
                tmp[forceEscape1] = false;
                tmp[forceEscape2] = false;
                noEscape = tmp;
            }

            // If the whole string is made up of ASCII unreserved chars, just return it.
            Debug.Assert(!noEscape['%'], "Need to treat % specially; it should be part of any escaped set");
            int i = 0;
            char c;
            for (; i < stringToEscape.Length && (c = stringToEscape[i]) <= 0x7F && noEscape[c]; i++) ;
            if (i == stringToEscape.Length)
            {
                return stringToEscape;
            }

            // Otherwise, create a ValueStringBuilder to store the escaped data into,
            // append to it all of the noEscape chars we already iterated through,
            // escape the rest, and return the result as a string.
            var vsb = new ValueStringBuilder(stackalloc char[Uri.StackallocThreshold]);
            vsb.Append(stringToEscape.AsSpan(0, i));
            EscapeStringToBuilder(stringToEscape.AsSpan(i), ref vsb, noEscape, checkExistingEscaped);
            return vsb.ToString();
        }

        internal static unsafe void EscapeString(ReadOnlySpan<char> stringToEscape, ref ValueStringBuilder dest,
            bool checkExistingEscaped, char forceEscape1 = '\0', char forceEscape2 = '\0')
        {
            // Get the table of characters that do not need to be escaped.
            ReadOnlySpan<bool> noEscape = stackalloc bool[0];
            if ((forceEscape1 | forceEscape2) == 0)
            {
                noEscape = UnreservedReservedTable;
            }
            else
            {
                Span<bool> tmp = stackalloc bool[0x80];
                UnreservedReservedTable.CopyTo(tmp);
                tmp[forceEscape1] = false;
                tmp[forceEscape2] = false;
                noEscape = tmp;
            }

            // If the whole string is made up of ASCII unreserved chars, take a fast pasth.  Per the contract, if
            // dest is null, just return it.  If it's not null, copy everything to it and update destPos accordingly;
            // if that requires resizing it, do so.
            Debug.Assert(!noEscape['%'], "Need to treat % specially in case checkExistingEscaped is true");
            int i = 0;
            char c;
            for (; i < stringToEscape.Length && (c = stringToEscape[i]) <= 0x7F && noEscape[c]; i++) ;
            if (i == stringToEscape.Length)
            {
                dest.Append(stringToEscape);
            }
            else
            {
                dest.Append(stringToEscape.Slice(0, i));

                // CS8350 & CS8352: We can't pass `noEscape` and `dest` as arguments together as that could leak the scope of the above stackalloc
                // As a workaround, re-create the Span in a way that avoids analysis
                ReadOnlySpan<bool> noEscapeCopy = MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetReference(noEscape), noEscape.Length);

                EscapeStringToBuilder(stringToEscape.Slice(i), ref dest, noEscapeCopy, checkExistingEscaped);
            }
        }

        private static void EscapeStringToBuilder(
            ReadOnlySpan<char> stringToEscape, ref ValueStringBuilder vsb,
            ReadOnlySpan<bool> noEscape, bool checkExistingEscaped)
        {
            // Allocate enough stack space to hold any Rune's UTF8 encoding.
            Span<byte> utf8Bytes = stackalloc byte[4];

            // Then enumerate every rune in the input.
            SpanRuneEnumerator e = stringToEscape.EnumerateRunes();
            while (e.MoveNext())
            {
                Rune r = e.Current;

                if (!r.IsAscii)
                {
                    // The rune is non-ASCII, so encode it as UTF8, and escape each UTF8 byte.
                    r.TryEncodeToUtf8(utf8Bytes, out int bytesWritten);
                    foreach (byte b in utf8Bytes.Slice(0, bytesWritten))
                    {
                        vsb.Append('%');
                        HexConverter.ToCharsBuffer(b, vsb.AppendSpan(2), 0, HexConverter.Casing.Upper);
                    }
                    continue;
                }

                // If the value doesn't need to be escaped, append it and continue.
                byte value = (byte)r.Value;
                if (noEscape[value])
                {
                    vsb.Append((char)value);
                    continue;
                }

                // If we're checking for existing escape sequences, then if this is the beginning of
                // one, check the next two characters in the sequence.  This is a little tricky to do
                // as we're using an enumerator, but luckily it's a ref struct-based enumerator: we can
                // make a copy and iterate through the copy without impacting the original, and then only
                // push the original ahead if we find what we're looking for in the copy.
                if (checkExistingEscaped && value == '%')
                {
                    // If the next two characters are valid escaped ASCII, then just output them as-is.
                    SpanRuneEnumerator tmpEnumerator = e;
                    if (tmpEnumerator.MoveNext())
                    {
                        Rune r1 = tmpEnumerator.Current;
                        if (r1.IsAscii && char.IsAsciiHexDigit((char)r1.Value) && tmpEnumerator.MoveNext())
                        {
                            Rune r2 = tmpEnumerator.Current;
                            if (r2.IsAscii && char.IsAsciiHexDigit((char)r2.Value))
                            {
                                vsb.Append('%');
                                vsb.Append((char)r1.Value);
                                vsb.Append((char)r2.Value);
                                e = tmpEnumerator;
                                continue;
                            }
                        }
                    }
                }

                // Otherwise, append the escaped character.
                vsb.Append('%');
                HexConverter.ToCharsBuffer(value, vsb.AppendSpan(2), 0, HexConverter.Casing.Upper);
            }
        }

        internal static unsafe char[] UnescapeString(string input, int start, int end, char[] dest,
            ref int destPosition, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser? syntax,
            bool isQuery)
        {
            fixed (char* pStr = input)
            {
                return UnescapeString(pStr, start, end, dest, ref destPosition, rsvd1, rsvd2, rsvd3, unescapeMode,
                    syntax, isQuery);
            }
        }

        internal static unsafe char[] UnescapeString(char* pStr, int start, int end, char[] dest, ref int destPosition,
            char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser? syntax, bool isQuery)
        {
            ValueStringBuilder vsb = new ValueStringBuilder(dest.Length);
            vsb.Append(dest.AsSpan(0, destPosition));
            UnescapeString(pStr, start, end, ref vsb, rsvd1, rsvd2, rsvd3, unescapeMode,
                    syntax, isQuery);

            if (vsb.Length > dest.Length)
            {
                dest = vsb.AsSpan().ToArray();
            }
            else
            {
                vsb.AsSpan(destPosition).TryCopyTo(dest.AsSpan(destPosition));
            }
            destPosition = vsb.Length;
            vsb.Dispose();
            return dest;
        }

        //
        // This method will assume that any good Escaped Sequence will be unescaped in the output
        // - Assumes Dest.Length - detPosition >= end-start
        // - UnescapeLevel controls various modes of operation
        // - Any "bad" escape sequence will remain as is or '%' will be escaped.
        // - destPosition tells the starting index in dest for placing the result.
        //   On return destPosition tells the last character + 1 position in the "dest" array.
        // - The control chars and chars passed in rsdvX parameters may be re-escaped depending on UnescapeLevel
        // - It is a RARE case when Unescape actually needs escaping some characters mentioned above.
        //   For this reason it returns a char[] that is usually the same ref as the input "dest" value.
        //
        internal static unsafe void UnescapeString(string input, int start, int end, ref ValueStringBuilder dest,
            char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser? syntax, bool isQuery)
        {
            fixed (char* pStr = input)
            {
                UnescapeString(pStr, start, end, ref dest, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
            }
        }
        internal static unsafe void UnescapeString(ReadOnlySpan<char> input, ref ValueStringBuilder dest,
           char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser? syntax, bool isQuery)
        {
            fixed (char* pStr = &MemoryMarshal.GetReference(input))
            {
                UnescapeString(pStr, 0, input.Length, ref dest, rsvd1, rsvd2, rsvd3, unescapeMode, syntax, isQuery);
            }
        }
        internal static unsafe void UnescapeString(char* pStr, int start, int end, ref ValueStringBuilder dest,
            char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser? syntax, bool isQuery)
        {
            if ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.CopyOnly)
            {
                dest.Append(pStr + start, end - start);
                return;
            }

            bool escapeReserved = false;
            bool iriParsing = Uri.IriParsingStatic(syntax)
                                && ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.EscapeUnescape);

            for (int next = start; next < end; )
            {
                char ch = (char)0;

                for (; next < end; ++next)
                {
                    if ((ch = pStr[next]) == '%')
                    {
                        if ((unescapeMode & UnescapeMode.Unescape) == 0)
                        {
                            // re-escape, don't check anything else
                            escapeReserved = true;
                        }
                        else if (next + 2 < end)
                        {
                            ch = DecodeHexChars(pStr[next + 1], pStr[next + 2]);
                            // Unescape a good sequence if full unescape is requested
                            if (unescapeMode >= UnescapeMode.UnescapeAll)
                            {
                                if (ch == Uri.c_DummyChar)
                                {
                                    if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow)
                                    {
                                        // Should be a rare case where the app tries to feed an invalid escaped sequence
                                        throw new UriFormatException(SR.net_uri_BadString);
                                    }
                                    continue;
                                }
                            }
                            // re-escape % from an invalid sequence
                            else if (ch == Uri.c_DummyChar)
                            {
                                if ((unescapeMode & UnescapeMode.Escape) != 0)
                                    escapeReserved = true;
                                else
                                    continue;   // we should throw instead but since v1.0 would just print '%'
                            }
                            // Do not unescape '%' itself unless full unescape is requested
                            else if (ch == '%')
                            {
                                next += 2;
                                continue;
                            }
                            // Do not unescape a reserved char unless full unescape is requested
                            else if (ch == rsvd1 || ch == rsvd2 || ch == rsvd3)
                            {
                                next += 2;
                                continue;
                            }
                            // Do not unescape a dangerous char unless it's V1ToStringFlags mode
                            else if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0 && IsNotSafeForUnescape(ch))
                            {
                                next += 2;
                                continue;
                            }
                            else if (iriParsing && ((ch <= '\x9F' && IsNotSafeForUnescape(ch)) ||
                                                    (ch > '\x9F' && !IriHelper.CheckIriUnicodeRange(ch, isQuery))))
                            {
                                // check if unenscaping gives a char outside iri range
                                // if it does then keep it escaped
                                next += 2;
                                continue;
                            }
                            // unescape escaped char or escape %
                            break;
                        }
                        else if (unescapeMode >= UnescapeMode.UnescapeAll)
                        {
                            if (unescapeMode >= UnescapeMode.UnescapeAllOrThrow)
                            {
                                // Should be a rare case where the app tries to feed an invalid escaped sequence
                                throw new UriFormatException(SR.net_uri_BadString);
                            }
                            // keep a '%' as part of a bogus sequence
                            continue;
                        }
                        else
                        {
                            escapeReserved = true;
                        }
                        // escape (escapeReserved==true) or otherwise unescape the sequence
                        break;
                    }
                    else if ((unescapeMode & (UnescapeMode.Unescape | UnescapeMode.UnescapeAll))
                        == (UnescapeMode.Unescape | UnescapeMode.UnescapeAll))
                    {
                        continue;
                    }
                    else if ((unescapeMode & UnescapeMode.Escape) != 0)
                    {
                        // Could actually escape some of the characters
                        if (ch == rsvd1 || ch == rsvd2 || ch == rsvd3)
                        {
                            // found an unescaped reserved character -> escape it
                            escapeReserved = true;
                            break;
                        }
                        else if ((unescapeMode & UnescapeMode.V1ToStringFlag) == 0
                            && (ch <= '\x1F' || (ch >= '\x7F' && ch <= '\x9F')))
                        {
                            // found an unescaped reserved character -> escape it
                            escapeReserved = true;
                            break;
                        }
                    }
                }

                //copy off previous characters from input
                while (start < next)
                    dest.Append(pStr[start++]);

                if (next != end)
                {
                    if (escapeReserved)
                    {
                        EscapeAsciiChar((byte)pStr[next], ref dest);
                        escapeReserved = false;
                        next++;
                    }
                    else if (ch <= 127)
                    {
                        dest.Append(ch);
                        next += 3;
                    }
                    else
                    {
                        // Unicode
                        int charactersRead = PercentEncodingHelper.UnescapePercentEncodedUTF8Sequence(
                            pStr + next,
                            end - next,
                            ref dest,
                            isQuery,
                            iriParsing);

                        Debug.Assert(charactersRead > 0);
                        next += charactersRead;
                    }

                    start = next;
                }
            }
        }

        internal static void EscapeAsciiChar(byte b, ref ValueStringBuilder to)
        {
            to.Append('%');
            HexConverter.ToCharsBuffer(b, to.AppendSpan(2), 0, HexConverter.Casing.Upper);
        }

        /// <summary>
        /// Converts 2 hex chars to a byte (returned in a char), e.g, "0a" becomes (char)0x0A.
        /// <para>If either char is not hex, returns <see cref="Uri.c_DummyChar"/>.</para>
        /// </summary>
        internal static char DecodeHexChars(int first, int second)
        {
            int a = HexConverter.FromChar(first);
            int b = HexConverter.FromChar(second);

            if ((a | b) == 0xFF)
            {
                // either a or b is 0xFF (invalid)
                return Uri.c_DummyChar;
            }

            return (char)((a << 4) | b);
        }

        internal const string RFC3986ReservedMarks = @";/?:@&=+$,#[]!'()*";
        private const string AdditionalUnsafeToUnescape = @"%\#"; // While not specified as reserved, these are still unsafe to unescape.

        // When unescaping in safe mode, do not unescape the RFC 3986 reserved set:
        // gen-delims  = ":" / "/" / "?" / "#" / "[" / "]" / "@"
        // sub-delims  = "!" / "$" / "&" / "'" / "(" / ")"
        //             / "*" / "+" / "," / ";" / "="
        //
        // In addition, do not unescape the following unsafe characters:
        // excluded    = "%" / "\"
        //
        // This implementation used to use the following variant of the RFC 2396 reserved set.
        // That behavior is now disabled by default, and is controlled by a UriSyntax property.
        // reserved    = ";" | "/" | "?" | "@" | "&" | "=" | "+" | "$" | ","
        // excluded    = control | "#" | "%" | "\"
        internal static bool IsNotSafeForUnescape(char ch)
        {
            if (ch <= '\x1F' || (ch >= '\x7F' && ch <= '\x9F'))
            {
                return true;
            }

            const string NotSafeForUnescape = RFC3986ReservedMarks + AdditionalUnsafeToUnescape;

            return NotSafeForUnescape.Contains(ch);
        }

        // "Reserved" and "Unreserved" characters are based on RFC 3986.

        internal static ReadOnlySpan<bool> UnreservedReservedTable => new bool[0x80]
        {
            // true for all ASCII letters and digits, as well as the RFC3986 reserved characters, unreserved characters, and hash
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, true,  false, true,  true,  false, true,  true,  true,  true,  true,  true,  true,  true,  true,  true,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, true,  false, true,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, true,  false, true,
            false, true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, false, false, true,  false,
        };

        internal static bool IsUnreserved(int c) => c < 0x80 && UnreservedTable[c];

        internal static ReadOnlySpan<bool> UnreservedTable => new bool[0x80]
        {
            // true for all ASCII letters and digits, as well as the RFC3986 unreserved marks '-', '_', '.', and '~'
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false,
            false, false, false, false, false, false, false, false, false, false, false, false, false, true,  true,  false,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, false, false, false, false, false,
            false, true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, false, false, false, true,
            false, true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,
            true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  true,  false, false, false, true,  false,
        };

        //
        // Is this a gen delim char from RFC 3986
        //
        internal static bool IsGenDelim(char ch)
        {
            return (ch == ':' || ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@');
        }

        internal static readonly char[] s_WSchars = new char[] { ' ', '\n', '\r', '\t' };

        internal static bool IsLWS(char ch)
        {
            return (ch <= ' ') && (ch == ' ' || ch == '\n' || ch == '\r' || ch == '\t');
        }

        //
        // Is this a Bidirectional control char.. These get stripped
        //
        internal static bool IsBidiControlCharacter(char ch)
        {
            return (ch == '\u200E' /*LRM*/ || ch == '\u200F' /*RLM*/ || ch == '\u202A' /*LRE*/ ||
                    ch == '\u202B' /*RLE*/ || ch == '\u202C' /*PDF*/ || ch == '\u202D' /*LRO*/ ||
                    ch == '\u202E' /*RLO*/);
        }

        //
        // Strip Bidirectional control characters from this string
        //
        internal static unsafe string StripBidiControlCharacters(ReadOnlySpan<char> strToClean, string? backingString = null)
        {
            Debug.Assert(backingString is null || strToClean.Length == backingString.Length);

            int charsToRemove = 0;
            foreach (char c in strToClean)
            {
                if ((uint)(c - '\u200E') <= ('\u202E' - '\u200E') && IsBidiControlCharacter(c))
                {
                    charsToRemove++;
                }
            }

            if (charsToRemove == 0)
            {
                return backingString ?? new string(strToClean);
            }

            if (charsToRemove == strToClean.Length)
            {
                return string.Empty;
            }

            fixed (char* pStrToClean = &MemoryMarshal.GetReference(strToClean))
            {
                return string.Create(strToClean.Length - charsToRemove, (StrToClean: (IntPtr)pStrToClean, strToClean.Length), (buffer, state) =>
                {
                    var strToClean = new ReadOnlySpan<char>((char*)state.StrToClean, state.Length);
                    int destIndex = 0;
                    foreach (char c in strToClean)
                    {
                        if ((uint)(c - '\u200E') > ('\u202E' - '\u200E') || !IsBidiControlCharacter(c))
                        {
                            buffer[destIndex++] = c;
                        }
                    }
                    Debug.Assert(buffer.Length == destIndex);
                });
            }
        }
    }
}
