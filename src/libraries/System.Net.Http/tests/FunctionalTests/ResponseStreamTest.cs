// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "dotnet/corefx #20010")]
    public class ResponseStreamTest : HttpClientTestBase
    {
        private readonly ITestOutputHelper _output;
        
        public ResponseStreamTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public async Task GetStreamAsync_ReadToEnd_Success(int readMode)
        {
            using (HttpClient client = CreateHttpClient())
            {
                string customHeaderValue = Guid.NewGuid().ToString("N");
                client.DefaultRequestHeaders.Add("X-ResponseStreamTest", customHeaderValue);

                using (Stream stream = await client.GetStreamAsync(Configuration.Http.RemoteEchoServer))
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
                            while ((bytesRead = stream.Read(new Span<byte>(buffer))) != 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }
                            responseBody = Encoding.UTF8.GetString(ms.ToArray());
                            break;

                        case 5:
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetAsync_UseResponseHeadersReadAndCallLoadIntoBuffer_Success()
        {
            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead))
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetAsync_UseResponseHeadersReadAndCopyToMemoryStream_Success()
        {
            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response = await client.GetAsync(Configuration.Http.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead))
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

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetStreamAsync_ReadZeroBytes_Success()
        {
            using (HttpClient client = CreateHttpClient())
            using (Stream stream = await client.GetStreamAsync(Configuration.Http.RemoteEchoServer))
            {
                Assert.Equal(0, stream.Read(new byte[1], 0, 0));
                Assert.Equal(0, stream.Read(new Span<byte>(new byte[1], 0, 0)));
                Assert.Equal(0, await stream.ReadAsync(new byte[1], 0, 0));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ReadAsStreamAsync_Cancel_TaskIsCanceled()
        {
            var cts = new CancellationTokenSource();

            using (HttpClient client = CreateHttpClient())
            using (HttpResponseMessage response =
                    await client.GetAsync(Configuration.Http.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead))
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[2048];
                Task task = stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();

                // Verify that the task completed.
                Assert.True(((IAsyncResult)task).AsyncWaitHandle.WaitOne(new TimeSpan(0, 5, 0)));
                Assert.True(task.IsCompleted, "Task was not yet completed");

                // Verify that the task completed successfully or is canceled.
                if (PlatformDetection.IsWindows)
                {
                    // On Windows, we may fault because canceling the task destroys the request handle
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

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(LoopbackServer.TransferType.ContentLength, LoopbackServer.TransferError.ContentLengthTooLarge)]
        [InlineData(LoopbackServer.TransferType.Chunked, LoopbackServer.TransferError.MissingChunkTerminator)]
        [InlineData(LoopbackServer.TransferType.Chunked, LoopbackServer.TransferError.ChunkSizeTooLarge)]
        public async Task ReadAsStreamAsync_InvalidServerResponse_ThrowsIOException(
            LoopbackServer.TransferType transferType,
            LoopbackServer.TransferError transferError)
        {
            IPEndPoint serverEndPoint;
            Task serverTask = LoopbackServer.StartTransferTypeAndErrorServer(transferType, transferError, out serverEndPoint);

            await Assert.ThrowsAsync<IOException>(() => ReadAsStreamHelper(serverEndPoint));

            await serverTask;
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(LoopbackServer.TransferType.None, LoopbackServer.TransferError.None)]
        [InlineData(LoopbackServer.TransferType.ContentLength, LoopbackServer.TransferError.None)]
        [InlineData(LoopbackServer.TransferType.Chunked, LoopbackServer.TransferError.None)]
        public async Task ReadAsStreamAsync_ValidServerResponse_Success(
            LoopbackServer.TransferType transferType,
            LoopbackServer.TransferError transferError)
        {
            IPEndPoint serverEndPoint;
            Task serverTask = LoopbackServer.StartTransferTypeAndErrorServer(transferType, transferError, out serverEndPoint);

            await ReadAsStreamHelper(serverEndPoint);

            await serverTask;
        }

        private async Task ReadAsStreamHelper(IPEndPoint serverEndPoint)
        {
            using (HttpClient client = CreateHttpClient())
            {
                using (var response = await client.GetAsync(
                    new Uri($"http://{serverEndPoint.Address}:{(serverEndPoint).Port}/"),
                    HttpCompletionOption.ResponseHeadersRead))
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    var buffer = new byte[1];
                    while (await stream.ReadAsync(buffer, 0, 1) > 0) ;
                }
            }
        }
    }
}
