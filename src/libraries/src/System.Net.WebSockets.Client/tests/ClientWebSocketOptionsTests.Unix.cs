// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Security;
using System.Net.Test.Common;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.WebSockets.Client.Tests
{
    public partial class ClientWebSocketOptionsTests
    {
        internal static bool BackendSupportsCustomCertificateHandling
        {
            get
            {
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
