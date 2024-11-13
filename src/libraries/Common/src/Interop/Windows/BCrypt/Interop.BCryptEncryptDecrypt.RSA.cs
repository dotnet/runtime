// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [Flags]
        private enum BCryptEncryptFlags : uint
        {
            BCRYPT_PAD_PKCS1 = 2,
            BCRYPT_PAD_OAEP = 4,
        }

        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptEncrypt(
            SafeBCryptKeyHandle hKey,
            byte* pbInput,
            int cbInput,
            void* paddingInfo,
            byte* pbIV,
            int cbIV,
            byte* pbOutput,
            int cbOutput,
            out int cbResult,
            BCryptEncryptFlags dwFlags);

        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptDecrypt(
            SafeBCryptKeyHandle hKey,
            byte* pbInput,
            int cbInput,
            void* paddingInfo,
            byte* pbIV,
            int cbIV,
            byte* pbOutput,
            int cbOutput,
            out int cbResult,
            BCryptEncryptFlags dwFlags);

        private static unsafe int BCryptEncryptRsa(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            void* pPaddingInfo,
            BCryptEncryptFlags dwFlags)
        {
            // BCryptEncrypt does not accept null/0, only non-null/0.
            ReadOnlySpan<byte> notNull = stackalloc byte[1];
            scoped ReadOnlySpan<byte> effectiveSource;

            if (source.IsEmpty)
            {
                effectiveSource = notNull.Slice(1);
            }
            else
            {
                effectiveSource = source;
            }

            NTSTATUS status;
            int written;

            fixed (byte* pSource = &MemoryMarshal.GetReference(effectiveSource))
            fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
            {
                status = BCryptEncrypt(
                    key,
                    pSource,
                    source.Length,
                    pPaddingInfo,
                    null,
                    0,
                    pDest,
                    destination.Length,
                    out written,
                    dwFlags);
            }

            if (status != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(status);
            }

            return written;
        }

        private static unsafe bool BCryptDecryptRsa(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            void* pPaddingInfo,
            BCryptEncryptFlags dwFlags,
            out int bytesWritten)
        {
            NTSTATUS status;
            int written;

            fixed (byte* pSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pDest = &MemoryMarshal.GetReference(destination))
            {
                status = BCryptDecrypt(
                    key,
                    pSource,
                    source.Length,
                    pPaddingInfo,
                    null,
                    0,
                    pDest,
                    destination.Length,
                    out written,
                    dwFlags);
            }

            // Windows 10.1903 can return success when it meant NTE_BUFFER_TOO_SMALL.
            if (status == NTSTATUS.STATUS_SUCCESS && written > destination.Length)
            {
                CryptographicOperations.ZeroMemory(destination);
                bytesWritten = 0;
                return false;
            }

            if (status == NTSTATUS.STATUS_SUCCESS)
            {
                bytesWritten = written;
                return true;
            }

            if (status == NTSTATUS.STATUS_BUFFER_TOO_SMALL)
            {
                bytesWritten = 0;
                return false;
            }

            throw CreateCryptographicException(status);
        }

        internal static unsafe int BCryptEncryptPkcs1(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> source,
            Span<byte> destination)
        {
            return BCryptEncryptRsa(
                key,
                source,
                destination,
                null,
                BCryptEncryptFlags.BCRYPT_PAD_PKCS1);
        }

        internal static unsafe int BCryptEncryptOaep(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            string? hashAlgorithmName)
        {
            fixed (char* pHashAlg = hashAlgorithmName)
            {
                BCRYPT_OAEP_PADDING_INFO paddingInfo = default;
                paddingInfo.pszAlgId = (IntPtr)pHashAlg;

                return BCryptEncryptRsa(
                    key,
                    source,
                    destination,
                    &paddingInfo,
                    BCryptEncryptFlags.BCRYPT_PAD_OAEP);
            }
        }

        internal static unsafe bool BCryptDecryptPkcs1(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            out int bytesWritten)
        {
            return BCryptDecryptRsa(
                key,
                source,
                destination,
                null,
                BCryptEncryptFlags.BCRYPT_PAD_PKCS1,
                out bytesWritten);
        }

        internal static unsafe bool BCryptDecryptOaep(
            SafeBCryptKeyHandle key,
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            string? hashAlgorithmName,
            out int bytesWritten)
        {
            fixed (char* pHashAlg = hashAlgorithmName)
            {
                BCRYPT_OAEP_PADDING_INFO paddingInfo = default;
                paddingInfo.pszAlgId = (IntPtr)pHashAlg;

                return BCryptDecryptRsa(
                    key,
                    source,
                    destination,
                    &paddingInfo,
                    BCryptEncryptFlags.BCRYPT_PAD_OAEP,
                    out bytesWritten);
            }
        }
    }
}
