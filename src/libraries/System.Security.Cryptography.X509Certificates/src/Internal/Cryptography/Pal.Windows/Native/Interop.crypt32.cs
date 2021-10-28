// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using CryptographicException = System.Security.Cryptography.CryptographicException;

using SafeBCryptKeyHandle = Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle;
using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;
using X509KeyUsageFlags = System.Security.Cryptography.X509Certificates.X509KeyUsageFlags;
using SafeNCryptKeyHandle = Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle;

using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using static Interop.Crypt32;

internal static partial class Interop
{
    public static partial class crypt32
    {
        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CertGetCertificateContextProperty", CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertGetCertificateContextPropertyString(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, byte* pvData, ref int pcbData);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, DATA_BLOB* pvData);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, CRYPT_KEY_PROV_INFO* pvData);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, SafeNCryptKeyHandle keyHandle);

        public static unsafe string CertGetNameString(
            SafeCertContextHandle certContext,
            CertNameType certNameType,
            CertNameFlags certNameFlags,
            CertNameStringType strType)
        {
            int cchCount = CertGetNameString(certContext, certNameType, certNameFlags, strType, null, 0);
            if (cchCount == 0)
            {
                throw Marshal.GetLastWin32Error().ToCryptographicException();
            }

            Span<char> buffer = cchCount <= 256 ? stackalloc char[cchCount] : new char[cchCount];
            fixed (char* ptr = &MemoryMarshal.GetReference(buffer))
            {
                if (CertGetNameString(certContext, certNameType, certNameFlags, strType, ptr, cchCount) == 0)
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                Debug.Assert(buffer[cchCount - 1] == '\0');
                return new string(buffer.Slice(0, cchCount - 1));
            }
        }

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CertGetNameStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial int CertGetNameString(SafeCertContextHandle pCertContext, CertNameType dwType, CertNameFlags dwFlags, in CertNameStringType pvTypePara, char* pszNameString, int cchNameString);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        public static partial SafeX509ChainHandle CertDuplicateCertificateChain(IntPtr pChainContext);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        internal static partial SafeCertStoreHandle CertDuplicateStore(IntPtr hCertStore);

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CertDuplicateCertificateContext", CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeCertContextHandleWithKeyContainerDeletion CertDuplicateCertificateContextWithKeyContainerDeletion(IntPtr pCertContext);

        public static SafeCertStoreHandle CertOpenStore(CertStoreProvider lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, string? pvPara)
        {
            return CertOpenStore((IntPtr)lpszStoreProvider, dwMsgAndCertEncodingType, hCryptProv, dwFlags, pvPara);
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial SafeCertStoreHandle CertOpenStore(IntPtr lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string? pvPara);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertAddCertificateContextToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertAddCertificateLinkToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        ///
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle
        /// and returns "false" to indicate the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, [NotNull] ref SafeCertContextHandle? pCertContext)
        {
            CERT_CONTEXT* pPrevCertContext;
            if (pCertContext == null)
            {
                pCertContext = new SafeCertContextHandle();
                pPrevCertContext = null;
            }
            else
            {
                pPrevCertContext = pCertContext.Disconnect();
            }

            pCertContext.SetHandle((IntPtr)CertEnumCertificatesInStore(hCertStore, pPrevCertContext));

            if (!pCertContext.IsInvalid)
            {
                return true;
            }

            pCertContext.Dispose();
            return false;
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial CERT_CONTEXT* CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, CERT_CONTEXT* pPrevCertContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial SafeCertStoreHandle PFXImportCertStore(ref DATA_BLOB pPFX, SafePasswordHandle password, PfxCertStoreFlags dwFlags);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, byte* pvData, ref int pcbData);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, out int pvData, ref int pcbData);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertSerializeCertificateStoreElement(SafeCertContextHandle pCertContext, int dwFlags, byte[]? pbElement, ref int pcbElement);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool PFXExportCertStore(SafeCertStoreHandle hStore, ref DATA_BLOB pPFX, SafePasswordHandle szPassword, PFXExportFlags dwFlags);

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CertStrToNameW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertStrToName(CertEncodingType dwCertEncodingType, string pszX500, CertNameStrTypeAndFlags dwStrType, IntPtr pvReserved, byte[]? pbEncoded, ref int pcbEncoded, IntPtr ppszError);

        public static bool CryptDecodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, byte[]? pvStructInfo, ref int pcbStructInfo)
        {
            return CryptDecodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool CryptDecodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, byte[]? pvStructInfo, ref int pcbStructInfo);

