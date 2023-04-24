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
        [LibraryImport(Libraries.BCrypt, StringMarshalling = StringMarshalling.Utf16)]
        private static partial NTSTATUS BCryptExportKey(
            SafeBCryptKeyHandle hKey,
            IntPtr hExportKey,
            string pszBlobType,
            byte[]? pbOutput,
            int cbOutput,
            out int pcbResult,
            int dwFlags);

        internal static ArraySegment<byte> BCryptExportKey(SafeBCryptKeyHandle key, string blobType)
        {
            int numBytesNeeded;
            NTSTATUS ntStatus = BCryptExportKey(key, IntPtr.Zero, blobType, null, 0, out numBytesNeeded, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }

            byte[] rented = CryptoPool.Rent(numBytesNeeded);
            ntStatus = BCryptExportKey(key, IntPtr.Zero, blobType, rented, numBytesNeeded, out numBytesNeeded, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }

            return new ArraySegment<byte>(rented, 0, numBytesNeeded);
        }
    }
}
