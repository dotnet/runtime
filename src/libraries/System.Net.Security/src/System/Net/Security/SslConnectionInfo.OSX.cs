// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using SafeNwHandle = Interop.SafeNwHandle;

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
        public void UpdateSslConnectionInfo(SafeDeleteContext context)
        {
            switch (context)
            {
                case SafeDeleteNwContext nwContext:
                    UpdateSslConnectionInfoNetworkFramework(nwContext);
                    break;
                case SafeDeleteSslContext sslContext:
                    UpdateSslConnectionInfoAppleCrypto(sslContext);
                    break;
                default:
                    throw new NotSupportedException("Unsupported context type.");
            }
        }

        private unsafe void UpdateSslConnectionInfoNetworkFramework(SafeDeleteNwContext context)
        {
            SafeNwHandle nwContext = context.ConnectionHandle;
            SslProtocols protocol;
            TlsCipherSuite cipherSuite;

            Span<byte> alpn = stackalloc byte[256]; // Ensure the stack is initialized for alpnPtr
            int alpnLength = alpn.Length;

            int osStatus;
            unsafe
            {
                fixed (byte* alpnPtr = alpn)
                {
                    // Call the native method to get connection info
                    osStatus = Interop.NetworkFramework.Tls.GetConnectionInfo(nwContext, context.StateHandle, out protocol, out cipherSuite, alpnPtr, ref alpnLength);
                }
            }

            if (osStatus != 0)
            {
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
            }

            if (alpnLength > 0)
            {
                ApplicationProtocol = alpn.Slice(0, alpnLength).ToArray();
            }

            Protocol = (int)protocol;
            TlsCipherSuite = cipherSuite;
            MapCipherSuite(cipherSuite);

            // Network Framework does not expose a public API to determine whether the TLS
            // session was resumed (sec_protocol_metadata only reports the negotiated protocol,
            // cipher suite and ALPN; sec_protocol_metadata_get_early_data_accepted covers TLS 1.3
            // 0-RTT only, not general resumption). Leave TlsResumed as false so that the peer
            // certificate is always revalidated on this backend, matching the safe fallback.
        }

        private void UpdateSslConnectionInfoAppleCrypto(SafeDeleteSslContext context)
        {
            SafeSslHandle sslContext = context.SslContext;
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

            // SecureTransport does not expose an API to determine whether the session was
            // resumed that is still present in current Apple SDKs (SSLGetResumableSessionInfo has
            // been removed), so TlsResumed is left as false here. As with the Network Framework
            // and Android backends this means the peer certificate is always revalidated on
            // resumption on this platform, matching the safe fallback.
            if (context.IsServer)
            {
                if (context.SelectedApplicationProtocol.Protocol.Length > 0)
                {
                    if (context.SelectedApplicationProtocol.Equals(SslApplicationProtocol.Http11.Protocol))
                    {
                        ApplicationProtocol = s_http1;
                    }
                    else if (context.SelectedApplicationProtocol.Equals(SslApplicationProtocol.Http2.Protocol))
                    {
                        ApplicationProtocol = s_http2;
                    }
                    else if (context.SelectedApplicationProtocol.Equals(SslApplicationProtocol.Http3.Protocol))
                    {
                        ApplicationProtocol = s_http3;
                    }
                    else
                    {
                        ApplicationProtocol = context.SelectedApplicationProtocol.Protocol.ToArray();
                    }
                }
            }
            else
            {
                ApplicationProtocol = Interop.AppleCrypto.SslGetAlpnSelected(sslContext);
            }

            MapCipherSuite(cipherSuite);
        }
    }
}
