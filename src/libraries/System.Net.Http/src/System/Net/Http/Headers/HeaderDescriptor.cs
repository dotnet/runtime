// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Unicode;

namespace System.Net.Http.Headers
{
    // This struct represents a particular named header --
    // if the header is one of our known headers, then it contains a reference to the KnownHeader object;
    // otherwise, for custom headers, it just contains a string for the header name.
    // Use HeaderDescriptor.TryGet to resolve an arbitrary header name to a HeaderDescriptor.
    internal readonly struct HeaderDescriptor : IEquatable<HeaderDescriptor>
    {
        private readonly string _headerName;
        private readonly KnownHeader? _knownHeader;

        public HeaderDescriptor(KnownHeader knownHeader)
        {
            _knownHeader = knownHeader;
            _headerName = knownHeader.Name;
        }

        // This should not be used directly; use static TryGet below
        private HeaderDescriptor(string headerName)
        {
            _headerName = headerName;
            _knownHeader = null;
        }

        public string Name => _headerName;
        public HttpHeaderParser? Parser => _knownHeader?.Parser;
        public HttpHeaderType HeaderType => _knownHeader == null ? HttpHeaderType.Custom : _knownHeader.HeaderType;
        public KnownHeader? KnownHeader => _knownHeader;

        public bool Equals(HeaderDescriptor other) =>
            _knownHeader == null ?
                string.Equals(_headerName, other._headerName, StringComparison.OrdinalIgnoreCase) :
                _knownHeader == other._knownHeader;
        public override int GetHashCode() => _knownHeader?.GetHashCode() ?? StringComparer.OrdinalIgnoreCase.GetHashCode(_headerName);
        public override bool Equals(object? obj) => throw new InvalidOperationException();   // Ensure this is never called, to avoid boxing

        // Returns false for invalid header name.
        public static bool TryGet(string headerName, out HeaderDescriptor descriptor)
        {
            Debug.Assert(!string.IsNullOrEmpty(headerName));

            KnownHeader? knownHeader = KnownHeaders.TryGetKnownHeader(headerName);
            if (knownHeader != null)
            {
                descriptor = new HeaderDescriptor(knownHeader);
                return true;
            }

            if (!HttpRuleParser.IsToken(headerName))
            {
                descriptor = default(HeaderDescriptor);
                return false;
            }

            descriptor = new HeaderDescriptor(headerName);
            return true;
        }

        // Returns false for invalid header name.
        public static bool TryGet(ReadOnlySpan<byte> headerName, out HeaderDescriptor descriptor)
        {
            Debug.Assert(headerName.Length > 0);

            KnownHeader? knownHeader = KnownHeaders.TryGetKnownHeader(headerName);
            if (knownHeader != null)
            {
                descriptor = new HeaderDescriptor(knownHeader);
                return true;
            }

            if (!HttpRuleParser.IsToken(headerName))
            {
                descriptor = default(HeaderDescriptor);
                return false;
            }

            descriptor = new HeaderDescriptor(HttpRuleParser.GetTokenString(headerName));
            return true;
        }

        internal static bool TryGetStaticQPackHeader(int index, out HeaderDescriptor descriptor, [NotNullWhen(true)] out string? knownValue)
        {
            Debug.Assert(index >= 0);
            Debug.Assert(s_qpackHeaderLookup.Length == 99);

            // Micro-opt: store field to variable to prevent Length re-read and use unsigned to avoid bounds check.
            (HeaderDescriptor descriptor, string value)[] qpackStaticTable = s_qpackHeaderLookup;
            uint uindex = (uint)index;

            if (uindex < (uint)qpackStaticTable.Length)
            {
                (descriptor, knownValue) = qpackStaticTable[uindex];
                return true;
            }
            else
            {
                descriptor = default;
                knownValue = null;
                return false;
            }
        }

        public HeaderDescriptor AsCustomHeader()
        {
            Debug.Assert(_knownHeader != null);
            Debug.Assert(_knownHeader.HeaderType != HttpHeaderType.Custom);
            return new HeaderDescriptor(_knownHeader.Name);
        }

        public string GetHeaderValue(ReadOnlySpan<byte> headerValue)
        {
            if (headerValue.Length == 0)
            {
                return string.Empty;
            }

            // If it's a known header value, use the known value instead of allocating a new string.
            if (_knownHeader != null)
            {
                string[]? knownValues = _knownHeader.KnownValues;
                if (knownValues != null)
                {
                    for (int i = 0; i < knownValues.Length; i++)
                    {
                        if (ByteArrayHelpers.EqualsOrdinalAsciiIgnoreCase(knownValues[i], headerValue))
                        {
                            return knownValues[i];
                        }
                    }
                }

                if (_knownHeader == KnownHeaders.ContentType)
                {
                    string? contentType = GetKnownContentType(headerValue);
                    if (contentType != null)
                    {
                        return contentType;
                    }
                }
                else if (_knownHeader == KnownHeaders.Location)
                {
                    // Normally Location should be in ISO-8859-1 but occasionally some servers respond with UTF-8.
                    if (TryDecodeUtf8(headerValue, out string? decoded))
                    {
                        return decoded;
                    }
                }
            }

            return HttpRuleParser.DefaultHttpEncoding.GetString(headerValue);
        }

        internal static string? GetKnownContentType(ReadOnlySpan<byte> contentTypeValue)
        {
            string? candidate = null;
            switch (contentTypeValue.Length)
            {
                case 8:
                    switch (contentTypeValue[7] | 0x20)
                    {
                        case 'l': candidate = "text/xml"; break; // text/xm[l]
                        case 's': candidate = "text/css"; break; // text/cs[s]
                        case 'v': candidate = "text/csv"; break; // text/cs[v]
                    }
                    break;

                case 9:
                    switch (contentTypeValue[6] | 0x20)
                    {
                        case 'g': candidate = "image/gif"; break; // image/[g]if
                        case 'p': candidate = "image/png"; break; // image/[p]ng
                        case 't': candidate = "text/html"; break; // text/h[t]ml
                    }
                    break;

                case 10:
                    switch (contentTypeValue[0] | 0x20)
                    {
                        case 't': candidate = "text/plain"; break; // [t]ext/plain
                        case 'i': candidate = "image/jpeg"; break; // [i]mage/jpeg
                    }
                    break;

                case 15:
                    switch (contentTypeValue[12] | 0x20)
                    {
                        case 'p': candidate = "application/pdf"; break; // application/[p]df
                        case 'x': candidate = "application/xml"; break; // application/[x]ml
                        case 'z': candidate = "application/zip"; break; // application/[z]ip
                    }
                    break;

                case 16:
                    switch (contentTypeValue[12] | 0x20)
                    {
                        case 'g': candidate = "application/grpc"; break; // application/[g]rpc
                        case 'j': candidate = "application/json"; break; // application/[j]son
                    }
                    break;

                case 19:
                    candidate = "multipart/form-data"; // multipart/form-data
                    break;

                case 22:
                    candidate = "application/javascript"; // application/javascript
                    break;

                case 24:
                    switch (contentTypeValue[0] | 0x20)
                    {
                        case 'a': candidate = "application/octet-stream"; break; // application/octet-stream
                        case 't': candidate = "text/html; charset=utf-8"; break; // text/html; charset=utf-8
                    }
                    break;

                case 25:
                    candidate = "text/plain; charset=utf-8"; // text/plain; charset=utf-8
                    break;

                case 31:
                    candidate = "application/json; charset=utf-8"; // application/json; charset=utf-8
                    break;

                case 33:
                    candidate = "application/x-www-form-urlencoded"; // application/x-www-form-urlencoded
                    break;
            }

            Debug.Assert(candidate is null || candidate.Length == contentTypeValue.Length);

            return candidate != null && ByteArrayHelpers.EqualsOrdinalAsciiIgnoreCase(candidate, contentTypeValue) ?
                candidate :
                null;
        }

        private static bool TryDecodeUtf8(ReadOnlySpan<byte> input, [NotNullWhen(true)] out string? decoded)
        {
            char[] rented = ArrayPool<char>.Shared.Rent(input.Length);

            try
            {
                if (Utf8.ToUtf16(input, rented, out _, out int charsWritten, replaceInvalidSequences: false) == OperationStatus.Done)
                {
                    decoded = new string(rented, 0, charsWritten);
                    return true;
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }

            decoded = null;
            return false;
        }

        // QPack Static Table
        // https://tools.ietf.org/html/draft-ietf-quic-qpack-11#appendix-A
        // TODO: can we put some of this logic into H3StaticTable and/or generate it using data that is already there?
        private static readonly (HeaderDescriptor descriptor, string value)[] s_qpackHeaderLookup = new (HeaderDescriptor descriptor, string value)[]
        {
            (new HeaderDescriptor(":authority"), ""),
            (new HeaderDescriptor(":path"), "/"),
            (new HeaderDescriptor(KnownHeaders.Age), "0"),
            (new HeaderDescriptor(KnownHeaders.ContentDisposition), ""),
            (new HeaderDescriptor(KnownHeaders.ContentLength), "0"),
            (new HeaderDescriptor(KnownHeaders.Date), ""),
            (new HeaderDescriptor(KnownHeaders.ETag), ""),
            (new HeaderDescriptor(KnownHeaders.IfModifiedSince), ""),
            (new HeaderDescriptor(KnownHeaders.IfNoneMatch), ""),
            (new HeaderDescriptor(KnownHeaders.LastModified), ""),
            (new HeaderDescriptor(KnownHeaders.Link), ""),
            (new HeaderDescriptor(KnownHeaders.Location), ""),
            (new HeaderDescriptor(KnownHeaders.Referer), ""),
            (new HeaderDescriptor(KnownHeaders.SetCookie), ""),
            (new HeaderDescriptor(":method"), "CONNECT"),
            (new HeaderDescriptor(":method"), "DELETE"),
            (new HeaderDescriptor(":method"), "GET"),
            (new HeaderDescriptor(":method"), "HEAD"),
            (new HeaderDescriptor(":method"), "OPTIONS"),
            (new HeaderDescriptor(":method"), "POST"),
            (new HeaderDescriptor(":method"), "PUT"),
            (new HeaderDescriptor(":scheme"), "http"),
            (new HeaderDescriptor(":scheme"), "https"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "103"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "200"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "304"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "404"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "503"),
            (new HeaderDescriptor(KnownHeaders.Accept), "*/*"),
            (new HeaderDescriptor(KnownHeaders.Accept), "application/dns-message"),
            (new HeaderDescriptor(KnownHeaders.AcceptEncoding), "gzip, deflate, br"),
            (new HeaderDescriptor(KnownHeaders.AcceptRanges), "bytes"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "cache-control"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "content-type"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "*"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowOrigin), "*"),
            (new HeaderDescriptor(KnownHeaders.CacheControl), "max-age=0"),
            (new HeaderDescriptor(KnownHeaders.CacheControl), "max-age=2592000"),
            (new HeaderDescriptor(KnownHeaders.CacheControl), "max-age=604800"),
            (new HeaderDescriptor(KnownHeaders.CacheControl), "no-cache"),
            (new HeaderDescriptor(KnownHeaders.CacheControl), "no-store"),
            (new HeaderDescriptor(KnownHeaders.CacheControl), "public, max-age=31536000"),
            (new HeaderDescriptor(KnownHeaders.ContentEncoding), "br"),
            (new HeaderDescriptor(KnownHeaders.ContentEncoding), "gzip"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/dns-message"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/javascript"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/json"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "application/x-www-form-urlencoded"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "image/gif"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "image/jpeg"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "image/png"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/css"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/html; charset=utf-8"), // Whitespace is correct, see spec.
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/plain"),
            (new HeaderDescriptor(KnownHeaders.ContentType), "text/plain;charset=utf-8"), // Whitespace is correct, see spec.
            (new HeaderDescriptor(KnownHeaders.Range), "bytes=0-"),
            (new HeaderDescriptor(KnownHeaders.StrictTransportSecurity), "max-age=31536000"),
            (new HeaderDescriptor(KnownHeaders.StrictTransportSecurity), "max-age=31536000; includesubdomains"),
            (new HeaderDescriptor(KnownHeaders.StrictTransportSecurity), "max-age=31536000; includesubdomains; preload"),
            (new HeaderDescriptor(KnownHeaders.Vary), "accept-encoding"),
            (new HeaderDescriptor(KnownHeaders.Vary), "origin"),
            (new HeaderDescriptor(KnownHeaders.XContentTypeOptions), "nosniff"),
            (new HeaderDescriptor("x-xss-protection"), "1; mode=block"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "100"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "204"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "206"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "302"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "400"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "403"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "421"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "425"),
            (new HeaderDescriptor(KnownHeaders.PseudoStatus), "500"),
            (new HeaderDescriptor(KnownHeaders.AcceptLanguage), ""),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowCredentials), "FALSE"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowCredentials), "TRUE"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowHeaders), "*"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowMethods), "get"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowMethods), "get, post, options"),
            (new HeaderDescriptor(KnownHeaders.AccessControlAllowMethods), "options"),
            (new HeaderDescriptor(KnownHeaders.AccessControlExposeHeaders), "content-length"),
            (new HeaderDescriptor("access-control-request-headers"), "content-type"),
            (new HeaderDescriptor("access-control-request-method"), "get"),
            (new HeaderDescriptor("access-control-request-method"), "post"),
            (new HeaderDescriptor(KnownHeaders.AltSvc), "clear"),
            (new HeaderDescriptor(KnownHeaders.Authorization), ""),
            (new HeaderDescriptor(KnownHeaders.ContentSecurityPolicy), "script-src 'none'; object-src 'none'; base-uri 'none'"),
            (new HeaderDescriptor("early-data"), "1"),
            (new HeaderDescriptor("expect-ct"), ""),
            (new HeaderDescriptor("forwarded"), ""),
            (new HeaderDescriptor(KnownHeaders.IfRange), ""),
            (new HeaderDescriptor(KnownHeaders.Origin), ""),
            (new HeaderDescriptor("purpose"), "prefetch"),
            (new HeaderDescriptor(KnownHeaders.Server), ""),
            (new HeaderDescriptor("timing-allow-origin"), "*"),
            (new HeaderDescriptor(KnownHeaders.UpgradeInsecureRequests), "1"),
            (new HeaderDescriptor(KnownHeaders.UserAgent), ""),
            (new HeaderDescriptor("x-forwarded-for"), ""),
            (new HeaderDescriptor(KnownHeaders.XFrameOptions), "deny"),
            (new HeaderDescriptor(KnownHeaders.XFrameOptions), "sameorigin")
        };
    }
}
