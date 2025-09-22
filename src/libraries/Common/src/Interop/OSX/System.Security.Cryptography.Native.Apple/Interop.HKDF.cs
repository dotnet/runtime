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

            PAL_HashAlgorithm algorithm = PalAlgorithmFromAlgorithmName(hashAlgorithm);

            fixed (byte* pPrk = prk)
            fixed (byte* pInfo = &GetSwiftRef(info))
            fixed (byte* pDestination = destination)
            {
                AppleCryptoNative_HKDFExpand(
                    algorithm,
                    new UnsafeBufferPointer<byte>(pPrk, prk.Length),
                    new UnsafeBufferPointer<byte>(pInfo, info.Length),
                    new UnsafeMutableBufferPointer<byte>(pDestination, destination.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }
        }

        internal static unsafe void HKDFExtract(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);

            PAL_HashAlgorithm algorithm = PalAlgorithmFromAlgorithmName(hashAlgorithm);

            fixed (byte* pIkm = &GetSwiftRef(ikm))
            fixed (byte* pSalt = &GetSwiftRef(salt))
            fixed (byte* pDestination = destination)
            {
                AppleCryptoNative_HKDFExtract(
                    algorithm,
                    new UnsafeBufferPointer<byte>(pIkm, ikm.Length),
                    new UnsafeBufferPointer<byte>(pSalt, salt.Length),
                    new UnsafeMutableBufferPointer<byte>(pDestination, destination.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }
        }

        internal static unsafe void HKDFDeriveKey(
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> ikm,
            ReadOnlySpan<byte> salt,
            ReadOnlySpan<byte> info,
            Span<byte> destination)
        {
            Debug.Assert(!destination.IsEmpty);

            PAL_HashAlgorithm algorithm = PalAlgorithmFromAlgorithmName(hashAlgorithm);

            fixed (byte* pIkm = &GetSwiftRef(ikm))
            fixed (byte* pSalt = &GetSwiftRef(salt))
            fixed (byte* pInfo = &GetSwiftRef(info))
            fixed (byte* pDestination = destination)
            {
                AppleCryptoNative_HKDFDeriveKey(
                    algorithm,
                    new UnsafeBufferPointer<byte>(pIkm, ikm.Length),
                    new UnsafeBufferPointer<byte>(pSalt, salt.Length),
                    new UnsafeBufferPointer<byte>(pInfo, info.Length),
                    new UnsafeMutableBufferPointer<byte>(pDestination, destination.Length),
                    out SwiftError error);

                if (error.Value != null)
                {
                    throw new CryptographicException();
                }
            }
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_HKDFExpand(
            PAL_HashAlgorithm hashAlgorithm,
            UnsafeBufferPointer<byte> prk,
            UnsafeBufferPointer<byte> info,
            UnsafeMutableBufferPointer<byte> destination,
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_HKDFExtract(
            PAL_HashAlgorithm hashAlgorithm,
            UnsafeBufferPointer<byte> ikm,
            UnsafeBufferPointer<byte> salt,
            UnsafeMutableBufferPointer<byte> destination,
            out SwiftError error);

        [LibraryImport(Libraries.AppleCryptoNative)]
        [UnmanagedCallConv(CallConvs = [ typeof(CallConvSwift) ])]
        private static unsafe partial void AppleCryptoNative_HKDFDeriveKey(
            PAL_HashAlgorithm hashAlgorithm,
            UnsafeBufferPointer<byte> ikm,
            UnsafeBufferPointer<byte> salt,
            UnsafeBufferPointer<byte> info,
            UnsafeMutableBufferPointer<byte> destination,
            out SwiftError error);
    }
}
