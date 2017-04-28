// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpRequestStreamTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private GetContextHelper _helper;

        public HttpRequestStreamTests()
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _helper = new GetContextHelper(_listener, _factory.ListeningUrl);
        }

        public void Dispose()
        {
            _factory.Dispose();
            _helper.Dispose();
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true, "")]
        [InlineData(false, "")]
        [InlineData(true, "Non-Empty")]
        [InlineData(false, "Non-Empty")]
        public async Task Read_FullLengthAsynchronous_Success(bool transferEncodingChunked, string text)
        {
            byte[] expected = Encoding.UTF8.GetBytes(text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(text));

                HttpListenerContext context = await contextTask;
                if (transferEncodingChunked)
                {
                    Assert.Equal(-1, context.Request.ContentLength64);
                    Assert.Equal("chunked", context.Request.Headers["Transfer-Encoding"]);
                }
                else
                {
                    Assert.Equal(expected.Length, context.Request.ContentLength64);
                    Assert.Null(context.Request.Headers["Transfer-Encoding"]);
                }

                byte[] buffer = new byte[expected.Length];
                int bytesRead = await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected, buffer);

                // Subsequent reads don't do anything.
                Assert.Equal(0, await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(expected, buffer);

                context.Response.Close();
                using (HttpResponseMessage response = await clientTask)
                {
                    Assert.Equal(200, (int)response.StatusCode);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true, "")]
        [InlineData(false, "")]
        [InlineData(true, "Non-Empty")]
        [InlineData(false, "Non-Empty")]
        public async Task Read_FullLengthSynchronous_Success(bool transferEncodingChunked, string text)
        {
            byte[] expected = Encoding.UTF8.GetBytes(text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(text));

                HttpListenerContext context = await contextTask;
                if (transferEncodingChunked)
                {
                    Assert.Equal(-1, context.Request.ContentLength64);
                    Assert.Equal("chunked", context.Request.Headers["Transfer-Encoding"]);
                }
                else
                {
                    Assert.Equal(expected.Length, context.Request.ContentLength64);
                    Assert.Null(context.Request.Headers["Transfer-Encoding"]);
                }

                byte[] buffer = new byte[expected.Length];
                int bytesRead = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected, buffer);

                // Subsequent reads don't do anything.
                Assert.Equal(0, context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Equal(expected, buffer);

                context.Response.Close();
                using (HttpResponseMessage response = await clientTask)
                {
                    Assert.Equal(200, (int)response.StatusCode);
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // Different behaviour when not chunked, that needs investigation.
        public async Task Read_LargeLengthAsynchronous_Success(bool transferEncodingChunked)
        {
            string text = new string('a', 128 * 1024 + 1); // More than 128kb
            byte[] expected = Encoding.UTF8.GetBytes(text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(text));

                HttpListenerContext context = await contextTask;

                // If the size is greater than 128K, then we limit the size, and have to do multiple reads on
                // Windows, which uses http.sys internally.
                byte[] buffer = new byte[expected.Length];
                int bytesRead = await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length);
                if (PlatformDetection.IsWindows)
                {
                    Assert.Equal(expected.Length - 1, bytesRead);
                    Assert.NotEqual(expected, buffer);

                    bytesRead = await context.Request.InputStream.ReadAsync(buffer, buffer.Length - 1, 1);
                    Assert.Equal(1, bytesRead);
                    Assert.Equal(expected, buffer);
                }
                else
                {
                    Assert.Equal(expected.Length, bytesRead);
                    Assert.Equal(expected, buffer);
                }

                // Subsequent reads don't do anything.
                Assert.Equal(0, await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(expected, buffer);

                context.Response.Close();
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // Different behaviour when not chunked, that needs investigation.
        public async Task Read_LargeLengthSynchronous_Success(bool transferEncodingChunked)
        {
            string text = new string('a', 128 * 1024 + 1); // More than 128kb
            byte[] expected = Encoding.UTF8.GetBytes(text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(text));

                HttpListenerContext context = await contextTask;

                // If the size is greater than 128K, then we limit the size, and have to do multiple reads on
                // Windows, which uses http.sys internally.
                byte[] buffer = new byte[expected.Length];
                int bytesRead = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                if (PlatformDetection.IsWindows)
                {
                    Assert.Equal(expected.Length - 1, bytesRead);
                    Assert.NotEqual(expected, buffer);

                    bytesRead = context.Request.InputStream.Read(buffer, buffer.Length - 1, 1);
                    Assert.Equal(1, bytesRead);
                    Assert.Equal(expected, buffer);
                }
                else
                {
                    Assert.Equal(expected.Length, bytesRead);
                    Assert.Equal(expected, buffer);
                }

                // Subsequent reads don't do anything.
                Assert.Equal(0, context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Equal(expected, buffer);

                context.Response.Close();
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_TooMuchAsynchronous_Success(bool transferEncodingChunked)
        {
            const string Text = "Some-String";
            byte[] expected = Encoding.UTF8.GetBytes(Text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(Text));

                HttpListenerContext context = await contextTask;

                byte[] buffer = new byte[expected.Length + 5];
                int bytesRead = await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected.Concat(new byte[5]), buffer);

                context.Response.Close();
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_TooMuchSynchronous_Success(bool transferEncodingChunked)
        {
            const string Text = "Some-String";
            byte[] expected = Encoding.UTF8.GetBytes(Text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(Text));

                HttpListenerContext context = await contextTask;

                byte[] buffer = new byte[expected.Length + 5];
                int bytesRead = context.Request.InputStream.Read(buffer, 0, buffer.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected.Concat(new byte[5]), buffer);

                context.Response.Close();
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_NotEnoughThenCloseAsynchronous_Success(bool transferEncodingChunked)
        {
            const string Text = "Some-String";
            byte[] expected = Encoding.UTF8.GetBytes(Text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(Text));

                HttpListenerContext context = await contextTask;

                byte[] buffer = new byte[expected.Length - 5];
                int bytesRead = await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length);
                Assert.Equal(buffer.Length, bytesRead);

                context.Response.Close();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_Disposed_ReturnsZero(bool transferEncodingChunked)
        {
            const string Text = "Some-String";
            int bufferSize = Encoding.UTF8.GetByteCount(Text);
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent(Text));

                HttpListenerContext context = await contextTask;
                context.Request.InputStream.Close();

                byte[] buffer = new byte[bufferSize];
                Assert.Equal(0, context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Equal(new byte[bufferSize], buffer);

                IAsyncResult result = context.Request.InputStream.BeginRead(buffer, 0, buffer.Length, null, null);
                Assert.Equal(0, context.Request.InputStream.EndRead(result));
                Assert.Equal(new byte[bufferSize], buffer);

                context.Response.Close();
            }
        }

        [Fact]
        public async Task CanSeek_Get_ReturnsFalse()
        {
            HttpListenerRequest response = await _helper.GetRequest(chunked: true);
            using (Stream inputStream = response.InputStream)
            {
                Assert.False(inputStream.CanSeek);

                Assert.Throws<NotSupportedException>(() => inputStream.Length);
                Assert.Throws<NotSupportedException>(() => inputStream.SetLength(1));

                Assert.Throws<NotSupportedException>(() => inputStream.Position);
                Assert.Throws<NotSupportedException>(() => inputStream.Position = 1);

                Assert.Throws<NotSupportedException>(() => inputStream.Seek(0, SeekOrigin.Begin));
            }
        }

        [Fact]
        public async Task CanRead_Get_ReturnsTrue()
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked: true);
            using (Stream inputStream = request.InputStream)
            {
                Assert.True(inputStream.CanRead);
            }
        }

        [Fact]
        public async Task CanWrite_Get_ReturnsFalse()
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked: true);
            using (Stream inputStream = request.InputStream)
            {
                Assert.False(inputStream.CanWrite);

                Assert.Throws<InvalidOperationException>(() => inputStream.Write(new byte[0], 0, 0));
                await Assert.ThrowsAsync<InvalidOperationException>(() => inputStream.WriteAsync(new byte[0], 0, 0));
                Assert.Throws<InvalidOperationException>(() => inputStream.EndWrite(null));

                // Flushing the output stream is a no-op.
                inputStream.Flush();
                Assert.Equal(Task.CompletedTask, inputStream.FlushAsync(CancellationToken.None));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_NullBuffer_ThrowsArgumentNullException(bool chunked)
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked);
            using (Stream inputStream = request.InputStream)
            {
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => inputStream.Read(null, 0, 0));
                await Assert.ThrowsAsync<ArgumentNullException>("buffer", () => inputStream.ReadAsync(null, 0, 0));
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(3, true)]
        [InlineData(-1, false)]
        [InlineData(3, false)]
        public async Task Read_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset, bool chunked)
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked);
            using (Stream inputStream = request.InputStream)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => inputStream.Read(new byte[2], offset, 0));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>("offset", () => inputStream.ReadAsync(new byte[2], offset, 0));
            }
        }

        [Theory]
        [InlineData(0, 3, true)]
        [InlineData(1, 2, true)]
        [InlineData(2, 1, true)]
        [InlineData(0, 3, false)]
        [InlineData(1, 2, false)]
        [InlineData(2, 1, false)]
        public async Task Read_InvalidOffsetSize_ThrowsArgumentOutOfRangeException(int offset, int size, bool chunked)
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked);
            using (Stream inputStream = request.InputStream)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("size", () => inputStream.Read(new byte[2], offset, size));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>("size", () => inputStream.ReadAsync(new byte[2], offset, size));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EndRead_NullAsyncResult_ThrowsArgumentNullException(bool chunked)
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked);
            using (Stream inputStream = request.InputStream)
            {
                AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => inputStream.EndRead(null));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // No validation performed
        public async Task EndRead_InvalidAsyncResult_ThrowsArgumentException(bool chunked)
        {
            HttpListenerRequest request1 = await _helper.GetRequest(chunked);
            HttpListenerRequest request2 = await _helper.GetRequest(chunked);

            using (Stream inputStream1 = request1.InputStream)
            using (Stream inputStream2 = request2.InputStream)
            {
                IAsyncResult beginReadResult = inputStream1.BeginRead(new byte[0], 0, 0, null, null);

                AssertExtensions.Throws<ArgumentException>("asyncResult", () => inputStream2.EndRead(new CustomAsyncResult()));
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => inputStream2.EndRead(beginReadResult));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // No validation performed
        public async Task EndRead_CalledTwice_ThrowsInvalidOperationException(bool chunked)
        {
            HttpListenerRequest request = await _helper.GetRequest(chunked);
            using (Stream inputStream = request.InputStream)
            {
                IAsyncResult beginReadResult = inputStream.BeginRead(new byte[0], 0, 0, null, null);
                inputStream.EndRead(beginReadResult);

                Assert.Throws<InvalidOperationException>(() => inputStream.EndRead(beginReadResult));
            }
        }

        [Fact]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // No exception thrown
        public async Task Read_FromClosedConnectionAsynchronously_ThrowsHttpListenerException()
        {
            const string Text = "Some-String";
            byte[] expected = Encoding.UTF8.GetBytes(Text);

            using (Socket client = _factory.GetConnectedSocket())
            {
                // Send a header to the HttpListener to give it a context.
                // Note: It's important here that we don't send the content.
                // If the content is missing, then the HttpListener needs
                // to get the content. However, the socket has been closed
                // before the reading of the content, so reading should fail.
                client.Send(_factory.GetContent(RequestTypes.POST, Text, headerOnly: true));
                HttpListenerContext context = await _listener.GetContextAsync();

                // Disconnect the Socket from the HttpListener.
                client.Shutdown(SocketShutdown.Both);
                Helpers.WaitForSocketShutdown(client);

                // Reading from a closed connection should fail.
                byte[] buffer = new byte[expected.Length];
                await Assert.ThrowsAsync<HttpListenerException>(() => context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));
                await Assert.ThrowsAsync<HttpListenerException>(() => context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));

                // Closing a response from a closed client if no writing has failed should fail.
                Assert.Throws<HttpListenerException>(() => context.Response.Close());
            }
        }

        [Fact]
        [ActiveIssue(18128, platforms: TestPlatforms.AnyUnix)] // No exception thrown
        public async Task Read_FromClosedConnectionSynchronously_ThrowsHttpListenerException()
        {
            const string Text = "Some-String";
            byte[] expected = Encoding.UTF8.GetBytes(Text);

            using (Socket client = _factory.GetConnectedSocket())
            {
                // Send a header to the HttpListener to give it a context.
                // Note: It's important here that we don't send the content.
                // If the content is missing, then the HttpListener needs
                // to get the content. However, the socket has been closed
                // before the reading of the content, so reading should fail.
                client.Send(_factory.GetContent(RequestTypes.POST, Text, headerOnly: true));
                HttpListenerContext context = await _listener.GetContextAsync();

                // Disconnect the Socket from the HttpListener.
                client.Shutdown(SocketShutdown.Both);
                Helpers.WaitForSocketShutdown(client);

                // Reading from a closed connection should fail.
                byte[] buffer = new byte[expected.Length];
                Assert.Throws<HttpListenerException>(() => context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Throws<HttpListenerException>(() => context.Request.InputStream.Read(buffer, 0, buffer.Length));

                // Closing a response from a closed client if no writing has occured should fail.
                Assert.Throws<HttpListenerException>(() => context.Response.Close());
            }
        }
    }
}
