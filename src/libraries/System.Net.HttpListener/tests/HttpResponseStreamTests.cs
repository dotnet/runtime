// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    public class HttpResponseStreamTests : IDisposable
    {
        private HttpListenerFactory _factory;
        private HttpListener _listener;
        private GetContextHelper _helper;

        public HttpResponseStreamTests()
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
        [InlineData(true)]
        [InlineData(false)]
        public async Task SimpleRequest_WriteAsynchronously_Succeeds(bool sendChunked)
        {
            const string expectedResponse = "hello from HttpListener";
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (HttpListenerResponse response = serverContext.Response)
                {
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                    response.ContentLength64 = responseBuffer.Length;
                    response.SendChunked = sendChunked;

                    Stream outputStream = response.OutputStream;
                    try
                    {
                        await outputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    }
                    finally
                    {
                        outputStream.Close();
                    }

                    byte[] extraBytesSentAfterClose = Encoding.UTF8.GetBytes("Should not be sent.");
                    await outputStream.WriteAsync(extraBytesSentAfterClose, 0, extraBytesSentAfterClose.Length);
                }

                string clientString = await clientTask;
                Assert.Equal(expectedResponse, clientString);
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SimpleRequest_WriteSynchronouslyNonEmpty_Succeeds(bool sendChunked)
        {
            const string expectedResponse = "hello from HttpListener";
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (HttpListenerResponse response = serverContext.Response)
                {
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                    response.ContentLength64 = responseBuffer.Length;
                    response.SendChunked = sendChunked;

                    Stream outputStream = response.OutputStream;
                    try
                    {
                        outputStream.Write(responseBuffer, 0, responseBuffer.Length);
                    }
                    finally
                    {
                        outputStream.Close();
                    }

                    byte[] extraBytesSentAfterClose = Encoding.UTF8.GetBytes("Should not be sent.");
                    outputStream.Write(extraBytesSentAfterClose, 0, extraBytesSentAfterClose.Length);
                }

                string clientString = await clientTask;
                Assert.Equal(expectedResponse, clientString);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task SimpleRequest_WriteAsynchronouslyInParts_Succeeds()
        {
            const string expectedResponse = "hello from HttpListener";
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (HttpListenerResponse response = serverContext.Response)
                {
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                    response.ContentLength64 = responseBuffer.Length;

                    using (Stream outputStream = response.OutputStream)
                    {
                        await outputStream.WriteAsync(responseBuffer, 0, 5);
                        await outputStream.WriteAsync(responseBuffer, 5, responseBuffer.Length - 5);
                    }
                }

                var clientString = await clientTask;

                Assert.Equal(expectedResponse, clientString);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task SimpleRequest_WriteSynchronouslyInParts_Succeeds()
        {
            const string expectedResponse = "hello from HttpListener";
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (HttpListenerResponse response = serverContext.Response)
                {
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(expectedResponse);
                    response.ContentLength64 = responseBuffer.Length;

                    using (Stream outputStream = response.OutputStream)
                    {
                        outputStream.Write(responseBuffer, 0, 5);
                        outputStream.Write(responseBuffer, 5, responseBuffer.Length - 5);
                    }
                }

                var clientString = await clientTask;

                Assert.Equal(expectedResponse, clientString);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task SimpleRequest_WriteAynchronouslyEmpty_Succeeds()
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (HttpListenerResponse response = serverContext.Response)
                {
                    response.ContentLength64 = 0;
                    using (Stream outputStream = response.OutputStream)
                    {
                        await outputStream.WriteAsync(new byte[0], 0, 0);
                    }
                }

                Assert.Empty(await clientTask);
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task SimpleRequest_WriteSynchronouslyEmpty_Succeeds()
        {
            Task<HttpListenerContext> serverContextTask = _listener.GetContextAsync();

            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await serverContextTask;
                using (HttpListenerResponse response = serverContext.Response)
                {
                    response.ContentLength64 = 0;
                    using (Stream outputStream = response.OutputStream)
                    {
                        outputStream.Write(new byte[0], 0, 0);
                    }
                }

                Assert.Empty(await clientTask);
            }
        }

        [Fact]
        public async Task CanSeek_Get_ReturnsFalse()
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.False(outputStream.CanSeek);

                Assert.Throws<NotSupportedException>(() => outputStream.Length);
                Assert.Throws<NotSupportedException>(() => outputStream.SetLength(1));

                Assert.Throws<NotSupportedException>(() => outputStream.Position);
                Assert.Throws<NotSupportedException>(() => outputStream.Position = 1);

                Assert.Throws<NotSupportedException>(() => outputStream.Seek(0, SeekOrigin.Begin));
            }
        }

        [Fact]
        public async Task CanRead_Get_ReturnsFalse()
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.False(outputStream.CanRead);

                Assert.Throws<InvalidOperationException>(() => outputStream.Read(new byte[0], 0, 0));
                await Assert.ThrowsAsync<InvalidOperationException>(() => outputStream.ReadAsync(new byte[0], 0, 0));
                Assert.Throws<InvalidOperationException>(() => outputStream.EndRead(null));
            }
        }

        [Fact]
        public async Task CanWrite_Get_ReturnsTrue()
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.True(outputStream.CanWrite);

                // Flushing the output stream is a no-op.
                outputStream.Flush();
                Assert.Equal(Task.CompletedTask, outputStream.FlushAsync(CancellationToken.None));
            }
        }

        [Fact]
        public async Task Write_NullBuffer_ThrowsArgumentNullException()
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.Throws<ArgumentNullException>("buffer", () => outputStream.Write(null, 0, 0));
                await Assert.ThrowsAsync<ArgumentNullException>("buffer", () => outputStream.WriteAsync(null, 0, 0));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public async Task Write_InvalidOffset_ThrowsArgumentOutOfRangeException(int offset)
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => outputStream.Write(new byte[2], offset, 0));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>("offset", () => outputStream.WriteAsync(new byte[2], offset, 0));
            }
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(1, 2)]
        [InlineData(2, 1)]
        public async Task Write_InvalidOffsetSize_ThrowsArgumentOutOfRangeException(int offset, int size)
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.Throws<ArgumentOutOfRangeException>("size", () => outputStream.Write(new byte[2], offset, size));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>("size", () => outputStream.WriteAsync(new byte[2], offset, size));
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task Write_TooMuch_ThrowsProtocolViolationException()
        {
            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;
                    byte[] responseBuffer = Encoding.UTF8.GetBytes("A long string");
                    response.ContentLength64 = responseBuffer.Length - 1;
                    try
                    {
                        Assert.Throws<ProtocolViolationException>(() => output.Write(responseBuffer, 0, responseBuffer.Length));
                        await Assert.ThrowsAsync<ProtocolViolationException>(() => output.WriteAsync(responseBuffer, 0, responseBuffer.Length));
                    }
                    finally
                    {
                        // Write the remaining bytes to guarantee a successful shutdown.
                        output.Write(responseBuffer, 0, (int)response.ContentLength64);
                        output.Close();
                    }
                }
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task Write_TooLittleAsynchronouslyAndClose_ThrowsInvalidOperationException()
        {
            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;

                    byte[] responseBuffer = Encoding.UTF8.GetBytes("A long string");
                    response.ContentLength64 = responseBuffer.Length + 1;

                    // Throws when there are bytes left to write
                    await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    Assert.Throws<InvalidOperationException>(() => output.Close());

                    // Write the final byte and make sure we can close.
                    await output.WriteAsync(new byte[1], 0, 1);
                    output.Close();
                }
            }
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        public async Task Write_TooLittleSynchronouslyAndClose_ThrowsInvalidOperationException()
        {
            using (HttpClient client = new HttpClient())
            {
                Task<string> clientTask = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;

                    byte[] responseBuffer = Encoding.UTF8.GetBytes("A long string");
                    response.ContentLength64 = responseBuffer.Length + 1;

                    // Throws when there are bytes left to write
                    output.Write(responseBuffer, 0, responseBuffer.Length);
                    Assert.Throws<InvalidOperationException>(() => output.Close());

                    // Write the final byte and make sure we can close.
                    output.Write(new byte[1], 0, 1);
                    output.Close();
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true, "Non-Empty")]
        [InlineData(false, "Non-Empty")]
        public async Task Write_ToClosedSocketAsynchronously_ThrowsHttpListenerException(bool ignoreWriteExceptions, string text)
        {
            if (PlatformDetection.IsWindows7)
            {
                // Websockets in WinHttp 5.1 is only supported from Windows 8+
                Assert.Throws<PlatformNotSupportedException>(() => new ClientWebSocket());
                return;
            }

            using (HttpListenerFactory factory = new HttpListenerFactory())
            {
                UriBuilder uriBuilder = new UriBuilder(factory.ListeningUrl) { Scheme = "ws" };

                HttpListener listener = factory.GetListener();
                listener.IgnoreWriteExceptions = ignoreWriteExceptions;

                Task<HttpListenerContext> serverContextTask = listener.GetContextAsync();
                using (ClientWebSocket clientWebSocket = new ClientWebSocket())
                {
                    Task clientConnectTask = clientWebSocket.ConnectAsync(uriBuilder.Uri, CancellationToken.None);

                    HttpListenerContext listenerContext = await serverContextTask;
                    byte[] receiveBuffer = Encoding.UTF8.GetBytes(text);
                    listenerContext.Response.ContentLength64 = receiveBuffer.Length;

                    // Disconnect the socket from the listener.
                    HttpListenerWebSocketContext wsContext = await listenerContext.AcceptWebSocketAsync(null);
                    await clientConnectTask;
                    clientWebSocket.Dispose();

                    // TODO: we need to sleep and wait to make sure the socket closes.
                    Thread.Sleep(1000);

                    // Try writing to the disconnected socket.
                    if (ignoreWriteExceptions)
                    {
                        await listenerContext.Response.OutputStream.WriteAsync(receiveBuffer, 0, receiveBuffer.Length);
                    }
                    else
                    {
                        await Assert.ThrowsAsync<HttpListenerException>(() => listenerContext.Response.OutputStream.WriteAsync(receiveBuffer, 0, receiveBuffer.Length));
                    }
                }
            }
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotOneCoreUAP))]
        [InlineData(true, "Non-Empty")]
        [InlineData(false, "Non-Empty")]
        public async void Write_ToClosedSocketSynchronously_ThrowsHttpListenerException(bool ignoreWriteExceptions, string text)
        {
            if (PlatformDetection.IsWindows7)
            {
                // Websockets in WinHttp 5.1 is only supported from Windows 8+
                Assert.Throws<PlatformNotSupportedException>(() => new ClientWebSocket());
                return;
            }

            using (HttpListenerFactory factory = new HttpListenerFactory())
            {
                UriBuilder uriBuilder = new UriBuilder(factory.ListeningUrl) { Scheme = "ws" };

                HttpListener listener = factory.GetListener();
                listener.IgnoreWriteExceptions = ignoreWriteExceptions;

                Task<HttpListenerContext> serverContextTask = listener.GetContextAsync();
                using (ClientWebSocket clientWebSocket = new ClientWebSocket())
                {
                    Task clientConnectTask = clientWebSocket.ConnectAsync(uriBuilder.Uri, CancellationToken.None);

                    HttpListenerContext listenerContext = await serverContextTask;
                    byte[] receiveBuffer = Encoding.UTF8.GetBytes(text);
                    listenerContext.Response.ContentLength64 = receiveBuffer.Length;

                    // Disconnect the socket from the listener.
                    HttpListenerWebSocketContext wsContext = await listenerContext.AcceptWebSocketAsync(null);
                    await clientConnectTask;
                    clientWebSocket.Dispose();

                    // TODO: we need to sleep and wait to make sure the socket closes.
                    Thread.Sleep(1000);

                    // Try writing to the disconnected socket.
                    if (ignoreWriteExceptions)
                    {
                        listenerContext.Response.OutputStream.Write(receiveBuffer, 0, receiveBuffer.Length);
                    }
                    else
                    {
                        Assert.Throws<HttpListenerException>(() => listenerContext.Response.OutputStream.Write(receiveBuffer, 0, receiveBuffer.Length));
                    }
                }
            }
        }

        [Fact]
        public async Task EndWrite_NullCallback_ThrowsArgumentNullException()
        {
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                Assert.Throws<ArgumentNullException>("asyncResult", () => outputStream.EndWrite(null));
            }
        }

        [Fact]
        public async Task EndWrite_InvalidCallback_ThrowsArgumentException()
        {
            using (HttpListenerResponse response1 = await _helper.GetResponse())
            using (Stream outputStream1 = response1.OutputStream)
            using (HttpListenerResponse response2 = await _helper.GetResponse())
            using (Stream outputStream2 = response2.OutputStream)
            {
                IAsyncResult beginWriteResult = outputStream1.BeginWrite(new byte[0], 0, 0, new AsyncCallback(Common.StubCallback), null);

                Assert.Throws<ArgumentException>("asyncResult", () => outputStream2.EndWrite(new CustomAsyncResult()));
                Assert.Throws<ArgumentException>("asyncResult", () => outputStream2.EndWrite(beginWriteResult));
            }
        }

        [Fact]
        public async Task EndWrite_CalledTwice_ThrowsInvalidOperationException()
        {
            using (HttpListenerResponse response1 = await _helper.GetResponse())
            using (Stream outputStream = response1.OutputStream)
            {
                IAsyncResult beginWriteResult = outputStream.BeginWrite(new byte[0], 0, 0, new AsyncCallback(Common.StubCallback), null);
                outputStream.EndWrite(beginWriteResult);

                Assert.Throws<InvalidOperationException>(() => outputStream.EndWrite(beginWriteResult));
            }
        }
    }
}
    