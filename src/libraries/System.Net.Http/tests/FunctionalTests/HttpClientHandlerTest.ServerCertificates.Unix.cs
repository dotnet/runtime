// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public partial class HttpClientHandler_ServerCertificates_Test
    {
        private static bool ShouldSuppressRevocationException
        {
            get
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return false;
                }

                // If a run on a clean macOS ever fails we need to consider that "false"
                // for CheckCertificateRevocationList is actually "use a system default" now,
                // and may require changing how this option is exposed. Considering the variety of
                // systems this should probably be complex like
                // enum RevocationCheckingOption {
                //     // Use it if able
                //     BestPlatformSecurity = 0,
                //     // Don't use it, if that's an option.
                //     BestPlatformPerformance,
                //     // Required
                //     MustCheck,
                //     // Prohibited
                //     MustNotCheck,
                // }

                if (CurlSslVersionDescription() == "SecureTransport")
                {
                    return true;
                }
                return false;
            }
        }

        internal bool BackendSupportsCustomCertificateHandling
        {
            get
            {
                if (UseManagedHandler)
                {
                    return true;
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return false;
                }

                // For other Unix-based systems it's true if (and only if) the openssl backend
                // is used with libcurl.
                return (CurlSslVersionDescription()?.StartsWith("OpenSSL") ?? false);
            }
        }

        [DllImport("System.Net.Http.Native", EntryPoint = "HttpNative_GetSslVersionDescription")]
        private static extern string CurlSslVersionDescription();

        [Theory]
        [InlineData(false, false, false, false, false)] // system -> ok
        // may fall back:
        [InlineData(true, true, false, false, false)]   // empty dir, system bundle -> ok
        [InlineData(false, false, true, true, true)]    // empty bundle -> fail
        // invalid:
        [InlineData(true, false, false, false, true)]   // non-existing dir -> fail
        [InlineData(false, false, true, false, true)]   // non-existing bundle -> fail
        // empty:
        [InlineData(true, true, true, true, true)]     // empty dir, empty bundle file -> fail
        public void HttpClientUsesSslCertEnvironmentVariables(bool setSslCertDir, bool createSslCertDir, bool setSslCertFile, bool createSslCertFile, bool expectedFailure)
        {
            bool badConfig = false;

            var psi = new ProcessStartInfo();
            if (setSslCertDir)
            {
                string sslCertDir = GetTestFilePath();
                if (createSslCertDir)
                {
                    Directory.CreateDirectory(sslCertDir);
                }
                else
                {
                    badConfig = true;
                }
                psi.Environment.Add("SSL_CERT_DIR", sslCertDir);
            }

            if (setSslCertFile)
            {
                string sslCertFile = GetTestFilePath();
                if (createSslCertFile)
                {
                    File.WriteAllText(sslCertFile, "");
                }
                else
                {
                    badConfig = true;
                }
                psi.Environment.Add("SSL_CERT_FILE", sslCertFile);
            }

            string arg = badConfig ? "badconfig" : expectedFailure ? "failure" : "success";
            RemoteInvoke(async expected =>
            {
                const string Url = "https://www.microsoft.com";
                using (HttpClient client = new HttpClient())
                {
                    if (expected != "success")
                    {
                        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(Url));
                    }
                    else
                    {
                        await client.GetAsync(Url);
                    }
                }
                return SuccessExitCode;
            }, arg, new RemoteInvokeOptions { StartInfo = psi }).Dispose();
        }
    }
}
