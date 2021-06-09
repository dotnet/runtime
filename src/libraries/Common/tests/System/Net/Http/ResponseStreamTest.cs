// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class ResponseStreamTest : HttpClientHandlerTestBase
    {
        public ResponseStreamTest(ITestOutputHelper output) : base(output) { }

        public static IEnumerable<object[]> RemoteServersAndReadModes()
        {
            foreach (Configuration.Http.RemoteServer remoteServer in Configuration.Http.RemoteServers)
            {
                for (int i = 0; i < 8; i++)
                {
                    yield return new object[] { remoteServer, i };
                }
            }

        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersAndReadModes))]
        public async Task GetStreamAsync_ReadToEnd_Success(Configuration.Http.RemoteServer remoteServer, int readMode)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            {
                string customHeaderValue = Guid.NewGuid().ToString("N");
                client.DefaultRequestHeaders.Add("X-ResponseStreamTest", customHeaderValue);

                using (Stream stream = await client.GetStreamAsync(remoteServer.EchoUri))
                {
                    var ms = new MemoryStream();
                    int bytesRead;
                    var buffer = new byte[10];
                    string responseBody;

                    // Read all of the response content in various ways
                    switch (readMode)
                    {
                        case 0:
                            // StreamReader.ReadToEnd
                            responseBody = new StreamReader(stream).ReadToEnd();
                            break;

                        case 1:
                            // StreamReader.ReadToEndAsync
                            responseBody = await new StreamReader(stream).ReadToEndAsync();
                            break;

                        case 2:
                            // Individual calls to Read(Array)
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        case 3:
                            // Individual calls to ReadAsync(Array)
                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        case 4:
                            // Individual calls to Read(Span)
#if !NETFRAMEWORK
                            while ((bytesRead = stream.Read(new Span<byte>(buffer))) != 0)
#else
                            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
#endif
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        case 5:
                            // ReadByte
                            int byteValue;
                            while ((byteValue = stream.ReadByte()) != -1)
                            {
                                ms.WriteByte((byte)byteValue);
                            }
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        case 6:
                            // CopyTo
                            stream.CopyTo(ms);
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        case 7:
                            // CopyToAsync
                            await stream.CopyToAsync(ms);
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        default:
                            throw new Exception($"Unexpected test mode {readMode}");
                    }

                    // Calling GetStreamAsync() means we don't have access to the HttpResponseMessage.
                    // So, we can't use the MD5 hash validation to verify receipt of the response body.
                    // For this test, we can use a simpler verification of a custom header echo'ing back.
                    _output.WriteLine(responseBody);
                    Assert.Contains(customHeaderValue, responseBody);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        public async Task GetAsync_UseResponseHeadersReadAndCallLoadIntoBuffer_Success(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response = await client.GetAsync(remoteServer.EchoUri, HttpCompletionOption.ResponseHeadersRead))
            {
                await response.Content.LoadIntoBufferAsync();

                string responseBody = await response.Content.ReadAsStringAsync();
                _output.WriteLine(responseBody);
                TestHelper.VerifyResponseBody(
                    responseBody,
                    response.Content.Headers.ContentMD5,
                    false,
                    null);
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
        public async Task GetAsync_UseResponseHeadersReadAndCopyToMemoryStream_Success(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response = await client.GetAsync(remoteServer.EchoUri, HttpCompletionOption.ResponseHeadersRead))
            {
                var memoryStream = new MemoryStream();
                await response.Content.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                using (var reader = new StreamReader(memoryStream))
                {
                    string responseBody = reader.ReadToEnd();
                    _output.WriteLine(responseBody);
                    TestHelper.VerifyResponseBody(
                        responseBody,
                        response.Content.Headers.ContentMD5,
                        false,
                        null);
                }
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task GetStreamAsync_ReadZeroBytes_Success(Configuration.Http.RemoteServer remoteServer)
        {
            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (Stream stream = await client.GetStreamAsync(remoteServer.EchoUri))
            {
                Assert.Equal(0, stream.Read(new byte[1], 0, 0));
#if !NETFRAMEWORK
                Assert.Equal(0, stream.Read(new Span<byte>(new byte[1], 0, 0)));
#endif
                Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 0));
            }
        }

        [OuterLoop("Uses external servers", typeof(PlatformDetection), nameof(PlatformDetection.LocalEchoServerIsNotAvailable))]
        [Theory, MemberData(nameof(RemoteServersMemberData))]
        public async Task ReadAsStreamAsync_Cancel_TaskIsCanceled(Configuration.Http.RemoteServer remoteServer)
        {
            var cts = new CancellationTokenSource();

            using (HttpClient client = CreateHttpClientForRemoteServer(remoteServer))
            using (HttpResponseMessage response =
                    await client.GetAsync(remoteServer.EchoUri, HttpCompletionOption.ResponseHeadersRead))
            using (Stream stream = await response.Content.ReadAsStreamAsync(TestAsync))
            {
                var buffer = new byte[2048];
                Task task = stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();

                // Verify that the task completed.
                Assert.True(((IAsyncResult)task).AsyncWaitHandle.WaitOne(new TimeSpan(0, 5, 0)));
                Assert.True(task.IsCompleted, "Task was not yet completed");

                // Verify that the task completed successfully or is canceled.
                if (IsWinHttpHandler)
                {
                    // With WinHttpHandler, we may fault because canceling the task destroys the request handle
                    // which may randomly cause an ObjectDisposedException (or other exception).
                    Assert.True(
                        task.Status == TaskStatus.RanToCompletion ||
                        task.Status == TaskStatus.Canceled ||
                        task.Status == TaskStatus.Faulted);
                }
                else
                {
                    if (task.IsFaulted)
                    {
                        // Propagate exception for debugging
                        task.GetAwaiter().GetResult();
                    }

                    Assert.True(
                        task.Status == TaskStatus.RanToCompletion ||
                        task.Status == TaskStatus.Canceled);
                }
            }
        }
#if NETCOREAPP
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
        [Theory]
        [InlineData(TransferType.ContentLength, TransferError.ContentLengthTooLarge)]
        [InlineData(TransferType.Chunked, TransferError.MissingChunkTerminator)]
        [InlineData(TransferType.Chunked, TransferError.ChunkSizeTooLarge)]
        public async Task ReadAsStreamAsync_InvalidServerResponse_ThrowsIOException(
            TransferType transferType,
            TransferError transferError)
        {
            await StartTransferTypeAndErrorServer(transferType, transferError, async uri =>
            {
                await Assert.ThrowsAsync<IOException>(() => ReadAsStreamHelper(uri));
            });
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
        [Theory]
        [InlineData(TransferType.None, TransferError.None)]
        [InlineData(TransferType.ContentLength, TransferError.None)]
        [InlineData(TransferType.Chunked, TransferError.None)]
        public async Task ReadAsStreamAsync_ValidServerResponse_Success(
            TransferType transferType,
            TransferError transferError)
        {
            await StartTransferTypeAndErrorServer(transferType, transferError, async uri =>
            {
                await ReadAsStreamHelper(uri);
            });
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/42852", TestPlatforms.Browser)]
        [Theory]
        [InlineData(TransferType.None, TransferError.None)]
        [InlineData(TransferType.ContentLength, TransferError.None)]
        [InlineData(TransferType.Chunked, TransferError.None)]
        public async Task ReadAsStreamAsync_StreamCanReadIsFalseAfterDispose(
            TransferType transferType,
            TransferError transferError)
        {
            await StartTransferTypeAndErrorServer(transferType, transferError, async uri =>
            {
                using (HttpClient client = CreateHttpClient())
                using (HttpResponseMessage response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
                {
                    Stream stream = await response.Content.ReadAsStreamAsync();
                    Assert.True(stream.CanRead);

                    stream.Dispose();

                    Assert.False(stream.CanRead);
                }
            });
        }
#endif

        public enum TransferType
        {
            None = 0,
            ContentLength,
            Chunked
        }

        public enum TransferError
        {
            None = 0,
            ContentLengthTooLarge,
            ChunkSizeTooLarge,
            MissingChunkTerminator
        }

        public static Task StartTransferTypeAndErrorServer(
            TransferType transferType,
            TransferError transferError,
            Func<Uri, Task> clientFunc)
        {
            return LoopbackServer.CreateClientAndServerAsync(
                clientFunc,
                server => server.AcceptConnectionAsync(async connection =>
                {
                    // Read past request headers.
                    await connection.ReadRequestHeaderAsync();

                    // Determine response transfer headers.
                    string transferHeader = null;
                    string content = "This is some response content.";
                    if (transferType == TransferType.ContentLength)
                    {
                        transferHeader = transferError == TransferError.ContentLengthTooLarge ?
                            $"Content-Length: {content.Length + 42}\r\n" :
                            $"Content-Length: {content.Length}\r\n";
                    }
                    else if (transferType == TransferType.Chunked)
                    {
                        transferHeader = "Transfer-Encoding: chunked\r\n";
                    }

                    // Write response header
                    await connection.WriteStringAsync("HTTP/1.1 200 OK\r\n").ConfigureAwait(false);
                    await connection.WriteStringAsync($"Date: {DateTimeOffset.UtcNow:R}\r\n").ConfigureAwait(false);
                    await connection.WriteStringAsync("Content-Type: text/plain\r\n").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(transferHeader))
                    {
                        await connection.WriteStringAsync(transferHeader).ConfigureAwait(false);
                    }
                    await connection.WriteStringAsync("\r\n").ConfigureAwait(false);

                    // Write response body
                    if (transferType == TransferType.Chunked)
                    {
                        string chunkSizeInHex = string.Format(
                            "{0:x}\r\n",
                            content.Length + (transferError == TransferError.ChunkSizeTooLarge ? 42 : 0));
                        await connection.WriteStringAsync(chunkSizeInHex).ConfigureAwait(false);
                        await connection.WriteStringAsync($"{content}\r\n").ConfigureAwait(false);
                        if (transferError != TransferError.MissingChunkTerminator)
                        {
                            await connection.WriteStringAsync("0\r\n\r\n").ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await connection.WriteStringAsync($"{content}").ConfigureAwait(false);
                    }
                }));
        }

        private async Task ReadAsStreamHelper(Uri serverUri)
        {
            using (HttpClient client = CreateHttpClient())
            {
                using (var response = await client.GetAsync(
                    serverUri,
                    HttpCompletionOption.ResponseHeadersRead))
                using (var stream = await response.Content.ReadAsStreamAsync(TestAsync))
                {
                    var buffer = new byte[1];
                    while (await stream.ReadAsync(buffer, 0, 1) > 0) ;
                }
            }
        }
    }
}
