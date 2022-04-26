// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxCreate")]
        internal static partial SafeEvpPKeyCtxHandle EvpPKeyCtxCreate(SafeEvpPKeyHandle pkey, SafeEvpPKeyHandle peerkey, out uint secretLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyDeriveSecretAgreement")]
        private static partial int EvpPKeyDeriveSecretAgreement(
            ref byte secret,
            uint secretLength,
            SafeEvpPKeyCtxHandle ctx);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyCtxDestroy")]
        internal static partial void EvpPKeyCtxDestroy(IntPtr ctx);

        internal static void EvpPKeyDeriveSecretAgreement(SafeEvpPKeyCtxHandle ctx, Span<byte> destination)
        {
            Debug.Assert(ctx != null);
            Debug.Assert(!ctx.IsInvalid);

            int ret = EvpPKeyDeriveSecretAgreement(
                ref MemoryMarshal.GetReference(destination),
                (uint)destination.Length,
                ctx);

            if (ret != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}
