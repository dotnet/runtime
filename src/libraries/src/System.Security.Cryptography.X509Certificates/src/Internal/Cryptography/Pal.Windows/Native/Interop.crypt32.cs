// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using CryptographicException = System.Security.Cryptography.CryptographicException;

using SafeBCryptKeyHandle = Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle;
using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;
using X509KeyUsageFlags = System.Security.Cryptography.X509Certificates.X509KeyUsageFlags;

using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;

internal static partial class Interop
{
    public static partial class crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            void* pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            out CertEncodingType pdwMsgAndCertEncodingType,
            out ContentType pdwContentType,
            out FormatType pdwFormatType,
            out SafeCertStoreHandle phCertStore,
            out SafeCryptMsgHandle phMsg,
            out SafeCertContextHandle ppvContext
            );

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            void* pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            IntPtr pdwMsgAndCertEncodingType,
            out ContentType pdwContentType,
            IntPtr pdwFormatType,
            IntPtr phCertStore,
            IntPtr phMsg,
            IntPtr ppvContext
            );

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern bool CryptQueryObject(
            CertQueryObjectType dwObjectType,
            void* pvObject,
            ExpectedContentTypeFlags dwExpectedContentTypeFlags,
            ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
            int dwFlags, // reserved - always pass 0
            IntPtr pdwMsgAndCertEncodingType,
            out ContentType pdwContentType,
            IntPtr pdwFormatType,
            out SafeCertStoreHandle phCertStore,
            IntPtr phMsg,
            IntPtr ppvContext
            );


        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertGetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, [Out] byte[] pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertGetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, [Out] out CRYPTOAPI_BLOB pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertGetCertificateContextProperty")]
        public static extern bool CertGetCertificateContextPropertyString(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, [Out] StringBuilder pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] CRYPTOAPI_BLOB* pvData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] CRYPT_KEY_PROV_INFO* pvData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertGetNameStringW")]
        public static extern int CertGetNameString(SafeCertContextHandle pCertContext, CertNameType dwType, CertNameFlags dwFlags, [In] ref CertNameStringType pvTypePara, [Out] StringBuilder pszNameString, int cchNameString);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeCertContextHandle CertDuplicateCertificateContext(IntPtr pCertContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertDuplicateCertificateContext")]
        public static extern SafeCertContextHandleWithKeyContainerDeletion CertDuplicateCertificateContextWithKeyContainerDeletion(IntPtr pCertContext);

        public static SafeCertStoreHandle CertOpenStore(CertStoreProvider lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, String pvPara)
        {
            return CertOpenStore((IntPtr)lpszStoreProvider, dwMsgAndCertEncodingType, hCryptProv, dwFlags, pvPara);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeCertStoreHandle CertOpenStore(IntPtr lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, [MarshalAs(UnmanagedType.LPWStr)] String pvPara);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertAddCertificateContextToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertAddCertificateLinkToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        /// 
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle 
        /// and returns "false" to indicate the the end of the store has been reached.
        /// </summary>
        public static bool CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, ref SafeCertContextHandle pCertContext)
        {
            unsafe
            {
                CERT_CONTEXT* pPrevCertContext = pCertContext == null ? null : pCertContext.Disconnect();
                pCertContext = CertEnumCertificatesInStore(hCertStore, pPrevCertContext);
                return !pCertContext.IsInvalid;
            }
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe SafeCertContextHandle CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, CERT_CONTEXT* pPrevCertContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeCertStoreHandle PFXImportCertStore([In] ref CRYPTOAPI_BLOB pPFX, String szPassword, PfxCertStoreFlags dwFlags);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, [Out] byte[] pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, out int pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertSerializeCertificateStoreElement(SafeCertContextHandle pCertContext, int dwFlags, [Out] byte[] pbElement, [In, Out] ref int pcbElement);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool PFXExportCertStore(SafeCertStoreHandle hStore, [In, Out] ref CRYPTOAPI_BLOB pPFX, String szPassword, PFXExportFlags dwFlags);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertNameToStrW")]
        public static extern int CertNameToStr(CertEncodingType dwCertEncodingType, [In] ref CRYPTOAPI_BLOB pName, CertNameStrTypeAndFlags dwStrType, StringBuilder psz, int csz);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertStrToNameW")]
        public static extern bool CertStrToName(CertEncodingType dwCertEncodingType, String pszX500, CertNameStrTypeAndFlags dwStrType, IntPtr pvReserved, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded, IntPtr ppszError);

        public static bool CryptFormatObject(CertEncodingType dwCertEncodingType, FormatObjectType dwFormatType, FormatObjectStringType dwFormatStrType, IntPtr pFormatStruct, FormatObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, StringBuilder pbFormat, ref int pcbFormat)
        {
            return CryptFormatObject(dwCertEncodingType, dwFormatType, dwFormatStrType, pFormatStruct, (IntPtr)lpszStructType, pbEncoded, cbEncoded, pbFormat, ref pcbFormat);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CryptFormatObject(CertEncodingType dwCertEncodingType, FormatObjectType dwFormatType, FormatObjectStringType dwFormatStrType, IntPtr pFormatStruct, IntPtr lpszStructType, [In] byte[] pbEncoded, int cbEncoded, [Out] StringBuilder pbFormat, [In, Out] ref int pcbFormat);

        public static bool CryptDecodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, byte[] pvStructInfo, ref int pcbStructInfo)
        {
            return CryptDecodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CryptDecodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] byte[] pvStructInfo, [In, Out] ref int pcbStructInfo);

        public static unsafe bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo)
        {
            return CryptDecodeObjectPointer(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptDecodeObject")]
        private static extern unsafe bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] void* pvStructInfo, [In, Out] ref int pcbStructInfo);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CryptDecodeObject")]
        public static extern unsafe bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] String lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] void* pvStructInfo, [In, Out] ref int pcbStructInfo);

        public static unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[] pbEncoded, ref int pcbEncoded)
        {
            return CryptEncodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] String lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded);

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

        public static unsafe byte[] EncodeObject(String lpszStructType, void* decoded)
        {
            int cb = 0;
            if (!Interop.crypt32.CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, null, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encoded = new byte[cb];
            if (!Interop.crypt32.CryptEncodeObject(CertEncodingType.All, lpszStructType, decoded, encoded, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encoded;
        }

        public static unsafe bool CertGetCertificateChain(ChainEngine hChainEngine, SafeCertContextHandle pCertContext, FILETIME* pTime, SafeCertStoreHandle hStore, [In] ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext)
        {
            return CertGetCertificateChain((IntPtr)hChainEngine, pCertContext, pTime, hStore, ref pChainPara, dwFlags, pvReserved, out ppChainContext);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe bool CertGetCertificateChain(IntPtr hChainEngine, SafeCertContextHandle pCertContext, FILETIME* pTime, SafeCertStoreHandle hStore, [In] ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptHashPublicKeyInfo(IntPtr hCryptProv, int algId, int dwFlags, CertEncodingType dwCertEncodingType, [In] ref CERT_PUBLIC_KEY_INFO pInfo, [Out] byte[] pbComputedHash, [In, Out] ref int pcbComputedHash);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertGetNameStringW")]
        public static extern int CertGetNameString(SafeCertContextHandle pCertContext, CertNameType dwType, CertNameFlags dwFlags, [In] ref CertNameStrTypeAndFlags pvPara, [Out] StringBuilder pszNameString, int cchNameString);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertSaveStore(SafeCertStoreHandle hCertStore, CertEncodingType dwMsgAndCertEncodingType, CertStoreSaveAs dwSaveAs, CertStoreSaveTo dwSaveTo, ref CRYPTOAPI_BLOB pvSaveToPara, int dwFlags);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        /// 
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle 
        /// and returns "false" to indicate the the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertFindType dwFindType, void* pvFindPara, ref SafeCertContextHandle pCertContext)
        {
            CERT_CONTEXT* pPrevCertContext = pCertContext == null ? null : pCertContext.Disconnect();
            pCertContext = CertFindCertificateInStore(hCertStore, CertEncodingType.All, CertFindFlags.None, dwFindType, pvFindPara, pPrevCertContext);
            return !pCertContext.IsInvalid;
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe extern SafeCertContextHandle CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertEncodingType dwCertEncodingType, CertFindFlags dwFindFlags, CertFindType dwFindType, void* pvFindPara, CERT_CONTEXT* pPrevCertContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern int CertVerifyTimeValidity([In] ref FILETIME pTimeToVerify, [In] CERT_INFO* pCertInfo);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern CERT_EXTENSION* CertFindExtension([MarshalAs(UnmanagedType.LPStr)] String pszObjId, int cExtensions, CERT_EXTENSION* rgExtensions);

        // Note: It's somewhat unusual to use an API enum as a parameter type to a P/Invoke but in this case, X509KeyUsageFlags was intentionally designed as bit-wise
        // identical to the wincrypt CERT_*_USAGE values.
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern bool CertGetIntendedKeyUsage(CertEncodingType dwCertEncodingType, CERT_INFO* pCertInfo, out X509KeyUsageFlags pbKeyUsage, int cbKeyUsage);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe extern bool CertGetValidUsages(int cCerts, [In] ref SafeCertContextHandle rghCerts, out int cNumOIDs, [Out] void* rghOIDs, [In, Out] ref int pcbOIDs);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertControlStore(SafeCertStoreHandle hCertStore, CertControlStoreFlags dwFlags, CertControlStoreType dwControlType, IntPtr pvCtrlPara);

        // Note: CertDeleteCertificateFromStore always calls CertFreeCertificateContext on pCertContext, even if an error is encountered.
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertDeleteCertificateFromStore(CERT_CONTEXT* pCertContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern void CertFreeCertificateChain(IntPtr pChainContext);

        public static bool CertVerifyCertificateChainPolicy(ChainPolicy pszPolicyOID, SafeX509ChainHandle pChainContext, ref CERT_CHAIN_POLICY_PARA pPolicyPara, ref CERT_CHAIN_POLICY_STATUS pPolicyStatus)
        {
            return CertVerifyCertificateChainPolicy((IntPtr)pszPolicyOID, pChainContext, ref pPolicyPara, ref pPolicyStatus);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CertVerifyCertificateChainPolicy(IntPtr pszPolicyOID, SafeX509ChainHandle pChainContext, [In] ref CERT_CHAIN_POLICY_PARA pPolicyPara, [In, Out] ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertFreeCertificateContext(IntPtr pCertContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertCloseStore(IntPtr hCertStore, int dwFlags);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptMsgClose(IntPtr hCryptMsg);

#if !NETNATIVE
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CryptImportPublicKeyInfoEx2(CertEncodingType dwCertEncodingType, CERT_PUBLIC_KEY_INFO* pInfo, int dwFlags, void* pvAuxInfo, out SafeBCryptKeyHandle phKey);
#endif
    }
}


