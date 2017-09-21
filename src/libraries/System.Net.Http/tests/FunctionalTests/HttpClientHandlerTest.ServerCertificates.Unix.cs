// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
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
    }
}
