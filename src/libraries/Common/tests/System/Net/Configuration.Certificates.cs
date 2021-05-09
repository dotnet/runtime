// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Test.Cryptography;
using Xunit;

namespace System.Net.Test.Common
{
    public static partial class Configuration
    {
        public static partial class Certificates
        {
            private const string CertificatePassword = "PLACEHOLDER";
            private const string TestDataFolder = "TestDataCertificates";
            private const int MutexTimeoutMs = 120_000;

            private static readonly X509Certificate2 s_serverCertificate;
            private static readonly X509Certificate2 s_clientCertificate;
            private static readonly X509Certificate2 s_noEKUCertificate;
            private static readonly X509Certificate2 s_selfSignedServerCertificate;
            private static readonly X509Certificate2 s_selfSignedClientCertificate;
            private static X509Certificate2 s_selfSigned13ServerCertificate;

            static Certificates()
            {
                Mutex mutex =
                    PlatformDetection.IsInAppContainer ? new Mutex(initiallyOwned: false, "Local\\CoreFXTest.Configuration.Certificates.LoadPfxCertificate") : // UWP doesn't support Global mutexes
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

            public static X509Certificate2 GetSelfSigned13ServerCertificate()
            {
                if (s_selfSigned13ServerCertificate == null)
                {
                    X509Certificate2 cert;

                    using (ECDsa dsa = ECDsa.Create())
                    {
                        var certReq = new CertificateRequest("CN=testservereku.contoso.com", dsa, HashAlgorithmName.SHA256);
                        certReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                        certReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));
                        certReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));

                        X509Certificate2 innerCert = certReq.CreateSelfSigned(DateTimeOffset.UtcNow.AddMonths(-1), DateTimeOffset.UtcNow.AddMonths(1));

                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                        {
                            using (innerCert)
                            {
                                cert = new X509Certificate2(innerCert.Export(X509ContentType.Pfx));
                            }
                        }
                        else
                        {
                            cert = innerCert;
                        }
                    }

                    if (Interlocked.CompareExchange(ref s_selfSigned13ServerCertificate, cert, null) != null)
                    {
                        // Lost a race to create.
                        cert.Dispose();
                    }
                }

                return new X509Certificate2(s_selfSigned13ServerCertificate);
            }
        }
    }
}
