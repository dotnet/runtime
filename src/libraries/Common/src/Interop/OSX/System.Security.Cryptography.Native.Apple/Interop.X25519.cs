// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519DeriveRawSecretAgreement")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int AppleCryptoNative_X25519DeriveRawSecretAgreement(
            SafeX25519KeyHandle key,
            SafeX25519KeyHandle peerKey,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519ExportPrivateKey")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int AppleCryptoNative_X25519ExportPrivateKey(
            SafeX25519KeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519ExportPublicKey")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int AppleCryptoNative_X25519ExportPublicKey(
            SafeX25519KeyHandle key,
            Span<byte> destination,
            int destinationLength);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519ImportPrivateKey")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial SafeX25519KeyHandle AppleCryptoNative_X25519ImportPrivateKey(
            ReadOnlySpan<byte> source,
            int sourceLength);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519ImportPublicKey")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial SafeX25519KeyHandle AppleCryptoNative_X25519ImportPublicKey(
            ReadOnlySpan<byte> source,
            int sourceLength);

        internal static void X25519DeriveRawSecretAgreement(SafeX25519KeyHandle key, SafeX25519KeyHandle peerKey, Span<byte> destination)
        {
            const int Success = 1;
            const int KeyDerivationFailed = 0;
            int ret = AppleCryptoNative_X25519DeriveRawSecretAgreement(
                key,
                peerKey,
                destination,
                destination.Length);

            switch (ret)
            {
                case Success:
                    return;
                case KeyDerivationFailed:
                    throw new CryptographicException();
                default:
                    Debug.Fail($"Unexpected result from {nameof(AppleCryptoNative_X25519DeriveRawSecretAgreement)}: {ret}.");
                    throw new CryptographicException();
            }
        }

        internal static void X25519ExportPrivateKey(SafeX25519KeyHandle key, Span<byte> destination)
        {
            const int Success = 1;
            int ret = AppleCryptoNative_X25519ExportPrivateKey(key, destination, destination.Length);

            if (ret != Success)
            {
                Debug.Fail($"Unexpected result from {nameof(AppleCryptoNative_X25519ExportPrivateKey)}: {ret}.");
                throw new CryptographicException();
            }
        }

        internal static void X25519ExportPublicKey(SafeX25519KeyHandle key, Span<byte> destination)
        {
            const int Success = 1;
            int ret = AppleCryptoNative_X25519ExportPublicKey(key, destination, destination.Length);

            if (ret != Success)
            {
                Debug.Fail($"Unexpected result from {nameof(AppleCryptoNative_X25519ExportPublicKey)}: {ret}.");
                throw new CryptographicException();
            }
        }

        internal static SafeX25519KeyHandle X25519ImportPrivateKey(ReadOnlySpan<byte> source)
        {
            SafeX25519KeyHandle ret = AppleCryptoNative_X25519ImportPrivateKey(source, source.Length);

            if (ret.IsInvalid)
            {
                ret.Dispose();
                throw new CryptographicException();
            }

            return ret;
        }

        internal static SafeX25519KeyHandle X25519ImportPublicKey(ReadOnlySpan<byte> source)
        {
            SafeX25519KeyHandle ret = AppleCryptoNative_X25519ImportPublicKey(source, source.Length);

            if (ret.IsInvalid)
            {
                ret.Dispose();
                throw new CryptographicException();
            }

            return ret;
        }

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519FreeKey")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static partial void X25519FreeKey(IntPtr ptr);

        [LibraryImport(Libraries.AppleCryptoNative, EntryPoint = "AppleCryptoNative_X25519GenerateKey")]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        internal static partial SafeX25519KeyHandle X25519GenerateKey();
    }
}

namespace System.Security.Cryptography.Apple
{
    internal sealed class SafeX25519KeyHandle : SafeHandle
    {
        public SafeX25519KeyHandle() : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.AppleCrypto.X25519FreeKey(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
