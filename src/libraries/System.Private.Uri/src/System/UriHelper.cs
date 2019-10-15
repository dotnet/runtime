// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    internal static class UriHelper
    {
        internal static ReadOnlySpan<byte> HexUpperChars => new byte[16]
        {
            (byte)'0', (byte)'1', (byte)'2', (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F'
        };

        internal static readonly Encoding s_noFallbackCharUTF8 = Encoding.GetEncoding(
            Encoding.UTF8.CodePage, new EncoderReplacementFallback(""), new DecoderReplacementFallback(""));

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
        internal static unsafe bool TestForSubPath(char* selfPtr, ushort selfLength, char* otherPtr, ushort otherLength,
            bool ignoreCase)
        {
            ushort i = 0;
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
            if (stringToEscape is null)
            {
                throw new ArgumentNullException(nameof(stringToEscape));
            }
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
            var vsb = new ValueStringBuilder(stackalloc char[256]);
            vsb.Append(stringToEscape.AsSpan(0, i));
            EscapeStringToBuilder(stringToEscape.AsSpan(i), ref vsb, noEscape, checkExistingEscaped);
            return vsb.ToString();
        }

        // forceX characters are always escaped if found
        // destPos  - starting offset in dest for output, on return this will be an exclusive "end" in the output.
        // In case "dest" has lack of space it will be reallocated by preserving the _whole_ content up to current destPos
        // Returns null if nothing has to be escaped AND passed dest was null, otherwise the resulting array with the updated destPos
        [return: NotNullIfNotNull("dest")]
        internal static char[]? EscapeString(
            ReadOnlySpan<char> stringToEscape,
            char[]? dest, ref int destPos,
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
                if (dest != null)
                {
                    EnsureCapacity(dest, destPos, stringToEscape.Length);
                    stringToEscape.CopyTo(dest.AsSpan(destPos));
                    destPos += stringToEscape.Length;
                }

                return dest;
            }

            // Otherwise, create a ValueStringBuilder to store the escaped data into,
            // append to it all of the noEscape chars we already iterated through, and
            // escape the rest into the ValueStringBuilder.
            var vsb = new ValueStringBuilder(stackalloc char[256]);
            vsb.Append(stringToEscape.Slice(0, i));
            EscapeStringToBuilder(stringToEscape.Slice(i), ref vsb, noEscape, checkExistingEscaped);

            // Finally update dest with the result.
            EnsureCapacity(dest, destPos, vsb.Length);
            vsb.TryCopyTo(dest.AsSpan(destPos), out int charsWritten);
            destPos += charsWritten;
            return dest;

            static void EnsureCapacity(char[]? dest, int destSize, int requiredSize)
            {
                if (dest == null || dest.Length - destSize < requiredSize)
                {
                    Array.Resize(ref dest, destSize + requiredSize + 120); // 120 == arbitrary minimum-empty space copied from previous implementation
                }
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
                        vsb.Append((char)HexUpperChars[(b & 0xf0) >> 4]);
                        vsb.Append((char)HexUpperChars[b & 0xf]);
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
                        if (r1.IsAscii && IsHexDigit((char)r1.Value) && tmpEnumerator.MoveNext())
                        {
                            Rune r2 = tmpEnumerator.Current;
                            if (r2.IsAscii && IsHexDigit((char)r2.Value))
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
                vsb.Append((char)HexUpperChars[(value & 0xf0) >> 4]);
                vsb.Append((char)HexUpperChars[value & 0xf]);
            }
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
            byte[]? bytes = null;
            byte escapedReallocations = 0;
            bool escapeReserved = false;
            int next = start;
            bool iriParsing = Uri.IriParsingStatic(syntax)
                                && ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.EscapeUnescape);
            char[]? unescapedChars = null;

            while (true)
            {
                // we may need to re-pin dest[]
                fixed (char* pDest = dest)
                {
                    if ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.CopyOnly)
                    {
                        while (start < end)
                            pDest[destPosition++] = pStr[start++];
                        return dest;
                    }

                    while (true)
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
                                    ch = EscapedAscii(pStr[next + 1], pStr[next + 2]);
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
                            pDest[destPosition++] = pStr[start++];

                        if (next != end)
                        {
                            if (escapeReserved)
                            {
                                //escape that char
                                // Since this should be _really_ rare case, reallocate with constant size increase of 30 rsvd-type characters.
                                if (escapedReallocations == 0)
                                {
                                    escapedReallocations = 30;
                                    char[] newDest = new char[dest.Length + escapedReallocations * 3];
                                    fixed (char* pNewDest = &newDest[0])
                                    {
                                        for (int i = 0; i < destPosition; ++i)
                                            pNewDest[i] = pDest[i];
                                    }
                                    dest = newDest;
                                    // re-pin new dest[] array
                                    goto dest_fixed_loop_break;
                                }
                                else
                                {
                                    --escapedReallocations;
                                    EscapeAsciiChar(pStr[next], dest, ref destPosition);
                                    escapeReserved = false;
                                    start = ++next;
                                    continue;
                                }
                            }

                            // unescaping either one Ascii or possibly multiple Unicode

                            if (ch <= '\x7F')
                            {
                                //ASCII
                                dest[destPosition++] = ch;
                                next += 3;
                                start = next;
                                continue;
                            }

                            // Unicode

                            int byteCount = 1;
                            // lazy initialization of max size, will reuse the array for next sequences
                            if ((object?)bytes == null)
                                bytes = new byte[end - next];

                            bytes[0] = (byte)ch;
                            next += 3;
                            while (next < end)
                            {
                                // Check on exit criterion
                                if ((ch = pStr[next]) != '%' || next + 2 >= end)
                                    break;

                                // already made sure we have 3 characters in str
                                ch = EscapedAscii(pStr[next + 1], pStr[next + 2]);

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
                            }

                            if (unescapedChars == null || unescapedChars.Length < bytes.Length)
                            {
                                unescapedChars = new char[bytes.Length];
                            }

                            int charCount = s_noFallbackCharUTF8.GetChars(bytes, 0, byteCount, unescapedChars, 0);

                            start = next;

                            // match exact bytes
                            // Do not unescape chars not allowed by Iri
                            // need to check for invalid utf sequences that may not have given any chars

                            MatchUTF8Sequence(pDest, dest, ref destPosition, unescapedChars.AsSpan(0, charCount), charCount, bytes,
                                byteCount, isQuery, iriParsing);
                        }

                        if (next == end)
                            goto done;
                    }
                dest_fixed_loop_break:;
                }
            }

        done: return dest;
        }

        //
        // Need to check for invalid utf sequences that may not have given any chars.
        // We got the unescaped chars, we then re-encode them and match off the bytes
        // to get the invalid sequence bytes that we just copy off
        //
        internal static unsafe void MatchUTF8Sequence(char* pDest, char[] dest, ref int destOffset, Span<char> unescapedChars,
            int charCount, byte[] bytes, int byteCount, bool isQuery, bool iriParsing)
        {
            Span<byte> maxUtf8EncodedSpan = stackalloc byte[4];

            int count = 0;
            fixed (char* unescapedCharsPtr = unescapedChars)
            {
                for (int j = 0; j < charCount; ++j)
                {
                    bool isHighSurr = char.IsHighSurrogate(unescapedCharsPtr[j]);
                    Span<byte> encodedBytes = maxUtf8EncodedSpan;
                    int bytesWritten = Encoding.UTF8.GetBytes(unescapedChars.Slice(j, isHighSurr ? 2 : 1), encodedBytes);
                    encodedBytes = encodedBytes.Slice(0, bytesWritten);

                    // we have to keep unicode chars outside Iri range escaped
                    bool inIriRange = false;
                    if (iriParsing)
                    {
                        if (!isHighSurr)
                            inIriRange = IriHelper.CheckIriUnicodeRange(unescapedChars[j], isQuery);
                        else
                        {
                            bool surrPair = false;
                            inIriRange = IriHelper.CheckIriUnicodeRange(unescapedChars[j], unescapedChars[j + 1],
                                                                   ref surrPair, isQuery);
                        }
                    }

                    while (true)
                    {
                        // Escape any invalid bytes that were before this character
                        while (bytes[count] != encodedBytes[0])
                        {
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            EscapeAsciiChar((char)bytes[count++], dest, ref destOffset);
                        }

                        // check if all bytes match
                        bool allBytesMatch = true;
                        int k = 0;
                        for (; k < encodedBytes.Length; ++k)
                        {
                            if (bytes[count + k] != encodedBytes[k])
                            {
                                allBytesMatch = false;
                                break;
                            }
                        }

                        if (allBytesMatch)
                        {
                            count += encodedBytes.Length;
                            if (iriParsing)
                            {
                                if (!inIriRange)
                                {
                                    // need to keep chars not allowed as escaped
                                    for (int l = 0; l < encodedBytes.Length; ++l)
                                    {
                                        Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                        EscapeAsciiChar((char)encodedBytes[l], dest, ref destOffset);
                                    }
                                }
                                else if (!UriHelper.IsBidiControlCharacter(unescapedCharsPtr[j]) || !UriParser.DontKeepUnicodeBidiFormattingCharacters)
                                {
                                    //copy chars
                                    Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                    pDest[destOffset++] = unescapedCharsPtr[j];
                                    if (isHighSurr)
                                    {
                                        Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                        pDest[destOffset++] = unescapedCharsPtr[j + 1];
                                    }
                                }
                            }
                            else
                            {
                                //copy chars
                                Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                pDest[destOffset++] = unescapedCharsPtr[j];

                                if (isHighSurr)
                                {
                                    Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                    pDest[destOffset++] = unescapedCharsPtr[j + 1];
                                }
                            }

                            break; // break out of while (true) since we've matched this char bytes
                        }
                        else
                        {
                            // copy bytes till place where bytes don't match
                            for (int l = 0; l < k; ++l)
                            {
                                Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                EscapeAsciiChar((char)bytes[count++], dest, ref destOffset);
                            }
                        }
                    }

                    if (isHighSurr) j++;
                }
            }

            // Include any trailing invalid sequences
            while (count < byteCount)
            {
                Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                EscapeAsciiChar((char)bytes[count++], dest, ref destOffset);
            }
        }

        internal static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = (char)HexUpperChars[(ch & 0xf0) >> 4];
            to[pos++] = (char)HexUpperChars[ch & 0xf];
        }

        internal static char EscapedAscii(char digit, char next)
        {
            if (!(((digit >= '0') && (digit <= '9'))
                || ((digit >= 'A') && (digit <= 'F'))
                || ((digit >= 'a') && (digit <= 'f'))))
            {
                return Uri.c_DummyChar;
            }

            int res = (digit <= '9')
                ? ((int)digit - (int)'0')
                : (((digit <= 'F')
                ? ((int)digit - (int)'A')
                : ((int)digit - (int)'a'))
                   + 10);

            if (!(((next >= '0') && (next <= '9'))
                || ((next >= 'A') && (next <= 'F'))
                || ((next >= 'a') && (next <= 'f'))))
            {
                return Uri.c_DummyChar;
            }

            return (char)((res << 4) + ((next <= '9')
                    ? ((int)next - (int)'0')
                    : (((next <= 'F')
                        ? ((int)next - (int)'A')
                        : ((int)next - (int)'a'))
                       + 10)));
        }

        internal const string RFC3986ReservedMarks = @";/?:@&=+$,#[]!'()*";
        private const string RFC2396ReservedMarks = @";/?:@&=+$,";
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
            else if (UriParser.DontEnableStrictRFC3986ReservedCharacterSets)
            {
                if ((ch != ':' && (RFC2396ReservedMarks.IndexOf(ch) >= 0) || (AdditionalUnsafeToUnescape.IndexOf(ch) >= 0)))
                {
                    return true;
                }
            }
            else if ((RFC3986ReservedMarks.IndexOf(ch) >= 0) || (AdditionalUnsafeToUnescape.IndexOf(ch) >= 0))
            {
                return true;
            }
            return false;
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

        internal static bool IsAsciiLetter(char character) =>
            (((uint)character - 'A') & ~0x20) < 26;

        internal static bool IsAsciiLetterOrDigit(char character) =>
            ((((uint)character - 'A') & ~0x20) < 26) ||
            (((uint)character - '0') < 10);

        internal static bool IsHexDigit(char character) =>
            ((((uint)character - 'A') & ~0x20) < 6) ||
            (((uint)character - '0') < 10);

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
        internal static unsafe string StripBidiControlCharacter(char* strToClean, int start, int length)
        {
            if (length <= 0) return "";

            char[] cleanStr = new char[length];
            int count = 0;
            for (int i = 0; i < length; ++i)
            {
                char c = strToClean[start + i];
                if (c < '\u200E' || c > '\u202E' || !IsBidiControlCharacter(c))
                {
                    cleanStr[count++] = c;
                }
            }
            return new string(cleanStr, 0, count);
        }
    }
}
