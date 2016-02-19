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
        public static bool TryGetHeaderName(char[] key, int startIndex, int length, out string name)
        {
            CharArrayHelpers.DebugAssertArrayInputs(key, startIndex, length);

            // When adding a new constant, add it to HttpKnownHeaderNames.cs as well.

            // The lookup works as follows: first switch on the length of the passed-in char[] array segment.
            //
            //  - If there is only one known header of that length and the char[] array segment matches that
            //    known header, set it as the out param and return true.
            //
            //  - If there are more than one known headers of that length, switch on a unique char from that
            //    set of same-length known headers. Typically this will be the first char, but some sets of
            //    same-length known headers do not have unique chars in the first position, so a char in a
            //    position further in the strings is used. If the char[] array segment matches one of the
            //    known headers, set it as the out param and return true.
            //
            //  - Otherwise, set the out param to null and return false.

            switch (length)
            {
                case 2:
                    return TryMatch(TE, key, startIndex, length, out name); // TE

                case 3:
                    switch (key[startIndex])
                    {
                        case 'A': return TryMatch(Age, key, startIndex, length, out name); // [A]ge
                        case 'P': return TryMatch(P3P, key, startIndex, length, out name); // [P]3P
                        case 'V': return TryMatch(Via, key, startIndex, length, out name); // [V]ia
                    }
                    break;

                case 4:
                    switch (key[startIndex])
                    {
                        case 'D': return TryMatch(Date, key, startIndex, length, out name); // [D]ate
                        case 'E': return TryMatch(ETag, key, startIndex, length, out name); // [E]Tag
                        case 'F': return TryMatch(From, key, startIndex, length, out name); // [F]rom
                        case 'H': return TryMatch(Host, key, startIndex, length, out name); // [H]ost
                        case 'V': return TryMatch(Vary, key, startIndex, length, out name); // [V]ary
                    }
                    break;

                case 5:
                    switch (key[startIndex])
                    {
                        case 'A': return TryMatch(Allow, key, startIndex, length, out name); // [A]llow
                        case 'R': return TryMatch(Range, key, startIndex, length, out name); // [R]ange
                    }
                    break;

                case 6:
                    switch (key[startIndex])
                    {
                        case 'A': return TryMatch(Accept, key, startIndex, length, out name); // [A]ccept
                        case 'C': return TryMatch(Cookie, key, startIndex, length, out name); // [C]ookie
                        case 'E': return TryMatch(Expect, key, startIndex, length, out name); // [E]xpect
                        case 'O': return TryMatch(Origin, key, startIndex, length, out name); // [O]rigin
                        case 'P': return TryMatch(Pragma, key, startIndex, length, out name); // [P]ragma
                        case 'S': return TryMatch(Server, key, startIndex, length, out name); // [S]erver
                    }
                    break;

                case 7:
                    switch (key[startIndex])
                    {
                        case 'C': return TryMatch(Cookie2, key, startIndex, length, out name); // [C]ookie2
                        case 'E': return TryMatch(Expires, key, startIndex, length, out name); // [E]xpires
                        case 'R': return TryMatch(Referer, key, startIndex, length, out name); // [R]eferer
                        case 'T': return TryMatch(Trailer, key, startIndex, length, out name); // [T]railer
                        case 'U': return TryMatch(Upgrade, key, startIndex, length, out name); // [U]pgrade
                        case 'W': return TryMatch(Warning, key, startIndex, length, out name); // [W]arning
                    }
                    break;

                case 8:
                    switch (key[startIndex + 3])
                    {
                        case 'M': return TryMatch(IfMatch, key, startIndex, length, out name);  // If-[M]atch
                        case 'R': return TryMatch(IfRange, key, startIndex, length, out name);  // If-[R]ange
                        case 'a': return TryMatch(Location, key, startIndex, length, out name); // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (key[startIndex])
                    {
                        case 'C': return TryMatch(Connection, key, startIndex, length, out name); // [C]onnection
                        case 'K': return TryMatch(KeepAlive, key, startIndex, length, out name);  // [K]eep-Alive
                        case 'S': return TryMatch(SetCookie, key, startIndex, length, out name);  // [S]et-Cookie
                        case 'U': return TryMatch(UserAgent, key, startIndex, length, out name);  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (key[startIndex])
                    {
                        case 'C': return TryMatch(ContentMD5, key, startIndex, length, out name); // [C]ontent-MD5
                        case 'R': return TryMatch(RetryAfter, key, startIndex, length, out name); // [R]etry-After
                        case 'S': return TryMatch(SetCookie2, key, startIndex, length, out name); // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (key[startIndex])
                    {
                        case 'C': return TryMatch(ContentType, key, startIndex, length, out name); // [C]ontent-Type
                        case 'M': return TryMatch(MaxForwards, key, startIndex, length, out name); // [M]ax-Forwards
                        case 'X': return TryMatch(XPoweredBy, key, startIndex, length, out name);  // [X]-Powered-By
                    }
                    break;

                case 13:
                    switch (key[startIndex + 6])
                    {
                        case '-': return TryMatch(AcceptRanges, key, startIndex, length, out name);  // Accept[-]Ranges
                        case 'i': return TryMatch(Authorization, key, startIndex, length, out name); // Author[i]zation
                        case 'C': return TryMatch(CacheControl, key, startIndex, length, out name);  // Cache-[C]ontrol
                        case 't': return TryMatch(ContentRange, key, startIndex, length, out name);  // Conten[t]-Range
                        case 'e': return TryMatch(IfNoneMatch, key, startIndex, length, out name);   // If-Non[e]-Match
                        case 'o': return TryMatch(LastModified, key, startIndex, length, out name);  // Last-M[o]dified
                    }
                    break;

                case 14:
                    switch (key[startIndex])
                    {
                        case 'A': return TryMatch(AcceptCharset, key, startIndex, length, out name); // [A]ccept-Charset
                        case 'C': return TryMatch(ContentLength, key, startIndex, length, out name); // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (key[startIndex + 7])
                    {
                        case 'E': return TryMatch(AcceptEncoding, key, startIndex, length, out name); // Accept-[E]ncoding
                        case 'L': return TryMatch(AcceptLanguage, key, startIndex, length, out name); // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (key[startIndex + 11])
                    {
                        case 'o': return TryMatch(ContentEncoding, key, startIndex, length, out name); // Content-Enc[o]ding
                        case 'g': return TryMatch(ContentLanguage, key, startIndex, length, out name); // Content-Lan[g]uage
                        case 'a': return TryMatch(ContentLocation, key, startIndex, length, out name); // Content-Loc[a]tion
                        case 'c': return TryMatch(ProxyConnection, key, startIndex, length, out name); // Proxy-Conne[c]tion
                        case 'i': return TryMatch(WWWAuthenticate, key, startIndex, length, out name); // WWW-Authent[i]cate
                        case 'r': return TryMatch(XAspNetVersion, key, startIndex, length, out name);  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (key[startIndex])
                    {
                        case 'I': return TryMatch(IfModifiedSince, key, startIndex, length, out name);  // [I]f-Modified-Since
                        case 'S': return TryMatch(SecWebSocketKey, key, startIndex, length, out name);  // [S]ec-WebSocket-Key
                        case 'T': return TryMatch(TransferEncoding, key, startIndex, length, out name); // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    return TryMatch(ProxyAuthenticate, key, startIndex, length, out name); // Proxy-Authenticate

                case 19:
                    switch (key[startIndex])
                    {
                        case 'C': return TryMatch(ContentDisposition, key, startIndex, length, out name); // [C]ontent-Disposition
                        case 'I': return TryMatch(IfUnmodifiedSince, key, startIndex, length, out name);  // [I]f-Unmodified-Since
                        case 'P': return TryMatch(ProxyAuthorization, key, startIndex, length, out name); // [P]roxy-Authorization
                    }
                    break;

                case 20:
                    return TryMatch(SecWebSocketAccept, key, startIndex, length, out name); // Sec-WebSocket-Accept

                case 21:
                    return TryMatch(SecWebSocketVersion, key, startIndex, length, out name); // Sec-WebSocket-Version

                case 22:
                    return TryMatch(SecWebSocketProtocol, key, startIndex, length, out name); // Sec-WebSocket-Protocol

                case 24:
                    return TryMatch(SecWebSocketExtensions, key, startIndex, length, out name); // Sec-WebSocket-Extensions
            }

            name = null;
            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="known"/> matches the <paramref name="key"/> char[] array segment,
        /// using an ordinal comparison.
        /// </summary>
        private static bool TryMatch(string known, char[] key, int startIndex, int length, out string name)
        {
            Debug.Assert(known != null);
            Debug.Assert(known.Length > 0);
            CharArrayHelpers.DebugAssertArrayInputs(key, startIndex, length);

            // The lengths should be equal because this method is only called
            // from within a "switch (length) { ... }".
            Debug.Assert(known.Length == length);

            if (CharArrayHelpers.EqualsOrdinal(known, key, startIndex, length))
            {
                name = known;
                return true;
            }

            name = null;
            return false;
        }
    }
}
