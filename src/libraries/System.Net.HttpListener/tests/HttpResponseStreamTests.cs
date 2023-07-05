// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/2391", TestRuntimes.Mono)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // httpsys component missing in Nano.
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

        [Theory]
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

                    byte[] extraBytesSentAfterClose = "Should not be sent."u8.ToArray();
                    await outputStream.WriteAsync(extraBytesSentAfterClose, 0, extraBytesSentAfterClose.Length);
                }

                string clientString = await clientTask;
                Assert.Equal(expectedResponse, clientString);
            }
        }

        [Theory]
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

                    byte[] extraBytesSentAfterClose = "Should not be sent."u8.ToArray();
                    outputStream.Write(extraBytesSentAfterClose, 0, extraBytesSentAfterClose.Length);
                }

                string clientString = await clientTask;
                Assert.Equal(expectedResponse, clientString);
            }
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
                AssertExtensions.Throws<ArgumentNullException>("buffer", () => outputStream.Write(null, 0, 0));
                await AssertExtensions.ThrowsAsync<ArgumentNullException>("buffer", () => outputStream.WriteAsync(null, 0, 0));
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
                Assert.Throws<ArgumentOutOfRangeException>(() => outputStream.Write(new byte[2], offset, 0));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => outputStream.WriteAsync(new byte[2], offset, 0));
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
                Assert.Throws<ArgumentOutOfRangeException>(() => outputStream.Write(new byte[2], offset, size));
                await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => outputStream.WriteAsync(new byte[2], offset, size));
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/21918", TestPlatforms.AnyUnix)]
        public async Task Write_TooMuch_ThrowsProtocolViolationException()
        {
            using (HttpClient client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;
                    byte[] responseBuffer = "A long string"u8.ToArray();
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

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/21918", TestPlatforms.AnyUnix)]
        public async Task Write_TooLittleAsynchronouslyAndClose_ThrowsInvalidOperationException()
        {
            using (HttpClient client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;

                    byte[] responseBuffer = "A long string"u8.ToArray();
                    response.ContentLength64 = responseBuffer.Length + 1;

                    // Throws when there are bytes left to write
                    await output.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    Assert.Throws<InvalidOperationException>(() => output.Close());

                    // Write the final byte and make sure we can close.
                    await output.WriteAsync(new byte[1],0, 1);
                    output.Close();
                }
            }
        }

        [ConditionalFact(nameof(Helpers) + "." + nameof(Helpers.IsWindowsImplementation))] // [ActiveIssue("https://github.com/dotnet/runtime/issues/21918", TestPlatforms.AnyUnix)]
        public async Task Write_TooLittleSynchronouslyAndClose_ThrowsInvalidOperationException()
        {
            using (HttpClient client = new HttpClient())
            {
                _ = client.GetStringAsync(_factory.ListeningUrl);

                HttpListenerContext serverContext = await _listener.GetContextAsync();
                using (HttpListenerResponse response = serverContext.Response)
                {
                    Stream output = response.OutputStream;

                    byte[] responseBuffer = "A long string"u8.ToArray();
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

        [ActiveIssue("https://github.com/dotnet/runtime/issues/21940")] // CI hanging frequently
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Write_HeadersToClosedConnectionAsynchronously_ThrowsHttpListenerException(bool ignoreWriteExceptions)
        {
            const string Text = "Some-String";
            byte[] buffer = Encoding.UTF8.GetBytes(Text);

            using (HttpListenerFactory factory = new HttpListenerFactory())
            using (Socket client = factory.GetConnectedSocket())
            {
                // Send a header to the HttpListener to give it a context.
                client.Send(factory.GetContent(RequestTypes.POST, Text, headerOnly: true));
                HttpListener listener = factory.GetListener();
                listener.IgnoreWriteExceptions = ignoreWriteExceptions;
                HttpListenerContext context = await listener.GetContextAsync();

                // Disconnect the Socket from the HttpListener.
                Helpers.WaitForSocketShutdown(client);

                // Writing to a disconnected client should fail.
                if (!PlatformDetection.IsWindows && ignoreWriteExceptions)
                {
                    // Windows sends headers first, followed by content. If headers fail to send, then an exception is always thrown.
                    // However, the managed implementation has already sent the headers by the time we run this test.
                    // This means that if the content fails to send, an exception is only thrown if ignoreWriteExceptions == false.
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    await Assert.ThrowsAsync<HttpListenerException>(() => context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length));
                }
                // Closing a response from a closed client if a writing has already failed should not fail.
                context.Response.Close();
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/21940")] // CI hanging frequently
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Write_HeadersToClosedConnectionSynchronously_ThrowsHttpListenerException(bool ignoreWriteExceptions)
        {
            const string Text = "Some-String";
            byte[] buffer = Encoding.UTF8.GetBytes(Text);

            using (HttpListenerFactory factory = new HttpListenerFactory())
            using (Socket client = factory.GetConnectedSocket())
            {
                // Send a header to the HttpListener to give it a context.
                client.Send(factory.GetContent(RequestTypes.POST, Text, headerOnly: true));
                HttpListener listener = factory.GetListener();
                listener.IgnoreWriteExceptions = ignoreWriteExceptions;
                HttpListenerContext context = await listener.GetContextAsync();

                // Disconnect the Socket from the HttpListener.
                Helpers.WaitForSocketShutdown(client);

                // Writing to a disconnected client should fail.
                if (!PlatformDetection.IsWindows && ignoreWriteExceptions)
                {
                    // Windows sends headers first, followed by content. If headers fail to send, then an exception is always thrown.
                    // However, the managed implementation has already sent the headers by the time we run this test.
                    // This means that if the content fails to send, an exception is only thrown if ignoreWriteExceptions == false.
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    Assert.Throws<HttpListenerException>(() => context.Response.OutputStream.Write(buffer, 0, buffer.Length));
                }

                // Closing a response from a closed client if a writing has already failed should not fail.
                context.Response.Close();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21022", platforms: TestPlatforms.Windows)] // Indeterminate failure - socket not always fully disconnected.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21590", TestPlatforms.OSX)]
        public async Task Write_ContentToClosedConnectionAsynchronously_ThrowsHttpListenerException(bool ignoreWriteExceptions)
        {
            const string Text = "Some-String";
            byte[] buffer = Encoding.UTF8.GetBytes(Text);

            using (HttpListenerFactory factory = new HttpListenerFactory())
            using (Socket client = factory.GetConnectedSocket())
            {
                // Send a header to the HttpListener to give it a context.
                client.Send(factory.GetContent(RequestTypes.POST, Text, headerOnly: true));
                HttpListener listener = factory.GetListener();
                listener.IgnoreWriteExceptions = ignoreWriteExceptions;
                HttpListenerContext context = await listener.GetContextAsync();

                // Write the headers to the Socket.
                await context.Response.OutputStream.WriteAsync(buffer, 0, 1);

                // Disconnect the Socket from the HttpListener.
                Helpers.WaitForSocketShutdown(client);

                // Writing non-header content to a disconnected client should fail, only if IgnoreWriteExceptions is false.
                if (ignoreWriteExceptions)
                {
                    await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    await Assert.ThrowsAsync<HttpListenerException>(() => context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length));
                }

                // Closing a response from a closed client if a writing has already failed should not fail.
                context.Response.Close();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21022", platforms: TestPlatforms.Windows)] // Indeterminate failure - socket not always fully disconnected.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/21590", TestPlatforms.OSX | TestPlatforms.FreeBSD)]
        public async Task Write_ContentToClosedConnectionSynchronously_ThrowsHttpListenerException(bool ignoreWriteExceptions)
        {
            const string Text = "Some-String";
            byte[] buffer = Encoding.UTF8.GetBytes(Text);

            using (HttpListenerFactory factory = new HttpListenerFactory())
            using (Socket client = factory.GetConnectedSocket())
            {
                // Send a header to the HttpListener to give it a context.
                client.Send(factory.GetContent(RequestTypes.POST, Text, headerOnly: true));
                HttpListener listener = factory.GetListener();
                listener.IgnoreWriteExceptions = ignoreWriteExceptions;
                HttpListenerContext context = await listener.GetContextAsync();

                // Write the headers to the Socket.
                context.Response.OutputStream.Write(buffer, 0, 1);

                // Disconnect the Socket from the HttpListener.
                Helpers.WaitForSocketShutdown(client);

                // Writing non-header content to a disconnected client should fail, only if IgnoreWriteExceptions is false.
                if (ignoreWriteExceptions)
                {
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else
                {
                    Assert.Throws<HttpListenerException>(() => context.Response.OutputStream.Write(buffer, 0, buffer.Length));
                }

                // Closing a response from a closed client if a writing has already failed should not fail.
                context.Response.Close();
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task EndWrite_NullAsyncResult_ThrowsArgumentNullException(bool ignoreWriteExceptions)
        {
            _listener.IgnoreWriteExceptions = ignoreWriteExceptions;
            using (HttpListenerResponse response = await _helper.GetResponse())
            using (Stream outputStream = response.OutputStream)
            {
                AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => outputStream.EndWrite(null));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Unix implementation uses Socket.Begin/EndSend, which doesn't fail in this case
        [Fact]
        public async Task EndWrite_InvalidAsyncResult_ThrowsArgumentException()
        {
            using (HttpListenerResponse response1 = await _helper.GetResponse())
            using (Stream outputStream1 = response1.OutputStream)
            using (HttpListenerResponse response2 = await _helper.GetResponse())
            using (Stream outputStream2 = response2.OutputStream)
            {
                IAsyncResult beginWriteResult = outputStream1.BeginWrite(new byte[0], 0, 0, null, null);

                AssertExtensions.Throws<ArgumentException>("asyncResult", () => outputStream2.EndWrite(new CustomAsyncResult()));
                AssertExtensions.Throws<ArgumentException>("asyncResult", () => outputStream2.EndWrite(beginWriteResult));
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)] // Unix implementation uses Socket.Begin/EndSend, which doesn't fail in this case
        [Fact]
        public async Task EndWrite_CalledTwice_ThrowsInvalidOperationException()
        {
            using (HttpListenerResponse response1 = await _helper.GetResponse())
            using (Stream outputStream = response1.OutputStream)
            {
                IAsyncResult beginWriteResult = outputStream.BeginWrite(new byte[0], 0, 0, null, null);
                outputStream.EndWrite(beginWriteResult);

                Assert.Throws<InvalidOperationException>(() => outputStream.EndWrite(beginWriteResult));
            }
        }
    }
}
