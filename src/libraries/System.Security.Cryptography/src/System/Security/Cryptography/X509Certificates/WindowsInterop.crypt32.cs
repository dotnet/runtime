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
using Microsoft.Win32.SafeHandles;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

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

            Marshal.InitHandle(pCertContext, (IntPtr)Crypt32.CertEnumCertificatesInStore(hCertStore, pPrevCertContext));

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
            return Interop.Crypt32.CryptDecodeObjectPointer(dwCertEncodingType, (IntPtr)lpszStructType, pbEncoded, cbEncoded, dwFlags, pvStructInfo, ref pcbStructInfo);
        }

        public static unsafe bool CryptDecodeObjectPointer(Interop.Crypt32.CertEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, ReadOnlySpan<byte> encoded, Interop.Crypt32.CryptDecodeObjectFlags dwFlags, void* pvStructInfo, ref int pcbStructInfo)
        {
            fixed (byte* pEncoded = encoded)
            {
                return Interop.Crypt32.CryptDecodeObjectPointer(dwCertEncodingType, (IntPtr)lpszStructType, pEncoded, encoded.Length, dwFlags, pvStructInfo, ref pcbStructInfo);
            }
        }

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

        internal static SafeChainEngineHandle CertCreateCertificateChainEngine(ref Interop.Crypt32.CERT_CHAIN_ENGINE_CONFIG config)
        {
            if (!Interop.Crypt32.CertCreateCertificateChainEngine(ref config, out SafeChainEngineHandle chainEngineHandle))
            {
                Exception e = Marshal.GetLastWin32Error().ToCryptographicException();
                chainEngineHandle.Dispose();
                throw e;
            }

            return chainEngineHandle;
        }

        /// <summary>
        /// A less error-prone wrapper for CertEnumCertificatesInStore().
        ///
        /// To begin the enumeration, set pCertContext to null. Each iteration replaces pCertContext with
        /// the next certificate in the iteration. The final call sets pCertContext to an invalid SafeCertStoreHandle
        /// and returns "false" to indicate the end of the store has been reached.
        /// </summary>
        public static unsafe bool CertFindCertificateInStore(SafeCertStoreHandle hCertStore, Interop.Crypt32.CertFindType dwFindType, void* pvFindPara, [NotNull] ref SafeCertContextHandle? pCertContext)
        {
            Interop.Crypt32.CERT_CONTEXT* pPrevCertContext = null;
            if (pCertContext != null)
            {
                pPrevCertContext = pCertContext.Disconnect();
                pCertContext.Dispose();
            }

            pCertContext = Interop.Crypt32.CertFindCertificateInStore(hCertStore, Interop.Crypt32.CertEncodingType.All, Interop.Crypt32.CertFindFlags.None, dwFindType, pvFindPara, pPrevCertContext);
            return !pCertContext.IsInvalid;
        }

        public static unsafe bool CertGetIntendedKeyUsage(Interop.Crypt32.CertEncodingType dwCertEncodingType, Interop.Crypt32.CERT_INFO* pCertInfo, out X509KeyUsageFlags pbKeyUsage, int cbKeyUsage)
        {
            bool result = Interop.Crypt32.CertGetIntendedKeyUsage(dwCertEncodingType, pCertInfo, out Interop.Crypt32.X509KeyUsageFlags x509KeyUsageFlags, cbKeyUsage);
            pbKeyUsage = (X509KeyUsageFlags)(int)x509KeyUsageFlags;
            return result;
        }

        public static bool CertVerifyCertificateChainPolicy(ChainPolicy pszPolicyOID, SafeX509ChainHandle pChainContext, ref Interop.Crypt32.CERT_CHAIN_POLICY_PARA pPolicyPara, ref Interop.Crypt32.CERT_CHAIN_POLICY_STATUS pPolicyStatus)
        {
            return Interop.Crypt32.CertVerifyCertificateChainPolicy((IntPtr)pszPolicyOID, pChainContext, ref pPolicyPara, ref pPolicyStatus);
        }

        public static bool CryptAcquireCertificatePrivateKey(SafeCertContextHandle pCert, Interop.Crypt32.CryptAcquireCertificatePrivateKeyFlags dwFlags, IntPtr pvParameters, out SafeNCryptKeyHandle phCryptProvOrNCryptKey, out int pdwKeySpec, out bool pfCallerFreeProvOrNCryptKey)
        {
            bool result = Interop.Crypt32.CryptAcquireCertificatePrivateKey(pCert, dwFlags, pvParameters, out phCryptProvOrNCryptKey, out Interop.Crypt32.CryptKeySpec pdwKeySpecEnum, out pfCallerFreeProvOrNCryptKey);
            pdwKeySpec = (int)pdwKeySpecEnum;
            return result;
        }
    }
}
