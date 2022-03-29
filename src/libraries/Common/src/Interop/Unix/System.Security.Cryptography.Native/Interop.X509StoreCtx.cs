// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxCreate")]
        internal static partial SafeX509StoreCtxHandle X509StoreCtxCreate();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxDestroy")]
        internal static partial void X509StoreCtxDestroy(IntPtr v);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetChain")]
        internal static partial SafeX509StackHandle X509StoreCtxGetChain(SafeX509StoreCtxHandle ctx);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetCurrentCert")]
        internal static partial SafeX509Handle X509StoreCtxGetCurrentCert(SafeX509StoreCtxHandle ctx);

        [GeneratedDllImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509StoreCtxCommitToChain(SafeX509StoreCtxHandle ctx);

        internal static void X509StoreCtxCommitToChain(SafeX509StoreCtxHandle ctx)
        {
            if (CryptoNative_X509StoreCtxCommitToChain(ctx) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509StoreCtxResetForSignatureError(
            SafeX509StoreCtxHandle ctx,
            out SafeX509StoreHandle newStore);

        internal static void X509StoreCtxResetForSignatureError(
            SafeX509StoreCtxHandle ctx,
            out SafeX509StoreHandle? newStore)
        {
            if (CryptoNative_X509StoreCtxResetForSignatureError(ctx, out newStore) != 1)
            {
                newStore.Dispose();
                newStore = null;
                throw CreateOpenSslCryptographicException();
            }

            if (newStore.IsInvalid)
            {
                newStore.Dispose();
                newStore = null;
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509StoreCtxGetSharedUntrusted")]
        private static partial SafeSharedX509StackHandle X509StoreCtxGetSharedUntrusted_private(SafeX509StoreCtxHandle ctx);

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
        public SafeX509StoreCtxHandle() :
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
