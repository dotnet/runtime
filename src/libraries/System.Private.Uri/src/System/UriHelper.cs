// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Diagnostics;

namespace System
{
    internal static class UriHelper
    {
        internal static readonly char[] s_hexUpperChars = {
                                   '0', '1', '2', '3', '4', '5', '6', '7',
                                   '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

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

        // - forceX characters are always escaped if found
        // - rsvd character will remain unescaped
        //
        // start    - starting offset from input
        // end      - the exclusive ending offset in input
        // destPos  - starting offset in dest for output, on return this will be an exclusive "end" in the output.
        //
        // In case "dest" has lack of space it will be reallocated by preserving the _whole_ content up to current destPos
        //
        // Returns null if nothing has to be escaped AND passed dest was null, otherwise the resulting array with the updated destPos
        //
        private const short c_MaxAsciiCharsReallocate = 40;
        private const short c_MaxUnicodeCharsReallocate = 40;
        private const short c_MaxUTF_8BytesPerUnicodeChar = 4;
        private const short c_EncodedCharsPerByte = 3;
        internal static unsafe char[] EscapeString(string input, int start, int end, char[] dest, ref int destPos,
            bool isUriString, char force1, char force2, char rsvd)
        {
            if (end - start >= Uri.c_MaxUriBufferSize)
                throw new UriFormatException(SR.net_uri_SizeLimit);

            int i = start;
            int prevInputPos = start;
            byte* bytes = stackalloc byte[c_MaxUnicodeCharsReallocate * c_MaxUTF_8BytesPerUnicodeChar];   // 40*4=160

            fixed (char* pStr = input)
            {
                for (; i < end; ++i)
                {
                    char ch = pStr[i];

                    // a Unicode ?
                    if (ch > '\x7F')
                    {
                        short maxSize = (short)Math.Min(end - i, (int)c_MaxUnicodeCharsReallocate - 1);

                        short count = 1;
                        for (; count < maxSize && pStr[i + count] > '\x7f'; ++count)
                            ;

                        // Is the last a high surrogate?
                        if (pStr[i + count - 1] >= 0xD800 && pStr[i + count - 1] <= 0xDBFF)
                        {
                            // Should be a rare case where the app tries to feed an invalid Unicode surrogates pair
                            if (count == 1 || count == end - i)
                                throw new UriFormatException(SR.net_uri_BadString);
                            // need to grab one more char as a Surrogate except when it's a bogus input
                            ++count;
                        }

                        dest = EnsureDestinationSize(pStr, dest, i,
                            (short)(count * c_MaxUTF_8BytesPerUnicodeChar * c_EncodedCharsPerByte),
                            c_MaxUnicodeCharsReallocate * c_MaxUTF_8BytesPerUnicodeChar * c_EncodedCharsPerByte,
                            ref destPos, prevInputPos);

                        short numberOfBytes = (short)Encoding.UTF8.GetBytes(pStr + i, count, bytes,
                            c_MaxUnicodeCharsReallocate * c_MaxUTF_8BytesPerUnicodeChar);

                        // This is the only exception that built in UriParser can throw after a Uri ctor.
                        // Should not happen unless the app tries to feed an invalid Unicode String
                        if (numberOfBytes == 0)
                            throw new UriFormatException(SR.net_uri_BadString);

                        i += (count - 1);

                        for (count = 0; count < numberOfBytes; ++count)
                            EscapeAsciiChar((char)bytes[count], dest, ref destPos);

                        prevInputPos = i + 1;
                    }
                    else if (ch == '%' && rsvd == '%')
                    {
                        // Means we don't reEncode '%' but check for the possible escaped sequence
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte,
                            c_MaxAsciiCharsReallocate * c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        if (i + 2 < end && EscapedAscii(pStr[i + 1], pStr[i + 2]) != Uri.c_DummyChar)
                        {
                            // leave it escaped
                            dest[destPos++] = '%';
                            dest[destPos++] = pStr[i + 1];
                            dest[destPos++] = pStr[i + 2];
                            i += 2;
                        }
                        else
                        {
                            EscapeAsciiChar('%', dest, ref destPos);
                        }
                        prevInputPos = i + 1;
                    }
                    else if (ch == force1 || ch == force2)
                    {
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte,
                            c_MaxAsciiCharsReallocate * c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i + 1;
                    }
                    else if (ch != rsvd && (isUriString ? !IsReservedUnreservedOrHash(ch) : !IsUnreserved(ch)))
                    {
                        dest = EnsureDestinationSize(pStr, dest, i, c_EncodedCharsPerByte,
                            c_MaxAsciiCharsReallocate * c_EncodedCharsPerByte, ref destPos, prevInputPos);
                        EscapeAsciiChar(ch, dest, ref destPos);
                        prevInputPos = i + 1;
                    }
                }

                if (prevInputPos != i)
                {
                    // need to fill up the dest array ?
                    if (prevInputPos != start || dest != null)
                        dest = EnsureDestinationSize(pStr, dest, i, 0, 0, ref destPos, prevInputPos);
                }
            }

            return dest;
        }

        //
        // ensure destination array has enough space and contains all the needed input stuff
        //
        private static unsafe char[] EnsureDestinationSize(char* pStr, char[] dest, int currentInputPos,
            short charsToAdd, short minReallocateChars, ref int destPos, int prevInputPos)
        {
            if ((object)dest == null || dest.Length < destPos + (currentInputPos - prevInputPos) + charsToAdd)
            {
                // allocating or reallocating array by ensuring enough space based on maxCharsToAdd.
                char[] newresult = new char[destPos + (currentInputPos - prevInputPos) + minReallocateChars];

                if ((object)dest != null && destPos != 0)
                    Buffer.BlockCopy(dest, 0, newresult, 0, destPos << 1);
                dest = newresult;
            }

            // ensuring we copied everything form the input string left before last escaping
            while (prevInputPos != currentInputPos)
                dest[destPos++] = pStr[prevInputPos++];
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
        internal static unsafe char[] UnescapeString(string input, int start, int end, char[] dest,
            ref int destPosition, char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax,
            bool isQuery)
        {
            fixed (char* pStr = input)
            {
                return UnescapeString(pStr, start, end, dest, ref destPosition, rsvd1, rsvd2, rsvd3, unescapeMode,
                    syntax, isQuery);
            }
        }
        internal static unsafe char[] UnescapeString(char* pStr, int start, int end, char[] dest, ref int destPosition,
            char rsvd1, char rsvd2, char rsvd3, UnescapeMode unescapeMode, UriParser syntax, bool isQuery)
        {
            byte[] bytes = null;
            byte escapedReallocations = 0;
            bool escapeReserved = false;
            int next = start;
            bool iriParsing = Uri.IriParsingStatic(syntax)
                                && ((unescapeMode & UnescapeMode.EscapeUnescape) == UnescapeMode.EscapeUnescape);
            char[] unescapedChars = null;

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
                            if ((object)bytes == null)
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
            to[pos++] = s_hexUpperChars[(ch & 0xf0) >> 4];
            to[pos++] = s_hexUpperChars[ch & 0xf];
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
        private const string RFC3986UnreservedMarks = @"-_.~";
        private const string RFC2396UnreservedMarks = @"-_.~*'()!";
        private const string AdditionalUnsafeToUnescape = @"%\#";// While not specified as reserved, these are still unsafe to unescape.

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

        private static unsafe bool IsReservedUnreservedOrHash(char c)
        {
            if (IsUnreserved(c))
            {
                return true;
            }
            return (RFC3986ReservedMarks.IndexOf(c) >= 0);
        }

        internal static unsafe bool IsUnreserved(char c)
        {
            if (UriHelper.IsAsciiLetterOrDigit(c))
            {
                return true;
            }
            return (RFC3986UnreservedMarks.IndexOf(c) >= 0);
        }

        internal static bool Is3986Unreserved(char c)
        {
            if (UriHelper.IsAsciiLetterOrDigit(c))
            {
                return true;
            }
            return (RFC3986UnreservedMarks.IndexOf(c) >= 0);
        }

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

        //Only consider ASCII characters
        internal static bool IsAsciiLetter(char character)
        {
            return (character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z');
        }

        internal static bool IsAsciiLetterOrDigit(char character)
        {
            return IsAsciiLetter(character) || (character >= '0' && character <= '9');
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
