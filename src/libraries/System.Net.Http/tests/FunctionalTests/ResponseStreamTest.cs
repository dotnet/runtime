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
    public class ResponseStreamTest
    {
        private readonly ITestOutputHelper _output;
        
        public ResponseStreamTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task GetStreamAsync_ReadToEnd_Success()
        {
            HttpClient client = GetHttpClient();
            
            Stream stream = await client.GetStreamAsync(HttpTestServers.RemoteEchoServer);
            using (var reader = new StreamReader(stream))
            {
                string responseBody = reader.ReadToEnd();
                _output.WriteLine(responseBody);
                Assert.True(IsValidResponseBody(responseBody));
            }
        }

        [Fact]
        public async Task GetAsync_UseResponseHeadersReadAndCallLoadIntoBuffer_Success()
        {
            HttpClient client = GetHttpClient();
            
            HttpResponseMessage response =
                await client.GetAsync(HttpTestServers.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead);
            await response.Content.LoadIntoBufferAsync();

            string responseBody = await response.Content.ReadAsStringAsync();
            _output.WriteLine(responseBody);
            Assert.True(IsValidResponseBody(responseBody));
        }

        [Fact]
        public async Task GetAsync_UseResponseHeadersReadAndCopyToMemoryStream_Success()
        {
            HttpClient client = GetHttpClient();
            
            HttpResponseMessage response =
                await client.GetAsync(HttpTestServers.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead);

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            
            using (var reader = new StreamReader(memoryStream))
            {
                string responseBody = reader.ReadToEnd();
                _output.WriteLine(responseBody);
                Assert.True(IsValidResponseBody(responseBody));
            }
        }

        [Fact]
        public async Task ReadAsStreamAsync_Cancel_TaskIsCanceled()
        {
            var cts = new CancellationTokenSource();

            using (var client = new HttpClient())
            using (HttpResponseMessage response =
                    await client.GetAsync(HttpTestServers.RemoteEchoServer, HttpCompletionOption.ResponseHeadersRead))
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

        [ActiveIssue(6231, PlatformID.Windows)]
        [Theory]
        [InlineData(LoopbackServer.TransferType.ContentLength, LoopbackServer.TransferError.ContentLengthTooLarge)]
        [InlineData(LoopbackServer.TransferType.Chunked, LoopbackServer.TransferError.MissingChunkTerminator)]
        [InlineData(LoopbackServer.TransferType.Chunked, LoopbackServer.TransferError.ChunkSizeTooLarge)]
        public async Task ReadAsStreamAsync_InvalidServerResponse_ThrowsIOException(
            LoopbackServer.TransferType transferType,
            LoopbackServer.TransferError transferError)
        {
            IPEndPoint serverEndPoint;
            Task serverTask = LoopbackServer.StartServer(transferType, transferError, out serverEndPoint);

            await Assert.ThrowsAsync<IOException>(() => ReadAsStreamHelper(serverEndPoint));

            await serverTask;
        }

        [Theory]
        [InlineData(LoopbackServer.TransferType.None, LoopbackServer.TransferError.None)]
        [InlineData(LoopbackServer.TransferType.ContentLength, LoopbackServer.TransferError.None)]
        [InlineData(LoopbackServer.TransferType.Chunked, LoopbackServer.TransferError.None)]
        public async Task ReadAsStreamAsync_ValidServerResponse_Success(
            LoopbackServer.TransferType transferType,
            LoopbackServer.TransferError transferError)
        {
            IPEndPoint serverEndPoint;
            Task serverTask = LoopbackServer.StartServer(transferType, transferError, out serverEndPoint);

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

        // These methods help to validate the response body since the endpoint will echo
        // all request headers.
        //
        // TODO: This validation will be improved in the future once the test server endpoint
        // is able to provide a custom response header with a SHA1 hash of the expected response body.
        private readonly string[] CustomHeaderValues = new string[] {"abcdefg", "12345678", "A1B2C3D4"};
        private HttpClient GetHttpClient()
        {
            var client = new HttpClient();
            int ndx = 1;
            foreach (string headerValue in CustomHeaderValues)
            {
                client.DefaultRequestHeaders.Add("X-Custom" + ndx.ToString(), headerValue);
                ndx++;
            }

            return client;
        }

        private bool IsValidResponseBody(string responseBody)
        {
            foreach (string headerValue in CustomHeaderValues)
            {
                if (!responseBody.Contains(headerValue))
                {
                    return false;
                }
            }            

            return true;
        }
    }
}
