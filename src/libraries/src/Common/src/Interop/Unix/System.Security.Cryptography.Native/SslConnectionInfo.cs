// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

namespace System.Net
{
    internal class SslConnectionInfo
    {
        public readonly int Protocol;
        public readonly int DataCipherAlg;
        public readonly int DataKeySize;
        public readonly int DataHashAlg;
        public readonly int DataHashKeySize;
        public readonly int KeyExchangeAlg;
        public readonly int KeyExchKeySize = 0;

        internal SslConnectionInfo(SafeSslHandle sslContext)
        {
            string protocolVersion = Interop.Ssl.GetProtocolVersion(sslContext);
            Protocol = (int)MapProtocolVersion(protocolVersion);

            if (!Interop.Ssl.GetSslConnectionInfo(
                sslContext,
                out DataCipherAlg,
                out KeyExchangeAlg,
                out DataHashAlg,
                out DataKeySize,
                out DataHashKeySize))
            {
                throw Interop.OpenSsl.CreateSslException(SR.net_ssl_get_connection_info_failed);
            }

            //Openssl does not provide a way to return a exchange key size.
            //It internally does calculate the key size before generating key to exchange
            //It is not a constant (Algorthim specific) either that we can hardcode and return. 
        }

        private SslProtocols MapProtocolVersion(string protocolVersion)
        {
            switch (protocolVersion)
            {
                case "SSLv2":
                    return SslProtocols.Ssl2;
                case "SSLv3":
                    return SslProtocols.Ssl3;
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
