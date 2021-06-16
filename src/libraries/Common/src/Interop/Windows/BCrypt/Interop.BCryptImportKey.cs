// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class BCrypt
    {
        internal static unsafe SafeKeyHandle BCryptImportKey(SafeAlgorithmHandle hAlg, ReadOnlySpan<byte> key)
        {
            const string BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";
            int keySize = key.Length;
            int blobSize = sizeof(BCRYPT_KEY_DATA_BLOB_HEADER) + keySize;
            byte[] blob = new byte[blobSize];
            fixed (byte* pbBlob = blob)
            {
                BCRYPT_KEY_DATA_BLOB_HEADER* pBlob = (BCRYPT_KEY_DATA_BLOB_HEADER*)pbBlob;
                pBlob->dwMagic = BCRYPT_KEY_DATA_BLOB_HEADER.BCRYPT_KEY_DATA_BLOB_MAGIC;
                pBlob->dwVersion = BCRYPT_KEY_DATA_BLOB_HEADER.BCRYPT_KEY_DATA_BLOB_VERSION1;
                pBlob->cbKeyData = (uint)keySize;
            }

            key.CopyTo(blob.AsSpan(sizeof(BCRYPT_KEY_DATA_BLOB_HEADER)));
            SafeKeyHandle hKey;
            NTSTATUS ntStatus = BCryptImportKey(hAlg, IntPtr.Zero, BCRYPT_KEY_DATA_BLOB, out hKey, IntPtr.Zero, 0, blob, blobSize, 0);
            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }

            return hKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BCRYPT_KEY_DATA_BLOB_HEADER
        {
            public uint dwMagic;
            public uint dwVersion;
            public uint cbKeyData;

            public const uint BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b;
            public const uint BCRYPT_KEY_DATA_BLOB_VERSION1 = 0x1;
        }

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
        private static extern NTSTATUS BCryptImportKey(SafeAlgorithmHandle hAlgorithm, IntPtr hImportKey, string pszBlobType, out SafeKeyHandle hKey, IntPtr pbKeyObject, int cbKeyObject, byte[] pbInput, int cbInput, int dwFlags);
    }
}
