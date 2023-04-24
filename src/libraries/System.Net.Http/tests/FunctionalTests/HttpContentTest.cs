// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    public class HttpContentTest : HttpClientHandlerTestBase
    {
        public HttpContentTest(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task CopyToAsync_CallWithMockContent_MockContentMethodCalled()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            var m = new MemoryStream();

            await content.CopyToAsync(m);

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.Equal(content.GetMockData(), m.ToArray());
        }

        [Fact]
        public async Task CopyToAsync_ThrowCustomExceptionInOverriddenMethod_ThrowsMockException()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            await Assert.ThrowsAsync<MockException>(() => t);
        }

        [Fact]
        public async Task CopyToAsync_ThrowObjectDisposedExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task CopyToAsync_ThrowIOExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new IOException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void CopyToAsync_ThrowCustomExceptionInOverriddenAsyncMethod_ExceptionBubblesUp()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInAsyncSerializeMethods);

            var m = new MemoryStream();
            Assert.Throws<MockException>(() => { content.CopyToAsync(m); });
        }

        [Fact]
        public async Task CopyToAsync_ThrowObjectDisposedExceptionInOverriddenAsyncMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task CopyToAsync_ThrowIOExceptionInOverriddenAsyncMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new IOException(), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.CopyToAsync(new MemoryStream());
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void CopyToAsync_MockContentReturnsNull_ThrowsInvalidOperationException()
        {
            // return 'null' when CopyToAsync() is called.
            var content = new MockContent(MockOptions.ReturnNullInCopyToAsync);
            var m = new MemoryStream();

            // The HttpContent derived class (MockContent in our case) must return a Task object when WriteToAsync()
            // is called. If not, HttpContent will throw.
            Assert.Throws<InvalidOperationException>(() => { content.CopyToAsync(m); });
        }

        [Fact]
        public async Task CopyToAsync_BufferContentFirst_UseBufferedStreamAsSource()
        {
            var data = new byte[10];
            var content = new MockContent(data);
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            var destination = new MemoryStream();
            await content.CopyToAsync(destination);

            // Our MockContent should not be called for the CopyTo() operation since the buffered stream should be
            // used.
            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.Equal(data.Length, destination.Length);
        }

        [Fact]
        public async Task CopyToAsync_Buffered_CanBeCanceled()
        {
            // Buffered CopyToAsync will pass the CT to the Stream.WriteAsync
            var content = new MockContent();

            Assert.Equal(0, content.SerializeToStreamAsyncCount);
            await content.LoadIntoBufferAsync();
            Assert.Equal(1, content.SerializeToStreamAsyncCount);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            using var ms = new MemoryStream();
            await Assert.ThrowsAsync<TaskCanceledException>(() => content.CopyToAsync(ms, cts.Token));
            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.Equal(0, content.CreateContentReadStreamCount);
        }

        [Fact]
        public async Task CopyToAsync_Unbuffered_CanBeCanceled()
        {
            // Unbuffered CopyToAsync will pass the CT to the SerializeToStreamAsync
            var content = new MockContent();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            using var ms = new MemoryStream();
            await Assert.ThrowsAsync<TaskCanceledException>(() => content.CopyToAsync(ms, cts.Token));
            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.Equal(0, content.CreateContentReadStreamCount);
        }

        [Fact]
        public void TryComputeLength_RetrieveContentLength_ComputeLengthShouldBeCalled()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            Assert.Equal(content.GetMockData().Length, content.Headers.ContentLength);
            Assert.Equal(1, content.TryComputeLengthCount);
        }

        [Fact]
        public async Task TryComputeLength_RetrieveContentLengthFromBufferedContent_ComputeLengthIsNotCalled()
        {
            var content = new MockContent();
            await content.LoadIntoBufferAsync();

            Assert.Equal(content.GetMockData().Length, content.Headers.ContentLength);

            // Called once to determine the size of the buffer.
            Assert.Equal(1, content.TryComputeLengthCount);
        }

        [Fact]
        public void TryComputeLength_ThrowCustomExceptionInOverriddenMethod_ExceptionBubblesUpToCaller()
        {
            var content = new MockContent(MockOptions.ThrowInTryComputeLength);

            var m = new MemoryStream();
            Assert.Throws<MockException>(() => content.Headers.ContentLength);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAsStreamAsync_GetFromUnbufferedContent_CreateContentReadStreamCalledOnce(bool readStreamAsync)
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            // Call multiple times: CreateContentReadStreamAsync() should be called only once.
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);
            stream = await content.ReadAsStreamAsync(readStreamAsync);
            stream = await content.ReadAsStreamAsync(readStreamAsync);

            Assert.Equal(1, content.CreateContentReadStreamCount);
            Assert.Equal(content.GetMockData().Length, stream.Length);
            Stream stream2 = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Same(stream, stream2);
        }

        [Fact]
        public async Task ReadAsStreamAsync_GetFromUnbufferedContent_SucceedsAfterReadAsStream()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            // Call multiple times: CreateContentReadStream() should be called only once.
            Stream stream = content.ReadAsStream();
            stream = content.ReadAsStream();
            Assert.Equal(1, content.CreateContentReadStreamCount);

            stream = await content.ReadAsStreamAsync();
            stream = await content.ReadAsStreamAsync();

            Assert.Equal(1, content.CreateContentReadStreamCount);
            Assert.Equal(content.GetMockData().Length, stream.Length);
            Stream stream2 = await content.ReadAsStreamAsync();
            Assert.Same(stream, stream2);
        }

        [Fact]
        public void ReadAsStream_GetFromUnbufferedContent_ThrowsAfterReadAsStreamsAsync()
        {
            var content = new MockContent();

            var task = content.ReadAsStreamAsync();
            AssertExtensions.Throws<HttpRequestException>(() => content.ReadAsStream());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAsStreamAsync_GetFromBufferedContent_CreateContentReadStreamCalled(bool readStreamAsync)
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            await content.LoadIntoBufferAsync();

            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);

            Assert.Equal(0, content.CreateContentReadStreamCount);
            Assert.Equal(content.GetMockData().Length, stream.Length);
            Stream stream2 = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Same(stream, stream2);
            Assert.Equal(0, stream.Position);
            Assert.Equal((byte)'d', stream.ReadByte());
        }

        [Fact]
        public async Task ReadAsStreamAsync_GetFromBufferedContent_SucceedsAfterReadAsStream()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            await content.LoadIntoBufferAsync();

            // Call multiple times: CreateContentReadStream() should be called only once.
            Stream stream = content.ReadAsStream();
            stream = content.ReadAsStream();
            Assert.Equal(0, content.CreateContentReadStreamCount);

            stream = await content.ReadAsStreamAsync();
            stream = await content.ReadAsStreamAsync();

            Assert.Equal(0, content.CreateContentReadStreamCount);
            Assert.Equal(content.GetMockData().Length, stream.Length);
            Stream stream2 = await content.ReadAsStreamAsync();
            Assert.Same(stream, stream2);
        }

        [Fact]
        public async Task ReadAsStream_GetFromBufferedContent_ThrowsAfterReadAsStreamsAsync()
        {
            var content = new MockContent();
            await content.LoadIntoBufferAsync();

            var task = content.ReadAsStreamAsync();
            AssertExtensions.Throws<HttpRequestException>(() => content.ReadAsStream());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAsStreamAsync_FirstGetFromUnbufferedContentThenGetFromBufferedContent_SameStream(bool readStreamAsync)
        {
            var content = new MockContent(MockOptions.CanCalculateLength);

            Stream before = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Equal(1, content.CreateContentReadStreamCount);

            await content.LoadIntoBufferAsync();

            Stream after = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Equal(1, content.CreateContentReadStreamCount);

            // Note that ContentReadStream returns always the same stream. If the user gets the stream, buffers content,
            // and gets the stream again, the same instance is returned. Returning a different instance could be
            // confusing, even though there shouldn't be any real world scenario for retrieving the read stream both
            // before and after buffering content.
            Assert.Equal(before, after);
        }

        [SkipOnPlatform(TestPlatforms.Browser, "Browser doesn't support Synchronous reads")]
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAsStreamAsync_UseBaseImplementation_ContentGetsBufferedThenMemoryStreamReturned(bool readStreamAsync)
        {
            var content = new MockContent(MockOptions.DontOverrideCreateContentReadStream);
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);

            Assert.NotNull(stream);
            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream2 = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Same(stream, stream2);
            Assert.Equal(0, stream.Position);
            Assert.Equal((byte)'d', stream.ReadByte());
        }

        [Fact]
        public async Task LoadIntoBufferAsync_BufferSizeSmallerThanContentSizeWithCalculatedContentLength_ThrowsHttpRequestException()
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            Task t = content.LoadIntoBufferAsync(content.GetMockData().Length - 1);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_BufferSizeSmallerThanContentSizeWithNullContentLength_ThrowsHttpRequestException()
        {
            var content = new MockContent();
            Task t = content.LoadIntoBufferAsync(content.GetMockData().Length - 1);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoadIntoBufferAsync_CallOnMockContentWithCalculatedContentLength_CopyToAsyncMemoryStreamCalled(bool readStreamAsync)
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            Assert.NotNull(content.Headers.ContentLength);
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.False(stream.CanWrite);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoadIntoBufferAsync_CallOnMockContentWithNullContentLength_CopyToAsyncMemoryStreamCalled(bool readStreamAsync)
        {
            var content = new MockContent();
            Assert.Null(content.Headers.ContentLength);
            await content.LoadIntoBufferAsync();
            Assert.NotNull(content.Headers.ContentLength);
            Assert.Equal(content.MockData.Length, content.Headers.ContentLength);

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.False(stream.CanWrite);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoadIntoBufferAsync_CallOnMockContentWithLessLengthThanContentLengthHeader_BufferedStreamLengthMatchesActualLengthNotContentLengthHeaderValue(bool readStreamAsync)
        {
            byte[] data = "16 bytes of data"u8.ToArray();
            var content = new MockContent(data);
            content.Headers.ContentLength = 32; // Set the Content-Length header to a value > actual data length.
            Assert.Equal(32, content.Headers.ContentLength);

            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Assert.NotNull(content.Headers.ContentLength);
            Assert.Equal(32, content.Headers.ContentLength);
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Equal(data.Length, stream.Length);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoadIntoBufferAsync_CallMultipleTimesWithCalculatedContentLength_CopyToAsyncMemoryStreamCalledOnce(bool readStreamAsync)
        {
            var content = new MockContent(MockOptions.CanCalculateLength);
            await content.LoadIntoBufferAsync();
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.False(stream.CanWrite);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task LoadIntoBufferAsync_CallMultipleTimesWithNullContentLength_CopyToAsyncMemoryStreamCalledOnce(bool readStreamAsync)
        {
            var content = new MockContent();
            await content.LoadIntoBufferAsync();
            await content.LoadIntoBufferAsync();

            Assert.Equal(1, content.SerializeToStreamAsyncCount);
            Stream stream = await content.ReadAsStreamAsync(readStreamAsync);
            Assert.False(stream.CanWrite);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowCustomExceptionInOverriddenMethod_ThrowsMockException()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            await Assert.ThrowsAsync<MockException>(() => t);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowObjectDisposedExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowIOExceptionInOverriddenMethod_ThrowsWrappedHttpRequestException()
        {
            MockContent content = new MockContent(new IOException(), MockOptions.ThrowInSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Fact]
        public void LoadIntoBufferAsync_ThrowCustomExceptionInOverriddenAsyncMethod_ExceptionBubblesUpToCaller()
        {
            var content = new MockContent(new MockException(), MockOptions.ThrowInAsyncSerializeMethods);

            Assert.Throws<MockException>(() => { content.LoadIntoBufferAsync(); });
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowObjectDisposedExceptionInOverriddenAsyncMethod_ThrowsHttpRequestException()
        {
            var content = new MockContent(new ObjectDisposedException(""), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<ObjectDisposedException>(ex.InnerException);
        }

        [Fact]
        public async Task LoadIntoBufferAsync_ThrowIOExceptionInOverriddenAsyncMethod_ThrowsHttpRequestException()
        {
            var content = new MockContent(new IOException(), MockOptions.ThrowInAsyncSerializeMethods);

            Task t = content.LoadIntoBufferAsync();
            HttpRequestException ex = await Assert.ThrowsAsync<HttpRequestException>(() => t);
            Assert.IsType<IOException>(ex.InnerException);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Dispose_GetReadStreamThenDispose_ReadStreamGetsDisposed(bool readStreamAsync)
        {
            var content = new MockContent();
            MockMemoryStream s = (MockMemoryStream)await content.ReadAsStreamAsync(readStreamAsync);
            Assert.Equal(1, content.CreateContentReadStreamCount);

            Assert.Equal(0, s.DisposeCount);
            content.Dispose();
            Assert.Equal(1, s.DisposeCount);
        }

        [Fact]
        public void Dispose_DisposeContentThenAccessContentLength_Throw()
        {
            var content = new MockContent();

            // This is not really typical usage of the type, but let's make sure we consider also this case: The user
            // keeps a reference to the Headers property before disposing the content. Then after disposing, the user
            // accesses the ContentLength property.
            var headers = content.Headers;
            content.Dispose();
            Assert.Throws<ObjectDisposedException>(() => headers.ContentLength.ToString());
        }

        [Fact]
        public async Task CopyToAsync_UseStreamWriteByteWithBufferSizeSmallerThanContentSize_ThrowsHttpRequestException()
        {
            // MockContent uses stream.WriteByte() rather than stream.Write(): Verify that the max. buffer size
            // is also checked when using WriteByte().
            var content = new MockContent(MockOptions.UseWriteByteInCopyTo);
            Task t = content.LoadIntoBufferAsync(content.GetMockData().Length - 1);
            await Assert.ThrowsAsync<HttpRequestException>(() => t);
        }

        [Fact]
        public async Task ReadAsStringAsync_EmptyContent_EmptyString()
        {
            var content = new MockContent(new byte[0]);
            string actualContent = await content.ReadAsStringAsync();
            Assert.Equal(string.Empty, actualContent);
        }

        [Theory]
        [InlineData("invalid")]
        [InlineData("\"\"")]
        public async Task ReadAsStringAsync_SetInvalidCharset_ThrowsInvalidOperationException(string charset)
        {
            string sourceString = "some string";
            byte[] contentBytes = Encoding.UTF8.GetBytes(sourceString);

            var content = new MockContent(contentBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Headers.ContentType.CharSet = charset;

            // This will throw because we have an invalid charset.
            Task t = content.ReadAsStringAsync();
            await Assert.ThrowsAsync<InvalidOperationException>(() => t);
        }

        [Fact]
        public async Task ReadAsStringAsync_SetNoCharset_DefaultCharsetUsed()
        {
            // Assorted latin letters with diaeresis
            string sourceString = "\u00C4\u00E4\u00FC\u00DC";
            Encoding defaultEncoding = Encoding.GetEncoding("utf-8");
            byte[] contentBytes = defaultEncoding.GetBytes(sourceString);

            var content = new MockContent(contentBytes);

            // Reading the string should consider the charset of the 'Content-Type' header.
            string result = await content.ReadAsStringAsync();

            Assert.Equal(sourceString, result);
        }

        [Fact]
        public async Task ReadAsStringAsync_SetQuotedCharset_ParsesContent()
        {
            string sourceString = "some string";
            byte[] contentBytes = Encoding.UTF8.GetBytes(sourceString);

            var content = new MockContent(contentBytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
            content.Headers.ContentType.CharSet = "\"utf-8\"";

            string result = await content.ReadAsStringAsync();

            Assert.Equal(sourceString, result);
        }

        [Theory]
        [InlineData("\"\"invalid")]
        [InlineData("invalid\"\"")]
        [InlineData("\"\"invalid\"\"")]
        [InlineData("\"invalid")]
        [InlineData("invalid\"")]
        public async Task ReadAsStringAsync_SetInvalidContentTypeHeader_DefaultCharsetUsed(string charset)
        {
            // Assorted latin letters with diaeresis
            string sourceString = "\u00C4\u00E4\u00FC\u00DC";

            // Because the Content-Type header is invalid, we expect to default to UTF-8.
            byte[] contentBytes = Encoding.UTF8.GetBytes(sourceString);
            var content = new MockContent(contentBytes);

            Assert.True(content.Headers.TryAddWithoutValidation("Content-Type", $"text/plain;charset={charset}"));

            string result = await content.ReadAsStringAsync();

            Assert.Equal(sourceString, result);
        }

        [Fact]
        public async Task ReadAsByteArrayAsync_EmptyContent_EmptyArray()
        {
            var content = new MockContent(new byte[0]);
            byte[] bytes = await content.ReadAsByteArrayAsync();
            Assert.Equal(0, bytes.Length);
        }

        [Fact]
        public void Dispose_DisposedObjectThenAccessMembers_ThrowsObjectDisposedException()
        {
            var content = new MockContent();
            content.Dispose();

            var m = new MemoryStream();

            Assert.Throws<ObjectDisposedException>(() => { content.CopyToAsync(m); });
            Assert.Throws<ObjectDisposedException>(() => { content.CopyTo(m, null, default); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsByteArrayAsync(); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsStringAsync(); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsStreamAsync(); });
            Assert.Throws<ObjectDisposedException>(() => { content.ReadAsStream(); });
            Assert.Throws<ObjectDisposedException>(() => { content.LoadIntoBufferAsync(); });

            // Note that we don't throw when users access the Headers property. This is useful e.g. to be able to
            // read the headers of a content, even though the content is already disposed. Note that the .NET guidelines
            // only require members to throw ObjectDisposedException for members "that cannot be used after the object
            // has been disposed of".
            _output.WriteLine(content.Headers.ToString());
        }


        [Fact]
        public async Task ReadAsStringAsync_Buffered_IgnoresCancellationToken()
        {
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseContentRead);

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    string received = await response.Content.ReadAsStringAsync(cts.Token);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Fact]
        public async Task ReadAsStringAsync_Unbuffered_CanBeCanceled_AlreadyCanceledCts()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => response.Content.ReadAsStringAsync(cts.Token));
                },
                async server =>
                {
                    try
                    {
                        await server.AcceptConnectionSendResponseAndCloseAsync();
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                    }
                });
        }

        [Fact]
        public async Task ReadAsStringAsync_Unbuffered_CanBeCanceled()
        {
            var cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                    await Assert.ThrowsAsync<TaskCanceledException>(() => response.Content.ReadAsStringAsync(cts.Token));
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                        await connection.SendResponseAsync(LoopbackServer.GetHttpResponseHeaders(contentLength: 100));
                        await Task.Delay(250);
                        cts.Cancel();
                        await Task.Delay(500);
                        try
                        {
                            await connection.SendResponseAsync(new string('a', 100));
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                        }
                    });
                });
        }

        [Fact]
        public async Task ReadAsByteArrayAsync_Buffered_IgnoresCancellationToken()
        {
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseContentRead);

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    byte[] receivedBytes = await response.Content.ReadAsByteArrayAsync(cts.Token);
                    string received = Encoding.UTF8.GetString(receivedBytes);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Fact]
        public async Task ReadAsByteArrayAsync_Unbuffered_CanBeCanceled_AlreadyCanceledCts()
        {
            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    await Assert.ThrowsAsync<TaskCanceledException>(() => response.Content.ReadAsByteArrayAsync(cts.Token));
                },
                async server =>
                {
                    try
                    {
                        await server.AcceptConnectionSendResponseAndCloseAsync();
                    }
                    catch (Exception ex)
                    {
                        _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                    }
                });
        }

        [Fact]
        public async Task ReadAsByteArrayAsync_Unbuffered_CanBeCanceled()
        {
            var cts = new CancellationTokenSource();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                    await Assert.ThrowsAsync<TaskCanceledException>(() => response.Content.ReadAsByteArrayAsync(cts.Token));
                },
                async server =>
                {
                    await server.AcceptConnectionAsync(async connection =>
                    {
                        await connection.ReadRequestHeaderAsync();
                        await connection.SendResponseAsync(LoopbackServer.GetHttpResponseHeaders(contentLength: 100));
                        await Task.Delay(250);
                        cts.Cancel();
                        await Task.Delay(500);
                        try
                        {
                            await connection.SendResponseAsync(new string('a', 100));
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"Ignored exception:{Environment.NewLine}{ex}");
                        }
                    });
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAsStreamAsync_Buffered_IgnoresCancellationToken(bool readStreamAsync)
        {
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseContentRead);

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    Stream receivedStream = await response.Content.ReadAsStreamAsync(readStreamAsync, cts.Token);
                    Assert.IsType<MemoryStream>(receivedStream);
                    byte[] receivedBytes = (receivedStream as MemoryStream).ToArray();
                    string received = Encoding.UTF8.GetString(receivedBytes);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ReadAsStreamAsync_Unbuffered_IgnoresCancellationToken(bool readStreamAsync)
        {
            if(PlatformDetection.IsBrowser && !readStreamAsync)
            {
                // syncronous operations are not supported on Browser
                return;
            }
            string content = Guid.NewGuid().ToString();

            await LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient httpClient = CreateHttpClient();

                    HttpResponseMessage response = await httpClient.GetAsync(
                        uri,
                        HttpCompletionOption.ResponseHeadersRead);

                    var cts = new CancellationTokenSource();
                    cts.Cancel();

                    Stream receivedStream = await response.Content.ReadAsStreamAsync(readStreamAsync, cts.Token);
                    var ms = new MemoryStream();
                    await receivedStream.CopyToAsync(ms);
                    byte[] receivedBytes = ms.ToArray();
                    string received = Encoding.UTF8.GetString(receivedBytes);
                    Assert.Equal(content, received);
                },
                async server =>
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync(content: content);
                });
        }

        [Fact]
        public async Task ReadAsStreamAsync_Unbuffered_CustomContent_CanBeCanceled()
        {
            var content = new MockContent();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() => content.ReadAsStreamAsync(cts.Token));
        }

        [Fact]
        public void ReadAsStream_Unbuffered_CustomContent_CanBeCanceled()
        {
            var content = new MockContent();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(() => content.ReadAsStream(cts.Token));
        }

        #region Helper methods

        private byte[] EncodeStringWithBOM(Encoding encoding, string str)
        {
            byte[] rawBytes = encoding.GetBytes(str);
            byte[] preamble = encoding.GetPreamble(); // Get the correct BOM characters
            byte[] contentBytes = new byte[preamble.Length + rawBytes.Length];
            Array.Copy(preamble, contentBytes, preamble.Length);
            Array.Copy(rawBytes, 0, contentBytes, preamble.Length, rawBytes.Length);
            return contentBytes;
        }

        public class MockException : Exception
        {
            public MockException() { }
            public MockException(string message) : base(message) { }
            public MockException(string message, Exception inner) : base(message, inner) { }
        }

        [Flags]
        private enum MockOptions
        {
            None = 0x0,
            ThrowInSerializeMethods = 0x1,
            ReturnNullInCopyToAsync = 0x2,
            UseWriteByteInCopyTo = 0x4,
            DontOverrideCreateContentReadStream = 0x8,
            CanCalculateLength = 0x10,
            ThrowInTryComputeLength = 0x20,
            ThrowInAsyncSerializeMethods = 0x40
        }

        private class MockContent : HttpContent
        {
            private byte[] _mockData;
            private MockOptions _options;
            private Exception _customException;

            public int TryComputeLengthCount { get; private set; }
            public int SerializeToStreamAsyncCount { get; private set; }
            public int CreateContentReadStreamCount { get; private set; }
            public int DisposeCount { get; private set; }

            public byte[] MockData
            {
                get { return _mockData; }
            }

            public MockContent()
                : this((byte[])null, MockOptions.None)
            {
            }

            public MockContent(byte[] mockData)
                : this(mockData, MockOptions.None)
            {
            }

            public MockContent(MockOptions options)
                : this((byte[])null, options)
            {
            }

            public MockContent(Exception customException, MockOptions options)
                : this((byte[])null, options)
            {
                _customException = customException;
            }

            public MockContent(byte[] mockData, MockOptions options)
            {
                _options = options;
                _mockData = mockData ?? "data"u8.ToArray();
            }

            public byte[] GetMockData()
            {
                return _mockData;
            }

            protected override bool TryComputeLength(out long length)
            {
                TryComputeLengthCount++;

                if ((_options & MockOptions.ThrowInTryComputeLength) != 0)
                {
                    throw new MockException();
                }

                if ((_options & MockOptions.CanCalculateLength) != 0)
                {
                    length = _mockData.Length;
                    return true;
                }
                else
                {
                    length = 0;
                    return false;
                }
            }

            protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
                => SerializeToStreamAsync(stream, context, cancellationToken).GetAwaiter().GetResult();

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context) =>
                throw new NotImplementedException(); // The overload with the CancellationToken should be called

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context, CancellationToken cancellationToken)
            {
                SerializeToStreamAsyncCount++;

                if (cancellationToken.IsCancellationRequested)
                {
                    return Task.FromCanceled(cancellationToken);
                }

                if ((_options & MockOptions.ReturnNullInCopyToAsync) != 0)
                {
                    return null;
                }

                if ((_options & MockOptions.ThrowInAsyncSerializeMethods) != 0)
                {
                    throw _customException;
                }

                return Task.Run(() =>
                {
                    CheckThrow();
                    return stream.WriteAsync(_mockData, 0, _mockData.Length);
                });
            }

            protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
            {
                CreateContentReadStreamCount++;

                if ((_options & MockOptions.DontOverrideCreateContentReadStream) != 0)
                {
                    return base.CreateContentReadStream(cancellationToken);
                }
                else
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cancellationToken);
                    }

                    return new MockMemoryStream(_mockData, 0, _mockData.Length, false);
                }
            }

            protected override Task<Stream> CreateContentReadStreamAsync(CancellationToken cancellationToken)
            {
                CreateContentReadStreamCount++;

                if ((_options & MockOptions.DontOverrideCreateContentReadStream) != 0)
                {
                    return base.CreateContentReadStreamAsync();
                }
                else
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return Task.FromCanceled<Stream>(cancellationToken);
                    }

                    return Task.FromResult<Stream>(new MockMemoryStream(_mockData, 0, _mockData.Length, false));
                }
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }

            private void CheckThrow()
            {
                if ((_options & MockOptions.ThrowInSerializeMethods) != 0)
                {
                    throw _customException;
                }
            }
        }

        private class MockMemoryStream : MemoryStream
        {
            public int DisposeCount { get; private set; }

            public MockMemoryStream(byte[] buffer, int index, int count, bool writable)
                : base(buffer, index, count, writable)
            {
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCount++;
                base.Dispose(disposing);
            }
        }

        #endregion
    }
}
