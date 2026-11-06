// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Swift.Runtime;

#pragma warning disable CS3016 // Arrays as attribute arguments are not CLS Compliant

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        internal static unsafe void HkdfExpand(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> prk,
            ReadOnlySpan<byte> info,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);
            Debug.Assert(!prk.IsEmpty);

            PAL_HashAlgorithm algorithm = PalAlgorithmFromHkdfAlgorithmName(hashAlgorithm);

            int ret = AppleCryptoNative_HKDFExpand(
                algorithm,
                prk,
                prk.Length,
                info,
                info.Length,
                destination,
                destination.Length);

            if (ret < 0)
            {
                throw new CryptographicException();
            }

            Debug.Assert(ret == destination.Length);
        }

        internal static unsafe void HKDFExtract(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);

            PAL_HashAlgorithm algorithm = PalAlgorithmFromHkdfAlgorithmName(hashAlgorithm);

            int ret = AppleCryptoNative_HKDFExtract(
                algorithm,
                ikm,
                ikm.Length,
                salt,
                salt.Length,
                destination,
                destination.Length);

            if (ret < 0)
            {
                throw new CryptographicException();
            }

            Debug.Assert(ret == destination.Length);
        }

        internal static unsafe void HKDFDeriveKey(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);

            PAL_HashAlgorithm algorithm = PalAlgorithmFromHkdfAlgorithmName(hashAlgorithm);

            int ret = AppleCryptoNative_HKDFDeriveKey(
                algorithm,
                ikm,
                ikm.Length,
                salt,
                salt.Length,
                info,
                info.Length,
                destination,
                destination.Length);

            if (ret < 0)
            {
                throw new CryptographicException();
            }

            Debug.Assert(ret == destination.Length);
        }

        private static PAL_HashAlgorithm PalAlgorithmFromHkdfAlgorithmName(HashAlgorithmName hashAlgorithmName) =>
            hashAlgorithmName == HashAlgorithmName.MD5 ? PAL_HashAlgorithm.Md5 :
            hashAlgorithmName == HashAlgorithmName.SHA1 ? PAL_HashAlgorithm.Sha1 :
            hashAlgorithmName == HashAlgorithmName.SHA256 ? PAL_HashAlgorithm.Sha256 :
            hashAlgorithmName == HashAlgorithmName.SHA384 ? PAL_HashAlgorithm.Sha384 :
            hashAlgorithmName == HashAlgorithmName.SHA512 ? PAL_HashAlgorithm.Sha512 :
            hashAlgorithmName == HashAlgorithmName.SHA3_256 ? PAL_HashAlgorithm.Sha3_256 :
            hashAlgorithmName == HashAlgorithmName.SHA3_384 ? PAL_HashAlgorithm.Sha3_384 :
            hashAlgorithmName == HashAlgorithmName.SHA3_512 ? PAL_HashAlgorithm.Sha3_512 :
            throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmName.Name);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int AppleCryptoNative_HKDFExpand(
            PAL_HashAlgorithm hashAlgorithm,
            ReadOnlySpan<byte> prkPtr,
            int prkLength,
            ReadOnlySpan<byte> infoPtr,
            int infoLength,
            Span<byte> destinationPtr,
            int destinationLength);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int AppleCryptoNative_HKDFExtract(
            PAL_HashAlgorithm hashAlgorithm,
            ReadOnlySpan<byte> ikmPtr,
            int ikmLength,
            ReadOnlySpan<byte> saltPtr,
            int saltLength,
            Span<byte> destinationPtr,
            int destinationLength);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static partial int AppleCryptoNative_HKDFDeriveKey(
            PAL_HashAlgorithm hashAlgorithm,
            ReadOnlySpan<byte> ikmPtr,
            int ikmLength,
            ReadOnlySpan<byte> saltPtr,
            int saltLength,
            ReadOnlySpan<byte> infoPtr,
            int infoLength,
            Span<byte> destinationPtr,
            int destinationLength);
    }
}
