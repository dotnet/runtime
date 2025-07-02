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

        private void UpdateSslConnectionInfoNetworkFramework(SafeDeleteNwContext context)
        {
            SafeNwHandle nwContext = context.ConnectionHandle;
            SslProtocols protocol;
            TlsCipherSuite cipherSuite;
            IntPtr alpnPtr = IntPtr.Zero;
            int alpnLength;

            int osStatus;
            unsafe
            {
                byte* alpnVoidPtr = (byte*)alpnPtr;
                osStatus = Interop.NetworkFramework.Tls.GetConnectionInfo(nwContext, out protocol, out cipherSuite, ref alpnVoidPtr, out int alpnIntLength);
                alpnPtr = (IntPtr)alpnVoidPtr;
                alpnLength = (int)alpnIntLength;
            }

            if (osStatus != 0)
            {
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
            }

            if (alpnPtr != IntPtr.Zero && alpnLength > 0)
            {
                unsafe
                {
                    Span<byte> alpn = new Span<byte>((void*)alpnPtr, alpnLength);
                    ApplicationProtocol = alpn.ToArray();
                }
            }

            Protocol = (int)protocol;
            TlsCipherSuite = cipherSuite;
            MapCipherSuite(cipherSuite);
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
