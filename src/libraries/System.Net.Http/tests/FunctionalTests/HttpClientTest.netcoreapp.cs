// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public sealed partial class HttpClientTest
    {
        [Fact]
        public void DefaultProxy_SetNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => HttpClient.DefaultProxy = null );
        }

        [Fact]
        public void DefaultProxy_Get_ReturnsNotNull()
        {
            IWebProxy proxy = HttpClient.DefaultProxy;
            Assert.NotNull(proxy);
        }

        [Fact]
        public void DefaultProxy_SetGet_Roundtrips()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy proxy = new WebProxy("http://localhost:3128/");
                HttpClient.DefaultProxy = proxy;
                Assert.True(Object.ReferenceEquals(proxy, HttpClient.DefaultProxy));

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

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

        [Fact]
        public void DefaultRequestVersion_InitialValueExpected()
        {
            using (var client = new HttpClient())
            {
                Assert.Equal(PlatformDetection.IsUap ? new Version(2, 0) : new Version(1, 1), client.DefaultRequestVersion);
                Assert.Same(client.DefaultRequestVersion, client.DefaultRequestVersion);
            }
        }

        [Fact]
        public void DefaultRequestVersion_Roundtrips()
        {
            using (var client = new HttpClient())
            {
                for (int i = 3; i < 5; i++)
                {
                    var newVersion = new Version(i, i, i, i);
                    client.DefaultRequestVersion = newVersion;
                    Assert.Same(newVersion, client.DefaultRequestVersion);
                }
            }
        }

        [Fact]
        public void DefaultRequestVersion_InvalidArgument_Throws()
        {
            using (var client = new HttpClient())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => client.DefaultRequestVersion = null);
                client.DefaultRequestVersion = new Version(1, 0); // still usable after
                Assert.Equal(new Version(1, 0), client.DefaultRequestVersion);
            }
        }

        [Fact]
        public async Task DefaultRequestVersion_SetAfterUse_Throws()
        {
            var handler = new StoreMessageHttpMessageInvoker();
            using (var client = new HttpClient(handler))
            {
                await client.GetAsync("http://doesntmatter", HttpCompletionOption.ResponseHeadersRead);
                Assert.Throws<InvalidOperationException>(() => client.DefaultRequestVersion = new Version(1, 1));
            }
        }

        [Fact]
        public async Task DefaultRequestVersion_UsedInCreatedMessages()
        {
            var handler = new StoreMessageHttpMessageInvoker();
            using (var client = new HttpClient(handler))
            {
                var version = new Version(1, 2, 3, 4);
                client.DefaultRequestVersion = version;
                await client.GetAsync("http://doesntmatter", HttpCompletionOption.ResponseHeadersRead);
                Assert.Same(version, handler.Message.Version);
            }
        }

        private sealed class StoreMessageHttpMessageInvoker : HttpMessageHandler
        {
            public HttpRequestMessage Message;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Message = request;
                return Task.FromResult(new HttpResponseMessage());
            }
        }
    }
}
