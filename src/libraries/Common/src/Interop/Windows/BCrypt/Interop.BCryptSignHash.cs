// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptSignHash(
            SafeBCryptKeyHandle hKey,
            void* pPaddingInfo,
            byte* pbInput,
            int cbInput,
            byte* pbOutput,
            int cbOutput,
            out int pcbResult,
            BCryptSignVerifyFlags dwFlags);

        internal static unsafe NTSTATUS BCryptSignHashPkcs1(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            string hashAlgorithmName,
            out int bytesWritten)
        {
            fixed (char* pHashAlgorithmName = hashAlgorithmName)
            fixed (byte* pHash = &MemoryMarshal.GetReference(hash))
            fixed (byte* pDest = &Helpers.GetNonNullPinnableReference(destination))
            {
                BCRYPT_PKCS1_PADDING_INFO paddingInfo = default;
                paddingInfo.pszAlgId = (IntPtr)pHashAlgorithmName;

                return BCryptSignHash(
                    key,
                    &paddingInfo,
                    pHash,
                    hash.Length,
                    pDest,
                    destination.Length,
                    out bytesWritten,
                    BCryptSignVerifyFlags.BCRYPT_PAD_PKCS1);
            }
        }

        internal static unsafe NTSTATUS BCryptSignHashPss(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> hash,
            Span<byte> destination,
            string hashAlgorithmName,
            out int bytesWritten)
        {
            fixed (char* pHashAlgorithmName = hashAlgorithmName)
            fixed (byte* pHash = &MemoryMarshal.GetReference(hash))
            fixed (byte* pDest = &Helpers.GetNonNullPinnableReference(destination))
            {
                BCRYPT_PSS_PADDING_INFO paddingInfo = default;
                paddingInfo.pszAlgId = (IntPtr)pHashAlgorithmName;
                paddingInfo.cbSalt = hash.Length;

                return BCryptSignHash(
                    key,
                    &paddingInfo,
                    pHash,
                    hash.Length,
                    pDest,
                    destination.Length,
                    out bytesWritten,
                    BCryptSignVerifyFlags.BCRYPT_PAD_PSS);
            }
        }

        internal static unsafe void BCryptSignHashPqcPure(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            NTSTATUS status;
            int bytesWritten;

            fixed (byte* pData = &MemoryMarshal.GetReference(data))
            fixed (byte* pDest = &Helpers.GetNonNullPinnableReference(destination))
            fixed (byte* pContext = &MemoryMarshal.GetReference(context))
            {
                BCRYPT_PQDSA_PADDING_INFO paddingInfo = default;
                paddingInfo.pbCtx = (IntPtr)pContext;
                paddingInfo.cbCtx = context.Length;

                status = BCryptSignHash(
                    key,
                    &paddingInfo,
                    pData,
                    data.Length,
                    pDest,
                    destination.Length,
                    out bytesWritten,
                    BCryptSignVerifyFlags.BCRYPT_PAD_PQDSA);
            }

            Debug.Assert(bytesWritten == destination.Length);

            if (status != Interop.BCrypt.NTSTATUS.STATUS_SUCCESS)
            {
                throw Interop.BCrypt.CreateCryptographicException(status);
            }
        }
    }
}
