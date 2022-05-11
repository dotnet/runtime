// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Security.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public class SslStreamEKUTest
    {
        public static bool IsRootCertificateInstalled => Capability.IsTrustedRootCertificateInstalled();
        public static bool DoesNotSendCAListByDefault => !PlatformDetection.SendsCAListByDefault;

        public const int TestTimeoutMilliseconds = 15 * 1000;

        public static X509Certificate2 serverCertificateServerEku = Configuration.Certificates.GetServerCertificate();
        public static X509Certificate2 serverCertificateNoEku = Configuration.Certificates.GetNoEKUCertificate();
        public static X509Certificate2 serverCertificateWrongEku = Configuration.Certificates.GetClientCertificate();

        public static X509Certificate2 clientCertificateWrongEku = Configuration.Certificates.GetServerCertificate();
        public static X509Certificate2 clientCertificateNoEku = Configuration.Certificates.GetNoEKUCertificate();
        public static X509Certificate2 clientCertificateClientEku = Configuration.Certificates.GetClientCertificate();

        [ConditionalFact(nameof(IsRootCertificateInstalled))]
        public async Task SslStream_NoEKUServerAuth_Ok()
        {
            var serverOptions = new HttpsTestServer.Options();
            serverOptions.ServerCertificate = serverCertificateNoEku;

            using (var server = new HttpsTestServer(serverOptions))
            {
                server.Start();

                var clientOptions = new HttpsTestClient.Options(new IPEndPoint(IPAddress.Loopback, server.Port));
                clientOptions.ServerName = serverOptions.ServerCertificate.GetNameInfo(X509NameType.SimpleName, false);

                var client = new HttpsTestClient(clientOptions);

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();
                tasks[1] = client.HttpsRequestAsync();

                await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMilliseconds(TestTimeoutMilliseconds));
            }
        }

        [ConditionalFact(nameof(IsRootCertificateInstalled))]
        public async Task SslStream_ClientEKUServerAuth_Fails()
        {
            var serverOptions = new HttpsTestServer.Options();
            serverOptions.ServerCertificate = serverCertificateWrongEku;

            using (var server = new HttpsTestServer(serverOptions))
            {
                server.Start();

                var clientOptions = new HttpsTestClient.Options(new IPEndPoint(IPAddress.Loopback, server.Port));
                clientOptions.ServerName = serverOptions.ServerCertificate.GetNameInfo(X509NameType.SimpleName, false);

                var client = new HttpsTestClient(clientOptions);

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();
                tasks[1] = client.HttpsRequestAsync();

                var exception = await Assert.ThrowsAsync<AuthenticationException>(() => tasks[1]);
            }
        }

        [ConditionalFact(nameof(IsRootCertificateInstalled))]
        public async Task SslStream_NoEKUClientAuth_Ok()
        {
            var serverOptions = new HttpsTestServer.Options();
            serverOptions.ServerCertificate = serverCertificateServerEku;
            serverOptions.RequireClientAuthentication = true;

            using (var server = new HttpsTestServer(serverOptions))
            {
                server.Start();

                var clientOptions = new HttpsTestClient.Options(new IPEndPoint(IPAddress.Loopback, server.Port));
                clientOptions.ServerName = serverOptions.ServerCertificate.GetNameInfo(X509NameType.SimpleName, false);
                clientOptions.ClientCertificate = clientCertificateNoEku;

                var client = new HttpsTestClient(clientOptions);

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();
                tasks[1] = client.HttpsRequestAsync();

                await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMilliseconds(TestTimeoutMilliseconds));
            }
        }

        [ConditionalFact(nameof(IsRootCertificateInstalled))]
        public async Task SslStream_ServerEKUClientAuth_Fails()
        {
            var serverOptions = new HttpsTestServer.Options();
            serverOptions.ServerCertificate = serverCertificateServerEku;
            serverOptions.RequireClientAuthentication = true;

            using (var server = new HttpsTestServer(serverOptions))
            {
                server.Start();

                var clientOptions = new HttpsTestClient.Options(new IPEndPoint(IPAddress.Loopback, server.Port));
                clientOptions.ServerName = serverOptions.ServerCertificate.GetNameInfo(X509NameType.SimpleName, false);
                clientOptions.ClientCertificate = clientCertificateWrongEku;

                var client = new HttpsTestClient(clientOptions);

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();
                tasks[1] = client.HttpsRequestAsync();

                await Assert.ThrowsAsync<AuthenticationException>(() => tasks[0]);

                if (OperatingSystem.IsWindows())
                {
                    // IOException is thrown when trying to read from a disconnected socket.
                    await Assert.ThrowsAsync<IOException>(() => tasks[1]);
                }
                else
                {
                    // Zero bytes read by client on disconnected socket.
                    await Assert.ThrowsAsync<InvalidOperationException>(() => tasks[1]);
                }
            }
        }

        [ConditionalFact(nameof(IsRootCertificateInstalled), nameof(DoesNotSendCAListByDefault))]
        public async Task SslStream_SelfSignedClientEKUClientAuth_Ok()
        {
            var serverOptions = new HttpsTestServer.Options();
            serverOptions.ServerCertificate = serverCertificateServerEku;
            serverOptions.RequireClientAuthentication = true;
            serverOptions.IgnoreSslPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors;

            using (var server = new HttpsTestServer(serverOptions))
            {
                server.Start();

                var clientOptions = new HttpsTestClient.Options(new IPEndPoint(IPAddress.Loopback, server.Port));
                clientOptions.ServerName = serverOptions.ServerCertificate.GetNameInfo(X509NameType.SimpleName, false);
                clientOptions.ClientCertificate = Configuration.Certificates.GetSelfSignedClientCertificate();

                var client = new HttpsTestClient(clientOptions);

                var tasks = new Task[2];
                tasks[0] = server.AcceptHttpsClientAsync();
                tasks[1] = client.HttpsRequestAsync();

                await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromMilliseconds(TestTimeoutMilliseconds));
            }
        }
    }
}
