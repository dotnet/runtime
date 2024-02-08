// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests
{
    [Collection(nameof(QuicCountersListener))]
    [ConditionalClass(typeof(QuicTestBase), nameof(QuicTestBase.IsSupported), nameof(QuicTestBase.IsNotArm32CoreClrStressTest))]
    [SkipOnPlatform(TestPlatforms.Windows, "CipherSuitesPolicy is not supported on Windows")]
    public class MsQuicCipherSuitesPolicyTests : QuicTestBase
    {
        public MsQuicCipherSuitesPolicyTests(ITestOutputHelper output) : base(output) { }

        private async Task TestConnection(CipherSuitesPolicy serverPolicy, CipherSuitesPolicy clientPolicy)
        {
            var listenerOptions = new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    var serverOptions = CreateQuicServerOptions();
                    serverOptions.ServerAuthenticationOptions.CipherSuitesPolicy = serverPolicy;
                    return ValueTask.FromResult(serverOptions);
                }
            };
            await using QuicListener listener = await CreateQuicListener(listenerOptions);

            var clientOptions = CreateQuicClientOptions(listener.LocalEndPoint);
            clientOptions.ClientAuthenticationOptions.CipherSuitesPolicy = clientPolicy;
            await using QuicConnection clientConnection = await CreateQuicConnection(clientOptions);

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
            var listenerOptions = new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(IPAddress.Loopback, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    var serverOptions = CreateQuicServerOptions();
                    serverOptions.ServerAuthenticationOptions.CipherSuitesPolicy = policy;
                    return ValueTask.FromResult(serverOptions);
                }
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await CreateQuicListener(listenerOptions));

            var clientOptions = CreateQuicClientOptions(new IPEndPoint(IPAddress.Loopback, 5000));
            clientOptions.ClientAuthenticationOptions.CipherSuitesPolicy = policy;
            Assert.ThrowsAsync<ArgumentException>(async () => await CreateQuicConnection(clientOptions));
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