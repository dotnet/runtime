// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Authentication;

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
        public unsafe void UpdateSslConnectionInfo(SafeDeleteSslContext context)
        {
            SafeSslHandle sslContext = context.SslContext;
            SslProtocols protocol;
            TlsCipherSuite cipherSuite;
            int osStatus;
            IntPtr alpnPtr = IntPtr.Zero;
            int alpnLength = 0;

            if (context.UseNwFramework)
            {
                osStatus = Interop.AppleCrypto.NwGetConnectionInfo(sslContext, out protocol, out cipherSuite, ref alpnPtr, ref alpnLength );

                if (osStatus != 0)
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

                if (alpnPtr != IntPtr.Zero && alpnLength > 0)
                {
                    Span<byte> alpn = new Span<byte>((void*)alpnPtr, alpnLength);
                    ApplicationProtocol = alpn.ToArray();
                }
            }
            else
            {
                osStatus = Interop.AppleCrypto.SslGetProtocolVersion(sslContext, out protocol);

                if (osStatus != 0)
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

                osStatus = Interop.AppleCrypto.SslGetCipherSuite(sslContext, out cipherSuite);

                if (osStatus != 0)
                    throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);
            }


            Protocol = (int)protocol;
            TlsCipherSuite = cipherSuite;
            MapCipherSuite(cipherSuite);

            if (context.UseNwFramework)
            {

                return;
            }

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
        }
    }
}
