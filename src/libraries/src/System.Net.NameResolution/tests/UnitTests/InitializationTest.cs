// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class InitializationTests
    {
        [Fact]
        public async Task Dns_GetHostAddressesAsync_CallsSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            await Assert.ThrowsAnyAsync<Exception>(() => Dns.GetHostAddressesAsync(null));
            Assert.Equal(1, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }
        
        [Fact]
        public async Task Dns_GetHostEntryAsync_CallsSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            await Assert.ThrowsAnyAsync<Exception>(() => Dns.GetHostEntryAsync((string)null));
            Assert.Equal(1, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostName_CallsSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostName());
            Assert.Equal(1, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }
    }
}
