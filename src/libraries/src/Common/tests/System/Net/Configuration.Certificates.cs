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

            private static Mutex m;
            private const int MutexTimeout = 120 * 1000;

            static Certificates()
            {
                if (PlatformDetection.IsUap)
                {
                    // UWP doesn't support Global mutexes.
                    m = new Mutex(false, "Local\\CoreFXTest.Configuration.Certificates.LoadPfxCertificate");
                }
                else
                {
                    m = new Mutex(false, "Global\\CoreFXTest.Configuration.Certificates.LoadPfxCertificate");
                }
            }

            public static X509Certificate2 GetServerCertificate() => GetCertWithPrivateKey(GetServerCertificateCollection());

            public static X509Certificate2 GetClientCertificate() => GetCertWithPrivateKey(GetClientCertificateCollection());

            public static X509Certificate2 GetNoEKUCertificate() => GetCertWithPrivateKey(GetNoEKUCertificateCollection());

            public static X509Certificate2 GetSelfSignedServerCertificate() => GetCertWithPrivateKey(GetSelfSignedServerCertificateCollection());

            public static X509Certificate2 GetSelfSignedClientCertificate() => GetCertWithPrivateKey(GetSelfSignedClientCertificateCollection());

            public static X509Certificate2Collection GetServerCertificateCollection() => GetCertificateCollection("testservereku.contoso.com.pfx");

            public static X509Certificate2Collection GetClientCertificateCollection() => GetCertificateCollection("testclienteku.contoso.com.pfx");

            public static X509Certificate2Collection GetNoEKUCertificateCollection() => GetCertificateCollection("testnoeku.contoso.com.pfx");

            public static X509Certificate2Collection GetSelfSignedServerCertificateCollection() => GetCertificateCollection("testselfsignedservereku.contoso.com.pfx");

            public static X509Certificate2Collection GetSelfSignedClientCertificateCollection() => GetCertificateCollection("testselfsignedclienteku.contoso.com.pfx");

            private static X509Certificate2Collection GetCertificateCollection(string certificateFileName)
            {
                // On Windows, .Net Core applications should not import PFX files in parallel to avoid a known system-level race condition.
                // This bug results in corrupting the X509Certificate2 certificate state.
                try
                {
                    Assert.True(m.WaitOne(MutexTimeout), "Cannot acquire the global certificate mutex.");

                    var certCollection = new X509Certificate2Collection();
                    certCollection.Import(Path.Combine(TestDataFolder, certificateFileName), CertificatePassword, X509KeyStorageFlags.DefaultKeySet);

                    return certCollection;
                }
                catch (Exception ex)
                {
                    Debug.Fail(nameof(Configuration.Certificates.GetCertificateCollection) + " threw " + ex.ToString());
                    throw;
                }
                finally
                {
                    m.ReleaseMutex();
                }
            }

            private static X509Certificate2 GetCertWithPrivateKey(X509Certificate2Collection certCollection)
            {
                X509Certificate2 certificate = null;

                foreach (X509Certificate2 c in certCollection)
                {
                    if (certificate == null && c.HasPrivateKey)
                    {
                        certificate = c;
                    }
                    else
                    {
                        c.Dispose();
                    }
                }

                Assert.NotNull(certificate);
                return certificate;
            }
        }
    }
}
