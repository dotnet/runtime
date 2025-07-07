// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {
        [LibraryImport(Libraries.NCrypt)]
        private static unsafe partial ErrorCode NCryptDecapsulate(
            SafeNCryptKeyHandle hKey,
            byte* pbCipherText,
            uint cbCipherText,
            byte* pbSecretKey,
            uint cbSecretKey,
            out uint pcbSecretKey,
            uint dwFlags);

        [LibraryImport(Libraries.NCrypt)]
        private static unsafe partial ErrorCode NCryptEncapsulate(
            SafeNCryptKeyHandle hKey,
            byte* pbSecretKey,
            uint cbSecretKey,
            out uint pcbSecretKey,
            byte* pbCipherText,
            uint cbCipherText,
            out uint pcbCipherText,
            uint dwFlags);

        internal static unsafe uint NCryptDecapsulate(
            SafeNCryptKeyHandle hKey,
            ReadOnlySpan<byte> cipherText,
            Span<byte> secretKey,
            uint dwFlags)
        {
            fixed (byte* pCiphertext = cipherText)
            fixed (byte* pSecretKey = secretKey)
            {
                ErrorCode error = NCryptDecapsulate(
                    hKey,
                    pCiphertext,
                    (uint)cipherText.Length,
                    pSecretKey,
                    (uint)secretKey.Length,
                    out uint pcbSecretKey,
                    dwFlags);

                if (error != ErrorCode.ERROR_SUCCESS)
                {
                    throw error.ToCryptographicException();
                }

                return pcbSecretKey;
            }
        }

        internal static unsafe void NCryptEncapsulate(
            SafeNCryptKeyHandle hKey,
            Span<byte> secretKey,
            Span<byte> cipherText,
            out uint pcbSecretKey,
            out uint pcbCipherText,
            uint dwFlags)
        {
            fixed (byte* pSecretKey = secretKey)
            fixed (byte* pCipherText = cipherText)
            {
                ErrorCode error = NCryptEncapsulate(
                    hKey,
                    pSecretKey,
                    (uint)secretKey.Length,
                    out pcbSecretKey,
                    pCipherText,
                    (uint)cipherText.Length,
                    out pcbCipherText,
                    dwFlags);

                if (error != ErrorCode.ERROR_SUCCESS)
                {
                    throw error.ToCryptographicException();
                }
            }
        }
    }
}
