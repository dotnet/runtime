// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public sealed partial class HttpClientTest
    {
        [Fact]
        public async Task PatchAsync_Canceled_Throws()
        {
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => WhenCanceled<HttpResponseMessage>(c))))
            {
                var content = new ByteArrayContent(new byte[1]);
                var cts = new CancellationTokenSource();
                
                Task t1 = client.PatchAsync(CreateFakeUri(), content, cts.Token);

                cts.Cancel();
                
                await Assert.ThrowsAsync<TaskCanceledException>(() => t1);
            }
        }

        [Fact]
        public async Task PatchAsync_Success()
        {
            Action<HttpResponseMessage> verify = message => { using (message) Assert.Equal(HttpStatusCode.OK, message.StatusCode); };
            using (var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage()))))
            {
                verify(await client.PatchAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])));
                verify(await client.PatchAsync(CreateFakeUri(), new ByteArrayContent(new byte[1]), CancellationToken.None));
            }
        }

        [Fact]
        public void Dispose_UsePatchAfterDispose_Throws()
        {
            var client = new HttpClient(new CustomResponseHandler((r, c) => Task.FromResult(new HttpResponseMessage())));
            client.Dispose();

            Assert.Throws<ObjectDisposedException>(() => { client.PatchAsync(CreateFakeUri(), new ByteArrayContent(new byte[1])); });
        }
    }
}
