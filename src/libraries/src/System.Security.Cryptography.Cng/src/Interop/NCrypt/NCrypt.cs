// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NCrypt
    {

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptOpenStorageProvider(out SafeNCryptProviderHandle phProvider, string pszProviderName, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptOpenKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszKeyName, int dwLegacyKeySpec, CngKeyOpenOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptImportKey(SafeNCryptProviderHandle hProvider, IntPtr hImportKey, string pszBlobType, IntPtr pParameterList, [Out] out SafeNCryptKeyHandle phKey, [In] byte[] pbData, int cbData, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptExportKey(SafeNCryptKeyHandle hKey, IntPtr hExportKey, string pszBlobType, IntPtr pParameterList, [Out] byte[] pbOutput, int cbOutput, [Out] out int pcbResult, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptDeleteKey(SafeNCryptKeyHandle hKey, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptFreeObject(IntPtr hObject);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptGetProperty(SafeNCryptHandle hObject, string pszProperty, [Out] void* pbOutput, int cbOutput, out int pcbResult, CngPropertyOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern unsafe ErrorCode NCryptSetProperty(SafeNCryptHandle hObject, string pszProperty, [In] void* pbInput, int cbInput, CngPropertyOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptCreatePersistedKey(SafeNCryptProviderHandle hProvider, out SafeNCryptKeyHandle phKey, string pszAlgId, string pszKeyName, int dwLegacyKeySpec, CngKeyCreationOptions dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static extern ErrorCode NCryptFinalizeKey(SafeNCryptKeyHandle hKey, int dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe extern ErrorCode NCryptEncrypt(SafeNCryptKeyHandle hKey, [In] byte[] pbInput, int cbInput, void* pPaddingInfo, [Out] byte[] pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe extern ErrorCode NCryptDecrypt(SafeNCryptKeyHandle hKey, [In] byte[] pbInput, int cbInput, void* pPaddingInfo, [Out] byte[] pbOutput, int cbOutput, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe extern ErrorCode NCryptSignHash(SafeNCryptKeyHandle hKey, void* pPaddingInfo, [In] byte[] pbHashValue, int cbHashValue, [Out] byte[] pbSignature, int cbSignature, out int pcbResult, AsymmetricPaddingMode dwFlags);

        [DllImport(Interop.Libraries.NCrypt, CharSet = CharSet.Unicode)]
        internal static unsafe extern ErrorCode NCryptVerifySignature(SafeNCryptKeyHandle hKey, void *pPaddingInfo, [In] byte[] pbHashValue, int cbHashValue, [In] byte[] pbSignature, int cbSignature, AsymmetricPaddingMode dwFlags);

        /// <summary>
        ///     Result codes from NCrypt APIs
        /// </summary>
        internal enum ErrorCode : int
        {
            ERROR_SUCCESS = 0,                                          
            NTE_BAD_SIGNATURE = unchecked((int)0x80090006),             
            NTE_NOT_FOUND = unchecked((int)0x80090011),                 
            NTE_BAD_KEYSET = unchecked((int)0x80090016),
            NTE_INVALID_PARAMETER = unchecked((int)0x80090027),
            NTE_BUFFER_TOO_SMALL = unchecked((int)0x80090028),      
            NTE_NO_MORE_ITEMS = unchecked((int)0x8009002a),
            E_FAIL = unchecked((int)0x80004005),
        } 

        [StructLayout(LayoutKind.Sequential)]
        internal struct NCRYPT_UI_POLICY
        {
            public int dwVersion;
            public CngUIProtectionLevels dwFlags;
            public IntPtr pszCreationTitle;
            public IntPtr pszFriendlyName;
            public IntPtr pszDescription;
        }

        internal enum AsymmetricPaddingMode : int
        {
            None = 0x00000000,
            NCRYPT_NO_PADDING_FLAG = 0x00000001,
            NCRYPT_PAD_PKCS1_FLAG = 0x00000002,    // NCryptEncrypt/Decrypt or NCryptSignHash/VerifySignature
            NCRYPT_PAD_OAEP_FLAG = 0x00000004,     // NCryptEncrypt/Decrypt
            NCRYPT_PAD_PSS_FLAG = 0x00000008,      // NCryptSignHash/VerifySignature
        }
    }
}
 