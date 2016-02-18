// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net
{
    internal static partial class HttpKnownHeaderNames
    {
        /// <summary>
        /// Gets a known header name string from a matching char[] array segment, using an ordinal comparison.
        /// Used to avoid allocating new strings for known header names.
        /// </summary>
        public static bool TryGetHeaderName(char[] array, int startIndex, int length, out string name)
        {
            CharArrayHelpers.DebugAssertArrayInputs(array, startIndex, length);

            return TryGetHeaderName(
                array, startIndex, length,
                (arr, index) => arr[index],
                (known, a, start, len) => CharArrayHelpers.EqualsOrdinal(known, a, start, len),
                out name);
        }

        /// <summary>
        /// Gets a known header name string from a matching IntPtr buffer, using an ordinal comparison.
        /// Used to avoid allocating new strings for known header names.
        /// </summary>
        public unsafe static bool TryGetHeaderName(IntPtr buffer, int length, out string name)
        {
            Debug.Assert(length >= 0);

            if (buffer == IntPtr.Zero)
            {
                name = null;
                return false;
            }

            // We always pass 0 for the startIndex, as buffer should already point to the start.
            const int startIndex = 0;

            return TryGetHeaderName(
                buffer, startIndex, length,
                (buf, index) => (char)((byte*)buf)[index],
                (known, buf, start, len) => EqualsOrdinal(known, buf, len),
                out name);
        }

        private static bool TryGetHeaderName<T>(
            T key, int startIndex, int length,
            CharAt<T> charAt,
            Equals<T> equals,
            out string name)
        {
            Debug.Assert(key != null);
            Debug.Assert(startIndex >= 0);
            Debug.Assert(length >= 0);
            Debug.Assert(charAt != null);
            Debug.Assert(equals != null);

            // When adding a new constant, add it to HttpKnownHeaderNames.cs as well.

            // The lookup works as follows: first switch on the length of the passed-in key.
            //
            //  - If there is only one known header of that length and the key matches that
            //    known header, set it as the out param and return true.
            //
            //  - If there are more than one known headers of that length, switch on a unique char from that
            //    set of same-length known headers. Typically this will be the first char, but some sets of
            //    same-length known headers do not have unique chars in the first position, so a char in a
            //    position further in the strings is used. If the key matches one of the known headers,
            //    set it as the out param and return true.
            //
            //  - Otherwise, set the out param to null and return false.

            switch (length)
            {
                case 2:
                    return TryMatch(TE, key, startIndex, length, equals, out name); // TE

                case 3:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': return TryMatch(Age, key, startIndex, length, equals, out name); // [A]ge
                        case 'P': return TryMatch(P3P, key, startIndex, length, equals, out name); // [P]3P
                        case 'V': return TryMatch(Via, key, startIndex, length, equals, out name); // [V]ia
                    }
                    break;

                case 4:
                    switch (charAt(key, startIndex))
                    {
                        case 'D': return TryMatch(Date, key, startIndex, length, equals, out name); // [D]ate
                        case 'E': return TryMatch(ETag, key, startIndex, length, equals, out name); // [E]Tag
                        case 'F': return TryMatch(From, key, startIndex, length, equals, out name); // [F]rom
                        case 'H': return TryMatch(Host, key, startIndex, length, equals, out name); // [H]ost
                        case 'V': return TryMatch(Vary, key, startIndex, length, equals, out name); // [V]ary
                    }
                    break;

                case 5:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': return TryMatch(Allow, key, startIndex, length, equals, out name); // [A]llow
                        case 'R': return TryMatch(Range, key, startIndex, length, equals, out name); // [R]ange
                    }
                    break;

                case 6:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': return TryMatch(Accept, key, startIndex, length, equals, out name); // [A]ccept
                        case 'C': return TryMatch(Cookie, key, startIndex, length, equals, out name); // [C]ookie
                        case 'E': return TryMatch(Expect, key, startIndex, length, equals, out name); // [E]xpect
                        case 'O': return TryMatch(Origin, key, startIndex, length, equals, out name); // [O]rigin
                        case 'P': return TryMatch(Pragma, key, startIndex, length, equals, out name); // [P]ragma
                        case 'S': return TryMatch(Server, key, startIndex, length, equals, out name); // [S]erver
                    }
                    break;

                case 7:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': return TryMatch(Cookie2, key, startIndex, length, equals, out name); // [C]ookie2
                        case 'E': return TryMatch(Expires, key, startIndex, length, equals, out name); // [E]xpires
                        case 'R': return TryMatch(Referer, key, startIndex, length, equals, out name); // [R]eferer
                        case 'T': return TryMatch(Trailer, key, startIndex, length, equals, out name); // [T]railer
                        case 'U': return TryMatch(Upgrade, key, startIndex, length, equals, out name); // [U]pgrade
                        case 'W': return TryMatch(Warning, key, startIndex, length, equals, out name); // [W]arning
                    }
                    break;

                case 8:
                    switch (charAt(key, startIndex + 3))
                    {
                        case 'M': return TryMatch(IfMatch, key, startIndex, length, equals, out name);  // If-[M]atch
                        case 'R': return TryMatch(IfRange, key, startIndex, length, equals, out name);  // If-[R]ange
                        case 'a': return TryMatch(Location, key, startIndex, length, equals, out name); // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': return TryMatch(Connection, key, startIndex, length, equals, out name); // [C]onnection
                        case 'K': return TryMatch(KeepAlive, key, startIndex, length, equals, out name);  // [K]eep-Alive
                        case 'S': return TryMatch(SetCookie, key, startIndex, length, equals, out name);  // [S]et-Cookie
                        case 'U': return TryMatch(UserAgent, key, startIndex, length, equals, out name);  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': return TryMatch(ContentMD5, key, startIndex, length, equals, out name); // [C]ontent-MD5
                        case 'R': return TryMatch(RetryAfter, key, startIndex, length, equals, out name); // [R]etry-After
                        case 'S': return TryMatch(SetCookie2, key, startIndex, length, equals, out name); // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': return TryMatch(ContentType, key, startIndex, length, equals, out name); // [C]ontent-Type
                        case 'M': return TryMatch(MaxForwards, key, startIndex, length, equals, out name); // [M]ax-Forwards
                        case 'X': return TryMatch(XPoweredBy, key, startIndex, length, equals, out name);  // [X]-Powered-By
                    }
                    break;

                case 13:
                    switch (charAt(key, startIndex + 6))
                    {
                        case '-': return TryMatch(AcceptRanges, key, startIndex, length, equals, out name);  // Accept[-]Ranges
                        case 'i': return TryMatch(Authorization, key, startIndex, length, equals, out name); // Author[i]zation
                        case 'C': return TryMatch(CacheControl, key, startIndex, length, equals, out name);  // Cache-[C]ontrol
                        case 't': return TryMatch(ContentRange, key, startIndex, length, equals, out name);  // Conten[t]-Range
                        case 'e': return TryMatch(IfNoneMatch, key, startIndex, length, equals, out name);   // If-Non[e]-Match
                        case 'o': return TryMatch(LastModified, key, startIndex, length, equals, out name);  // Last-M[o]dified
                    }
                    break;

                case 14:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': return TryMatch(AcceptCharset, key, startIndex, length, equals, out name); // [A]ccept-Charset
                        case 'C': return TryMatch(ContentLength, key, startIndex, length, equals, out name); // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (charAt(key, startIndex + 7))
                    {
                        case 'E': return TryMatch(AcceptEncoding, key, startIndex, length, equals, out name); // Accept-[E]ncoding
                        case 'L': return TryMatch(AcceptLanguage, key, startIndex, length, equals, out name); // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (charAt(key, startIndex + 11))
                    {
                        case 'o': return TryMatch(ContentEncoding, key, startIndex, length, equals, out name); // Content-Enc[o]ding
                        case 'g': return TryMatch(ContentLanguage, key, startIndex, length, equals, out name); // Content-Lan[g]uage
                        case 'a': return TryMatch(ContentLocation, key, startIndex, length, equals, out name); // Content-Loc[a]tion
                        case 'c': return TryMatch(ProxyConnection, key, startIndex, length, equals, out name); // Proxy-Conne[c]tion
                        case 'i': return TryMatch(WWWAuthenticate, key, startIndex, length, equals, out name); // WWW-Authent[i]cate
                        case 'r': return TryMatch(XAspNetVersion, key, startIndex, length, equals, out name);  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (charAt(key, startIndex))
                    {
                        case 'I': return TryMatch(IfModifiedSince, key, startIndex, length, equals, out name);  // [I]f-Modified-Since
                        case 'S': return TryMatch(SecWebSocketKey, key, startIndex, length, equals, out name);  // [S]ec-WebSocket-Key
                        case 'T': return TryMatch(TransferEncoding, key, startIndex, length, equals, out name); // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    return TryMatch(ProxyAuthenticate, key, startIndex, length, equals, out name); // Proxy-Authenticate

                case 19:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': return TryMatch(ContentDisposition, key, startIndex, length, equals, out name); // [C]ontent-Disposition
                        case 'I': return TryMatch(IfUnmodifiedSince, key, startIndex, length, equals, out name);  // [I]f-Unmodified-Since
                        case 'P': return TryMatch(ProxyAuthorization, key, startIndex, length, equals, out name); // [P]roxy-Authorization
                    }
                    break;

                case 20:
                    return TryMatch(SecWebSocketAccept, key, startIndex, length, equals, out name); // Sec-WebSocket-Accept

                case 21:
                    return TryMatch(SecWebSocketVersion, key, startIndex, length, equals, out name); // Sec-WebSocket-Version

                case 22:
                    return TryMatch(SecWebSocketProtocol, key, startIndex, length, equals, out name); // Sec-WebSocket-Protocol

                case 24:
                    return TryMatch(SecWebSocketExtensions, key, startIndex, length, equals, out name); // Sec-WebSocket-Extensions
            }

            name = null;
            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="known"/> matches the <paramref name="key"/> char[] array segment,
        /// using an ordinal comparison.
        /// </summary>
        private static bool TryMatch<T>(string known, T key, int startIndex, int length, Equals<T> equals, out string name)
        {
            Debug.Assert(known != null);
            Debug.Assert(known.Length > 0);
            Debug.Assert(startIndex >= 0);
            Debug.Assert(length > 0);
            Debug.Assert(equals != null);

            // The lengths should be equal because this method is only called
            // from within a "switch (length) { ... }".
            Debug.Assert(known.Length == length);

            if (equals(known, key, startIndex, length))
            {
                name = known;
                return true;
            }

            name = null;
            return false;
        }

        private unsafe static bool EqualsOrdinal(string left, IntPtr right, int rightLength)
        {
            Debug.Assert(left != null);
            Debug.Assert(right != IntPtr.Zero);
            Debug.Assert(rightLength > 0);

            // At this point the lengths have already been determined to be equal.
            Debug.Assert(left.Length == rightLength);

            byte* pRight = (byte*)right;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != pRight[i])
                {
                    return false;
                }
            }

            return true;
        }

        private delegate char CharAt<T>(T key, int index);

        private delegate bool Equals<T>(string left, T key, int startIndex, int length);
    }
}
