// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Security.Tests
{
    public static class TestHelper
    {
        private static readonly X509KeyUsageExtension s_eeKeyUsage =
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                critical: false);

        private static readonly X509EnhancedKeyUsageExtension s_tlsServerEku =
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.1", null)
                },
                false);

        private static readonly X509BasicConstraintsExtension s_eeConstraints =
            new X509BasicConstraintsExtension(false, false, 0, false);

        private static readonly byte[] s_ping = Encoding.UTF8.GetBytes("PING");
        private static readonly byte[] s_pong = Encoding.UTF8.GetBytes("PONG");

        public static (SslStream ClientStream, SslStream ServerStream) GetConnectedSslStreams()
        {
            (Stream clientStream, Stream serverStream) = GetConnectedStreams();
            return (new SslStream(clientStream), new SslStream(serverStream));
        }

        public static (Stream ClientStream, Stream ServerStream) GetConnectedStreams()
        {
            if (Capability.SecurityForceSocketStreams())
            {
                // DOTNET_TEST_NET_SECURITY_FORCE_SOCKET_STREAMS is set.
                return GetConnectedTcpStreams();
            }

            return ConnectedStreams.CreateBidirectional(initialBufferSize: 4096, maxBufferSize: int.MaxValue);
        }

        internal static (NetworkStream ClientStream, NetworkStream ServerStream) GetConnectedTcpStreams()
        {
            using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.Connect(listener.LocalEndPoint);
                Socket serverSocket = listener.Accept();

                serverSocket.NoDelay = true;
                clientSocket.NoDelay = true;

                return (new NetworkStream(clientSocket, ownsSocket: true), new NetworkStream(serverSocket, ownsSocket: true));
            }

        }

        internal static void CleanupCertificates(string testName)
        {
            string caName = $"O={testName}";
            try
            {
                using (X509Store store = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadWrite);
                    foreach (X509Certificate2 cert in store.Certificates)
                    {
                        if (cert.Subject.Contains(caName))
                        {
                            store.Remove(cert);
                        }
                    }
                }
            }
            catch { };

            try
            {
                using (X509Store store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadWrite);
                    foreach (X509Certificate2 cert in store.Certificates)
                    {
                        if (cert.Subject.Contains(caName))
                        {
                            store.Remove(cert);
                        }
                    }
                }
            }
            catch { };
        }

        internal static (X509Certificate2 certificate, X509Certificate2Collection) GenerateCertificates(string targetName, [CallerMemberName] string? testName = null)
        {
            if (PlatformDetection.IsWindows && testName != null)
            {
                CleanupCertificates(testName);
            }

            X509Certificate2Collection chain = new X509Certificate2Collection();
            X509ExtensionCollection extensions = new X509ExtensionCollection();

            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName(targetName);
            extensions.Add(builder.Build());
            extensions.Add(s_eeConstraints);
            extensions.Add(s_eeKeyUsage);
            extensions.Add(s_tlsServerEku);

            CertificateAuthority.BuildPrivatePki(
                PkiOptions.IssuerRevocationViaCrl,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                subjectName: targetName,
                testName: testName,
                keySize: 2048,
                extensions: extensions);

            chain.Add(intermediate.CloneIssuerCert());
            chain.Add(root.CloneIssuerCert());

            responder.Dispose();
            root.Dispose();
            intermediate.Dispose();

            if (PlatformDetection.IsWindows)
            {
                endEntity = new X509Certificate2(endEntity.Export(X509ContentType.Pfx));
            }

            return (endEntity, chain);
        }

        internal static async Task PingPong(SslStream client, SslStream server)
        {
            byte[] buffer = new byte[s_ping.Length];
            ValueTask t = client.WriteAsync(s_ping);

            int remains = s_ping.Length;
            while (remains > 0)
            {
                int readLength = await server.ReadAsync(buffer, buffer.Length - remains, remains);
                Assert.True(readLength > 0);
                remains -= readLength;
            }
            Assert.Equal(s_ping, buffer);
            await t;

            t = server.WriteAsync(s_pong);
            remains = s_pong.Length;
            while (remains > 0)
            {
                int readLength = await client.ReadAsync(buffer, buffer.Length - remains, remains);
                Assert.True(readLength > 0);
                remains -= readLength;
            }

            Assert.Equal(s_pong, buffer);
            await t;
        }
    }
}
