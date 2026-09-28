// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Principal;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class DnsResolverImpersonatedTest : IClassFixture<WindowsIdentityFixture>
    {
        private const string TestHost = "microsoft.com";

        private readonly WindowsIdentityFixture _fixture;

        public DnsResolverImpersonatedTest(WindowsIdentityFixture windowsIdentityFixture)
        {
            _fixture = windowsIdentityFixture;

            Assert.False(_fixture.TestAccount.AccountTokenHandle.IsInvalid);
            Assert.False(string.IsNullOrEmpty(_fixture.TestAccount.AccountName));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.CanRunImpersonatedTests))]
        [OuterLoop]
        public void ResolveAddresses_RunImpersonated_Works()
        {
            WindowsIdentity.RunImpersonated(_fixture.TestAccount.AccountTokenHandle, () =>
            {
                using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
                {
                    Assert.Equal(_fixture.TestAccount.AccountName, currentIdentity.Name);
                }

                using DnsResolver resolver = new DnsResolver();
                DnsResult<AddressRecord> result = resolver.ResolveAddresses(TestHost);

                Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
                Assert.NotEmpty(result.Records);
            });
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.CanRunImpersonatedTests))]
        [OuterLoop]
        public async Task ResolveAddressesAsync_RunImpersonated_Works()
        {
            await WindowsIdentity.RunImpersonatedAsync(_fixture.TestAccount.AccountTokenHandle, async () =>
            {
                using (WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent())
                {
                    Assert.Equal(_fixture.TestAccount.AccountName, currentIdentity.Name);
                }

                using DnsResolver resolver = new DnsResolver();
                DnsResult<AddressRecord> result = await resolver.ResolveAddressesAsync(TestHost);

                Assert.Equal(DnsResponseCode.NoError, result.ResponseCode);
                Assert.NotEmpty(result.Records);
            });
        }
    }
}
