// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        internal delegate int AppVerifyCallback(IntPtr storeCtx, IntPtr arg);
        internal delegate int ClientCertCallback(IntPtr ssl, out IntPtr x509, out IntPtr pkey);
        internal delegate int SslCtxSetAplnCallback(IntPtr ssl, out IntPtr outp, out byte outlen, IntPtr inp, uint inlen, IntPtr arg);

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

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetAplnSelectCb")]
        internal static unsafe extern void SslCtxSetAplnSelectCb(SafeSslContextHandle ctx, SslCtxSetAplnCallback callback, IntPtr arg);

        internal static unsafe int SslCtxSetAplnProtos(SafeSslContextHandle ctx, IList<string> protocols)
        {
            byte[] buffer = AlpnStringListToByteArray(protocols);
            fixed (byte* b = buffer)
            {
                return SslCtxSetAlpnProtos(ctx, (IntPtr)b, buffer.Length);
            }
        }

        internal static byte[] AlpnStringListToByteArray(IList<string> protocols)
        {
            int protocolSize = 0;
            foreach (string protocol in protocols)
            {
                if (string.IsNullOrEmpty(protocol) || protocol.Length > byte.MaxValue)
                {
                    throw new ArgumentException(SR.net_ssl_app_protocols_invalid, nameof(protocols));
                }

                protocolSize += protocol.Length + 1;
            }

            byte[] buffer = new byte[protocolSize];
            var offset = 0;
            foreach (string protocol in protocols)
            {
                buffer[offset++] = (byte)(protocol.Length);
                offset += Encoding.ASCII.GetBytes(protocol, 0, protocol.Length, buffer, offset);
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
