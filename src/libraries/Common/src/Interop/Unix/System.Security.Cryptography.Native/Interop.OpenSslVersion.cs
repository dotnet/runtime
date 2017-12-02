// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class OpenSsl
    {
        private static Version s_opensslVersion;

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SSLEayVersion")]
        private static extern string OpenSslVersionDescription();

        internal static Version OpenSslVersion
        {
            get
            {
                if (s_opensslVersion == null)
                {
                    const string OpenSSL = "OpenSSL ";

                    // Skip OpenSSL part, and get the version string of format x.y.z
                    if (!Version.TryParse(OpenSslVersionDescription().AsReadOnlySpan().Slice(OpenSSL.Length, 5), out s_opensslVersion))
                    {
                        s_opensslVersion = new Version(0, 0, 0);
                    }
                }

                return s_opensslVersion;
            }
        }
    }
}
