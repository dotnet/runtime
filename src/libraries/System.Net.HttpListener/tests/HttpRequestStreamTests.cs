// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2391", TestRuntimes.Mono)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
    public class HttpRequestStreamTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private GetContextHelper _helper;
        private const int TimeoutMilliseconds = 60*1000;
        private readonly ITestOutputHelper _output;

        public HttpRequestStreamTests(ITestOutputHelper output)
        {
            _factory = new HttpListenerFactory();
            _listener = _factory.GetListener();
            _helper = new GetContextHelper(_listener, _factory.ListeningUrl);
            _output = output;
        }

        public void Dispose()
        {
            _factory.Dispose();
            _helper.Dispose();
        }

        // Try to read 'length' bytes from stream or fail after timeout.
        private async Task<int> ReadLengthAsync(Stream stream, byte[] array, int offset, int length)
        {
            int remaining = length;
            int readLength;

            do
            {
                readLength = await stream.ReadAsync(array, offset, remaining).WaitAsync(TimeSpan.FromMilliseconds(TimeoutMilliseconds));
                if (readLength <= 0)
                {
                    break;
                }
                remaining -= readLength;
                offset += readLength;
            }
            while (remaining > 0);

            if (remaining != 0)
            {
                _output.WriteLine("Expected {0} bytes but got {1}", length, length-remaining);
            }

            return length - remaining;
        }

        // Synchronous version of ReadLengthAsync above.
        private int ReadLength(Stream stream, byte[] array, int offset, int length)
        {
            int remaining = length;
            int readLength;

            do
            {
                readLength = stream.Read(array, offset, remaining);
                if (readLength <= 0)
                {
                    break;
                }
                remaining -= readLength;
                offset += readLength;
            }
            while (remaining > 0);

            if (remaining != 0)
            {
                _output.WriteLine("Expected {0} bytes but got {1}", length, length-remaining);
            }

            return length - remaining;
        }

        [Theory]
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
                int bytesRead = await ReadLengthAsync(context.Request.InputStream, buffer, 0, expected.Length);
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

        [Theory]
        [InlineData(true, "")]
        [InlineData(false, "")]
        [InlineData(true, "Non-Empty")]
        [InlineData(false, "Non-Empty")]
        public async Task Read_FullLengthAsynchronous_PadBuffer_Success(bool transferEncodingChunked, string text)
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

                const int pad = 128;

                // Add padding at beginning and end to test for correct offset/size handling
                byte[] buffer = new byte[pad + expected.Length + pad];
                int bytesRead = await ReadLengthAsync(context.Request.InputStream, buffer, pad, expected.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected, buffer.Skip(pad).Take(bytesRead));

                // Subsequent reads don't do anything.
                Assert.Equal(0, await context.Request.InputStream.ReadAsync(buffer, pad, 1));

                context.Response.Close();
                using (HttpResponseMessage response = await clientTask)
                {
                    Assert.Equal(200, (int)response.StatusCode);
                }
            }
        }

        [Theory]
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
                int bytesRead = ReadLength(context.Request.InputStream, buffer, 0, buffer.Length);
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_LargeLengthAsynchronous_Success(bool transferEncodingChunked)
        {
            var rand = new Random(42);
            byte[] expected = Enumerable
                .Range(0, 128*1024 + 1) // More than 128kb
                .Select(_ => (byte)('a' + rand.Next(0, 26)))
                .ToArray();

            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new ByteArrayContent(expected));

                HttpListenerContext context = await contextTask;

                // If the size is greater than 128K, then we limit the size, and have to do multiple reads on
                // Windows, which uses http.sys internally.
                byte[] buffer = new byte[expected.Length];
                int totalRead = 0;
                while (totalRead < expected.Length)
                {
                    int bytesRead = await context.Request.InputStream.ReadAsync(buffer, totalRead, expected.Length - totalRead);
                    Assert.InRange(bytesRead, 1, expected.Length - totalRead);
                    totalRead += bytesRead;
                }

                // Subsequent reads don't do anything.
                Assert.Equal(0, await context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));
                Assert.Equal(expected, buffer);

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_LargeLengthSynchronous_Success(bool transferEncodingChunked)
        {
            var rand = new Random(42);
            byte[] expected = Enumerable
                .Range(0, 128 * 1024 + 1) // More than 128kb
                .Select(_ => (byte)('a' + rand.Next(0, 26)))
                .ToArray();

            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = transferEncodingChunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new ByteArrayContent(expected));

                HttpListenerContext context = await contextTask;

                // If the size is greater than 128K, then we limit the size, and have to do multiple reads on
                // Windows, which uses http.sys internally.
                byte[] buffer = new byte[expected.Length];
                int totalRead = 0;
                while (totalRead < expected.Length)
                {
                    int bytesRead = context.Request.InputStream.Read(buffer, totalRead, expected.Length - totalRead);
                    Assert.InRange(bytesRead, 1, expected.Length - totalRead);
                    totalRead += bytesRead;
                }

                // Subsequent reads don't do anything.
                Assert.Equal(0, context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Equal(expected, buffer);

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
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
                int bytesRead = await ReadLengthAsync(context.Request.InputStream, buffer, 0, buffer.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected.Concat(new byte[5]), buffer);

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
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
                int bytesRead = ReadLength(context.Request.InputStream, buffer, 0, buffer.Length);
                Assert.Equal(expected.Length, bytesRead);
                Assert.Equal(expected.Concat(new byte[5]), buffer);

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
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
                int bytesRead = await ReadLengthAsync(context.Request.InputStream, buffer, 0, buffer.Length);
                Assert.Equal(buffer.Length, bytesRead);

                context.Response.Close();
                await clientTask;
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
                await clientTask;
            }
        }

        [Fact]
        public async Task CanSeek_Get_ReturnsFalse()
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    Assert.False(inputStream.CanSeek);

                    Assert.Throws<NotSupportedException>(() => inputStream.Length);
                    Assert.Throws<NotSupportedException>(() => inputStream.SetLength(1));

                    Assert.Throws<NotSupportedException>(() => inputStream.Position);
                    Assert.Throws<NotSupportedException>(() => inputStream.Position = 1);

                    Assert.Throws<NotSupportedException>(() => inputStream.Seek(0, SeekOrigin.Begin));
                }

                context.Response.Close();
                await clientTask;
            }
        }

        [Fact]
        public async Task CanRead_Get_ReturnsTrue()
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    Assert.True(inputStream.CanRead);
                }

                context.Response.Close();
                await clientTask;
            }
        }

        [Fact]
        public async Task CanWrite_Get_ReturnsFalse()
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

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

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_NullBuffer_ThrowsArgumentNullException(bool chunked)
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    AssertExtensions.Throws<ArgumentNullException>("buffer", () => inputStream.Read(null, 0, 0));
                    await AssertExtensions.ThrowsAsync<ArgumentNullException>("buffer", () => inputStream.ReadAsync(null, 0, 0));
                }

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
        [InlineData(-1, true)]
        [InlineData(3, true)]
        [InlineData(-1, false)]
        [InlineData(3, false)]
        public async Task Read_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset, bool chunked)
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => inputStream.Read(new byte[2], offset, 0));
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => inputStream.ReadAsync(new byte[2], offset, 0));
                }

                context.Response.Close();
                await clientTask;
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
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => inputStream.Read(new byte[2], offset, size));
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => inputStream.ReadAsync(new byte[2], offset, size));
                }

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EndRead_NullAsyncResult_ThrowsArgumentNullException(bool chunked)
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => inputStream.EndRead(null));
                }

                context.Response.Close();
                await clientTask;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EndRead_InvalidAsyncResult_ThrowsArgumentException(bool chunked)
        {
            using (HttpClient client = new HttpClient())
            {
                Task<HttpListenerContext> contextTask1 = _listener.GetContextAsync();
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask1 = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));
                HttpListenerContext context1 = await contextTask1;
                HttpListenerRequest request1 = context1.Request;

                Task<HttpListenerContext> contextTask2 = _listener.GetContextAsync();
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask2 = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));
                HttpListenerContext context2 = await contextTask2;
                HttpListenerRequest request2 = context2.Request;

                using (Stream inputStream1 = request1.InputStream)
                using (Stream inputStream2 = request2.InputStream)
                {
                    IAsyncResult beginReadResult = inputStream1.BeginRead(new byte[0], 0, 0, null, null);

                    AssertExtensions.Throws<ArgumentException>("asyncResult", () => inputStream2.EndRead(new CustomAsyncResult()));
                    AssertExtensions.Throws<ArgumentException>("asyncResult", () => inputStream2.EndRead(beginReadResult));
                }

                context1.Response.Close();
                await clientTask1;

                context2.Response.Close();
                await clientTask2;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EndRead_CalledTwice_ThrowsInvalidOperationException(bool chunked)
        {
            Task<HttpListenerContext> contextTask = _listener.GetContextAsync();
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.TransferEncodingChunked = chunked;
                Task<HttpResponseMessage> clientTask = client.PostAsync(_factory.ListeningUrl, new StringContent("Hello"));

                HttpListenerContext context = await contextTask;
                HttpListenerRequest request = context.Request;

                using (Stream inputStream = request.InputStream)
                {
                    IAsyncResult beginReadResult = inputStream.BeginRead(new byte[0], 0, 0, null, null);
                    inputStream.EndRead(beginReadResult);

                    Assert.Throws<InvalidOperationException>(() => inputStream.EndRead(beginReadResult));
                }

                context.Response.Close();
                await clientTask;
            }
        }

        [Fact]
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
                Helpers.WaitForSocketShutdown(client);

                // Reading from a closed connection should fail.
                byte[] buffer = new byte[expected.Length];
                await Assert.ThrowsAsync<HttpListenerException>(() => context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));
                await Assert.ThrowsAsync<HttpListenerException>(() => context.Request.InputStream.ReadAsync(buffer, 0, buffer.Length));
            }
        }

        [Fact]
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
                Helpers.WaitForSocketShutdown(client);

                // Reading from a closed connection should fail.
                byte[] buffer = new byte[expected.Length];
                Assert.Throws<HttpListenerException>(() => context.Request.InputStream.Read(buffer, 0, buffer.Length));
                Assert.Throws<HttpListenerException>(() => context.Request.InputStream.Read(buffer, 0, buffer.Length));
            }
        }
    }
}
