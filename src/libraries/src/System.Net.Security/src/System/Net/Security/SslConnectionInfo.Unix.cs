// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Security.Authentication;

namespace System.Net.Security
{
    internal partial class SslConnectionInfo 
    {
        public SslConnectionInfo(SafeSslHandle sslContext)
        {
            Protocol = (int)MapProtocolVersion(Interop.Ssl.SslGetVersion(sslContext));

            int dataCipherAlg;
            int keyExchangeAlg;
            int dataHashAlg;
            int dataKeySize;
            int dataHashKeySize;

            if (!Interop.Ssl.GetSslConnectionInfo(
                sslContext,
                out dataCipherAlg,
                out keyExchangeAlg,
                out dataHashAlg,
                out dataKeySize,
                out dataHashKeySize))
            {
                throw Interop.OpenSsl.CreateSslException(SR.net_ssl_get_connection_info_failed);
            }

            DataCipherAlg = dataCipherAlg;
            KeyExchangeAlg = keyExchangeAlg;
            DataHashAlg = dataHashAlg;
            DataKeySize = dataKeySize;
            DataHashKeySize = dataHashKeySize;

            //Openssl does not provide a way to return an exchange key size.
            //It internally does calculate the key size before generating key to exchange
            //It is not a constant (Algorthim specific) either that we can hardcode and return. 
            KeyExchKeySize = 0;
        }

        private unsafe SslProtocols MapProtocolVersion(IntPtr protocolVersion)
        {
            // protocolVersion points to a static ASCII string that's one of:
            //     TLSv1
            //     TLSv1.1
            //     TLSv1.2
            //     TLSv1.3
            //     SSLv2
            //     SSLv3
            //     unknown
            // Regardless, it's null terminated.

            byte* b = (byte*)protocolVersion;
            if (b[0] == 'T')
            {
                if (b[1] == 'L' &&
                    b[2] == 'S' &&
                    b[3] == 'v' &&
                    b[4] == '1')
                {
                    if (b[5] == '\0')
                    {
                        return SslProtocols.Tls;
                    }
                    else if (b[5] == '.' && b[6] != '\0' && b[7] == '\0')
                    {
                        switch (b[6])
                        {
                            case (byte)'1': return SslProtocols.Tls11;
                            case (byte)'2': return SslProtocols.Tls12;
                            case (byte)'3': return SslProtocols.Tls13;
                        }
                    }
                }
            }
            else if (b[0] == 'S')
            {
                if (b[1] == 'S' &&
                    b[2] == 'L' &&
                    b[3] == 'v')
                {
#pragma warning disable 0618 // Ssl2, Ssl3 are deprecated.                
                    if (b[4] == '2' && b[5] == '\0')
                    {
                        return SslProtocols.Ssl2;
                    }
                    else if (b[4] == '3' && b[5] == '\0')
                    {
                        return SslProtocols.Ssl3;
                    }
#pragma warning restore                    
                }
            }

            return SslProtocols.None;
        }
    }
}
