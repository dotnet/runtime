// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        private static readonly SafeCreateHandle s_nullExportString = new SafeCreateHandle();

        private static int AppleCryptoNative_SecKeyImportEphemeral(
            ReadOnlySpan<byte> pbKeyBlob,
            int isPrivateKey,
            out SafeSecKeyRefHandle ppKeyOut,
            out int pOSStatus) =>
            AppleCryptoNative_SecKeyImportEphemeral(
                ref MemoryMarshal.GetReference(pbKeyBlob),
                pbKeyBlob.Length,
                isPrivateKey,
                out ppKeyOut,
                out pOSStatus);

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SecKeyImportEphemeral(
            ref byte pbKeyBlob,
            int cbKeyBlob,
            int isPrivateKey,
            out SafeSecKeyRefHandle ppKeyOut,
            out int pOSStatus);

        internal static SafeSecKeyRefHandle ImportEphemeralKey(ReadOnlySpan<byte> keyBlob, bool hasPrivateKey)
        {
            Debug.Assert(keyBlob != default);

            SafeSecKeyRefHandle keyHandle;
            int osStatus;

            int ret = AppleCryptoNative_SecKeyImportEphemeral(
                keyBlob,
                hasPrivateKey ? 1 : 0,
                out keyHandle,
                out osStatus);

            if (ret == 1 && !keyHandle.IsInvalid)
            {
                return keyHandle;
            }

            if (ret == 0)
            {
                Exception e = CreateExceptionForOSStatus(osStatus);
                keyHandle.Dispose();
                throw e;
            }

            Debug.Fail($"SecKeyImportEphemeral returned {ret}");
            keyHandle.Dispose();
            throw new CryptographicException();
        }

        [LibraryImport(Libraries.AppleCryptoNative)]
        private static partial int AppleCryptoNative_SecKeyExport(
            SafeSecKeyRefHandle? key,
            int exportPrivate,
            SafeCreateHandle cfExportPassphrase,
            out SafeCFDataHandle cfDataOut,
            out int pOSStatus);

        internal static SafeCFDataHandle SecKeyExportData(
            SafeSecKeyRefHandle? key,
            bool exportPrivate,
            ReadOnlySpan<char> password)
        {
            SafeCreateHandle exportPassword = exportPrivate
                ? CoreFoundation.CFStringCreateFromSpan(password)
                : s_nullExportString;

            int ret;
            SafeCFDataHandle cfData;
            int osStatus;

            try
            {
                ret = AppleCryptoNative_SecKeyExport(
                    key,
                    exportPrivate ? 1 : 0,
                    exportPassword,
                    out cfData,
                    out osStatus);
            }
            finally
            {
                if (exportPassword != s_nullExportString)
                {
                    exportPassword.Dispose();
                }
            }

            if (ret == 1)
            {
                return cfData;
            }

            cfData.Dispose();

            if (ret == 0)
            {
                throw CreateExceptionForOSStatus(osStatus);
            }

            Debug.Fail($"AppleCryptoNative_SecKeyExport returned {ret}");
            throw new CryptographicException();
        }

        internal static byte[] SecKeyExport(
            SafeSecKeyRefHandle? key,
            bool exportPrivate,
            string password)
        {
            using (SafeCFDataHandle cfData = SecKeyExportData(key, exportPrivate, password))
            {
                return CoreFoundation.CFGetData(cfData);
            }
        }
    }
}
