// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Ssl
    {
        internal delegate int AppVerifyCallback(IntPtr storeCtx, IntPtr arg);
        internal delegate int ClientCertCallback(IntPtr ssl, out IntPtr x509, out IntPtr pkey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxCreate")]
        internal static extern SafeSslContextHandle SslCtxCreate(IntPtr method);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxDestroy")]
        internal static extern void SslCtxDestroy(IntPtr ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetCertVerifyCallback")]
        internal static extern void SslCtxSetCertVerifyCallback(IntPtr ctx, AppVerifyCallback cb, IntPtr arg);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_SslCtxSetClientCertCallback")]
        internal static extern void SslCtxSetClientCertCallback(IntPtr ctx, ClientCertCallback callback);
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
