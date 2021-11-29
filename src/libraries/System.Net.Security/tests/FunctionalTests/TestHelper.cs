// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
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

        private static readonly X509EnhancedKeyUsageExtension s_tlsClientEku =
            new X509EnhancedKeyUsageExtension(
                new OidCollection
                {
                    new Oid("1.3.6.1.5.5.7.3.2", null)
                },
                false);

        private static readonly X509BasicConstraintsExtension s_eeConstraints =
            new X509BasicConstraintsExtension(false, false, 0, false);

        public static readonly byte[] s_ping = Encoding.UTF8.GetBytes("PING");
        public static readonly byte[] s_pong = Encoding.UTF8.GetBytes("PONG");

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

        internal static void CleanupCertificates([CallerMemberName] string? testName = null)
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

        internal static (X509Certificate2 certificate, X509Certificate2Collection) GenerateCertificates(string targetName, [CallerMemberName] string? testName = null, bool longChain = false, bool serverCertificate = true)
        {
            const int keySize = 2048;
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
            extensions.Add(serverCertificate ? s_tlsServerEku : s_tlsClientEku);

            CertificateAuthority.BuildPrivatePki(
                PkiOptions.IssuerRevocationViaCrl,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority[] intermediates,
                out X509Certificate2 endEntity,
                intermediateAuthorityCount: longChain ? 3 : 1,
                subjectName: targetName,
                testName: testName,
                keySize: keySize,
                extensions: extensions);

            // Walk the intermediates backwards so we build the chain collection as
            // Issuer3
            // Issuer2
            // Issuer1
            // Root
            for (int i = intermediates.Length - 1; i >= 0; i--)
            {
                CertificateAuthority authority = intermediates[i];

                chain.Add(authority.CloneIssuerCert());
                authority.Dispose();
            }

            chain.Add(root.CloneIssuerCert());

            responder.Dispose();
            root.Dispose();

            if (PlatformDetection.IsWindows)
            {
                X509Certificate2 ephemeral = endEntity;
                endEntity = new X509Certificate2(endEntity.Export(X509ContentType.Pfx));
                ephemeral.Dispose();
            }

            return (endEntity, chain);
        }

        internal static async Task PingPong(SslStream client, SslStream server, CancellationToken cancellationToken = default)
        {
            byte[] buffer = new byte[s_ping.Length];
            ValueTask t = client.WriteAsync(s_ping, cancellationToken);

            int remains = s_ping.Length;
            while (remains > 0)
            {
                int readLength = await server.ReadAsync(buffer, buffer.Length - remains, remains, cancellationToken);
                Assert.True(readLength > 0);
                remains -= readLength;
            }
            Assert.Equal(s_ping, buffer);
            await t;

            t = server.WriteAsync(s_pong, cancellationToken);
            remains = s_pong.Length;
            while (remains > 0)
            {
                int readLength = await client.ReadAsync(buffer, buffer.Length - remains, remains, cancellationToken);
                Assert.True(readLength > 0);
                remains -= readLength;
            }

            Assert.Equal(s_pong, buffer);
            await t;
        }

        internal static string GetTestSNIName(string testMethodName, params SslProtocols?[] protocols)
        {
            static string ProtocolToString(SslProtocols? protocol)
            {
                return (protocol?.ToString() ?? "null").Replace(", ", "-");
            }

            var args = string.Join(".", protocols.Select(p => ProtocolToString(p)));
            var name = testMethodName.Length > 63 ? testMethodName.Substring(0, 63) : testMethodName;

            name = $"{name}.{args}";
            if (PlatformDetection.IsAndroid)
            {
                // Android does not support underscores in host names
                name = name.Replace("_", string.Empty);
            }

            return name;
        }
    }
}
