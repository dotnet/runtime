// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

internal static partial class Interop
{
    public static partial class crypt32
    {
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CryptQueryObject(
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
        public static extern unsafe bool CryptQueryObject(
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
        public static extern unsafe bool CryptQueryObject(
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

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertGetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, [Out] out IntPtr pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertGetCertificateContextProperty")]
        public static extern unsafe bool CertGetCertificateContextPropertyString(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, byte* pvData, ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] CRYPTOAPI_BLOB* pvData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] CRYPT_KEY_PROV_INFO* pvData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertSetCertificateContextProperty(SafeCertContextHandle pCertContext, CertContextPropId dwPropId, CertSetPropertyFlags dwFlags, [In] SafeNCryptKeyHandle keyHandle);

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

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertGetNameStringW")]
        private static extern unsafe int CertGetNameString(SafeCertContextHandle pCertContext, CertNameType dwType, CertNameFlags dwFlags, in CertNameStringType pvTypePara, char* pszNameString, int cchNameString);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeCertContextHandle CertDuplicateCertificateContext(IntPtr pCertContext);

        [DllImport(Libraries.Crypt32, SetLastError = true)]
        public static extern SafeX509ChainHandle CertDuplicateCertificateChain(IntPtr pChainContext);

        [DllImport(Libraries.Crypt32, SetLastError = true)]
        internal static extern SafeCertStoreHandle CertDuplicateStore(IntPtr hCertStore);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertDuplicateCertificateContext")]
        public static extern SafeCertContextHandleWithKeyContainerDeletion CertDuplicateCertificateContextWithKeyContainerDeletion(IntPtr pCertContext);

        public static SafeCertStoreHandle CertOpenStore(CertStoreProvider lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, string pvPara)
        {
            return CertOpenStore((IntPtr)lpszStoreProvider, dwMsgAndCertEncodingType, hCryptProv, dwFlags, pvPara);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeCertStoreHandle CertOpenStore(IntPtr lpszStoreProvider, CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, CertStoreFlags dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pvPara);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertAddCertificateContextToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertAddCertificateLinkToStore(SafeCertStoreHandle hCertStore, SafeCertContextHandle pCertContext, CertStoreAddDisposition dwAddDisposition, IntPtr ppStoreContext);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        /// 
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle 
        /// and returns "false" to indicate the end of the store has been reached.
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
        public static extern SafeCertStoreHandle PFXImportCertStore([In] ref CRYPTOAPI_BLOB pPFX, SafePasswordHandle password, PfxCertStoreFlags dwFlags);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, byte* pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptMsgGetParam(SafeCryptMsgHandle hCryptMsg, CryptMessageParameterType dwParamType, int dwIndex, out int pvData, [In, Out] ref int pcbData);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertSerializeCertificateStoreElement(SafeCertContextHandle pCertContext, int dwFlags, [Out] byte[] pbElement, [In, Out] ref int pcbElement);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool PFXExportCertStore(SafeCertStoreHandle hStore, [In, Out] ref CRYPTOAPI_BLOB pPFX, SafePasswordHandle szPassword, PFXExportFlags dwFlags);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CertStrToNameW")]
        public static extern bool CertStrToName(CertEncodingType dwCertEncodingType, string pszX500, CertNameStrTypeAndFlags dwStrType, IntPtr pvReserved, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded, IntPtr ppszError);

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
        public static extern unsafe bool CryptDecodeObjectPointer(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, [In] byte[] pbEncoded, int cbEncoded, CryptDecodeObjectFlags dwFlags, [Out] void* pvStructInfo, [In, Out] ref int pcbStructInfo);

        public static unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[] pbEncoded, ref int pcbEncoded)
        {
            return CryptEncodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, IntPtr lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CryptEncodeObject(CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded);

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

        public static unsafe bool CertGetCertificateChain(ChainEngine hChainEngine, SafeCertContextHandle pCertContext, FILETIME* pTime, SafeCertStoreHandle hStore, [In] ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext)
        {
            return CertGetCertificateChain((IntPtr)hChainEngine, pCertContext, pTime, hStore, ref pChainPara, dwFlags, pvReserved, out ppChainContext);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe bool CertGetCertificateChain(IntPtr hChainEngine, SafeCertContextHandle pCertContext, FILETIME* pTime, SafeCertStoreHandle hStore, [In] ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptHashPublicKeyInfo(IntPtr hCryptProv, int algId, int dwFlags, CertEncodingType dwCertEncodingType, [In] ref CERT_PUBLIC_KEY_INFO pInfo, [Out] byte[] pbComputedHash, [In, Out] ref int pcbComputedHash);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CertSaveStore(SafeCertStoreHandle hCertStore, CertEncodingType dwMsgAndCertEncodingType, CertStoreSaveAs dwSaveAs, CertStoreSaveTo dwSaveTo, ref CRYPTOAPI_BLOB pvSaveToPara, int dwFlags);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        /// 
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle 
        /// and returns "false" to indicate the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertFindType dwFindType, void* pvFindPara, ref SafeCertContextHandle pCertContext)
        {
            CERT_CONTEXT* pPrevCertContext = pCertContext == null ? null : pCertContext.Disconnect();
            pCertContext = CertFindCertificateInStore(hCertStore, CertEncodingType.All, CertFindFlags.None, dwFindType, pvFindPara, pPrevCertContext);
            return !pCertContext.IsInvalid;
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe SafeCertContextHandle CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertEncodingType dwCertEncodingType, CertFindFlags dwFindFlags, CertFindType dwFindType, void* pvFindPara, CERT_CONTEXT* pPrevCertContext);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe int CertVerifyTimeValidity([In] ref FILETIME pTimeToVerify, [In] CERT_INFO* pCertInfo);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe CERT_EXTENSION* CertFindExtension([MarshalAs(UnmanagedType.LPStr)] string pszObjId, int cExtensions, CERT_EXTENSION* rgExtensions);

        // Note: It's somewhat unusual to use an API enum as a parameter type to a P/Invoke but in this case, X509KeyUsageFlags was intentionally designed as bit-wise
        // identical to the wincrypt CERT_*_USAGE values.
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertGetIntendedKeyUsage(CertEncodingType dwCertEncodingType, CERT_INFO* pCertInfo, out X509KeyUsageFlags pbKeyUsage, int cbKeyUsage);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern unsafe bool CertGetValidUsages(int cCerts, [In] ref SafeCertContextHandle rghCerts, out int cNumOIDs, [Out] void* rghOIDs, [In, Out] ref int pcbOIDs);

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
        public static extern unsafe bool CryptImportPublicKeyInfoEx2(CertEncodingType dwCertEncodingType, CERT_PUBLIC_KEY_INFO* pInfo, int dwFlags, void* pvAuxInfo, out SafeBCryptKeyHandle phKey);

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptAcquireCertificatePrivateKey(SafeCertContextHandle pCert, CryptAcquireFlags dwFlags, IntPtr pvParameters, out SafeNCryptKeyHandle phCryptProvOrNCryptKey, out int pdwKeySpec, out bool pfCallerFreeProvOrNCryptKey);
    }
}


