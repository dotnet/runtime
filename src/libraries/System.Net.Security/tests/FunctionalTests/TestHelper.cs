// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace System.Net.Security.Tests
{
    public static class TestHelper
    {
        public static (Stream ClientStream, Stream ServerStream) GetConnectedStreams()
        {
            if (Capability.SecurityForceSocketStreams())
            {
                return GetConnectedTcpStreams();
            }

            return GetConnectedVirtualStreams();
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

        internal static (VirtualNetworkStream ClientStream, VirtualNetworkStream ServerStream) GetConnectedVirtualStreams()
        {
            VirtualNetwork vn = new VirtualNetwork();

            return (new VirtualNetworkStream(vn, isServer: false), new VirtualNetworkStream(vn, isServer: true));
        }

        internal static (X509Certificate2 certificate, X509Certificate2Collection) GenerateCertificates(string name, DateTimeOffset startTime, string? caUrl = null)
        {
            X509Certificate2Collection chain = new X509Certificate2Collection();

            using (RSA root = RSA.Create())
            using (RSA intermediate = RSA.Create())
            using (RSA server = RSA.Create())
            {
                CertificateRequest rootReq = new CertificateRequest(
                    "CN=Root",
                    root,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                rootReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));
                rootReq.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(rootReq.PublicKey, false));
                rootReq.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, false));

                //DateTimeOffset start = DateTimeOffset.UtcNow.AddMinutes(-5);
                DateTimeOffset endTime = startTime.AddMonths(1);

                X509Certificate2 rootCertWithKey = rootReq.CreateSelfSigned(startTime, endTime);

                CertificateRequest intermedReq = new CertificateRequest(
                   "CN=Intermediate",
                   intermediate,
                   HashAlgorithmName.SHA256,
                   RSASignaturePadding.Pkcs1);

                intermedReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));
                intermedReq.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(intermedReq.PublicKey, false));
                intermedReq.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, false));

                byte[] serial = new byte[8];
                RandomNumberGenerator.Fill(serial);

                X509Certificate2 intermedCertWithKey;
                using (X509Certificate2 intermedPub = intermedReq.Create(rootCertWithKey, startTime, endTime, serial))
                {
                    intermedCertWithKey = intermedPub.CopyWithPrivateKey(intermediate);
                }

                CertificateRequest serverReq = new CertificateRequest(
                     $"CN={name}",
                     server,
                     HashAlgorithmName.SHA256,
                     RSASignaturePadding.Pkcs1);

                serverReq.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(false, false, 0, false));
                serverReq.CertificateExtensions.Add(
                    new X509SubjectKeyIdentifierExtension(serverReq.PublicKey, false));

                // Add Issuer KeyIdentifier
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] data = new byte[24];
                    data[0] = 0x30; //SEQUENCE
                    data[1] = 22;
                    data[2] = 0x80;
                    data[3] = 20;
                    Buffer.BlockCopy(sha1.ComputeHash(intermedCertWithKey.PublicKey.EncodedKeyValue.RawData), 0, data, 4, 20);
                    serverReq.CertificateExtensions.Add(new X509Extension(new Oid("2.5.29.35"), data, false));
                }

                // 1.3.6.1.5.5.7.1.1
                if (caUrl != null)
                {
                    var urlBytes = Encoding.ASCII.GetBytes(caUrl);

                    byte[] data = new byte[urlBytes.Length + 16];
                    data[0] = 0x30; //SEQUENCE
                    data[1] = (byte)(urlBytes.Length + 14);
                    data[2] = 0x30; //SEQUENCE;
                    data[3] = (byte)(urlBytes.Length + 12);
                    data[4] = 6; // OBJECT
                    data[5] = 8; // LENGTH
                    // OID
                    data[6] = 0x2b;
                    data[7] = 0x6;
                    data[8] = 0x1;
                    data[9] = 0x5;
                    data[10] = 0x5;
                    data[11] = 0x7;
                    data[12] = 0x30; // SEQUENCE
                    data[13] = 02;
                    data[14] = 0x86;
                    data[15] = (byte)(urlBytes.Length);
                    data[16] = 0x74;
                    data[17] = 0x74;
                    data[18] = 0x70;
                    Buffer.BlockCopy(urlBytes, 0, data, 16, urlBytes.Length);

                    serverReq.CertificateExtensions.Add(new X509Extension(new Oid("1.3.6.1.5.5.7.1.1"), data, false));
                }

                serverReq.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                        false));
                serverReq.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection()
                        {
                            new Oid("1.3.6.1.5.5.7.3.1", null),
                        },
                        false));

                SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
                builder.AddDnsName(name);
                builder.AddIpAddress(IPAddress.Loopback);
                builder.AddIpAddress(IPAddress.IPv6Loopback);
                serverReq.CertificateExtensions.Add(builder.Build());

                RandomNumberGenerator.Fill(serial);

                X509Certificate2 serverCert = serverReq.Create(intermedCertWithKey, startTime, endTime, serial);
                X509Certificate2 serverCertWithKey = serverCert.CopyWithPrivateKey(server);

                chain.Add(intermedCertWithKey);
                chain.Add(rootCertWithKey);

                return (serverCertWithKey, chain);
            }
        }
    }
}
