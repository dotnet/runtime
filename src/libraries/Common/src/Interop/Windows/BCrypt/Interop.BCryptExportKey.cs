// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
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
                CryptoPool.Return(rented);
                throw CreateCryptographicException(ntStatus);
            }

            return new ArraySegment<byte>(rented, 0, numBytesNeeded);
        }

        internal static T BCryptExportKey<T>(SafeBCryptKeyHandle key, string blobType, Func<byte[], T> callback)
        {
            int numBytesNeeded;
            NTSTATUS ntStatus = BCryptExportKey(key, IntPtr.Zero, blobType, null, 0, out numBytesNeeded, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }

            // Array must be precisely-sized, so no renting.
            byte[] destination = new byte[numBytesNeeded];

            using (PinAndClear.Track(destination))
            {
                ntStatus = BCryptExportKey(key, IntPtr.Zero, blobType, destination, numBytesNeeded, out numBytesNeeded, 0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw CreateCryptographicException(ntStatus);
                }

                if (numBytesNeeded != destination.Length)
                {
                    Debug.Fail("Written byte count does not match required byte count.");
                    throw new CryptographicException();
                }

                return callback(destination);
            }
        }

        internal delegate T ExportKeyCallback<T>(ReadOnlySpan<byte> keyBytes);

        internal static T BCryptExportKey<T>(SafeBCryptKeyHandle key, string blobType, ExportKeyCallback<T> callback)
        {
            int numBytesNeeded;
            NTSTATUS ntStatus = BCryptExportKey(key, IntPtr.Zero, blobType, null, 0, out numBytesNeeded, 0);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                throw CreateCryptographicException(ntStatus);
            }

            byte[] rented = CryptoPool.Rent(numBytesNeeded);

            try
            {
                using (PinAndClear.Track(rented))
                {
                    ntStatus = BCryptExportKey(key, IntPtr.Zero, blobType, rented, numBytesNeeded, out numBytesNeeded, 0);

                    if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw CreateCryptographicException(ntStatus);
                    }

                    return callback(rented.AsSpan(0, numBytesNeeded));
                }
            }
            finally
            {
                // PinAndClear will clear
                CryptoPool.Return(rented, clearSize: 0);
            }
        }
    }
}
