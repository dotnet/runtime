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

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_OpenSslVersionNumber")]
        private static extern uint OpenSslVersionNumber();

        internal static Version OpenSslVersion
        {
            get
            {
                if (s_opensslVersion == null)
                {
                    uint versionNumber = OpenSslVersionNumber();
                    s_opensslVersion = new Version((int)(versionNumber >> 28), (int)((versionNumber >> 20) & 0xff), (int)((versionNumber >> 12) & 0xff));
                }

                return s_opensslVersion;
            }
        }
    }
}
