// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Http.Functional.Tests
{
    [ConditionalClass(typeof(SocketsHttpHandler), nameof(SocketsHttpHandler.IsSupported))]
    public sealed class SocketsHttpHandler_Http1KeepAlive_Test : HttpClientHandlerTestBase
    {
        public SocketsHttpHandler_Http1KeepAlive_Test(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task Http10Response_ConnectionIsReusedFor10And11()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                await client.SendAsync(CreateRequest(HttpMethod.Get, uri, HttpVersion.Version10, exactVersion: true));
                await client.SendAsync(CreateRequest(HttpMethod.Get, uri, HttpVersion.Version11, exactVersion: true));
                await client.SendAsync(CreateRequest(HttpMethod.Get, uri, HttpVersion.Version10, exactVersion: true));
            },
            server => server.AcceptConnectionAsync(async connection =>
            {
                HttpRequestData request = await connection.ReadRequestDataAsync();
                Assert.Equal(0, request.Version.Minor);
                await connection.WriteStringAsync("HTTP/1.0 200 OK\r\nContent-Length: 1\r\n\r\n1");
                connection.CompleteRequestProcessing();

                request = await connection.ReadRequestDataAsync();
                Assert.Equal(1, request.Version.Minor);
                await connection.WriteStringAsync("HTTP/1.0 200 OK\r\nContent-Length: 1\r\n\r\n2");
                connection.CompleteRequestProcessing();

                request = await connection.ReadRequestDataAsync();
                Assert.Equal(0, request.Version.Minor);
                await connection.WriteStringAsync("HTTP/1.0 200 OK\r\nContent-Length: 1\r\n\r\n3");
            }));
        }

        [OuterLoop("Uses Task.Delay")]
        [Fact]
        public async Task Http10ResponseWithKeepAliveTimeout_ConnectionRecycledAfterTimeout()
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                await client.GetAsync(uri);

                await Task.Delay(2000);
                await client.GetAsync(uri);
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestDataAsync();
                    await connection.WriteStringAsync("HTTP/1.0 200 OK\r\nKeep-Alive: timeout=1\r\nContent-Length: 1\r\n\r\n1");
                    connection.CompleteRequestProcessing();

                    await Assert.ThrowsAnyAsync<Exception>(() => connection.ReadRequestDataAsync());
                });

                await server.AcceptConnectionSendResponseAndCloseAsync();
            });
        }

        [Theory]
        [InlineData("timeout=1000", true)]
        [InlineData("timeout=30", true)]
        [InlineData("timeout=0", false)]
        [InlineData("foo, bar=baz, timeout=30", true)]
        [InlineData("foo, bar=baz, timeout=0", false)]
        [InlineData("timeout=-1", true)]
        [InlineData("timeout=abc", true)]
        [InlineData("max=1", true)]
        [InlineData("max=0", false)]
        [InlineData("max=-1", true)]
        [InlineData("max=abc", true)]
        [InlineData("timeout=30, max=1", true)]
        [InlineData("timeout=30, max=0", false)]
        [InlineData("timeout=0, max=1", false)]
        [InlineData("timeout=0, max=0", false)]
        public async Task Http10ResponseWithKeepAlive_ConnectionNotReusedForShortTimeoutOrMax0(string keepAlive, bool shouldReuseConnection)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                await client.GetAsync(uri);
                await client.GetAsync(uri);
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestDataAsync();
                    await connection.WriteStringAsync($"HTTP/1.0 200 OK\r\nKeep-Alive: {keepAlive}\r\nContent-Length: 1\r\n\r\n1");
                    connection.CompleteRequestProcessing();

                    if (shouldReuseConnection)
                    {
                        await connection.HandleRequestAsync();
                    }
                    else
                    {
                        await Assert.ThrowsAnyAsync<Exception>(() => connection.ReadRequestDataAsync());
                    }
                });

                if (!shouldReuseConnection)
                {
                    await server.AcceptConnectionSendResponseAndCloseAsync();
                }
            });
        }

        [Theory]
        [InlineData("timeout=1")]
        [InlineData("timeout=0")]
        [InlineData("max=1")]
        [InlineData("max=0")]
        public async Task Http11ResponseWithKeepAlive_KeepAliveIsIgnored(string keepAlive)
        {
            await LoopbackServer.CreateClientAndServerAsync(async uri =>
            {
                using HttpClient client = CreateHttpClient();

                await client.GetAsync(uri);
                await client.GetAsync(uri);
            },
            async server =>
            {
                await server.AcceptConnectionAsync(async connection =>
                {
                    await connection.ReadRequestDataAsync();
                    await connection.WriteStringAsync($"HTTP/1.1 200 OK\r\nKeep-Alive: {keepAlive}\r\nContent-Length: 1\r\n\r\n1");
                    connection.CompleteRequestProcessing();

                    await connection.HandleRequestAsync();
                });
            });
        }
    }
}
