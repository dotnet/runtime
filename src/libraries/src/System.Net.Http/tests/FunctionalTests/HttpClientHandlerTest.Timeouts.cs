// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public class HttpClientHandler_Timeouts_Test
    {
        [Fact]
        public void ConnectTimeout_Default()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Equal(Timeout.InfiniteTimeSpan, handler.ConnectTimeout);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        [InlineData(int.MaxValue + 1L)]
        public void ConnectTimeout_InvalidValues(long ms)
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.ConnectTimeout = TimeSpan.FromMilliseconds(ms));
            }
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MaxValue)]
        public void ConnectTimeout_ValidValues_Roundtrip(long ms)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ConnectTimeout = TimeSpan.FromMilliseconds(ms);
                Assert.Equal(TimeSpan.FromMilliseconds(ms), handler.ConnectTimeout);
            }
        }

        [Fact]
        public async Task ConnectTimeout_SetAfterUse_Throws()
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.ConnectTimeout = TimeSpan.FromMilliseconds(int.MaxValue);
                await client.GetAsync(HttpTestServers.RemoteEchoServer);
                Assert.Equal(TimeSpan.FromMilliseconds(int.MaxValue), handler.ConnectTimeout);
                Assert.Throws<InvalidOperationException>(() => handler.ConnectTimeout = TimeSpan.FromMilliseconds(1));
            }
        }

        [ActiveIssue(8181, PlatformID.Linux)] // failing to timeout on Ubuntu 16.04 in CI
        [Fact]
        public async Task ConnectTimeout_TimesOut_Throws()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                using (var handler = new HttpClientHandler() { ConnectTimeout = TimeSpan.FromMilliseconds(10) })
                using (var client = new HttpClient(handler))
                {
                    const int NumBacklogSockets = 16; // must be larger than OS' queue length when using Listen(1).
                    var socketBacklog = new List<Socket>(NumBacklogSockets);
                    try
                    {
                        // Listen's backlog is only advisory; the OS may actually allow a larger backlog than that.
                        // As such, create a bunch of clients to connect to the endpoint so that our actual request
                        // will timeout while trying to connect.
                        for (int i = 0; i < NumBacklogSockets; i++)
                        {
                            var tmpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            var ignored = tmpClient.ConnectAsync(new IPEndPoint(IPAddress.Parse(url.Host), url.Port));
                            socketBacklog.Add(tmpClient);
                        }

                        // Make the actual connection.  It should timeout in connect.
                        var sw = Stopwatch.StartNew();
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.GetAsync(url));
                        sw.Stop();

                        Assert.InRange(sw.ElapsedMilliseconds, 1, 10 * 1000); // allow a very wide range
                    }
                    finally
                    {
                        foreach (Socket c in socketBacklog)
                        {
                            c.Dispose();
                        }
                    }
                }
            });
        }
    }
}
