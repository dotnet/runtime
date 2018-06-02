// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Xunit;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Certificates
        {
            private const string CertificatePassword = "testcertificate";
            private const string TestDataFolder = "TestData";

            public static X509Certificate2 GetServerCertificate() => GetCertificate("testservereku.contoso.com.pfx");

            public static X509Certificate2 GetClientCertificate() => GetCertificate("testclienteku.contoso.com.pfx");

            public static X509Certificate2 GetNoEKUCertificate() => GetCertificate("testnoeku.contoso.com.pfx");

            public static X509Certificate2 GetSelfSignedServerCertificate() => GetCertificate("testselfsignedservereku.contoso.com.pfx");

            public static X509Certificate2 GetSelfSignedClientCertificate() => GetCertificate("testselfsignedclienteku.contoso.com.pfx");

            public static X509Certificate2Collection GetServerCertificateCollection()
            {
                var certs = new X509Certificate2Collection();
                certs.Add(GetServerCertificate());

                return certs;
            }

            public static X509Certificate2Collection GetClientCertificateCollection()
            {
                var certs = new X509Certificate2Collection();
                certs.Add(GetClientCertificate());

                return certs;
            }

            private static X509Certificate2 GetCertificate(string certificateFileName)
            {
                try
                {
                    return new X509Certificate2(
                        File.ReadAllBytes(Path.Combine(TestDataFolder, certificateFileName)),
                        CertificatePassword,
                        X509KeyStorageFlags.DefaultKeySet);
                }
                catch (Exception ex)
                {
                    Debug.Fail(nameof(Configuration.Certificates.GetCertificate) + " threw " + ex.ToString());
                    throw;
                }
            }
        }
    }
}
