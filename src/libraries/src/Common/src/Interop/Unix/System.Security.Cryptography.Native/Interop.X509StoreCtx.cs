// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxCreate")]
        internal static extern SafeX509StoreCtxHandle X509StoreCtxCreate();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxDestroy")]
        internal static extern void X509StoreCtxDestroy(IntPtr v);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetChain")]
        internal static extern SafeX509StackHandle X509StoreCtxGetChain(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetSharedUntrusted")]
        private static extern SafeSharedX509StackHandle X509StoreCtxGetSharedUntrusted_private(SafeX509StoreCtxHandle ctx);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetTargetCert")]
        internal static extern IntPtr X509StoreCtxGetTargetCert(SafeX509StoreCtxHandle ctx);

        internal static SafeSharedX509StackHandle X509StoreCtxGetSharedUntrusted(SafeX509StoreCtxHandle ctx)
        {
            return SafeInteriorHandle.OpenInteriorHandle(
                x => X509StoreCtxGetSharedUntrusted_private(x),
                ctx);
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeX509StoreCtxHandle : SafeHandle
    {
        private SafeX509StoreCtxHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        internal SafeX509StoreCtxHandle(IntPtr handle, bool ownsHandle) :
            base(handle, ownsHandle)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.X509StoreCtxDestroy(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}
