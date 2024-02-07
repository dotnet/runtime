// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static class UriHelper
    {
        public static unsafe string SpanToLowerInvariantString(ReadOnlySpan<char> span)
        {
#pragma warning disable CS8500 // takes address of managed type
            return string.Create(span.Length, (IntPtr)(&span), static (buffer, spanPtr) =>
            {
                int charsWritten = (*(ReadOnlySpan<char>*)spanPtr).ToLowerInvariant(buffer);
                Debug.Assert(charsWritten == buffer.Length);
            });
#pragma warning restore CS8500
        }

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

        public static bool TryEscapeDataString(ReadOnlySpan<char> charsToEscape, Span<char> destination, out int charsWritten)
        {
            if (destination.Length < charsToEscape.Length)
            {
                charsWritten = 0;
                return false;
            }

            int indexOfFirstToEscape = charsToEscape.IndexOfAnyExcept(Unreserved);
            if (indexOfFirstToEscape < 0)
            {
                // Nothing to escape, just copy the original chars.
                charsToEscape.CopyTo(destination);
                charsWritten = charsToEscape.Length;
                return true;
            }

            // We may throw for very large inputs (when growing the ValueStringBuilder).
            scoped ValueStringBuilder vsb;

            // If the input and destination buffers overlap, we must take care not to overwrite parts of the input before we've processed it.
            bool overlapped = charsToEscape.Overlaps(destination);

            if (overlapped)
            {
                vsb = new ValueStringBuilder(stackalloc char[Uri.StackallocThreshold]);
                vsb.EnsureCapacity(charsToEscape.Length);
            }
            else
            {
                vsb = new ValueStringBuilder(destination.Slice(indexOfFirstToEscape));
            }

            EscapeStringToBuilder(charsToEscape.Slice(indexOfFirstToEscape), ref vsb, Unreserved, checkExistingEscaped: false);

            int newLength = checked(indexOfFirstToEscape + vsb.Length);
            Debug.Assert(newLength > charsToEscape.Length);

            if (destination.Length >= newLength)
            {
                charsToEscape.Slice(0, indexOfFirstToEscape).CopyTo(destination);

                if (overlapped)
                {
                    vsb.AsSpan().CopyTo(destination.Slice(indexOfFirstToEscape));
                    vsb.Dispose();
                }
                else
                {
                    // We are expecting the builder not to grow if the original span was large enough.
                    // This means that we MUST NOT over allocate anywhere in EscapeStringToBuilder (e.g. append and then decrease the length).
                    Debug.Assert(vsb.RawChars.Overlaps(destination));
                }

                charsWritten = newLength;
                return true;
            }

            vsb.Dispose();
            charsWritten = 0;
            return false;
        }

        public static string EscapeString(string stringToEscape, bool checkExistingEscaped, SearchValues<char> noEscape)
        {
            ArgumentNullException.ThrowIfNull(stringToEscape);

            return EscapeString(stringToEscape, checkExistingEscaped, noEscape, stringToEscape);
        }

        public static string EscapeString(ReadOnlySpan<char> charsToEscape, bool checkExistingEscaped, SearchValues<char> noEscape, string? backingString)
        {
            Debug.Assert(!noEscape.Contains('%'), "Need to treat % specially; it should be part of any escaped set");
            Debug.Assert(backingString is null || backingString.Length == charsToEscape.Length);

            int indexOfFirstToEscape = charsToEscape.IndexOfAnyExcept(noEscape);
            if (indexOfFirstToEscape < 0)
            {
                // Nothing to escape, just return the original value.
                return backingString ?? charsToEscape.ToString();
            }

            // Otherwise, create a ValueStringBuilder to store the escaped data into,
            // escape the rest, and concat the result with the characters we skipped above.
            var vsb = new ValueStringBuilder(stackalloc char[Uri.StackallocThreshold]);

            // We may throw for very large inputs (when growing the ValueStringBuilder).
            vsb.EnsureCapacity(charsToEscape.Length);

            EscapeStringToBuilder(charsToEscape.Slice(indexOfFirstToEscape), ref vsb, noEscape, checkExistingEscaped);

            string result = string.Concat(charsToEscape.Slice(0, indexOfFirstToEscape), vsb.AsSpan());
            vsb.Dispose();
            return result;
        }

        internal static unsafe void EscapeString(scoped ReadOnlySpan<char> stringToEscape, ref ValueStringBuilder dest,
            bool checkExistingEscaped, SearchValues<char> noEscape)
        {
            Debug.Assert(!noEscape.Contains('%'), "Need to treat % specially; it should be part of any escaped set");

            int indexOfFirstToEscape = stringToEscape.IndexOfAnyExcept(noEscape);
            if (indexOfFirstToEscape < 0)
            {
                // Nothing to escape, just copy the whole span.
                dest.Append(stringToEscape);
            }
            else
            {
                dest.Append(stringToEscape.Slice(0, indexOfFirstToEscape));

                EscapeStringToBuilder(stringToEscape.Slice(indexOfFirstToEscape), ref dest, noEscape, checkExistingEscaped);
            }
        }

        private static void EscapeStringToBuilder(
            scoped ReadOnlySpan<char> stringToEscape, ref ValueStringBuilder vsb,
            SearchValues<char> noEscape, bool checkExistingEscaped)
        {
            Debug.Assert(!stringToEscape.IsEmpty && !noEscape.Contains(stringToEscape[0]));

            // Allocate enough stack space to hold any Rune's UTF8 encoding.
            Span<byte> utf8Bytes = stackalloc byte[4];

            while (!stringToEscape.IsEmpty)
            {
                char c = stringToEscape[0];

                if (!char.IsAscii(c))
                {
                    if (Rune.DecodeFromUtf16(stringToEscape, out Rune r, out int charsConsumed) != OperationStatus.Done)
                    {
                        r = Rune.ReplacementChar;
                    }

                    Debug.Assert(stringToEscape.EnumerateRunes() is { } e && e.MoveNext() && e.Current == r);
                    Debug.Assert(charsConsumed is 1 or 2);

                    stringToEscape = stringToEscape.Slice(charsConsumed);

                    // The rune is non-ASCII, so encode it as UTF8, and escape each UTF8 byte.
                    r.TryEncodeToUtf8(utf8Bytes, out int bytesWritten);
                    foreach (byte b in utf8Bytes.Slice(0, bytesWritten))
                    {
                        PercentEncodeByte(b, ref vsb);
                    }

                    continue;
                }

                if (!noEscape.Contains(c))
                {
                    // If we're checking for existing escape sequences, then if this is the beginning of
                    // one, check the next two characters in the sequence.
                    if (c == '%' && checkExistingEscaped)
                    {
                        // If the next two characters are valid escaped ASCII, then just output them as-is.
                        if (stringToEscape.Length > 2 && char.IsAsciiHexDigit(stringToEscape[1]) && char.IsAsciiHexDigit(stringToEscape[2]))
                        {
                            vsb.Append('%');
                            vsb.Append(stringToEscape[1]);
                            vsb.Append(stringToEscape[2]);
                            stringToEscape = stringToEscape.Slice(3);
                            continue;
                        }
                    }

                    PercentEncodeByte((byte)c, ref vsb);
                    stringToEscape = stringToEscape.Slice(1);
                    continue;
                }

                // We have a character we don't want to escape. It's likely there are more, do a vectorized search.
                int charsToCopy = stringToEscape.IndexOfAnyExcept(noEscape);
                if (charsToCopy < 0)
                {
                    charsToCopy = stringToEscape.Length;
                }
                Debug.Assert(charsToCopy > 0);

                vsb.Append(stringToEscape.Slice(0, charsToCopy));
                stringToEscape = stringToEscape.Slice(charsToCopy);
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
        internal static unsafe void UnescapeString(scoped ReadOnlySpan<char> input, scoped ref ValueStringBuilder dest,
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

            for (int next = start; next < end;)
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
                        PercentEncodeByte((byte)pStr[next], ref dest);
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

        internal static void PercentEncodeByte(byte b, ref ValueStringBuilder to)
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

        // true for all ASCII letters and digits, as well as the RFC3986 unreserved marks '-', '_', '.', and '~'
        public static readonly SearchValues<char> Unreserved =
            SearchValues.Create("-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz~");

        // true for all ASCII letters and digits, as well as the RFC3986 reserved characters, unreserved characters, and hash
        public static readonly SearchValues<char> UnreservedReserved =
            SearchValues.Create("!#$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

        public static readonly SearchValues<char> UnreservedReservedExceptHash =
            SearchValues.Create("!$&'()*+,-./0123456789:;=?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

        public static readonly SearchValues<char> UnreservedReservedExceptQuestionMarkHash =
            SearchValues.Create("!$&'()*+,-./0123456789:;=@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]_abcdefghijklmnopqrstuvwxyz~");

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

        // Is this a Bidirectional control char.. These get stripped
        internal static bool IsBidiControlCharacter(char ch) =>
            char.IsBetween(ch, '\u200E', '\u202E') && !char.IsBetween(ch, '\u2010', '\u2029');

        // Strip Bidirectional control characters from this string
        internal static unsafe string StripBidiControlCharacters(ReadOnlySpan<char> strToClean, string? backingString = null)
        {
            Debug.Assert(backingString is null || strToClean.Length == backingString.Length);

            int charsToRemove = 0;

            int indexOfPossibleCharToRemove = strToClean.IndexOfAnyInRange('\u200E', '\u202E');
            if (indexOfPossibleCharToRemove >= 0)
            {
                // Slow path: Contains chars that fall in the [u200E, u202E] range (so likely Bidi)
                foreach (char c in strToClean.Slice(indexOfPossibleCharToRemove))
                {
                    if (IsBidiControlCharacter(c))
                    {
                        charsToRemove++;
                    }
                }
            }

            if (charsToRemove == 0)
            {
                // Hot path
                return backingString ?? new string(strToClean);
            }

#pragma warning disable CS8500 // takes address of managed type
            ReadOnlySpan<char> tmpStrToClean = strToClean; // avoid address exposing the span and impacting the other code in the method that uses it
            return string.Create(tmpStrToClean.Length - charsToRemove, (IntPtr)(&tmpStrToClean), static (buffer, strToCleanPtr) =>
            {
                int destIndex = 0;
                foreach (char c in *(ReadOnlySpan<char>*)strToCleanPtr)
                {
                    if (!IsBidiControlCharacter(c))
                    {
                        buffer[destIndex++] = c;
                    }
                }
                Debug.Assert(buffer.Length == destIndex);
            });
#pragma warning restore CS8500
        }
    }
}
