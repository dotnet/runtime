// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Http.Functional.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class HttpClientHandler_MaxConnectionsPerServer_Test
    {
        [Fact]
        public void Default_ExpectedValue()
        {
            using (var handler = new HttpClientHandler())
            {
                Assert.Equal(int.MaxValue, handler.MaxConnectionsPerServer);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Set_InvalidValues_Throws(int invalidValue)
        {
            using (var handler = new HttpClientHandler())
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
            using (var handler = new HttpClientHandler())
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

        [Fact]
        public async Task GetAsync_Max1_ConcurrentCallsStillSucceed()
        {
            using (var handler = new HttpClientHandler())
            using (var client = new HttpClient(handler))
            {
                try
                {
                    handler.MaxConnectionsPerServer = 1;
                }
                catch (PlatformNotSupportedException)
                {
                    // Some older libcurls used in some of our Linux CI systems don't support this
                    Assert.True(RuntimeInformation.IsOSPlatform(OSPlatform.Linux));
                }

                await Task.WhenAll(
                    from i in Enumerable.Range(0, 5)
                    select client.GetAsync(Configuration.Http.RemoteEchoServer));
            }
        }
    }
}
