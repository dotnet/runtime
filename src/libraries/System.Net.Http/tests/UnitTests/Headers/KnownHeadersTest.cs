// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
