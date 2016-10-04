// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

using Internal.Cryptography.Pal.Native;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class CertificatePal : IDisposable, ICertificatePal
    {
        public static ICertificatePal FromBlob(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(rawData, null, password, keyStorageFlags);
        }

        public static ICertificatePal FromFile(string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            return FromBlobOrFile(null, fileName, password, keyStorageFlags);
        }

        private static ICertificatePal FromBlobOrFile(byte[] rawData, string fileName, string password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(rawData != null || fileName != null);

            bool loadFromFile = (fileName != null);

            PfxCertStoreFlags pfxCertStoreFlags = MapKeyStorageFlags(keyStorageFlags);
            bool persistKeySet = (0 != (keyStorageFlags & X509KeyStorageFlags.PersistKeySet));

            CertEncodingType msgAndCertEncodingType;
            ContentType contentType;
            FormatType formatType;
            SafeCertStoreHandle hCertStore = null;
            SafeCryptMsgHandle hCryptMsg = null;
            SafeCertContextHandle pCertContext = null;

            try
            {
                unsafe
                {
                    fixed (byte* pRawData = rawData)
                    {
                        fixed (char* pFileName = fileName)
                        {
                            CRYPTOAPI_BLOB certBlob = new CRYPTOAPI_BLOB(loadFromFile ? 0 : rawData.Length, pRawData);

                            CertQueryObjectType objectType = loadFromFile ? CertQueryObjectType.CERT_QUERY_OBJECT_FILE : CertQueryObjectType.CERT_QUERY_OBJECT_BLOB;
                            void* pvObject = loadFromFile ? (void*)pFileName : (void*)&certBlob;

                            bool success = Interop.crypt32.CryptQueryObject(
                                objectType,
                                pvObject,
                                X509ExpectedContentTypeFlags,
                                X509ExpectedFormatTypeFlags,
                                0,
                                out msgAndCertEncodingType,
                                out contentType,
                                out formatType,
                                out hCertStore,
                                out hCryptMsg,
                                out pCertContext
                                    );
                            if (!success)
                            {
                                int hr = Marshal.GetHRForLastWin32Error();
                                throw hr.ToCryptographicException();
                            }
                        }
                    }

                    if (contentType == ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED || contentType == ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED)
                    {
                        pCertContext = GetSignerInPKCS7Store(hCertStore, hCryptMsg);
                    }
                    else if (contentType == ContentType.CERT_QUERY_CONTENT_PFX)
                    {
                        if (loadFromFile)
                            rawData = File.ReadAllBytes(fileName);
                        pCertContext = FilterPFXStore(rawData, password, pfxCertStoreFlags);
                    }

                    CertificatePal pal = new CertificatePal(pCertContext, deleteKeyContainer: !persistKeySet);
                    pCertContext = null;
                    return pal;
                }
            }
            finally
            {
                if (hCertStore != null)
                    hCertStore.Dispose();
                if (hCryptMsg != null)
                    hCryptMsg.Dispose();
                if (pCertContext != null)
                    pCertContext.Dispose();
            }
        }

        private static SafeCertContextHandle GetSignerInPKCS7Store(SafeCertStoreHandle hCertStore, SafeCryptMsgHandle hCryptMsg)
        {
            // make sure that there is at least one signer of the certificate store
            int dwSigners;
            int cbSigners = sizeof(int);
            if (!Interop.crypt32.CryptMsgGetParam(hCryptMsg, CryptMessageParameterType.CMSG_SIGNER_COUNT_PARAM, 0, out dwSigners, ref cbSigners))
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;
            if (dwSigners == 0)
                throw ErrorCode.CRYPT_E_SIGNER_NOT_FOUND.ToCryptographicException();

            // get the first signer from the store, and use that as the loaded certificate
            int cbData = 0;
            if (!Interop.crypt32.CryptMsgGetParam(hCryptMsg, CryptMessageParameterType.CMSG_SIGNER_INFO_PARAM, 0, null, ref cbData))
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

            byte[] cmsgSignerBytes = new byte[cbData];
            if (!Interop.crypt32.CryptMsgGetParam(hCryptMsg, CryptMessageParameterType.CMSG_SIGNER_INFO_PARAM, 0, cmsgSignerBytes, ref cbData))
                throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

            CERT_INFO certInfo = default(CERT_INFO);
            unsafe
            {
                fixed (byte* pCmsgSignerBytes = cmsgSignerBytes)
                {
                    CMSG_SIGNER_INFO_Partial* pCmsgSignerInfo = (CMSG_SIGNER_INFO_Partial*)pCmsgSignerBytes;
                    certInfo.Issuer.cbData = pCmsgSignerInfo->Issuer.cbData;
                    certInfo.Issuer.pbData = pCmsgSignerInfo->Issuer.pbData;
                    certInfo.SerialNumber.cbData = pCmsgSignerInfo->SerialNumber.cbData;
                    certInfo.SerialNumber.pbData = pCmsgSignerInfo->SerialNumber.pbData;
                }

                SafeCertContextHandle pCertContext = null;
                if (!Interop.crypt32.CertFindCertificateInStore(hCertStore, CertFindType.CERT_FIND_SUBJECT_CERT, &certInfo, ref pCertContext))
                    throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;

                return pCertContext;
            }
        }

        private static SafeCertContextHandle FilterPFXStore(byte[] rawData, string password, PfxCertStoreFlags pfxCertStoreFlags)
        {
            SafeCertStoreHandle hStore;
            unsafe
            {
                fixed (byte* pbRawData = rawData)
                {
                    CRYPTOAPI_BLOB certBlob = new CRYPTOAPI_BLOB(rawData.Length, pbRawData);
                    hStore = Interop.crypt32.PFXImportCertStore(ref certBlob, password, pfxCertStoreFlags);
                    if (hStore.IsInvalid)
                        throw Marshal.GetHRForLastWin32Error().ToCryptographicException();;
                }
            }

            try
            {
                // Find the first cert with private key. If none, then simply take the very first cert. Along the way, delete the keycontainers
                // of any cert we don't accept.
                SafeCertContextHandle pCertContext = SafeCertContextHandle.InvalidHandle;
                SafeCertContextHandle pEnumContext = null;
                while (Interop.crypt32.CertEnumCertificatesInStore(hStore, ref pEnumContext))
                {
                    if (pEnumContext.ContainsPrivateKey)
                    {
                        if ((!pCertContext.IsInvalid) && pCertContext.ContainsPrivateKey)
                        {
                            // We already found our chosen one. Free up this one's key and move on.

                            // If this one has a persisted private key, clean up the key file.
                            // If it was an ephemeral private key no action is required.
                            if (pEnumContext.HasPersistedPrivateKey)
                            {
                                SafeCertContextHandleWithKeyContainerDeletion.DeleteKeyContainer(pEnumContext);
                            }
                        }
                        else
                        {
                            // Found our first cert that has a private key. Set him up as our chosen one but keep iterating
                            // as we need to free up the keys of any remaining certs.
                            pCertContext.Dispose();
                            pCertContext = pEnumContext.Duplicate();
                        }
                    }
                    else
                    {
                        if (pCertContext.IsInvalid)
                            pCertContext = pEnumContext.Duplicate(); // Doesn't have a private key but hang on to it anyway in case we don't find any certs with a private key.
                    }
                }

                if (pCertContext.IsInvalid)
                {
                    // For compat, setting "hr" to ERROR_INVALID_PARAMETER even though ERROR_INVALID_PARAMETER is not actually an HRESULT.
                    throw ErrorCode.ERROR_INVALID_PARAMETER.ToCryptographicException();
                }

                return pCertContext;
            }
            finally
            {
                hStore.Dispose();
            }
        }

        private static PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            if ((keyStorageFlags & X509Certificate.KeyStorageFlagsAll) != keyStorageFlags)
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(keyStorageFlags));

            PfxCertStoreFlags pfxCertStoreFlags = 0;
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_EXPORTABLE;
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                pfxCertStoreFlags |= PfxCertStoreFlags.CRYPT_USER_PROTECTED;

            // If a user is asking for an Ephemeral key they should be willing to test their code to find out
            // that it will no longer import into CAPI. This solves problems of legacy CSPs being
            // difficult to do SHA-2 RSA signatures with, simplifies the story for UWP, and reduces the
            // complexity of pointer interpretation.
            if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                pfxCertStoreFlags |= PfxCertStoreFlags.PKCS12_NO_PERSIST_KEY | PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP;

            // In the full .NET Framework loading a PFX then adding the key to the Windows Certificate Store would
            // enable a native application compiled against CAPI to find that private key and interoperate with it.
            //
            // For CoreFX this behavior is being retained.
            //
            // For .NET Native (UWP) the API used to delete the private key (if it wasn't added to a store) is not
            // allowed to be called if the key is stored in CAPI.  So for UWP force the key to be stored in the
            // CNG Key Storage Provider, then deleting the key with CngKey.Delete will clean up the file on disk, too.
#if NETNATIVE
            pfxCertStoreFlags |= PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP;
#endif

            return pfxCertStoreFlags;
        }

        private const ExpectedContentTypeFlags X509ExpectedContentTypeFlags =
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_CERT |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_SERIALIZED_CERT |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED |
            ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PFX;

        private const ExpectedFormatTypeFlags X509ExpectedFormatTypeFlags = ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_ALL;
    }
}
