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
            string protocolVersion = Interop.Ssl.GetProtocolVersion(sslContext);
            Protocol = (int)MapProtocolVersion(protocolVersion);

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

        private SslProtocols MapProtocolVersion(string protocolVersion)
        {
            switch (protocolVersion)
            {
#pragma warning disable 0618 // Ssl2, Ssl3 are deprecated.                
                case "SSLv2":
                    return SslProtocols.Ssl2;
                case "SSLv3":
                    return SslProtocols.Ssl3;
#pragma warning restore                    
                case "TLSv1":
                    return SslProtocols.Tls;
                case "TLSv1.1":
                    return SslProtocols.Tls11;
                case "TLSv1.2":
                    return SslProtocols.Tls12;
                default:
                    return SslProtocols.None;
            }
        }
    }
}
