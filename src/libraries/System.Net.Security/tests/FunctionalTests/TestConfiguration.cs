// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace System.Net.Security.Tests
{
    internal static class TestConfiguration
    {
        public const SslProtocols DefaultSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;

        public static X509Certificate2 GetServerCertificate()
        {
            X509Certificate2 certificate = null;

            var certCollection = new X509Certificate2Collection();
            certCollection.Import(Path.Combine("TestData", "DummyTcpServer.pfx"));

            foreach (X509Certificate2 c in certCollection)
            {
                if (c.HasPrivateKey)
                {
                    certificate = c;
                    break;
                }
            }

            Assert.NotNull(certificate);
            return certificate;
        }
    }
}
