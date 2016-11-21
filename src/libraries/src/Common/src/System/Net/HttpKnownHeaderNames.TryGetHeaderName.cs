// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net
{
    internal static partial class HttpKnownHeaderNames
    {
        /// <summary>
        /// Gets a known header name string from a matching char[] array segment, using a case-sensitive
        /// ordinal comparison. Used to avoid allocating new strings for known header names.
        /// </summary>
        public static bool TryGetHeaderName(char[] array, int startIndex, int length, out string name)
        {
            CharArrayHelpers.DebugAssertArrayInputs(array, startIndex, length);

            return TryGetHeaderName(
                array, startIndex, length,
                (arr, index) => arr[index],
                (known, arr, start, len) => CharArrayHelpers.EqualsOrdinal(known, arr, start, len),
                out name);
        }

        /// <summary>
        /// Gets a known header name string from a matching IntPtr buffer, using a case-sensitive
        /// ordinal comparison. Used to avoid allocating new strings for known header names.
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
            Func<T, int, char> charAt,
            Func<string, T, int, int, bool> equals,
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
            //  - If there is only one known header of that length, set potentialHeader to that known header
            //    and goto TryMatch to see if the key fully matches potentialHeader.
            //
            //  - If there are more than one known headers of that length, switch on a unique char from that
            //    set of same-length known headers. Typically this will be the first char, but some sets of
            //    same-length known headers do not have unique chars in the first position, so a char in a
            //    position further in the strings is used. If the char from the key matches one of the
            //    known headers, set potentialHeader to that known header and goto TryMatch to see if the key
            //    fully matches potentialHeader.
            //
            //  - Otherwise, there is no match, so set the out param to null and return false.
            //
            // Matching is case-sensitive: we only want to return a known header that exactly matches the key.

            string potentialHeader = null;

            switch (length)
            {
                case 2:
                    potentialHeader = TE; goto TryMatch; // TE

                case 3:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': potentialHeader = Age; goto TryMatch; // [A]ge
                        case 'P': potentialHeader = P3P; goto TryMatch; // [P]3P
                        case 'T': potentialHeader = TSV; goto TryMatch; // [T]SV
                        case 'V': potentialHeader = Via; goto TryMatch; // [V]ia
                    }
                    break;

                case 4:
                    switch (charAt(key, startIndex))
                    {
                        case 'D': potentialHeader = Date; goto TryMatch; // [D]ate
                        case 'E': potentialHeader = ETag; goto TryMatch; // [E]Tag
                        case 'F': potentialHeader = From; goto TryMatch; // [F]rom
                        case 'H': potentialHeader = Host; goto TryMatch; // [H]ost
                        case 'L': potentialHeader = Link; goto TryMatch; // [L]ink
                        case 'V': potentialHeader = Vary; goto TryMatch; // [V]ary
                    }
                    break;

                case 5:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': potentialHeader = Allow; goto TryMatch; // [A]llow
                        case 'R': potentialHeader = Range; goto TryMatch; // [R]ange
                    }
                    break;

                case 6:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': potentialHeader = Accept; goto TryMatch; // [A]ccept
                        case 'C': potentialHeader = Cookie; goto TryMatch; // [C]ookie
                        case 'E': potentialHeader = Expect; goto TryMatch; // [E]xpect
                        case 'O': potentialHeader = Origin; goto TryMatch; // [O]rigin
                        case 'P': potentialHeader = Pragma; goto TryMatch; // [P]ragma
                        case 'S': potentialHeader = Server; goto TryMatch; // [S]erver
                    }
                    break;

                case 7:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': potentialHeader = AltSvc; goto TryMatch;  // [A]lt-Svc
                        case 'C': potentialHeader = Cookie2; goto TryMatch; // [C]ookie2
                        case 'E': potentialHeader = Expires; goto TryMatch; // [E]xpires
                        case 'R': potentialHeader = Referer; goto TryMatch; // [R]eferer
                        case 'T': potentialHeader = Trailer; goto TryMatch; // [T]railer
                        case 'U': potentialHeader = Upgrade; goto TryMatch; // [U]pgrade
                        case 'W': potentialHeader = Warning; goto TryMatch; // [W]arning
                    }
                    break;

                case 8:
                    switch (charAt(key, startIndex + 3))
                    {
                        case 'M': potentialHeader = IfMatch; goto TryMatch;  // If-[M]atch
                        case 'R': potentialHeader = IfRange; goto TryMatch;  // If-[R]ange
                        case 'a': potentialHeader = Location; goto TryMatch; // Loc[a]tion
                    }
                    break;

                case 10:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': potentialHeader = Connection; goto TryMatch; // [C]onnection
                        case 'K': potentialHeader = KeepAlive; goto TryMatch;  // [K]eep-Alive
                        case 'S': potentialHeader = SetCookie; goto TryMatch;  // [S]et-Cookie
                        case 'U': potentialHeader = UserAgent; goto TryMatch;  // [U]ser-Agent
                    }
                    break;

                case 11:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': potentialHeader = ContentMD5; goto TryMatch; // [C]ontent-MD5
                        case 'R': potentialHeader = RetryAfter; goto TryMatch; // [R]etry-After
                        case 'S': potentialHeader = SetCookie2; goto TryMatch; // [S]et-Cookie2
                    }
                    break;

                case 12:
                    switch (charAt(key, startIndex + 2))
                    {
                        case 'c': potentialHeader = AcceptPatch; goto TryMatch; // Ac[c]ept-Patch
                        case 'n': potentialHeader = ContentType; goto TryMatch; // Co[n]tent-Type
                        case 'x': potentialHeader = MaxForwards; goto TryMatch; // Ma[x]-Forwards
                        case 'P': potentialHeader = XPoweredBy; goto TryMatch;  // X-[P]owered-By
                        case 'R': potentialHeader = XRequestID; goto TryMatch;  // X-[R]equest-ID
                    }
                    break;

                case 13:
                    switch (charAt(key, startIndex + 6))
                    {
                        case '-': potentialHeader = AcceptRanges; goto TryMatch;  // Accept[-]Ranges
                        case 'i': potentialHeader = Authorization; goto TryMatch; // Author[i]zation
                        case 'C': potentialHeader = CacheControl; goto TryMatch;  // Cache-[C]ontrol
                        case 't': potentialHeader = ContentRange; goto TryMatch;  // Conten[t]-Range
                        case 'e': potentialHeader = IfNoneMatch; goto TryMatch;   // If-Non[e]-Match
                        case 'o': potentialHeader = LastModified; goto TryMatch;  // Last-M[o]dified
                    }
                    break;

                case 14:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': potentialHeader = AcceptCharset; goto TryMatch; // [A]ccept-Charset
                        case 'C': potentialHeader = ContentLength; goto TryMatch; // [C]ontent-Length
                    }
                    break;

                case 15:
                    switch (charAt(key, startIndex + 7))
                    {
                        case '-': potentialHeader = XFrameOptions; goto TryMatch;  // X-Frame[-]Options
                        case 'm': potentialHeader = XUACompatible; goto TryMatch;  // X-UA-Co[m]patible
                        case 'E': potentialHeader = AcceptEncoding; goto TryMatch; // Accept-[E]ncoding
                        case 'K': potentialHeader = PublicKeyPins; goto TryMatch;  // Public-[K]ey-Pins
                        case 'L': potentialHeader = AcceptLanguage; goto TryMatch; // Accept-[L]anguage
                    }
                    break;

                case 16:
                    switch (charAt(key, startIndex + 11))
                    {
                        case 'o': potentialHeader = ContentEncoding; goto TryMatch; // Content-Enc[o]ding
                        case 'g': potentialHeader = ContentLanguage; goto TryMatch; // Content-Lan[g]uage
                        case 'a': potentialHeader = ContentLocation; goto TryMatch; // Content-Loc[a]tion
                        case 'c': potentialHeader = ProxyConnection; goto TryMatch; // Proxy-Conne[c]tion
                        case 'i': potentialHeader = WWWAuthenticate; goto TryMatch; // WWW-Authent[i]cate
                        case 'r': potentialHeader = XAspNetVersion; goto TryMatch;  // X-AspNet-Ve[r]sion
                    }
                    break;

                case 17:
                    switch (charAt(key, startIndex))
                    {
                        case 'I': potentialHeader = IfModifiedSince; goto TryMatch;  // [I]f-Modified-Since
                        case 'S': potentialHeader = SecWebSocketKey; goto TryMatch;  // [S]ec-WebSocket-Key
                        case 'T': potentialHeader = TransferEncoding; goto TryMatch; // [T]ransfer-Encoding
                    }
                    break;

                case 18:
                    switch (charAt(key, startIndex))
                    {
                        case 'P': potentialHeader = ProxyAuthenticate; goto TryMatch; // [P]roxy-Authenticate
                        case 'X': potentialHeader = XContentDuration; goto TryMatch;  // [X]-Content-Duration
                    }
                    break;

                case 19:
                    switch (charAt(key, startIndex))
                    {
                        case 'C': potentialHeader = ContentDisposition; goto TryMatch; // [C]ontent-Disposition
                        case 'I': potentialHeader = IfUnmodifiedSince; goto TryMatch;  // [I]f-Unmodified-Since
                        case 'P': potentialHeader = ProxyAuthorization; goto TryMatch; // [P]roxy-Authorization
                    }
                    break;

                case 20:
                    potentialHeader = SecWebSocketAccept; goto TryMatch; // Sec-WebSocket-Accept

                case 21:
                    potentialHeader = SecWebSocketVersion; goto TryMatch; // Sec-WebSocket-Version

                case 22:
                    switch (charAt(key, startIndex))
                    {
                        case 'A': potentialHeader = AccessControlMaxAge; goto TryMatch;  // [A]ccess-Control-Max-Age
                        case 'S': potentialHeader = SecWebSocketProtocol; goto TryMatch; // [S]ec-WebSocket-Protocol
                        case 'X': potentialHeader = XContentTypeOptions; goto TryMatch;  // [X]-Content-Type-Options
                    }
                    break;

                case 23:
                    potentialHeader = ContentSecurityPolicy; goto TryMatch; // Content-Security-Policy

                case 24:
                    potentialHeader = SecWebSocketExtensions; goto TryMatch; // Sec-WebSocket-Extensions

                case 25:
                    switch (charAt(key, startIndex))
                    {
                        case 'S': potentialHeader = StrictTransportSecurity; goto TryMatch; // [S]trict-Transport-Security
                        case 'U': potentialHeader = UpgradeInsecureRequests; goto TryMatch; // [U]pgrade-Insecure-Requests
                    }
                    break;

                case 27:
                    potentialHeader = AccessControlAllowOrigin; goto TryMatch; // Access-Control-Allow-Origin

                case 28:
                    switch (charAt(key, startIndex + 21))
                    {
                        case 'H': potentialHeader = AccessControlAllowHeaders; goto TryMatch; // Access-Control-Allow-[H]eaders
                        case 'M': potentialHeader = AccessControlAllowMethods; goto TryMatch; // Access-Control-Allow-[M]ethods
                    }
                    break;

                case 29:
                    potentialHeader = AccessControlExposeHeaders; goto TryMatch; // Access-Control-Expose-Headers

                case 32:
                    potentialHeader = AccessControlAllowCredentials; goto TryMatch; // Access-Control-Allow-Credentials
            }

            name = null;
            return false;

            TryMatch:
            Debug.Assert(potentialHeader != null);
            return TryMatch(potentialHeader, key, startIndex, length, equals, out name);
        }

        /// <summary>
        /// Returns true if <paramref name="known"/> matches the <paramref name="key"/> char[] array segment,
        /// using an ordinal comparison.
        /// </summary>
        private static bool TryMatch<T>(string known, T key, int startIndex, int length, Func<string, T, int, int, bool> equals, out string name)
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
    }
}
