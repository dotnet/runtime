// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class SslStreamDisposeTest
    {
        [Fact]
        public async Task DisposeAsync_NotConnected_ClosesStream()
        {
            var network = new VirtualNetwork();
            var clientNet = new VirtualNetworkStream(network, isServer: false);
            var serverNet = new VirtualNetworkStream(network, isServer: true);

            var clientStream = new SslStream(clientNet, false, delegate { return true; });
            var serverStream = new SslStream(serverNet, false, delegate { return true; });

            Assert.False(clientNet.Disposed);
            await clientStream.DisposeAsync();
            Assert.True(clientNet.Disposed);

            Assert.False(serverNet.Disposed);
            await serverStream.DisposeAsync();
            Assert.True(serverNet.Disposed);
        }

        [Fact]
        public async Task DisposeAsync_Connected_ClosesStream()
        {
            var network = new VirtualNetwork();
            var clientNet = new VirtualNetworkStream(network, isServer: false);
            var serverNet = new VirtualNetworkStream(network, isServer: true);

            var clientStream = new SslStream(clientNet, false, delegate { return true; });
            var serverStream = new SslStream(serverNet, false, delegate { return true; });

            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                await TestConfiguration.WhenAllOrAnyFailedWithTimeout(
                    clientStream.AuthenticateAsClientAsync(certificate.GetNameInfo(X509NameType.SimpleName, false)),
                    serverStream.AuthenticateAsServerAsync(certificate));
            }

            Assert.False(clientNet.Disposed);
            await clientStream.DisposeAsync();
            Assert.True(clientNet.Disposed);

            Assert.False(serverNet.Disposed);
            await serverStream.DisposeAsync();
            Assert.True(serverNet.Disposed);
        }
    }
}
