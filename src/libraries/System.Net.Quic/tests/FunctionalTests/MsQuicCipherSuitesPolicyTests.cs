// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [ConditionalClass(typeof(QuicTestBase<MsQuicProviderFactory>), nameof(IsSupported))]
    [Collection(nameof(DisableParallelization))]
    [SkipOnPlatform(TestPlatforms.Windows, "CipherSuitesPolicy is not supported on Windows")]
    public class MsQuicCipherSuitesPolicyTests : QuicTestBase<MsQuicProviderFactory>
    {
        public MsQuicCipherSuitesPolicyTests(ITestOutputHelper output) : base(output) { }

        private async Task TestConnection(CipherSuitesPolicy serverPolicy, CipherSuitesPolicy clientPolicy)
        {
            var listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ServerAuthenticationOptions.CipherSuitesPolicy = serverPolicy;
            using QuicListener listener = CreateQuicListener(listenerOptions);

            var clientOptions = CreateQuicClientOptions();
            clientOptions.ClientAuthenticationOptions.CipherSuitesPolicy = clientPolicy;
            clientOptions.RemoteEndPoint = listener.ListenEndPoint;
            using QuicConnection clientConnection = CreateQuicConnection(clientOptions);

            await clientConnection.ConnectAsync();
            await clientConnection.CloseAsync(0);
        }

        [Fact]
        public Task SupportedCipher_Success()
        {
            CipherSuitesPolicy policy = new CipherSuitesPolicy(new[] { TlsCipherSuite.TLS_AES_128_GCM_SHA256 });
            return TestConnection(policy, policy);
        }

        [Theory]
        [InlineData(new TlsCipherSuite[] { })]
        [InlineData(new[] { TlsCipherSuite.TLS_AES_128_CCM_8_SHA256 })]
        public void NoSupportedCiphers_ThrowsArgumentException(TlsCipherSuite[] ciphers)
        {
            CipherSuitesPolicy policy = new CipherSuitesPolicy(ciphers);
            var listenerOptions = CreateQuicListenerOptions();
            listenerOptions.ServerAuthenticationOptions.CipherSuitesPolicy = policy;
            Assert.Throws<ArgumentException>(() => CreateQuicListener(listenerOptions));

            var clientOptions = CreateQuicClientOptions();
            clientOptions.ClientAuthenticationOptions.CipherSuitesPolicy = policy;
            clientOptions.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 5000);
            Assert.Throws<ArgumentException>(() => CreateQuicConnection(clientOptions));
        }

        [Fact]
        public async Task MismatchedCipherPolicies_ConnectAsync_ThrowsQuicException()
        {
            await Assert.ThrowsAsync<QuicException>(() => TestConnection(
               new CipherSuitesPolicy(new[] { TlsCipherSuite.TLS_AES_128_GCM_SHA256 }),
               new CipherSuitesPolicy(new[] { TlsCipherSuite.TLS_AES_256_GCM_SHA384 })
            ));
        }
    }
}