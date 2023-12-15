﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    public partial class X509StoreTests
    {
        [ConditionalFact(nameof(NotRunningAsRootAndRemoteExecutorSupported))] // root can read '2.pem'
        [PlatformSpecific(TestPlatforms.Linux)] // Windows/OSX doesn't use SSL_CERT_{DIR,FILE}.
        private void X509Store_MachineStoreLoadSkipsInvalidFiles()
        {
            // We create a folder for our machine store and use it by setting SSL_CERT_{DIR,FILE}.
            // In the store we'll add some invalid files, but we start and finish with a valid file.
            // This is to account for the order in which the store is populated.
            string sslCertDir = GetTestFilePath();
            Directory.CreateDirectory(sslCertDir);

            // Valid file.
            File.WriteAllBytes(Path.Combine(sslCertDir, "0.pem"), TestData.SelfSigned1PemBytes);

            // File with invalid content.
            File.WriteAllText(Path.Combine(sslCertDir, "1.pem"), "This is not a valid cert");

            // File which is not readable by the current user.
            string unreadableFileName = Path.Combine(sslCertDir, "2.pem");
            File.WriteAllBytes(unreadableFileName, TestData.SelfSigned2PemBytes);
            File.SetUnixFileMode(unreadableFileName, UnixFileMode.None);

            // Valid file.
            File.WriteAllBytes(Path.Combine(sslCertDir, "3.pem"), TestData.SelfSigned3PemBytes);

            var psi = new ProcessStartInfo();
            psi.Environment.Add("SSL_CERT_DIR", sslCertDir);
            psi.Environment.Add("SSL_CERT_FILE", "/nonexisting");
            RemoteExecutor.Invoke(() =>
            {
                using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.OpenExistingOnly);

                    // Check nr of certificates in store.
                    Assert.Equal(2, store.Certificates.Count);
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // Windows/OSX doesn't use SSL_CERT_{DIR,FILE}.
        private void X509Store_MachineStoreLoadsMutipleSslCertDirectories()
        {
            // Create 3 certificates and place them in two directories that will be passed
            // using SSL_CERT_DIR.
            string sslCertDir1 = GetTestFilePath();
            Directory.CreateDirectory(sslCertDir1);
            File.WriteAllBytes(Path.Combine(sslCertDir1, "1.pem"), TestData.SelfSigned1PemBytes);
            File.WriteAllBytes(Path.Combine(sslCertDir1, "2.pem"), TestData.SelfSigned2PemBytes);
            string sslCertDir2 = GetTestFilePath();
            Directory.CreateDirectory(sslCertDir2);
            File.WriteAllBytes(Path.Combine(sslCertDir2, "3.pem"), TestData.SelfSigned3PemBytes);

            // Add a non-existing directory after each valid directory to verify they are ignored.
            string sslCertDir = string.Join(Path.PathSeparator,
                new[] {
                        sslCertDir1,
                        sslCertDir2,
                        "",          // empty string
                        sslCertDir2, // duplicate directory
                        "/invalid2", // path that does not exist
            });

            var psi = new ProcessStartInfo();
            psi.Environment.Add("SSL_CERT_DIR", sslCertDir);
            // Set SSL_CERT_FILE to avoid loading the default bundle file.
            psi.Environment.Add("SSL_CERT_FILE", "/nonexisting");
            RemoteExecutor.Invoke(() =>
            {
                Assert.NotNull(Environment.GetEnvironmentVariable("SSL_CERT_DIR"));
                using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.OpenExistingOnly);

                    // Check nr of certificates in store.
                    Assert.Equal(3, store.Certificates.Count);
                }
            }, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }

        public static bool NotRunningAsRootAndRemoteExecutorSupported => !Environment.IsPrivilegedProcess && RemoteExecutor.IsSupported;
    }
}