        public static unsafe bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo)
        {
            return CryptDecodeObjectPointer(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CryptDecodeObject", CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo);

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CryptDecodeObject", CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo);

        public static unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded)
        {
            return CryptEncodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial bool CryptEncodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CryptEncodeObject(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded);

        public static unsafe byte[] EncodeObject(CryptDecodeObjectStructType lpszStructType, void* decoded)
        {
            int cb = 0;
            if (!Interop.crypt32.CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, null, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encoded = new byte[cb];
            if (!Interop.crypt32.CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, encoded, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encoded;
        }

        public static unsafe byte[] EncodeObject(string lpszStructType, void* decoded)
        {
            int cb = 0;
            if (!Interop.crypt32.CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, null, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encoded = new byte[cb];
            if (!Interop.crypt32.CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, encoded, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encoded;
        }

        internal static SafeChainEngineHandle CertCreateCertificateChainEngine(ref CERT_CHAIN_ENGINE_CONFIG config)
        {
            if (!CertCreateCertificateChainEngine(ref config, out SafeChainEngineHandle chainEngineHandle))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw errorCode.ToCryptographicException();
            }

            return chainEngineHandle;
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool CertCreateCertificateChainEngine(ref CERT_CHAIN_ENGINE_CONFIG pConfig, out SafeChainEngineHandle hChainEngineHandle);

        [DllImport(Libraries.Crypt32)]
        public static extern void CertFreeCertificateChainEngine(IntPtr hChainEngine);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        public static unsafe partial bool CertGetCertificateChain(IntPtr hChainEngine, SafeCertContextHandle pCertContext, FILETIME* pTime, SafeCertStoreHandle hStore, ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CryptHashPublicKeyInfo(IntPtr hCryptProv, int algId, int dwFlags, CertEncodingType dwCertEncodingType, ref CERT_PUBLIC_KEY_INFO pInfo, byte[] pbComputedHash, ref int pcbComputedHash);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertSaveStore(SafeCertStoreHandle hCertStore, CertEncodingType dwMsgAndCertEncodingType, CertStoreSaveAs dwSaveAs, CertStoreSaveTo dwSaveTo, ref DATA_BLOB pvSaveToPara, int dwFlags);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        ///
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle
        /// and returns "false" to indicate the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertFindType dwFindType, void* pvFindPara, [NotNull] ref SafeCertContextHandle? pCertContext)
        {
            CERT_CONTEXT* pPrevCertContext = pCertContext == null ? null : pCertContext.Disconnect();
            pCertContext = CertFindCertificateInStore(hCertStore, CertEncodingType.All, CertFindFlags.None, dwFindType, pvFindPara, pPrevCertContext);
            return !pCertContext.IsInvalid;
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial SafeCertContextHandle CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertEncodingType dwCertEncodingType, CertFindFlags dwFindFlags, CertFindType dwFindType, void* pvFindPara, CERT_CONTEXT* pPrevCertContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial int CertVerifyTimeValidity(ref FILETIME pTimeToVerify, CERT_INFO* pCertInfo);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial CERT_EXTENSION* CertFindExtension([MarshalAs(UnmanagedType.LPStr)] string pszObjId, int cExtensions, IntPtr rgExtensions);

        // Note: It's somewhat unusual to use an API enum as a parameter type to a P/Invoke but in this case, X509KeyUsageFlags was intentionally designed as bit-wise
        // identical to the wincrypt CERT_*_USAGE values.
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertGetIntendedKeyUsage(CertEncodingType dwCertEncodingType, CERT_INFO* pCertInfo, out X509KeyUsageFlags pbKeyUsage, int cbKeyUsage);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertGetValidUsages(int cCerts, ref SafeCertContextHandle rghCerts, out int cNumOIDs, void* rghOIDs, ref int pcbOIDs);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertControlStore(SafeCertStoreHandle hCertStore, CertControlStoreFlags dwFlags, CertControlStoreType dwControlType, IntPtr pvCtrlPara);

        // Note: CertDeleteCertificateFromStore always calls CertFreeCertificateContext on pCertContext, even if an error is encountered.
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertDeleteCertificateFromStore(CERT_CONTEXT* pCertContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial void CertFreeCertificateChain(IntPtr pChainContext);

        public static bool CertVerifyCertificateChainPolicy(ChainPolicy pszPolicyOID, SafeX509ChainHandle pChainContext, ref CERT_CHAIN_POLICY_PARA pPolicyPara, ref CERT_CHAIN_POLICY_STATUS pPolicyStatus)
        {
            return CertVerifyCertificateChainPolicy((IntPtr)pszPolicyOID, pChainContext, ref pPolicyPara, ref pPolicyStatus);
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool CertVerifyCertificateChainPolicy(IntPtr pszPolicyOID, SafeX509ChainHandle pChainContext, ref CERT_CHAIN_POLICY_PARA pPolicyPara, ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CryptImportPublicKeyInfoEx2(CertEncodingType dwCertEncodingType, CERT_PUBLIC_KEY_INFO* pInfo, CryptImportPublicKeyInfoFlags dwFlags, void* pvAuxInfo, out SafeBCryptKeyHandle phKey);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CryptAcquireCertificatePrivateKey(SafeCertContextHandle pCert, CryptAcquireFlags dwFlags, IntPtr pvParameters, out SafeNCryptKeyHandle phCryptProvOrNCryptKey, out int pdwKeySpec, out bool pfCallerFreeProvOrNCryptKey);
    }
}
