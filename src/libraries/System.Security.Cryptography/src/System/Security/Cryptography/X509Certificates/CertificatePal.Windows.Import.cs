// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.X509Certificates
{
    internal sealed partial class CertificatePal
    {
        internal static partial ICertificatePal FromBlob(ReadOnlySpan<byte> rawData, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(rawData, null, password, keyStorageFlags);
        }

        internal static partial ICertificatePal FromFile(string fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(ReadOnlySpan<byte>.Empty, fileName, password, keyStorageFlags);
        }

        private static CertificatePal FromBlobOrFile(ReadOnlySpan<byte> rawData, string? fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(!rawData.IsEmpty || fileName != null);
            Debug.Assert(password != null);

            bool loadFromFile = (fileName != null);
            bool deleteKeyContainer = false;

            Interop.Crypt32.ContentType contentType;
            SafeCertStoreHandle? hCertStore = null;
            SafeCryptMsgHandle? hCryptMsg = null;
            SafeCertContextHandle? pCertContext = null;

            try
            {
                unsafe
                {
                    fixed (byte* pRawData = rawData)
                    {
                        fixed (char* pFileName = fileName)
                        {
                            Interop.Crypt32.DATA_BLOB certBlob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pRawData), (uint)(loadFromFile ? 0 : rawData.Length));

                            Interop.Crypt32.CertQueryObjectType objectType = loadFromFile ? Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_FILE : Interop.Crypt32.CertQueryObjectType.CERT_QUERY_OBJECT_BLOB;
                            void* pvObject = loadFromFile ? (void*)pFileName : (void*)&certBlob;

                            bool success = Interop.Crypt32.CryptQueryObject(
                                objectType,
                                pvObject,
                                X509ExpectedContentTypeFlags,
                                X509ExpectedFormatTypeFlags,
                                0,
                                out _,
                                out contentType,
                                out _,
                                out hCertStore,
                                out hCryptMsg,
                                out pCertContext);

                            if (!success)
                            {
                                int hr = Marshal.GetHRForLastWin32Error();
                                throw hr.ToCryptographicException();
                            }
                        }
                    }

                    if (contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED || contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED)
                    {
                        pCertContext?.Dispose();
                        pCertContext = GetSignerInPKCS7Store(hCertStore, hCryptMsg);
                    }
                    else if (contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX)
                    {
                        try
                        {
                            Pkcs12LoaderLimits limits = X509Certificate.GetPkcs12Limits(loadFromFile, password);

                            if (loadFromFile)
                            {
                                Debug.Assert(fileName is not null);

                                return (CertificatePal)X509CertificateLoader.LoadPkcs12PalFromFile(
                                    fileName,
                                    password.DangerousGetSpan(),
                                    keyStorageFlags,
                                    limits);
                            }
                            else
                            {
                                return (CertificatePal)X509CertificateLoader.LoadPkcs12Pal(
                                    rawData,
                                    password.DangerousGetSpan(),
                                    keyStorageFlags,
                                    limits);
                            }
                        }
                        catch (Pkcs12LoadLimitExceededException e)
                        {
                            throw new CryptographicException(
                                SR.Cryptography_X509_PfxWithoutPassword_MaxAllowedIterationsExceeded,
                                e);
                        }
                    }

                    CertificatePal pal = new CertificatePal(pCertContext, deleteKeyContainer);
                    pCertContext = null;
                    return pal;
                }
            }
            finally
            {
                hCertStore?.Dispose();
                hCryptMsg?.Dispose();
                pCertContext?.Dispose();
            }
        }

        private static unsafe SafeCertContextHandle GetSignerInPKCS7Store(SafeCertStoreHandle hCertStore, SafeCryptMsgHandle hCryptMsg)
        {
            // make sure that there is at least one signer of the certificate store
            int dwSigners;
            int cbSigners = sizeof(int);
            if (!Interop.Crypt32.CryptMsgGetParam(hCryptMsg, Interop.Crypt32.CryptMsgParamType.CMSG_SIGNER_COUNT_PARAM, 0, out dwSigners, ref cbSigners))
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();
            if (dwSigners == 0)
                throw ErrorCode.CRYPT_E_SIGNER_NOT_FOUND.ToCryptographicException();

            // get the first signer from the store, and use that as the loaded certificate
            int cbData = 0;
            if (!Interop.Crypt32.CryptMsgGetParam(hCryptMsg, Interop.Crypt32.CryptMsgParamType.CMSG_SIGNER_INFO_PARAM, 0, default(byte*), ref cbData))
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

            fixed (byte* pCmsgSignerBytes = new byte[cbData])
            {
                if (!Interop.Crypt32.CryptMsgGetParam(hCryptMsg, Interop.Crypt32.CryptMsgParamType.CMSG_SIGNER_INFO_PARAM, 0, pCmsgSignerBytes, ref cbData))
                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();

                CMSG_SIGNER_INFO_Partial* pCmsgSignerInfo = (CMSG_SIGNER_INFO_Partial*)pCmsgSignerBytes;

                Interop.Crypt32.CERT_INFO certInfo = default(Interop.Crypt32.CERT_INFO);
                certInfo.Issuer.cbData = pCmsgSignerInfo->Issuer.cbData;
                certInfo.Issuer.pbData = pCmsgSignerInfo->Issuer.pbData;
                certInfo.SerialNumber.cbData = pCmsgSignerInfo->SerialNumber.cbData;
                certInfo.SerialNumber.pbData = pCmsgSignerInfo->SerialNumber.pbData;

                SafeCertContextHandle? pCertContext = null;
                if (!Interop.crypt32.CertFindCertificateInStore(hCertStore, Interop.Crypt32.CertFindType.CERT_FIND_SUBJECT_CERT, &certInfo, ref pCertContext))
                {
                    Exception e = Marshal.GetHRForLastWin32Error().ToCryptographicException();
                    pCertContext.Dispose();
                    throw e;
                }

                return pCertContext;
            }
        }

        private const Interop.Crypt32.ExpectedContentTypeFlags X509ExpectedContentTypeFlags =
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
            Interop.Crypt32.ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX;

        private const Interop.Crypt32.ExpectedFormatTypeFlags X509ExpectedFormatTypeFlags = Interop.Crypt32.ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL;
    }
}
