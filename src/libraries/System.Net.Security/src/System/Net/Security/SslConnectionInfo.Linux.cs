// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
        public void UpdateSslConnectionInfo(SafeSslHandle sslContext)
        {
            Protocol = (int)MapProtocolVersion(Interop.Ssl.SslGetVersion(sslContext));
            ReadOnlySpan<byte> alpn = Interop.Ssl.SslGetAlpnSelected(sslContext);
            if (alpn.SequenceEqual(s_http1))
            {
                ApplicationProtocol = s_http1;
            }
            else if (alpn.SequenceEqual(s_http2))
            {
                ApplicationProtocol = s_http2;
            }
            else if (alpn.SequenceEqual(s_http3))
            {
                ApplicationProtocol = s_http3;
            }
            else if (alpn.Length > 0)
            {
                ApplicationProtocol = alpn.ToArray();
            }
#if DEBUG
            TlsResumed = Interop.Ssl.SslSessionReused(sslContext);
#endif
            MapCipherSuite(SslGetCurrentCipherSuite(sslContext));
        }

        private static TlsCipherSuite SslGetCurrentCipherSuite(SafeSslHandle ssl)
        {
            int cipherSuite;
            if (!Interop.Ssl.SslGetCurrentCipherId(ssl, out cipherSuite))
            {
                throw Interop.OpenSsl.CreateSslException(SR.net_ssl_get_connection_info_failed);
            }

            return (TlsCipherSuite)cipherSuite;
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
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                        return SslProtocols.Tls;
                    }
                    else if (b[5] == '.' && b[6] != '\0' && b[7] == '\0')
                    {
                        switch (b[6])
                        {
                            case (byte)'1': return SslProtocols.Tls11;
#pragma warning restore SYSLIB0039
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
