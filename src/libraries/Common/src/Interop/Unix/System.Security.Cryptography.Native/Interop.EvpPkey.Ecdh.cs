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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyDeriveSecretAgreement")]
        private static partial int EvpPKeyDeriveSecretAgreement(
            SafeEvpPKeyHandle pkey,
            IntPtr extraHandle,
            SafeEvpPKeyHandle peerKey,
            ref byte secret,
            uint secretLength);

        internal static int EvpPKeyDeriveSecretAgreement(SafeEvpPKeyHandle pkey, SafeEvpPKeyHandle peerKey, Span<byte> destination)
        {
            Debug.Assert(pkey != null);
            Debug.Assert(!pkey.IsInvalid);
            Debug.Assert(peerKey != null);
            Debug.Assert(!peerKey.IsInvalid);

            int written = EvpPKeyDeriveSecretAgreement(
                pkey,
                pkey.ExtraHandle,
                peerKey,
                ref MemoryMarshal.GetReference(destination),
                (uint)destination.Length);

            if (written <= 0)
            {
                Debug.Assert(written == 0);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }
    }
}
