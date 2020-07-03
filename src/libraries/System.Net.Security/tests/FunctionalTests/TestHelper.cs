// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Sockets;
using System.Net.Test.Common;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.X509Certificates.Tests.Common;
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

        internal static (X509Certificate2 certificate, X509Certificate2Collection) GenerateCertificates(string name, string? testName = null)
        {
            X509Certificate2Collection chain = new X509Certificate2Collection();

            CertificateAuthority.BuildPrivatePki(
                PkiOptions.IssuerRevocationViaCrl,
                out RevocationResponder responder,
                out CertificateAuthority root,
                out CertificateAuthority intermediate,
                out X509Certificate2 endEntity,
                subjectName: name,
                testName: testName);

            chain.Add(intermediate.CloneIssuerCert());
            chain.Add(root.CloneIssuerCert());

            responder.Dispose();
            root.Dispose();
            intermediate.Dispose();

            return (endEntity, chain);
        }
    }
}
