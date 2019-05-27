// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpResponseMessageTest
    {
        [Fact]
        public void Ctor_Default_CorrectDefaults()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.Equal(HttpStatusCode.OK, rm.StatusCode);
                Assert.Equal("OK", rm.ReasonPhrase);
                Assert.Equal(new Version(1, 1), rm.Version);
                Assert.Equal(null, rm.Content);
                Assert.Equal(null, rm.RequestMessage);
            }
        }

        [Fact]
        public void Ctor_SpecifiedValues_CorrectValues()
        {
            using (var rm = new HttpResponseMessage(HttpStatusCode.Accepted))
            {
                Assert.Equal(HttpStatusCode.Accepted, rm.StatusCode);
                Assert.Equal("Accepted", rm.ReasonPhrase);
                Assert.Equal(new Version(1, 1), rm.Version);
                Assert.Equal(null, rm.Content);
                Assert.Equal(null, rm.RequestMessage);
            }
        }

        [Fact]
        public void Ctor_InvalidStatusCodeRange_Throw()
        {
            int x = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => new HttpResponseMessage((HttpStatusCode)x));
            x = 1000;
            Assert.Throws<ArgumentOutOfRangeException>(() => new HttpResponseMessage((HttpStatusCode)x));
        }

        [Fact]
        public void Dispose_DisposeObject_ContentGetsDisposedAndSettersWillThrowButGettersStillWork()
        {
            using (var rm = new HttpResponseMessage(HttpStatusCode.OK))
            {
                var content = new MockContent();
                rm.Content = content;
                Assert.False(content.IsDisposed);

                rm.Dispose();
                rm.Dispose(); // Multiple calls don't throw.

                Assert.True(content.IsDisposed);
                Assert.Throws<ObjectDisposedException>(() => { rm.StatusCode = HttpStatusCode.BadRequest; });
                Assert.Throws<ObjectDisposedException>(() => { rm.ReasonPhrase = "Bad Request"; });
                Assert.Throws<ObjectDisposedException>(() => { rm.Version = new Version(1, 0); });
                Assert.Throws<ObjectDisposedException>(() => { rm.Content = null; });

                // Property getters should still work after disposing.
                Assert.Equal(HttpStatusCode.OK, rm.StatusCode);
                Assert.Equal("OK", rm.ReasonPhrase);
                Assert.Equal(new Version(1, 1), rm.Version);
                Assert.Equal(content, rm.Content);
            }
        }

        [Fact]
        public void Headers_ReadProperty_HeaderCollectionInitialized()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.NotNull(rm.Headers);
            }
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData(HttpStatusCode.OK, true)]
        [InlineData(HttpStatusCode.PartialContent, true)]
        [InlineData(HttpStatusCode.MultipleChoices, false)]
        [InlineData(HttpStatusCode.Continue, false)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.BadGateway, false)]
        public void IsSuccessStatusCode_VariousStatusCodes_ReturnTrueFor2xxFalseOtherwise(HttpStatusCode? status, bool expectedSuccess)
        {
            using (var m = status.HasValue ? new HttpResponseMessage(status.Value) : new HttpResponseMessage())
            {
                Assert.Equal(expectedSuccess, m.IsSuccessStatusCode);
            }
        }

        [Fact]
        public void EnsureSuccessStatusCode_VariousStatusCodes_ThrowIfNot2xx()
        {
            using (var m = new HttpResponseMessage(HttpStatusCode.MultipleChoices))
            {
                Assert.Throws<HttpRequestException>(() => m.EnsureSuccessStatusCode());
            }

            using (var m = new HttpResponseMessage(HttpStatusCode.BadGateway))
            {
                Assert.Throws<HttpRequestException>(() => m.EnsureSuccessStatusCode());
            }

            using (var response = new HttpResponseMessage(HttpStatusCode.OK))
            {
                Assert.Same(response, response.EnsureSuccessStatusCode());
            }
        }

        [Fact]
        public void EnsureSuccessStatusCode_SuccessStatusCode_ContentIsNotDisposed()
        {
            using (var response200 = new HttpResponseMessage(HttpStatusCode.OK))
            {
                response200.Content = new MockContent();
                response200.EnsureSuccessStatusCode(); // No exception.
                Assert.False((response200.Content as MockContent).IsDisposed);
            }
        }

        [Fact]
        public void EnsureSuccessStatusCode_NonSuccessStatusCode_ContentIsNotDisposed()
        {
            using (var response404 = new HttpResponseMessage(HttpStatusCode.NotFound))
            {
                response404.Content = new MockContent();
                Assert.Throws<HttpRequestException>(() => response404.EnsureSuccessStatusCode());
                Assert.False((response404.Content as MockContent).IsDisposed);
            }
        }

        [Fact]
        public void Properties_SetPropertiesAndGetTheirValue_MatchingValues()
        {
            using (var rm = new HttpResponseMessage())
            {
                var content = new MockContent();
                HttpStatusCode statusCode = HttpStatusCode.LengthRequired;
                string reasonPhrase = "Length Required";
                var version = new Version(1, 0);
                var requestMessage = new HttpRequestMessage();

                rm.Content = content;
                rm.ReasonPhrase = reasonPhrase;
                rm.RequestMessage = requestMessage;
                rm.StatusCode = statusCode;
                rm.Version = version;

                Assert.Equal(content, rm.Content);
                Assert.Equal(reasonPhrase, rm.ReasonPhrase);
                Assert.Equal(requestMessage, rm.RequestMessage);
                Assert.Equal(statusCode, rm.StatusCode);
                Assert.Equal(version, rm.Version);

                Assert.NotNull(rm.Headers);
            }
        }

        [Fact]
        public void Version_SetToNull_ThrowsArgumentNullException()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.Throws<ArgumentNullException>(() => { rm.Version = null; });
            }
        }

        [Fact]
        public void ReasonPhrase_ContainsCRChar_ThrowsFormatException()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.Throws<FormatException>(() => { rm.ReasonPhrase = "text\rtext"; });
            }
        }

        [Fact]
        public void ReasonPhrase_ContainsLFChar_ThrowsFormatException()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.Throws<FormatException>(() => { rm.ReasonPhrase = "text\ntext"; });
            }
        }

        [Fact]
        public void ReasonPhrase_SetToNull_Accepted()
        {
            using (var rm = new HttpResponseMessage())
            {
                rm.ReasonPhrase = null;
                Assert.Equal("OK", rm.ReasonPhrase); // Default provided.
            }
        }

        [Fact]
        public void ReasonPhrase_UnknownStatusCode_Null()
        {
            using (var rm = new HttpResponseMessage())
            {
                rm.StatusCode = (HttpStatusCode)150; // Default reason unknown.
                Assert.Null(rm.ReasonPhrase); // No default provided.
            }
        }

        [Fact]
        public void ReasonPhrase_SetToEmpty_Accepted()
        {
            using (var rm = new HttpResponseMessage())
            {
                rm.ReasonPhrase = string.Empty;
                Assert.Equal(string.Empty, rm.ReasonPhrase);
            }
        }

        [Fact]
        public void Content_SetToNull_Accepted()
        {
            using (var rm = new HttpResponseMessage())
            {
                rm.Content = null;
                Assert.Null(rm.Content);
            }
        }

        [Fact]
        public void StatusCode_InvalidStatusCodeRange_ThrowsArgumentOutOfRangeException()
        {
            using (var rm = new HttpResponseMessage())
            {
                int x = -1;
                Assert.Throws<ArgumentOutOfRangeException>(() => { rm.StatusCode = (HttpStatusCode)x; });
                x = 1000;
                Assert.Throws<ArgumentOutOfRangeException>(() => { rm.StatusCode = (HttpStatusCode)x; });
            }
        }

        [Fact]
        public void ToString_DefaultAndNonDefaultInstance_DumpAllFields()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.Equal("StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: <null>, Headers:\r\n{\r\n}", rm.ToString());

                rm.StatusCode = HttpStatusCode.BadRequest;
                rm.ReasonPhrase = null;
                rm.Version = new Version(1, 0);
                rm.Content = new StringContent("content");

                // Note that there is no Content-Length header: The reason is that the value for Content-Length header
                // doesn't get set by StringContent..ctor, but only if someone actually accesses the ContentLength property.
                Assert.Equal(
                    "StatusCode: 400, ReasonPhrase: 'Bad Request', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:\r\n" +
                    "{\r\n" +
                    "  Content-Type: text/plain; charset=utf-8\r\n" +
                    "}", rm.ToString());

                rm.Headers.AcceptRanges.Add("bytes");
                rm.Headers.AcceptRanges.Add("pages");
                rm.Headers.Add("Custom-Response-Header", "value1");
                rm.Content.Headers.Add("Custom-Content-Header", "value2");

                Assert.Equal(
                    "StatusCode: 400, ReasonPhrase: 'Bad Request', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:\r\n" +
                    "{\r\n" +
                    "  Accept-Ranges: bytes\r\n" +
                    "  Accept-Ranges: pages\r\n" +
                    "  Custom-Response-Header: value1\r\n" +
                    "  Content-Type: text/plain; charset=utf-8\r\n" +
                    "  Custom-Content-Header: value2\r\n" +
                    "}", rm.ToString());
            }
        }

        #region Helper methods

        private class MockContent : HttpContent
        {
            public bool IsDisposed { get; private set; }

            protected override bool TryComputeLength(out long length)
            {
                throw new NotImplementedException();
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        #endregion   
    }
}
