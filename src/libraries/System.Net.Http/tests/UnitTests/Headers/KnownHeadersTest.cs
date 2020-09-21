// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Headers;
using Xunit;

namespace System.Net.Http.Tests
{
    public class KnownHeadersTest
    {
        [Theory]
        [InlineData(":status")]
        [InlineData("Accept")]
        [InlineData("Accept-Charset")]
        [InlineData("Accept-Encoding")]
        [InlineData("Accept-Language")]
        [InlineData("Accept-Patch")]
        [InlineData("Accept-Ranges")]
        [InlineData("Access-Control-Allow-Credentials")]
        [InlineData("Access-Control-Allow-Headers")]
        [InlineData("Access-Control-Allow-Methods")]
        [InlineData("Access-Control-Allow-Origin")]
        [InlineData("Access-Control-Expose-Headers")]
        [InlineData("Access-Control-Max-Age")]
        [InlineData("Age")]
        [InlineData("Allow")]
        [InlineData("Alt-Svc")]
        [InlineData("Alt-Used")]
        [InlineData("Authorization")]
        [InlineData("Cache-Control")]
        [InlineData("Connection")]
        [InlineData("Content-Disposition")]
        [InlineData("Content-Encoding")]
        [InlineData("Content-Language")]
        [InlineData("Content-Length")]
        [InlineData("Content-Location")]
        [InlineData("Content-MD5")]
        [InlineData("Content-Range")]
        [InlineData("Content-Security-Policy")]
        [InlineData("Content-Type")]
        [InlineData("Cookie")]
        [InlineData("Cookie2")]
        [InlineData("Date")]
        [InlineData("ETag")]
        [InlineData("Expect")]
        [InlineData("Expect-CT")]
        [InlineData("Expires")]
        [InlineData("From")]
        [InlineData("grpc-encoding")]
        [InlineData("grpc-message")]
        [InlineData("grpc-status")]
        [InlineData("Host")]
        [InlineData("If-Match")]
        [InlineData("If-Modified-Since")]
        [InlineData("If-None-Match")]
        [InlineData("If-Range")]
        [InlineData("If-Unmodified-Since")]
        [InlineData("Keep-Alive")]
        [InlineData("Last-Modified")]
        [InlineData("Link")]
        [InlineData("Location")]
        [InlineData("Max-Forwards")]
        [InlineData("Origin")]
        [InlineData("P3P")]
        [InlineData("Pragma")]
        [InlineData("Proxy-Authenticate")]
        [InlineData("Proxy-Authorization")]
        [InlineData("Proxy-Connection")]
        [InlineData("Proxy-Support")]
        [InlineData("Public-Key-Pins")]
        [InlineData("Range")]
        [InlineData("Referer")]
        [InlineData("Referrer-Policy")]
        [InlineData("Refresh")]
        [InlineData("Retry-After")]
        [InlineData("Sec-WebSocket-Accept")]
        [InlineData("Sec-WebSocket-Extensions")]
        [InlineData("Sec-WebSocket-Key")]
        [InlineData("Sec-WebSocket-Protocol")]
        [InlineData("Sec-WebSocket-Version")]
        [InlineData("Server")]
        [InlineData("Server-Timing")]
        [InlineData("Set-Cookie")]
        [InlineData("Set-Cookie2")]
        [InlineData("Strict-Transport-Security")]
        [InlineData("TE")]
        [InlineData("Trailer")]
        [InlineData("Transfer-Encoding")]
        [InlineData("TSV")]
        [InlineData("Upgrade")]
        [InlineData("Upgrade-Insecure-Requests")]
        [InlineData("User-Agent")]
        [InlineData("Vary")]
        [InlineData("Via")]
        [InlineData("Warning")]
        [InlineData("WWW-Authenticate")]
        [InlineData("X-AspNet-Version")]
        [InlineData("X-Cache")]
        [InlineData("X-Content-Duration")]
        [InlineData("X-Content-Type-Options")]
        [InlineData("X-Frame-Options")]
        [InlineData("X-MSEdge-Ref")]
        [InlineData("X-Powered-By")]
        [InlineData("X-Request-ID")]
        [InlineData("X-UA-Compatible")]
        [InlineData("X-XSS-Protection")]
        public void TryGetKnownHeader_Known_Found(string name)
        {
            foreach (string casedName in new[] { name, name.ToUpperInvariant(), name.ToLowerInvariant() })
            {
                Validate(casedName, KnownHeaders.TryGetKnownHeader(casedName));
                Validate(casedName, KnownHeaders.TryGetKnownHeader(casedName.Select(c => (byte)c).ToArray()));
            }

            static void Validate(string name, KnownHeader h)
            {
                Assert.NotNull(h);
                Assert.Same(h, KnownHeaders.TryGetKnownHeader(name));

                Assert.Same(h, h.Descriptor.KnownHeader);
                Assert.Equal(name, h.Name, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(name, h.Descriptor.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t ")]
        [InlineData("Something")]
        [InlineData("X-Unknown")]
        public void TryGetKnownHeader_Unknown_NotFound(string name)
        {
            foreach (string casedName in new[] { name, name.ToUpperInvariant(), name.ToLowerInvariant() })
            {
                Assert.Null(KnownHeaders.TryGetKnownHeader(casedName));
                Assert.Null(KnownHeaders.TryGetKnownHeader(casedName.Select(c => (byte)c).ToArray()));
            }
        }

        [Theory]
        [InlineData("Access-Control-Allow-Credentials", "true")]
        [InlineData("Access-Control-Allow-Headers", "*")]
        [InlineData("Access-Control-Allow-Methods", "*")]
        [InlineData("Access-Control-Allow-Origin", "*")]
        [InlineData("Access-Control-Allow-Origin", "null")]
        [InlineData("Access-Control-Expose-Headers", "*")]
        [InlineData("Cache-Control", "must-revalidate")]
        [InlineData("Cache-Control", "no-cache")]
        [InlineData("Cache-Control", "no-store")]
        [InlineData("Cache-Control", "no-transform")]
        [InlineData("Cache-Control", "private")]
        [InlineData("Cache-Control", "proxy-revalidate")]
        [InlineData("Cache-Control", "public")]
        [InlineData("Connection", "close")]
        [InlineData("Content-Disposition", "attachment")]
        [InlineData("Content-Disposition", "inline")]
        [InlineData("Content-Encoding", "gzip")]
        [InlineData("Content-Encoding", "deflate")]
        [InlineData("Content-Encoding", "br")]
        [InlineData("Content-Encoding", "compress")]
        [InlineData("Content-Encoding", "identity")]
        [InlineData("Content-Type", "text/xml")]
        [InlineData("Content-Type", "text/css")]
        [InlineData("Content-Type", "text/csv")]
        [InlineData("Content-Type", "image/gif")]
        [InlineData("Content-Type", "image/png")]
        [InlineData("Content-Type", "text/html")]
        [InlineData("Content-Type", "text/plain")]
        [InlineData("Content-Type", "image/jpeg")]
        [InlineData("Content-Type", "application/pdf")]
        [InlineData("Content-Type", "application/xml")]
        [InlineData("Content-Type", "application/zip")]
        [InlineData("Content-Type", "application/grpc")]
        [InlineData("Content-Type", "application/json")]
        [InlineData("Content-Type", "multipart/form-data")]
        [InlineData("Content-Type", "application/javascript")]
        [InlineData("Content-Type", "application/octet-stream")]
        [InlineData("Content-Type", "text/html; charset=utf-8")]
        [InlineData("Content-Type", "text/plain; charset=utf-8")]
        [InlineData("Content-Type", "application/json; charset=utf-8")]
        [InlineData("Content-Type", "application/x-www-form-urlencoded")]
        [InlineData("Expect", "100-continue")]
        [InlineData("grpc-encoding", "identity")]
        [InlineData("grpc-encoding", "gzip")]
        [InlineData("grpc-encoding", "deflate")]
        [InlineData("grpc-status", "0")]
        [InlineData("Pragma", "no-cache")]
        [InlineData("Referrer-Policy", "strict-origin-when-cross-origin")]
        [InlineData("Referrer-Policy", "origin-when-cross-origin")]
        [InlineData("Referrer-Policy", "strict-origin")]
        [InlineData("Referrer-Policy", "origin")]
        [InlineData("Referrer-Policy", "same-origin")]
        [InlineData("Referrer-Policy", "no-referrer-when-downgrade")]
        [InlineData("Referrer-Policy", "no-referrer")]
        [InlineData("Referrer-Policy", "unsafe-url")]
        [InlineData("TE", "trailers")]
        [InlineData("TE", "compress")]
        [InlineData("TE", "deflate")]
        [InlineData("TE", "gzip")]
        [InlineData("Transfer-Encoding", "chunked")]
        [InlineData("Transfer-Encoding", "compress")]
        [InlineData("Transfer-Encoding", "deflate")]
        [InlineData("Transfer-Encoding", "gzip")]
        [InlineData("Transfer-Encoding", "identity")]
        [InlineData("Upgrade-Insecure-Requests", "1")]
        [InlineData("Vary", "*")]
        [InlineData("X-Content-Type-Options", "nosniff")]
        [InlineData("X-Frame-Options", "DENY")]
        [InlineData("X-Frame-Options", "SAMEORIGIN")]
        [InlineData("X-XSS-Protection", "0")]
        [InlineData("X-XSS-Protection", "1")]
        [InlineData("X-XSS-Protection", "1; mode=block")]
        public void GetKnownHeaderValue_Known_Found(string name, string value)
        {
            foreach (string casedValue in new[] { value, value.ToUpperInvariant(), value.ToLowerInvariant() })
            {
                Validate(KnownHeaders.TryGetKnownHeader(name), casedValue);
            }

            static void Validate(KnownHeader knownHeader, string value)
            {
                Assert.NotNull(knownHeader);

                string v1 = knownHeader.Descriptor.GetHeaderValue(value.Select(c => (byte)c).ToArray(), valueEncoding: null);
                Assert.NotNull(v1);
                Assert.Equal(value, v1, StringComparer.OrdinalIgnoreCase);

                string v2 = knownHeader.Descriptor.GetHeaderValue(value.Select(c => (byte)c).ToArray(), valueEncoding: null);
                Assert.Same(v1, v2);
            }
        }

        [Theory]
        [InlineData("Content-Type", "application/jsot")]
        [InlineData("Content-Type", "application/jsons")]
        public void GetKnownHeaderValue_Unknown_NotFound(string name, string value)
        {
            KnownHeader knownHeader = KnownHeaders.TryGetKnownHeader(name);
            Assert.NotNull(knownHeader);

            string v1 = knownHeader.Descriptor.GetHeaderValue(value.Select(c => (byte)c).ToArray(), valueEncoding: null);
            string v2 = knownHeader.Descriptor.GetHeaderValue(value.Select(c => (byte)c).ToArray(), valueEncoding: null);
            Assert.Equal(value, v1);
            Assert.Equal(value, v2);
            Assert.NotSame(v1, v2);
        }
    }
}
