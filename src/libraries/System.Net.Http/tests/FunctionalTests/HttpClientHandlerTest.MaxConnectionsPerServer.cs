// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "dotnet/corefx #20010")]
    public class HttpClientHandler_MaxConnectionsPerServer_Test : HttpClientTestBase
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "MaxConnectionsPerServer either returns two or int.MaxValue depending if ctor of HttpClientHandlerTest executed first. Disabling cause of random xunit execution order.")]
        public void Default_ExpectedValue()
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Set_InvalidValues_Throws(int invalidValue)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => handler.MaxConnectionsPerServer = invalidValue);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MaxValue - 1)]
        public void Set_ValidValues_Success(int validValue)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            {
                try
                {
                    handler.MaxConnectionsPerServer = validValue;
                }
                catch (PlatformNotSupportedException)
                {
                    // Some older libcurls used in some of our Linux CI systems don't support this
                    Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                }
            }
        }

        [Theory]
        [InlineData(1, 5, false)]
        [InlineData(1, 5, true)]
        [InlineData(2, 2, false)]
        [InlineData(2, 2, true)]
        [InlineData(3, 2, false)]
        [InlineData(3, 2, true)]
        [InlineData(3, 5, false)]
        public async Task GetAsync_MaxLimited_ConcurrentCallsStillSucceed(int maxConnections, int numRequests, bool secure)
        {
            using (HttpClientHandler handler = CreateHttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                handler.MaxConnectionsPerServer = maxConnections;
                await Task.WhenAll(
                    from i in Enumerable.Range(0, numRequests)
                    select client.GetAsync(secure ? Configuration.Http.RemoteEchoServer : Configuration.Http.SecureRemoteEchoServer));
            }
        }
    }
}
