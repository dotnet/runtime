// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        internal delegate int AppVerifyCallback(IntPtr storeCtx, IntPtr arg);
        internal delegate int ClientCertCallback(IntPtr ssl, out IntPtr x509, out IntPtr pkey);
        internal unsafe delegate int SslCtxSetAlpnCallback(IntPtr ssl, out byte* outp, out byte outlen, byte* inp, uint inlen, IntPtr arg);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCreate")]
        internal static extern SafeSslContextHandle SslCtxCreate(IntPtr method);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxDestroy")]
        internal static extern void SslCtxDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCertVerifyCallback")]
        internal static extern void SslCtxSetCertVerifyCallback(IntPtr ctx, AppVerifyCallback cb, IntPtr arg);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetClientCertCallback")]
        internal static extern void SslCtxSetClientCertCallback(IntPtr ctx, ClientCertCallback callback);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetAlpnProtos")]
        internal static extern int SslCtxSetAlpnProtos(SafeSslContextHandle ctx, IntPtr protos, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetAlpnSelectCb")]
        internal static unsafe extern void SslCtxSetAlpnSelectCb(SafeSslContextHandle ctx, SslCtxSetAlpnCallback callback, IntPtr arg);

        internal static unsafe int SslCtxSetAlpnProtos(SafeSslContextHandle ctx, List<SslApplicationProtocol> protocols)
        {
            byte[] buffer = ConvertAlpnProtocolListToByteArray(protocols);
            fixed (byte* b = buffer)
            {
                return SslCtxSetAlpnProtos(ctx, (IntPtr)b, buffer.Length);
            }
        }

        internal static byte[] ConvertAlpnProtocolListToByteArray(List<SslApplicationProtocol> applicationProtocols)
        {
            int protocolSize = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                if (protocol.Protocol.Length == 0 || protocol.Protocol.Length > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(applicationProtocols));
                }

                protocolSize += protocol.Protocol.Length + 1;
            }

            byte[] buffer = new byte[protocolSize];
            var offset = 0;
            foreach (SslApplicationProtocol protocol in applicationProtocols)
            {
                buffer[offset++] = (byte)(protocol.Protocol.Length);
                protocol.Protocol.Span.CopyTo(new Span<byte>(buffer).Slice(offset));
                offset += protocol.Protocol.Length;
            }

            return buffer;
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeSslContextHandle : SafeHandle
    {
        private SafeSslContextHandle()
            : base(IntPtr.Zero, true)
        {
        }

        internal SafeSslContextHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            Interop.Ssl.SslCtxDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
