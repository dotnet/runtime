// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static class IriHelper
    {
        //
        // Checks if provided non surrogate char lies in iri range
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
        //
        internal static bool CheckIriUnicodeRange(char highSurr, char lowSurr, ref bool surrogatePair, bool isQuery)
        {
            bool inRange = false;
            surrogatePair = false;

            Debug.Assert(char.IsHighSurrogate(highSurr));

            if (char.IsSurrogatePair(highSurr, lowSurr))
            {
                surrogatePair = true;
                char[] chars = new char[2] { highSurr, lowSurr };
                string surrPair = new string(chars);
                if (((string.CompareOrdinal(surrPair, "\U00010000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0001FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00020000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0002FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00030000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0003FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00040000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0004FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00050000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0005FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00060000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0006FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00070000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0007FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00080000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0008FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U00090000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U0009FFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U000A0000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U000AFFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U000B0000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U000BFFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U000C0000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U000CFFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U000D0000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U000DFFFD") <= 0)) ||
                    ((string.CompareOrdinal(surrPair, "\U000E1000") >= 0)
                        && (string.CompareOrdinal(surrPair, "\U000EFFFD") <= 0)) ||
                    (isQuery &&
                        (((string.CompareOrdinal(surrPair, "\U000F0000") >= 0)
                            && (string.CompareOrdinal(surrPair, "\U000FFFFD") <= 0)) ||
                            ((string.CompareOrdinal(surrPair, "\U00100000") >= 0)
                            && (string.CompareOrdinal(surrPair, "\U0010FFFD") <= 0)))))
                {
                    inRange = true;
                }
            }

            return inRange;
        }

        //
        // Check reserved chars according to RFC 3987 in a specific component
        //
        internal static bool CheckIsReserved(char ch, UriComponents component)
        {
            if ((component != UriComponents.Scheme) &&
                    (component != UriComponents.UserInfo) &&
                    (component != UriComponents.Host) &&
                    (component != UriComponents.Port) &&
                    (component != UriComponents.Path) &&
                    (component != UriComponents.Query) &&
                    (component != UriComponents.Fragment)
                )
            {
                return (component == (UriComponents)0) ? UriHelper.IsGenDelim(ch) : false;
            }
            else if (UriParser.DontEnableStrictRFC3986ReservedCharacterSets)
            {
                // Since we aren't enabling strict RFC 3986 reserved sets, we stick with the old behavior
                // (for app-compat) which was a broken mix of RFCs 2396 and 3986.
                switch (component)
                {
                    case UriComponents.UserInfo:
                        if (ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@')
                            return true;
                        break;
                    case UriComponents.Host:
                        if (ch == ':' || ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']' || ch == '@')
                            return true;
                        break;
                    case UriComponents.Path:
                        if (ch == '/' || ch == '?' || ch == '#' || ch == '[' || ch == ']')
                            return true;
                        break;
                    case UriComponents.Query:
                        if (ch == '#' || ch == '[' || ch == ']')
                            return true;
                        break;
                    case UriComponents.Fragment:
                        if (ch == '#' || ch == '[' || ch == ']')
                            return true;
                        break;
                    default:
                        break;
                }
                return false;
            }
            else
            {
                return (UriHelper.RFC3986ReservedMarks.IndexOf(ch) >= 0);
            }
        }

        //
        // IRI normalization for strings containing characters that are not allowed or 
        // escaped characters that should be unescaped in the context of the specified Uri component.
        //
        internal static unsafe string EscapeUnescapeIri(char* pInput, int start, int end, UriComponents component)
        {
            char[] dest = new char[end - start];
            byte[] bytes = null;

            // Pin the array to do pointer accesses
            GCHandle destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned);
            char* pDest = (char*)destHandle.AddrOfPinnedObject();

            const int percentEncodingLen = 3; // Escaped UTF-8 will take 3 chars: %AB.
            const int bufferCapacityIncrease = 30 * percentEncodingLen;
            int bufferRemaining = 0;

            int next = start;
            int destOffset = 0;
            char ch;
            bool escape = false;
            bool surrogatePair = false;

            for (; next < end; ++next)
            {
                escape = false;
                surrogatePair = false;

                if ((ch = pInput[next]) == '%')
                {
                    if (next + 2 < end)
                    {
                        ch = UriHelper.EscapedAscii(pInput[next + 1], pInput[next + 2]);

                        // Do not unescape a reserved char
                        if (ch == Uri.c_DummyChar || ch == '%' || CheckIsReserved(ch, component) || UriHelper.IsNotSafeForUnescape(ch))
                        {
                            // keep as is
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            pDest[destOffset++] = pInput[next++];
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            pDest[destOffset++] = pInput[next++];
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            pDest[destOffset++] = pInput[next];
                            continue;
                        }
                        else if (ch <= '\x7F')
                        {
                            Debug.Assert(ch < 0xFF, "Expecting ASCII character.");
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            //ASCII
                            pDest[destOffset++] = ch;
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
                            if ((object)bytes == null)
                                bytes = new byte[end - next];

                            bytes[0] = (byte)ch;
                            next += 3;
                            while (next < end)
                            {
                                // Check on exit criterion
                                if ((ch = pInput[next]) != '%' || next + 2 >= end)
                                    break;

                                // already made sure we have 3 characters in str
                                ch = UriHelper.EscapedAscii(pInput[next + 1], pInput[next + 2]);

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
                                UriHelper.MatchUTF8Sequence(pDest, dest, ref destOffset, unescapedChars, charCount, bytes,
                                    byteCount, component == UriComponents.Query, true);
                            }
                            else
                            {
                                // copy escaped sequence as is
                                for (int i = startSeq; i <= next; ++i)
                                {
                                    Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                    pDest[destOffset++] = pInput[i];
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                        pDest[destOffset++] = pInput[next];
                    }
                }
                else if (ch > '\x7f')
                {
                    // unicode

                    char ch2;

                    if ((char.IsHighSurrogate(ch)) && (next + 1 < end))
                    {
                        ch2 = pInput[next + 1];
                        escape = !CheckIriUnicodeRange(ch, ch2, ref surrogatePair, component == UriComponents.Query);
                        if (!escape)
                        {
                            // copy the two chars
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            pDest[destOffset++] = pInput[next++];
                            Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                            pDest[destOffset++] = pInput[next];
                        }
                    }
                    else
                    {
                        if (CheckIriUnicodeRange(ch, component == UriComponents.Query))
                        {
                            if (!UriHelper.IsBidiControlCharacter(ch) || !UriParser.DontKeepUnicodeBidiFormattingCharacters)
                            {
                                // copy it
                                Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                                pDest[destOffset++] = pInput[next];
                            }
                        }
                        else
                        {
                            // escape it
                            escape = true;
                        }
                    }
                }
                else
                {
                    // just copy the character
                    Debug.Assert(dest.Length > destOffset, "Destination length exceeded destination offset.");
                    pDest[destOffset++] = pInput[next];
                }

                if (escape)
                {
                    const int MaxNumberOfBytesEncoded = 4;

                    if (bufferRemaining < MaxNumberOfBytesEncoded * percentEncodingLen)
                    {
                        int newBufferLength = 0;

                        checked
                        {
                            // may need more memory since we didn't anticipate escaping
                            newBufferLength = dest.Length + bufferCapacityIncrease;
                            bufferRemaining += bufferCapacityIncrease;
                        }

                        char[] newDest = new char[newBufferLength];

                        fixed (char* pNewDest = newDest)
                        {
                            Buffer.MemoryCopy((byte*)pDest, (byte*)pNewDest, newBufferLength * sizeof(char), destOffset * sizeof(char));
                        }

                        if (destHandle.IsAllocated)
                        {
                            destHandle.Free();
                        }

                        dest = newDest;

                        // re-pin new dest[] array
                        destHandle = GCHandle.Alloc(dest, GCHandleType.Pinned);
                        pDest = (char*)destHandle.AddrOfPinnedObject();
                    }

                    byte[] encodedBytes = new byte[MaxNumberOfBytesEncoded];
                    fixed (byte* pEncodedBytes = &encodedBytes[0])
                    {
                        int encodedBytesCount = Encoding.UTF8.GetBytes(pInput + next, surrogatePair ? 2 : 1, pEncodedBytes, MaxNumberOfBytesEncoded);
                        Debug.Assert(encodedBytesCount <= MaxNumberOfBytesEncoded, "UTF8 encoder should not exceed specified byteCount");

                        bufferRemaining -= encodedBytesCount * percentEncodingLen;

                        for (int count = 0; count < encodedBytesCount; ++count)
                        {
                            UriHelper.EscapeAsciiChar((char)encodedBytes[count], dest, ref destOffset);
                        }
                    }
                }
            }

            if (destHandle.IsAllocated)
                destHandle.Free();

            Debug.Assert(destOffset <= dest.Length, "Destination length met or exceeded destination offset.");
            return new string(dest, 0, destOffset);
        }
    }
}
