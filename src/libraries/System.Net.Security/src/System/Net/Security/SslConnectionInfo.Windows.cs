// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

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

            ApplicationProtocol = GetNegotiatedApplicationProtocol(securityContext);
        }
    }
}
