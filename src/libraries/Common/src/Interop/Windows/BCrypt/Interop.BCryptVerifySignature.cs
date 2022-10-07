// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [Flags]
        private enum BCryptSignVerifyFlags : uint
        {
            BCRYPT_PAD_PKCS1 = 2,
            BCRYPT_PAD_PSS = 8,
        }

        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptVerifySignature(
            SafeBCryptKeyHandle hKey,
            void* pPaddingInfo,
            byte* pbHash,
            int cbHash,
            byte* pbSignature,
            int cbSignature,
            BCryptSignVerifyFlags dwFlags);

        internal static unsafe bool BCryptVerifySignaturePkcs1(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            string hashAlgorithmName)
        {
            NTSTATUS status;

            fixed (char* pHashAlgorithmName = hashAlgorithmName)
            fixed (byte* pHash = &MemoryMarshal.GetReference(hash))
            fixed (byte* pSignature = &MemoryMarshal.GetReference(signature))
            {
                BCRYPT_PKCS1_PADDING_INFO paddingInfo = default;
                paddingInfo.pszAlgId = (IntPtr)pHashAlgorithmName;

                status = BCryptVerifySignature(
                    key,
                    &paddingInfo,
                    pHash,
                    hash.Length,
                    pSignature,
                    signature.Length,
                    BCryptSignVerifyFlags.BCRYPT_PAD_PKCS1);
            }

            return status == NTSTATUS.STATUS_SUCCESS;
        }

        internal static unsafe bool BCryptVerifySignaturePss(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> hash,
            ReadOnlySpan<byte> signature,
            string hashAlgorithmName)
        {

            NTSTATUS status;

            fixed (char* pHashAlgorithmName = hashAlgorithmName)
            fixed (byte* pHash = &MemoryMarshal.GetReference(hash))
            fixed (byte* pSignature = &MemoryMarshal.GetReference(signature))
            {
                BCRYPT_PSS_PADDING_INFO paddingInfo = default;
                paddingInfo.pszAlgId = (IntPtr)pHashAlgorithmName;
                paddingInfo.cbSalt = hash.Length;

                status = BCryptVerifySignature(
                    key,
                    &paddingInfo,
                    pHash,
                    hash.Length,
                    pSignature,
                    signature.Length,
                    BCryptSignVerifyFlags.BCRYPT_PAD_PSS);
            }

            return status == NTSTATUS.STATUS_SUCCESS;
        }
    }
}
