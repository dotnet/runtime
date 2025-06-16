// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptDecapsulate(
            SafeBCryptKeyHandle hKey,
            byte* pbCipherText,
            uint cbCipherText,
            byte* pbSecretKey,
            uint cbSecretKey,
            out uint pcbSecretKey,
            uint dwFlags);

        [LibraryImport(Libraries.BCrypt)]
        private static unsafe partial NTSTATUS BCryptEncapsulate(
            SafeBCryptKeyHandle hKey,
            byte* pbSecretKey,
            uint cbSecretKey,
            out uint pcbSecretKey,
            byte* pbCipherText,
            uint cbCipherText,
            out uint pcbCipherText,
            uint dwFlags);

        internal static unsafe uint BCryptDecapsulate(
            SafeBCryptKeyHandle hKey,
            ReadOnlySpan<byte> cipherText,
            Span<byte> secretKey,
            uint dwFlags)
        {
            fixed (byte* pCiphertext = cipherText)
            fixed (byte* pSecretKey = secretKey)
            {
                NTSTATUS status = BCryptDecapsulate(
                    hKey,
                    pCiphertext,
                    (uint)cipherText.Length,
                    pSecretKey,
                    (uint)secretKey.Length,
                    out uint pcbSecretKey,
                    dwFlags);

                if (status != NTSTATUS.STATUS_SUCCESS)
                {
                    throw CreateCryptographicException(status);
                }

                return pcbSecretKey;
            }
        }

        internal static unsafe void BCryptEncapsulate(
            SafeBCryptKeyHandle hKey,
            Span<byte> secretKey,
            Span<byte> cipherText,
            out uint pcbSecretKey,
            out uint pcbCipherText,
            uint dwFlags)
        {
            fixed (byte* pSecretKey = secretKey)
            fixed (byte* pCipherText = cipherText)
            {
                NTSTATUS status = BCryptEncapsulate(
                    hKey,
                    pSecretKey,
                    (uint)secretKey.Length,
                    out pcbSecretKey,
                    pCipherText,
                    (uint)cipherText.Length,
                    out pcbCipherText,
                    dwFlags);

                if (status != NTSTATUS.STATUS_SUCCESS)
                {
                    throw CreateCryptographicException(status);
                }
            }
        }
    }
}
