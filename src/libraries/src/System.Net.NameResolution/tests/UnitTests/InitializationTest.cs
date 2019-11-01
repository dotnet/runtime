// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable 0618 // using obsolete methods

using System.Threading.Tasks;

using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class InitializationTests
    {
        [Fact]
        public void Dns_BeginGetHostAddresses_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.BeginGetHostAddresses(null, null, null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_BeginGetHostByName_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.BeginGetHostByName(null, null, null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_BeginGetHostEntry_String_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.BeginGetHostEntry((string)null, null, null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_BeginGetHostEntry_IPAddress_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.BeginGetHostEntry((IPAddress)null, null, null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostAddresses_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostAddresses(null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostByAddress_String_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostByAddress((string)null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostByAddress_IPAddress_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostByAddress((IPAddress)null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostByName_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostByName(null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostEntry_String_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostEntry((string)null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostEntry_IPAddress_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostEntry((IPAddress)null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_Resolve_CallSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.Resolve(null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public async Task Dns_GetHostAddressesAsync_CallsSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            await Assert.ThrowsAnyAsync<Exception>(() => Dns.GetHostAddressesAsync(null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public async Task Dns_GetHostEntryAsync_CallsSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            await Assert.ThrowsAnyAsync<Exception>(() => Dns.GetHostEntryAsync((string)null));
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }

        [Fact]
        public void Dns_GetHostName_CallsSocketInit_Ok()
        {
            NameResolutionPal.FakesReset();
            Assert.ThrowsAny<Exception>(() => Dns.GetHostName());
            Assert.NotEqual(0, NameResolutionPal.FakesEnsureSocketsAreInitializedCallCount);
        }
    }
}
