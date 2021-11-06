// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SafeBCryptKeyHandle = Microsoft.Win32.SafeHandles.SafeBCryptKeyHandle;
using SafeX509ChainHandle = Microsoft.Win32.SafeHandles.SafeX509ChainHandle;
using X509KeyUsageFlags = System.Security.Cryptography.X509Certificates.X509KeyUsageFlags;
using SafeNCryptKeyHandle = Microsoft.Win32.SafeHandles.SafeNCryptKeyHandle;

using Internal.Cryptography;
using Internal.Cryptography.Pal.Native;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;

internal static partial class Interop
{
    public static partial class crypt32
    {
        public static unsafe string CertGetNameString(
            SafeCertContextHandle certContext,
            Interop.Crypt32.CertNameType certNameType,
            Interop.Crypt32.CertNameFlags certNameFlags,
            Interop.Crypt32.CertNameStringType strType)
        {
            int cchCount = Crypt32.CertGetNameString(certContext, certNameType, certNameFlags, strType, null, 0);
            if (cchCount == 0)
            {
                throw Marshal.GetLastWin32Error().ToCryptographicException();
            }

            Span<char> buffer = cchCount <= 256 ? stackalloc char[cchCount] : new char[cchCount];
            fixed (char* ptr = &MemoryMarshal.GetReference(buffer))
            {
                if (Crypt32.CertGetNameString(certContext, certNameType, certNameFlags, strType, ptr, cchCount) == 0)
                {
                    throw Marshal.GetLastWin32Error().ToCryptographicException();
                }

                Debug.Assert(buffer[cchCount - 1] == '\0');
                return new string(buffer.Slice(0, cchCount - 1));
            }
        }

        public static SafeCertStoreHandle CertOpenStore(CertStoreProvider lpszStoreProvider, Interop.Crypt32.CertEncodingType dwMsgAndCertEncodingType, IntPtr hCryptProv, Interop.Crypt32.CertStoreFlags dwFlags, string? pvPara)
        {
            return Crypt32.CertOpenStore((IntPtr)lpszStoreProvider, dwMsgAndCertEncodingType, hCryptProv, dwFlags, pvPara);
        }

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        ///
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle
        /// and returns "false" to indicate the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertEnumCertificatesInStore(SafeCertStoreHandle hCertStore, [NotNull] ref SafeCertContextHandle? pCertContext)
        {
            Interop.Crypt32.CERT_CONTEXT* pPrevCertContext;
            if (pCertContext == null)
            {
                pCertContext = new SafeCertContextHandle();
                pPrevCertContext = null;
            }
            else
            {
                pPrevCertContext = pCertContext.Disconnect();
            }

            pCertContext.SetHandle((IntPtr)Crypt32.CertEnumCertificatesInStore(hCertStore, pPrevCertContext));

            if (!pCertContext.IsInvalid)
            {
                return true;
            }

            pCertContext.Dispose();
            return false;
        }

