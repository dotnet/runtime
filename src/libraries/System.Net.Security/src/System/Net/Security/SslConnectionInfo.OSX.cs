// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
        public void UpdateSslConnectionInfo(SafeSslHandle sslContext)
        {
            SslProtocols protocol;
            TlsCipherSuite cipherSuite;

            int osStatus = Interop.AppleCrypto.SslGetProtocolVersion(sslContext, out protocol);

            if (osStatus != 0)
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

            osStatus = Interop.AppleCrypto.SslGetCipherSuite(sslContext, out cipherSuite);

            if (osStatus != 0)
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

            Protocol = (int)protocol;
            TlsCipherSuite = cipherSuite;
            ApplicationProtocol = Interop.AppleCrypto.SslGetAlpnSelected(sslContext);

            MapCipherSuite(cipherSuite);
        }
    }
}
