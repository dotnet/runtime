// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Security.Authentication;
using static Interop.SspiCli;

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
        private static byte[]? GetNegotiatedApplicationProtocol(SafeDeleteContext context)
        {
            Interop.SecPkgContext_ApplicationProtocol alpnContext = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, context, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_APPLICATION_PROTOCOL, ref alpnContext);

            // Check if the context returned is alpn data, with successful negotiation.
            if (success &&
                alpnContext.ProtoNegoExt == Interop.ApplicationProtocolNegotiationExt.ALPN &&
                alpnContext.ProtoNegoStatus == Interop.ApplicationProtocolNegotiationStatus.Success)
            {
                if (alpnContext.Protocol.SequenceEqual(s_http1))
                {
                    return s_http1;
                }
                else if (alpnContext.Protocol.SequenceEqual(s_http2))
                {
                    return s_http2;
                }
                else if (alpnContext.Protocol.SequenceEqual(s_http3))
                {
                    return s_http3;
                }

                return alpnContext.Protocol.ToArray();
            }

            return null;
        }

        public void UpdateSslConnectionInfo(SafeDeleteContext securityContext)
        {
            SecPkgContext_ConnectionInfo interopConnectionInfo = default;
            bool success = SSPIWrapper.QueryBlittableContextAttributes(
                GlobalSSPI.SSPISecureChannel,
                securityContext,
                Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CONNECTION_INFO,
                ref interopConnectionInfo);
            Debug.Assert(success);

            TlsCipherSuite cipherSuite = default;
            SecPkgContext_CipherInfo cipherInfo = default;

            success = SSPIWrapper.QueryBlittableContextAttributes(GlobalSSPI.SSPISecureChannel, securityContext, Interop.SspiCli.ContextAttribute.SECPKG_ATTR_CIPHER_INFO, ref cipherInfo);
            if (success)
            {
                cipherSuite = (TlsCipherSuite)cipherInfo.dwCipherSuite;
            }

            Protocol = interopConnectionInfo.Protocol;
            DataCipherAlg = interopConnectionInfo.DataCipherAlg;
            DataKeySize = interopConnectionInfo.DataKeySize;
            DataHashAlg = interopConnectionInfo.DataHashAlg;
            DataHashKeySize = interopConnectionInfo.DataHashKeySize;
            KeyExchangeAlg = interopConnectionInfo.KeyExchangeAlg;
            KeyExchKeySize = interopConnectionInfo.KeyExchKeySize;

            TlsCipherSuite = cipherSuite;

            // In TLS1.3, Schannel may erroneously report empty ALPN after
            // receiving resumption ticket (fake Renegotiation). Avoid updating
            // ApplicationProtocol in this case if we already have some, TLS1.3
            // does not allow ALPN changes after the initial handshake.
            //
            // TLS 1.2 and below theoretically support ALPN changes during
            // Renegotiation.
            if (ApplicationProtocol == null || (Protocol & (int)SslProtocols.Tls13) == 0)
            {
                ApplicationProtocol = GetNegotiatedApplicationProtocol(securityContext);
            }

#if DEBUG
            SecPkgContext_SessionInfo info = default;
            TlsResumed = SSPIWrapper.QueryBlittableContextAttributes(
                                    GlobalSSPI.SSPISecureChannel,
                                    securityContext,
                                    Interop.SspiCli.ContextAttribute.SECPKG_ATTR_SESSION_INFO,
                                    ref info) &&
               ((SecPkgContext_SessionInfo.Flags)info.dwFlags).HasFlag(SecPkgContext_SessionInfo.Flags.SSL_SESSION_RECONNECT);
#endif
        }
    }
}
