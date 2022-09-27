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
        private enum BCryptEncryptFlags
        {
            BCRYPT_PAD_PKCS1 = 2,
            BCRYPT_PAD_OAEP = 4,
        }

        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptEncrypt(
            SafeBCryptKeyHandle hKey,
            ref byte pbInput,
            int cbInput,
            void* paddingInfo,
            byte* pbIV,
            int cbIV,
            ref byte pbOutput,
            int cbOutput,
            out int cbResult,
            BCryptEncryptFlags dwFlags);

        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptDecrypt(
            SafeBCryptKeyHandle hKey,
            ref byte pbInput,
            int cbInput,
            void* paddingInfo,
            byte* pbIV,
            int cbIV,
            ref byte pbOutput,
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
            Span<byte> notNull = stackalloc byte[1];
            scoped ReadOnlySpan<byte> effectiveSource;

            if (source.IsEmpty)
            {
                effectiveSource = notNull.Slice(1);
            }
            else
            {
                effectiveSource = source;
            }

            NTSTATUS status = BCryptEncrypt(
                key,
                ref MemoryMarshal.GetReference(effectiveSource),
                source.Length,
                pPaddingInfo,
                null,
                0,
                ref MemoryMarshal.GetReference(destination),
                destination.Length,
                out int written,
                dwFlags);

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
            NTSTATUS status = BCryptDecrypt(
                key,
                ref MemoryMarshal.GetReference(source),
                source.Length,
                pPaddingInfo,
                null,
                0,
                ref MemoryMarshal.GetReference(destination),
                destination.Length,
                out int written,
                dwFlags);

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
