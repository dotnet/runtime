// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class ResponseStreamTest
    {
        private readonly ITestOutputHelper _output;
        
        public ResponseStreamTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetStreamAsync_ReadToEnd_Success()
        {
            var customHeaderValue = Guid.NewGuid().ToString("N");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-ResponseStreamTest", customHeaderValue);

                Stream stream = await client.GetStreamAsync(Configuration.Http.RemoteEchoServer);
                using (var reader = new StreamReader(stream))
                {
                    string responseBody = reader.ReadToEnd();
                    _output.WriteLine(responseBody);

                    // Calling GetStreamAsync() means we don't have access to the HttpResponseMessage.
                    // So, we can't use the MD5 hash validation to verify receipt of the response body.
                    // For this test, we can use a simpler verification of a custom header echo'ing back.
                    Assert.True(responseBody.Contains(customHeaderValue));
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task GetAsync_UseResponseHeadersReadAndCallLoadIntoBuffer_Success()
        {
            using (var client = new HttpClient())
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
            using (var client = new HttpClient())
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
        public async Task ReadAsStreamAsync_Cancel_TaskIsCanceled()
        {
            var cts = new CancellationTokenSource();

            using (var client = new HttpClient())
            using (HttpResponseMessage response =
                    await client.GetAsync(Configuration.Http.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead))
            using (Stream stream = await response.Content.ReadAsStreamAsync())
            {
                var buffer = new byte[2048];
                Task task = stream.ReadAsync(buffer, 0, buffer.Length, cts.Token);
                cts.Cancel();

                // Verify that the task completes successfully or is canceled.
                Assert.True(((IAsyncResult)task).AsyncWaitHandle.WaitOne(new TimeSpan(0, 0, 3)));
                Assert.True(task.Status == TaskStatus.RanToCompletion || task.Status == TaskStatus.Canceled);
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
            using (var client = new HttpClient())
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
