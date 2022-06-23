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
            string protocolString = Interop.AndroidCrypto.SSLStreamGetProtocol(sslContext);
            SslProtocols protocol = protocolString switch
            {
#pragma warning disable 0618 // 'SslProtocols.Ssl3' is obsolete
                "SSLv3" => SslProtocols.Ssl3,
#pragma warning restore
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                "TLSv1" => SslProtocols.Tls,
                "TLSv1.1" => SslProtocols.Tls11,
#pragma warning restore SYSLIB0039
                "TLSv1.2" => SslProtocols.Tls12,
                "TLSv1.3" => SslProtocols.Tls13,
                _ => SslProtocols.None,
            };
            Protocol = (int)protocol;
            ApplicationProtocol = Interop.AndroidCrypto.SSLStreamGetApplicationProtocol(sslContext);

            // Enum value names should match the cipher suite name, so we just parse the
            string cipherSuite = Interop.AndroidCrypto.SSLStreamGetCipherSuite(sslContext);
            MapCipherSuite(Enum.Parse<TlsCipherSuite>(cipherSuite));
        }
    }
}
