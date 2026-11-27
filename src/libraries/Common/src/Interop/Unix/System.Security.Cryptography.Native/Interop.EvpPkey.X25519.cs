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
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519Available")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X25519Available();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519ExportPrivateKey")]
        private static partial int X25519ExportPrivateKey(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519ExportPublicKey")]
        private static partial int X25519ExportPublicKey(
            SafeEvpPKeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519GenerateKey")]
        private static partial SafeEvpPKeyHandle CryptoNative_X25519GenerateKey();

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519DeriveSecretAgreementWithBytes")]
        private static partial int CryptoNative_X25519DeriveSecretAgreementWithBytes(
            SafeEvpPKeyHandle key,
            IntPtr extraHandle,
            ReadOnlySpan<byte> peerKey,
            int peerKeyLength,
            Span<byte> secret,
            uint secretLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519IsValidHandle")]
        private static partial int CryptoNative_X25519IsValidHandle(
            SafeEvpPKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] out bool hasPrivateKey);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519ImportPrivateKey")]
        private static partial SafeEvpPKeyHandle X25519ImportPrivateKey(ReadOnlySpan<byte> source, int sourceLength);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X25519ImportPublicKey")]
        private static partial SafeEvpPKeyHandle X25519ImportPublicKey(ReadOnlySpan<byte> source, int sourceLength);

        internal static void X25519ExportPrivateKey(SafeEvpPKeyHandle key, Span<byte> destination)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = X25519ExportPrivateKey(key, destination, destination.Length);

            switch (ret)
            {
                case Success:
                    return;
                case Fail:
                    throw CreateOpenSslCryptographicException();
                default:
                    Debug.Fail($"{nameof(X25519ExportPrivateKey)} returned '{ret}' unexpectedly.");
                    throw new CryptographicException();
            }
        }

        internal static void X25519ExportPublicKey(SafeEvpPKeyHandle key, Span<byte> destination)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = X25519ExportPublicKey(key, destination, destination.Length);

            switch (ret)
            {
                case Success:
                    return;
                case Fail:
                    throw CreateOpenSslCryptographicException();
                default:
                    Debug.Fail($"{nameof(X25519ExportPublicKey)} returned '{ret}' unexpectedly.");
                    throw new CryptographicException();
            }
        }

        internal static SafeEvpPKeyHandle X25519GenerateKey()
        {
            SafeEvpPKeyHandle key = CryptoNative_X25519GenerateKey();

            if (key.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                key.Dispose();
                throw ex;
            }

            return key;
        }

        internal static bool X25519IsValidHandle(SafeEvpPKeyHandle key, out bool hasPrivateKey)
        {
            const int Success = 1;
            const int Fail = 0;

            int ret = CryptoNative_X25519IsValidHandle(key, out hasPrivateKey);

            switch (ret)
            {
                case Success:
                    return true;
                case Fail:
                    hasPrivateKey = false;
                    return false;
                default:
                    Debug.Fail($"{nameof(CryptoNative_X25519IsValidHandle)} returned '{ret}' unexpectedly.");
                    throw CreateOpenSslCryptographicException();
            }
        }

        internal static SafeEvpPKeyHandle X25519ImportPrivateKey(ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = X25519ImportPrivateKey(source, source.Length);

            if (key.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                key.Dispose();
                throw ex;
            }

            return key;
        }

        internal static int X25519DeriveSecretAgreementWithBytes(
            SafeEvpPKeyHandle key,
            ReadOnlySpan<byte> peerKey,
            Span<byte> destination)
        {
            Debug.Assert(key != null);
            Debug.Assert(peerKey.Length == X25519DiffieHellman.PublicKeySizeInBytes);
            Debug.Assert(destination.Length == X25519DiffieHellman.SecretAgreementSizeInBytes);

            int written = CryptoNative_X25519DeriveSecretAgreementWithBytes(
                key,
                GetExtraHandle(key),
                peerKey,
                peerKey.Length,
                destination,
                (uint)destination.Length);

            if (written <= 0)
            {
                Debug.Assert(written == 0);
                throw CreateOpenSslCryptographicException();
            }

            return written;
        }

        internal static SafeEvpPKeyHandle X25519ImportPublicKey(ReadOnlySpan<byte> source)
        {
            SafeEvpPKeyHandle key = X25519ImportPublicKey(source, source.Length);

            if (key.IsInvalid)
            {
                Exception ex = CreateOpenSslCryptographicException();
                key.Dispose();
                throw ex;
            }

            return key;
        }
    }
}
