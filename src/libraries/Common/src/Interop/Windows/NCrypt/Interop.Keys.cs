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
        internal const string NCRYPT_CIPHER_KEY_BLOB = "CipherKeyBlob";
        internal const string NCRYPT_PKCS8_PRIVATE_KEY_BLOB = "PKCS8_PRIVATEKEY";

        internal const int NCRYPT_CIPHER_KEY_BLOB_MAGIC = 0x52485043;  //'CPHR'

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptOpenKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszKeyName, int dwLegacyKeySpec, CngKeyOpenOptions dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptImportKey(SafeNCryptProviderHandle hProvider, IntPtr hImportKey, string pszBlobType, IntPtr pParameterList, out SafeNCryptKeyHandle phKey, ref byte pbData, int cbData, int dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptImportKey(SafeNCryptProviderHandle hProvider, IntPtr hImportKey, string pszBlobType, ref NCryptBufferDesc pParameterList, out SafeNCryptKeyHandle phKey, ref byte pbData, int cbData, int dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptExportKey(SafeNCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, IntPtr pParameterList, byte[]? pbOutput, int cbOutput, out int pcbResult, int dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptExportKey(SafeNCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, IntPtr pParameterList, ref byte pbOutput, int cbOutput, out int pcbResult, int dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptExportKey(SafeNCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, ref NCryptBufferDesc pParameterList, ref byte pbOutput, int cbOutput, out int pcbResult, int dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptDeleteKey(SafeNCryptKeyHandle hKey, int dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptCreatePersistedKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszAlgId, string? pszKeyName, int dwLegacyKeySpec, CngKeyCreationOptions dwFlags);

        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial ErrorCode NCryptFinalizeKey(SafeNCryptKeyHandle hKey, int dwFlags);

        [StructLayout(LayoutKind.Sequential)]
        internal struct CRYPT_PKCS12_PBE_PARAMS
        {
            internal int iIterations;
            internal int cbSalt;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PBE_PARAMS
        {
            internal const int RgbSaltSize = 8;

            internal CRYPT_PKCS12_PBE_PARAMS Params;
            internal fixed byte rgbSalt[RgbSaltSize];
        }
    }
}
