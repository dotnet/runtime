// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Threading;

namespace System.Security.Cryptography
{
    public sealed partial class SafeEvpPKeyHandle
    {
        private static long _openSslVersion;

        /// <summary>
        /// The runtime version number for the loaded version of OpenSSL.
        /// </summary>
        /// <remarks>
        /// For OpenSSL 1.1+ this is the result of <code>OpenSSL_version_num()</code>,
        /// for OpenSSL 1.0.x this is the result of <code>SSLeay()</code>.
        /// </remarks>
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("windows")]
        public static long OpenSslVersion
        {
            get
            {
                long version = Volatile.Read(ref _openSslVersion);

                if (version == 0)
                {
                    if (!Interop.OpenSslNoInit.OpenSslIsAvailable)
                    {
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSL);
                    }

                    version = Interop.OpenSsl.OpenSslVersionNumber();
                    Volatile.Write(ref _openSslVersion, version);
                }

                return version;
            }
        }
    }
}