        public static bool CryptDecodeObject(Interop.Crypt32.CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, Interop.Crypt32.CryptDecodeObjectFlags dwFlags, byte[]? pvStructInfo, ref int pcbStructInfo)
        {
            return Interop.Crypt32.CryptDecodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        public static unsafe bool CryptDecodeObjectPointer(Interop.Crypt32.CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, byte[] pbEncoded, int cbEncoded, Interop.Crypt32.CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo)
        {
            return CryptDecodeObjectPointer(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CryptDecodeObject", CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial bool CryptDecodeObjectPointer(Interop.Crypt32.CertEncodingType dwCertEncodingType, IntPtr lpszStructType, byte[] pbEncoded, int cbEncoded, Interop.Crypt32.CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo);

        [GeneratedDllImport(Libraries.Crypt32, EntryPoint = "CryptDecodeObject", CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CryptDecodeObjectPointer(Interop.Crypt32.CertEncodingType dwCertEncodingType, [MarshalAs(UnmanagedType.LPStr)] string lpszStructType, byte[] pbEncoded, int cbEncoded, Interop.Crypt32.CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo);

        public static unsafe bool CryptEncodeObject(Interop.Crypt32.CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded)
        {
            return Interop.Crypt32.CryptEncodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
        }

        public static unsafe byte[] EncodeObject(CryptDecodeObjectStructType lpszStructType, void* decoded)
        {
            int cb = 0;
            if (!Interop.crypt32.CryptEncodeObject(Interop.Crypt32.CertEncodingType.All, lpszStructType, decoded, null, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encoded = new byte[cb];
            if (!Interop.crypt32.CryptEncodeObject(Interop.Crypt32.CertEncodingType.All, lpszStructType, decoded, encoded, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            return encoded;
        }

        public static unsafe byte[] EncodeObject(string lpszStructType, void* decoded)
        {
            int cb = 0;
            if (!Interop.Crypt32.CryptEncodeObject(Interop.Crypt32.CertEncodingType.All, lpszStructType, decoded, null, ref cb))
                throw Marshal.GetLastWin32Error().ToCryptographicException();

            byte[] encoded = new byte[cb];
            if (!Interop.Crypt32.CryptEncodeObject(Interop.Crypt32.CertEncodingType.All, lpszStructType, decoded, encoded, ref cb))
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

        [GeneratedDllImport(Libraries.Crypt32)]
        public static partial void CertFreeCertificateChainEngine(IntPtr hChainEngine);

        [GeneratedDllImport(Libraries.Crypt32, SetLastError = true)]
        public static unsafe partial bool CertGetCertificateChain(IntPtr hChainEngine, SafeCertContextHandle pCertContext, Interop.Crypt32.FILETIME* pTime, SafeCertStoreHandle hStore, ref CERT_CHAIN_PARA pChainPara, CertChainFlags dwFlags, IntPtr pvReserved, out SafeX509ChainHandle ppChainContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CryptHashPublicKeyInfo(IntPtr hCryptProv, int algId, int dwFlags, Interop.Crypt32.CertEncodingType dwCertEncodingType, ref Interop.Crypt32.CERT_PUBLIC_KEY_INFO pInfo, byte[] pbComputedHash, ref int pcbComputedHash);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertSaveStore(SafeCertStoreHandle hCertStore, Interop.Crypt32.CertEncodingType dwMsgAndCertEncodingType, CertStoreSaveAs dwSaveAs, CertStoreSaveTo dwSaveTo, ref Interop.Crypt32.DATA_BLOB pvSaveToPara, int dwFlags);

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        ///
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle
        /// and returns "false" to indicate the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertFindCertificateInStore(SafeCertStoreHandle hCertStore, CertFindType dwFindType, void* pvFindPara, [NotNull] ref SafeCertContextHandle? pCertContext)
        {
            Interop.Crypt32.CERT_CONTEXT* pPrevCertContext = pCertContext == null ? null : pCertContext.Disconnect();
            pCertContext = CertFindCertificateInStore(hCertStore, Interop.Crypt32.CertEncodingType.All, CertFindFlags.None, dwFindType, pvFindPara, pPrevCertContext);
            return !pCertContext.IsInvalid;
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static unsafe partial SafeCertContextHandle CertFindCertificateInStore(SafeCertStoreHandle hCertStore, Interop.Crypt32.CertEncodingType dwCertEncodingType, CertFindFlags dwFindFlags, CertFindType dwFindType, void* pvFindPara, Interop.Crypt32.CERT_CONTEXT* pPrevCertContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial int CertVerifyTimeValidity(ref Interop.Crypt32.FILETIME pTimeToVerify, Interop.Crypt32.CERT_INFO* pCertInfo);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial Interop.Crypt32.CERT_EXTENSION* CertFindExtension([MarshalAs(UnmanagedType.LPStr)] string pszObjId, int cExtensions, IntPtr rgExtensions);

        // Note: It's somewhat unusual to use an API enum as a parameter type to a P/Invoke but in this case, X509KeyUsageFlags was intentionally designed as bit-wise
        // identical to the wincrypt CERT_*_USAGE values.
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertGetIntendedKeyUsage(Interop.Crypt32.CertEncodingType dwCertEncodingType, Interop.Crypt32.CERT_INFO* pCertInfo, out X509KeyUsageFlags pbKeyUsage, int cbKeyUsage);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertGetValidUsages(int cCerts, ref SafeCertContextHandle rghCerts, out int cNumOIDs, void* rghOIDs, ref int pcbOIDs);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CertControlStore(SafeCertStoreHandle hCertStore, CertControlStoreFlags dwFlags, CertControlStoreType dwControlType, IntPtr pvCtrlPara);

        // Note: CertDeleteCertificateFromStore always calls CertFreeCertificateContext on pCertContext, even if an error is encountered.
        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CertDeleteCertificateFromStore(Interop.Crypt32.CERT_CONTEXT* pCertContext);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial void CertFreeCertificateChain(IntPtr pChainContext);

        public static bool CertVerifyCertificateChainPolicy(ChainPolicy pszPolicyOID, SafeX509ChainHandle pChainContext, ref CERT_CHAIN_POLICY_PARA pPolicyPara, ref CERT_CHAIN_POLICY_STATUS pPolicyStatus)
        {
            return CertVerifyCertificateChainPolicy((IntPtr)pszPolicyOID, pChainContext, ref pPolicyPara, ref pPolicyStatus);
        }

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static partial bool CertVerifyCertificateChainPolicy(IntPtr pszPolicyOID, SafeX509ChainHandle pChainContext, ref CERT_CHAIN_POLICY_PARA pPolicyPara, ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static unsafe partial bool CryptImportPublicKeyInfoEx2(Interop.Crypt32.CertEncodingType dwCertEncodingType, Interop.Crypt32.CERT_PUBLIC_KEY_INFO* pInfo, CryptImportPublicKeyInfoFlags dwFlags, void* pvAuxInfo, out SafeBCryptKeyHandle phKey);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        public static partial bool CryptAcquireCertificatePrivateKey(SafeCertContextHandle pCert, CryptAcquireFlags dwFlags, IntPtr pvParameters, out SafeNCryptKeyHandle phCryptProvOrNCryptKey, out int pdwKeySpec, out bool pfCallerFreeProvOrNCryptKey);
    }
}
