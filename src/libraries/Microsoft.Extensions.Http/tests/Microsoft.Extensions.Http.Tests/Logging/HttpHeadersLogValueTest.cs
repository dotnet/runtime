// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Xunit;

namespace Microsoft.Extensions.Http.Logging
{
    public class HttpHeadersLogValueTest
    {
        [Fact]
        public void HttpHeadersLogValue_ToString_HidesOnlyLogSensitiveHeadersValue()
        {
            // Arrange
            var headers = new TestHttpHeaders
            {
                { "secureHeader1", "value1" },
                { "unsecureHeader1", "value1" }
            };
            var contentHeaders = new TestHttpHeaders
            {
                { "unsecureHeader2", "value2" },
                { "secureHeader2", "value2" }
            };
            var headersToRedact = new HashSet<string>
            {
                "secureHeader1",
                "secureHeader2",
            };
            var sensitiveHeaders = new HashSet<string>(headersToRedact, StringComparer.OrdinalIgnoreCase);

            Func<string, bool> shouldRedactHeaderValue = (header) => sensitiveHeaders.Contains(header);

            var httpHeadersLogValue = new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, headers, contentHeaders, shouldRedactHeaderValue);

            // Act
            var result = httpHeadersLogValue.ToString();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(
                "Request Headers:" + Environment.NewLine +
                "secureHeader1: *" + Environment.NewLine +
                "unsecureHeader1: value1" + Environment.NewLine +
                "unsecureHeader2: value2" + Environment.NewLine +
                "secureHeader2: *" + Environment.NewLine,
                result);

            // Redaction is applied to the structured values, not just to the formatted string.
            Assert.Equal("secureHeader1", httpHeadersLogValue[0].Key);
            Assert.Equal("*", httpHeadersLogValue[0].Value);
            Assert.Equal("secureHeader2", httpHeadersLogValue[3].Key);
            Assert.Equal("*", httpHeadersLogValue[3].Value);
        }

#if NET
        [Fact]
        public void HttpHeadersLogValue_DoesNotValidateHeaderValues()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
            request.Headers.TryAddWithoutValidation("Accept", "application/vnd.example+json;version=1");

            var httpHeadersLogValue = new HttpHeadersLogValue(HttpHeadersLogValue.Kind.Request, request.Headers, contentHeaders: null, _ => false);

            Assert.Equal(
                "Request Headers:" + Environment.NewLine +
                "Accept: application/vnd.example+json;version=1" + Environment.NewLine,
                httpHeadersLogValue.ToString());

            Assert.True(request.Headers.NonValidated.TryGetValues("Accept", out HeaderStringValues values));
            Assert.Equal("application/vnd.example+json;version=1", Assert.Single(values));
        }
#endif

        private class TestHttpHeaders : HttpHeaders { }
    }
}
