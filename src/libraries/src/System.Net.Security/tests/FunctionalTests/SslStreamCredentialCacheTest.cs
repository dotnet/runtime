// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates; 
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamCredentialCacheTest
    {
        [Fact]
        public void SslStream_SameCertUsedForClientAndServer_Ok()
        {
            SslSessionsCacheTest().GetAwaiter().GetResult();
        }   

        private static async Task SslSessionsCacheTest()
        {
            VirtualNetwork network = new VirtualNetwork();

            using (var clientStream = new VirtualNetworkStream(network, isServer: false))
            using (var serverStream = new VirtualNetworkStream(network, isServer: true))
            using (var client = new SslStream(clientStream, true, AllowAnyCertificate))
            using (var server = new SslStream(serverStream, true, AllowAnyCertificate))
            using (X509Certificate2 certificate = Configuration.Certificates.GetServerCertificate())
            {
                // Using the same certificate for server and client auth.
                X509Certificate2Collection clientCertificateCollection = 
                    new X509Certificate2Collection(certificate);

                var tasks = new Task[2];

                tasks[0] = server.AuthenticateAsServerAsync(certificate, true, false);
                tasks[1] = client.AuthenticateAsClientAsync(
                                            certificate.GetNameInfo(X509NameType.SimpleName, false),
                                            clientCertificateCollection, false);


                await Task.WhenAll(tasks).TimeoutAfter(15 * 1000);

                Assert.True(client.IsMutuallyAuthenticated);
                Assert.True(server.IsMutuallyAuthenticated);
            }
        }

        private static bool AllowAnyCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
