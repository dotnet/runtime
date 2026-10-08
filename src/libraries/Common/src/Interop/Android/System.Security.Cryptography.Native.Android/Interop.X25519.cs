// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519IsSupported")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool X25519IsSupported();

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519DestroyKey")]
        internal static partial void X25519DestroyKey(IntPtr key);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519GenerateKey")]
        private static partial int X25519GenerateKeyNative(
            out SafeX25519PublicKeyHandle publicKey,
            out SafeX25519PrivateKeyHandle privateKey);

        internal static void X25519GenerateKey(
            out SafeX25519PublicKeyHandle publicKey,
            out SafeX25519PrivateKeyHandle privateKey)
        {
            const int Success = 1;

            int result = X25519GenerateKeyNative(out publicKey, out privateKey);

            if (result != Success)
            {
                publicKey.Dispose();
                privateKey.Dispose();
                throw new CryptographicException();
            }
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519ExportSubjectPublicKeyInfo")]
        private static partial int X25519ExportSubjectPublicKeyInfoNative(
            SafeX25519PublicKeyHandle publicKey,
            Span<byte> buffer,
            int bufferLength,
            out int bytesWritten);

        internal static bool X25519TryExportSubjectPublicKeyInfo(
            SafeX25519PublicKeyHandle publicKey,
            Span<byte> buffer,
            out int bytesWritten)
        {
            const int Success = 1;
            const int InsufficientBuffer = -1;

            int result = X25519ExportSubjectPublicKeyInfoNative(
                publicKey,
                buffer,
                buffer.Length,
                out bytesWritten);

            return result switch
            {
                Success => true,
                InsufficientBuffer => false,
                _ => throw new CryptographicException(),
            };
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519ExportPkcs8PrivateKey")]
        private static partial int X25519ExportPkcs8PrivateKeyNative(
            SafeX25519PrivateKeyHandle privateKey,
            Span<byte> buffer,
            int bufferLength,
            out int bytesWritten);

        internal static bool X25519TryExportPkcs8PrivateKey(
            SafeX25519PrivateKeyHandle privateKey,
            Span<byte> buffer,
            out int bytesWritten)
        {
            const int Success = 1;
            const int InsufficientBuffer = -1;

            int result = X25519ExportPkcs8PrivateKeyNative(
                privateKey,
                buffer,
                buffer.Length,
                out bytesWritten);

            return result switch
            {
                Success => true,
                InsufficientBuffer => false,
                _ => throw new CryptographicException(),
            };
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519ImportSubjectPublicKeyInfo")]
        private static partial SafeX25519PublicKeyHandle X25519ImportSubjectPublicKeyInfoNative(
            ReadOnlySpan<byte> buffer,
            int bufferLength);

        internal static SafeX25519PublicKeyHandle X25519ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> spki)
        {
            SafeX25519PublicKeyHandle handle = X25519ImportSubjectPublicKeyInfoNative(spki, spki.Length);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            return handle;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519ImportPkcs8PrivateKey")]
        private static partial SafeX25519PrivateKeyHandle X25519ImportPkcs8PrivateKeyNative(
            ReadOnlySpan<byte> buffer,
            int bufferLength);

        internal static SafeX25519PrivateKeyHandle X25519ImportPkcs8PrivateKey(ReadOnlySpan<byte> pkcs8)
        {
            SafeX25519PrivateKeyHandle handle = X25519ImportPkcs8PrivateKeyNative(pkcs8, pkcs8.Length);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            return handle;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519DeriveSecret")]
        private static partial int X25519DeriveSecretNative(
            SafeX25519PrivateKeyHandle privateKey,
            SafeX25519PublicKeyHandle publicKey,
            Span<byte> destination,
            int destinationLength);

        internal static void X25519DeriveSecret(
            SafeX25519PrivateKeyHandle privateKey,
            SafeX25519PublicKeyHandle publicKey,
            Span<byte> destination)
        {
            const int Success = 1;

            int result = X25519DeriveSecretNative(privateKey, publicKey, destination, destination.Length);

            if (result != Success)
            {
                throw new CryptographicException();
            }
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_X25519DeriveSecretWithSubjectPublicKeyInfo")]
        private static partial int X25519DeriveSecretWithSubjectPublicKeyInfoNative(
            SafeX25519PrivateKeyHandle privateKey,
            ReadOnlySpan<byte> subjectPublicKeyInfo,
            int subjectPublicKeyInfoLength,
            Span<byte> destination,
            int destinationLength);

        internal static void X25519DeriveSecretWithSubjectPublicKeyInfo(
            SafeX25519PrivateKeyHandle privateKey,
            ReadOnlySpan<byte> subjectPublicKeyInfo,
            Span<byte> destination)
        {
            const int Success = 1;

            int result = X25519DeriveSecretWithSubjectPublicKeyInfoNative(
                privateKey,
                subjectPublicKeyInfo,
                subjectPublicKeyInfo.Length,
                destination,
                destination.Length);

            if (result != Success)
            {
                throw new CryptographicException();
            }
        }
    }
}

namespace System.Security.Cryptography
{
    internal sealed class SafeX25519PublicKeyHandle : SafeHandle
    {
        public SafeX25519PublicKeyHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.X25519DestroyKey(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }

    internal sealed class SafeX25519PrivateKeyHandle : SafeHandle
    {
        public SafeX25519PrivateKeyHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Interop.AndroidCrypto.X25519DestroyKey(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }
    }
}
