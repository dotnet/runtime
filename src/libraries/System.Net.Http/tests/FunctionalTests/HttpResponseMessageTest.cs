// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
                Assert.NotNull(rm.Content);
                Assert.Null(rm.RequestMessage);
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
                Assert.NotNull(rm.Content);
                Assert.Null(rm.RequestMessage);
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
                var ex = Assert.Throws<HttpRequestException>(() => m.EnsureSuccessStatusCode());
                Assert.Equal(HttpStatusCode.MultipleChoices, ex.StatusCode);
                Assert.Contains(((int)HttpStatusCode.MultipleChoices).ToString(), ex.Message);
                Assert.Contains("(", ex.Message);
            }

            using (var m = new HttpResponseMessage(HttpStatusCode.BadGateway))
            {
                var ex = Assert.Throws<HttpRequestException>(() => m.EnsureSuccessStatusCode());
                Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
                Assert.Contains(((int)HttpStatusCode.BadGateway).ToString(), ex.Message);
                Assert.Contains("(", ex.Message);
            }

            using (var m = new HttpResponseMessage(HttpStatusCode.BadGateway))
            {
                m.ReasonPhrase = " \t ";
                var ex = Assert.Throws<HttpRequestException>(() => m.EnsureSuccessStatusCode());
                Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
                Assert.Contains(((int)HttpStatusCode.BadGateway).ToString(), ex.Message);
                Assert.DoesNotContain("(", ex.Message);
                Assert.DoesNotContain(" \t ", ex.Message);
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
                HttpContent c1 = rm.Content;
                Assert.Same(c1, rm.Content);

                rm.Content = null;

                HttpContent c2 = rm.Content;
                Assert.Same(c2, rm.Content);

                Assert.NotSame(c1, c2);
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
        public async Task DefaultContent_ReadableNotWritable_Success()
        {
            var resp = new HttpResponseMessage();

            HttpContent c = resp.Content;
            Assert.NotNull(c);
            Assert.Same(c, resp.Content);
            Assert.NotSame(resp.Content, new HttpResponseMessage().Content);

            Assert.Equal(0, c.Headers.ContentLength);

            Task<Stream> t = c.ReadAsStreamAsync();
            Assert.Equal(TaskStatus.RanToCompletion, t.Status);

            Stream s = await t;
            Assert.NotNull(s);

            Assert.Equal(-1, s.ReadByte());
            Assert.Equal(0, s.Read(new byte[1], 0, 1));
            Assert.Equal(0, await s.ReadAsync(new byte[1], 0, 1));
            Assert.Equal(0, await s.ReadAsync(new Memory<byte>(new byte[1])));

            Assert.Throws<NotSupportedException>(() => s.WriteByte(0));
            Assert.Throws<NotSupportedException>(() => s.Write(new byte[1], 0, 1));
            await Assert.ThrowsAsync<NotSupportedException>(() => s.WriteAsync(new byte[1], 0, 1));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await s.WriteAsync(new ReadOnlyMemory<byte>(new byte[1])));
        }

        [Fact]
        public void ToString_DefaultAndNonDefaultInstance_DumpAllFields()
        {
            using (var rm = new HttpResponseMessage())
            {
                Assert.Equal($"StatusCode: 200, ReasonPhrase: 'OK', Version: 1.1, Content: <null>, Headers:{Environment.NewLine}{{{Environment.NewLine}}}", rm.ToString());

                rm.StatusCode = HttpStatusCode.BadRequest;
                rm.ReasonPhrase = null;
                rm.Version = new Version(1, 0);
                rm.Content = new StringContent("content");

                // Note that there is no Content-Length header: The reason is that the value for Content-Length header
                // doesn't get set by StringContent..ctor, but only if someone actually accesses the ContentLength property.
                Assert.Equal(
                    "StatusCode: 400, ReasonPhrase: 'Bad Request', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:" + Environment.NewLine +
                    "{" + Environment.NewLine +
                    "  Content-Type: text/plain; charset=utf-8" + Environment.NewLine +
                    "}", rm.ToString());

                rm.Headers.AcceptRanges.Add("bytes");
                rm.Headers.AcceptRanges.Add("pages");
                rm.Headers.Add("Custom-Response-Header", "value1");
                rm.Content.Headers.Add("Custom-Content-Header", "value2");

                Assert.Equal(
                    "StatusCode: 400, ReasonPhrase: 'Bad Request', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:" + Environment.NewLine +
                    "{" + Environment.NewLine +
                    "  Accept-Ranges: bytes" + Environment.NewLine +
                    "  Accept-Ranges: pages" + Environment.NewLine +
                    "  Custom-Response-Header: value1" + Environment.NewLine +
                    "  Content-Type: text/plain; charset=utf-8" + Environment.NewLine +
                    "  Custom-Content-Header: value2" + Environment.NewLine +
                    "}", rm.ToString());

                rm.TrailingHeaders.Add("Custom-Trailing-Header", "value3");
                rm.TrailingHeaders.Add("Content-MD5", "Q2hlY2sgSW50ZWdyaXR5IQ==");

                Assert.Equal(
                    "StatusCode: 400, ReasonPhrase: 'Bad Request', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:" + Environment.NewLine +
                    "{" + Environment.NewLine +
                    "  Accept-Ranges: bytes" + Environment.NewLine +
                    "  Accept-Ranges: pages" + Environment.NewLine +
                    "  Custom-Response-Header: value1" + Environment.NewLine +
                    "  Content-Type: text/plain; charset=utf-8" + Environment.NewLine +
                    "  Custom-Content-Header: value2" + Environment.NewLine +
                    "}, Trailing Headers:" + Environment.NewLine +
                    "{" + Environment.NewLine +
                    "  Custom-Trailing-Header: value3" + Environment.NewLine +
                    "  Content-MD5: Q2hlY2sgSW50ZWdyaXR5IQ==" + Environment.NewLine +
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
