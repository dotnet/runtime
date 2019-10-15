// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
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
            private const int MutexTimeoutMs = 120_000;

            private static readonly X509Certificate2 s_serverCertificate;
            private static readonly X509Certificate2 s_clientCertificate;
            private static readonly X509Certificate2 s_noEKUCertificate;
            private static readonly X509Certificate2 s_selfSignedServerCertificate;
            private static readonly X509Certificate2 s_selfSignedClientCertificate;

            static Certificates()
            {
                Mutex mutex =
                    PlatformDetection.IsUap ? new Mutex(initiallyOwned: false, "Local\\CoreFXTest.Configuration.Certificates.LoadPfxCertificate") : // UWP doesn't support Global mutexes
                    PlatformDetection.IsWindows ? new Mutex(initiallyOwned: false, "Global\\CoreFXTest.Configuration.Certificates.LoadPfxCertificate") :
                    null;
                using (mutex)
                {
                    try
                    {
                        byte[] serverCertificateBytes = File.ReadAllBytes(Path.Combine(TestDataFolder, "testservereku.contoso.com.pfx"));
                        byte[] clientCertificateBytes = File.ReadAllBytes(Path.Combine(TestDataFolder, "testclienteku.contoso.com.pfx"));
                        byte[] noEKUCertificateBytes = File.ReadAllBytes(Path.Combine(TestDataFolder, "testnoeku.contoso.com.pfx"));
                        byte[] selfSignedServerCertificateBytes = File.ReadAllBytes(Path.Combine(TestDataFolder, "testselfsignedservereku.contoso.com.pfx"));
                        byte[] selfSignedClientCertificateBytes = File.ReadAllBytes(Path.Combine(TestDataFolder, "testselfsignedclienteku.contoso.com.pfx"));

                        // On Windows, applications should not import PFX files in parallel to avoid a known system-level
                        // race condition bug in native code which can cause crashes/corruption of the certificate state.
                        Assert.True(mutex?.WaitOne(MutexTimeoutMs) ?? true, "Could not acquire the global certificate mutex.");
                        try
                        {
                            s_serverCertificate = new X509Certificate2(serverCertificateBytes, CertificatePassword, X509KeyStorageFlags.Exportable);
                            s_clientCertificate = new X509Certificate2(clientCertificateBytes, CertificatePassword, X509KeyStorageFlags.Exportable);
                            s_noEKUCertificate = new X509Certificate2(noEKUCertificateBytes, CertificatePassword, X509KeyStorageFlags.Exportable);
                            s_selfSignedServerCertificate = new X509Certificate2(selfSignedServerCertificateBytes, CertificatePassword, X509KeyStorageFlags.Exportable);
                            s_selfSignedClientCertificate = new X509Certificate2(selfSignedClientCertificateBytes, CertificatePassword, X509KeyStorageFlags.Exportable);
                        }
                        finally { mutex?.ReleaseMutex(); }
                    }
                    catch (Exception ex)
                    {
                        Trace.Fail(nameof(Certificates) + " cctor threw " + ex.ToString());
                        throw;
                    }
                }
            }
            
            // These Get* methods make a copy of the certificates so that consumers own the lifetime of the
            // certificates handed back.  Consumers are expected to dispose of their certs when done with them.

            public static X509Certificate2 GetServerCertificate() => new X509Certificate2(s_serverCertificate);
            public static X509Certificate2 GetClientCertificate() => new X509Certificate2(s_clientCertificate);
            public static X509Certificate2 GetNoEKUCertificate() => new X509Certificate2(s_noEKUCertificate);
            public static X509Certificate2 GetSelfSignedServerCertificate() => new X509Certificate2(s_selfSignedServerCertificate);
            public static X509Certificate2 GetSelfSignedClientCertificate() => new X509Certificate2(s_selfSignedClientCertificate);
        }
    }
}
