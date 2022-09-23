// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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

        private static ICertificatePal FromBlobOrFile(ReadOnlySpan<byte> rawData, string? fileName, SafePasswordHandle password, X509KeyStorageFlags keyStorageFlags)
        {
            Debug.Assert(!rawData.IsEmpty || fileName != null);
            Debug.Assert(password != null);

            bool loadFromFile = (fileName != null);

            Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags = MapKeyStorageFlags(keyStorageFlags);
            bool deleteKeyContainer = false;

            Interop.Crypt32.CertEncodingType msgAndCertEncodingType;
            Interop.Crypt32.ContentType contentType;
            Interop.Crypt32.FormatType formatType;
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

                    if (contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED || contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED)
                    {
                        pCertContext?.Dispose();
                        pCertContext = GetSignerInPKCS7Store(hCertStore, hCryptMsg);
                    }
                    else if (contentType == Interop.Crypt32.ContentType.CERT_QUERY_CONTENT_PFX)
                    {
                        if (loadFromFile)
                        {
                            rawData = File.ReadAllBytes(fileName!);
                        }

                        pCertContext?.Dispose();
                        pCertContext = FilterPFXStore(rawData, password, pfxCertStoreFlags);

                        // If PersistKeySet is set we don't delete the key, so that it persists.
                        // If EphemeralKeySet is set we don't delete the key, because there's no file, so it's a wasteful call.
                        const X509KeyStorageFlags DeleteUnless =
                            X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.EphemeralKeySet;

                        deleteKeyContainer = ((keyStorageFlags & DeleteUnless) == 0);
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

        private static SafeCertContextHandle FilterPFXStore(
            ReadOnlySpan<byte> rawData,
            SafePasswordHandle password,
            Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags)
        {
            SafeCertStoreHandle hStore;
            unsafe
            {
                fixed (byte* pbRawData = rawData)
                {
                    Interop.Crypt32.DATA_BLOB certBlob = new Interop.Crypt32.DATA_BLOB(new IntPtr(pbRawData), (uint)rawData.Length);
                    hStore = Interop.Crypt32.PFXImportCertStore(ref certBlob, password, pfxCertStoreFlags);
                    if (hStore.IsInvalid)
                    {
                        Exception e = Marshal.GetHRForLastWin32Error().ToCryptographicException();
                        hStore.Dispose();
                        throw e;
                    }
                }
            }

            try
            {
                // Find the first cert with private key. If none, then simply take the very first cert. Along the way, delete the keycontainers
                // of any cert we don't accept.
                SafeCertContextHandle pCertContext = SafeCertContextHandle.InvalidHandle;
                SafeCertContextHandle? pEnumContext = null;
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
                            // Found our first cert that has a private key. Set it up as our chosen one but keep iterating
                            // as we need to free up the keys of any remaining certs.
                            pCertContext.Dispose();
                            pCertContext = pEnumContext.Duplicate();
                        }
                    }
                    else
                    {
                        if (pCertContext.IsInvalid)
                        {
                            // Doesn't have a private key but hang on to it anyway in case we don't find any certs with a private key.
                            pCertContext.Dispose();
                            pCertContext = pEnumContext.Duplicate();
                        }
                    }
                }

                if (pCertContext.IsInvalid)
                {
                    pCertContext.Dispose();
                    throw new CryptographicException(SR.Cryptography_Pfx_NoCertificates);
                }

                return pCertContext;
            }
            finally
            {
                hStore.Dispose();
            }
        }

        private static Interop.Crypt32.PfxCertStoreFlags MapKeyStorageFlags(X509KeyStorageFlags keyStorageFlags)
        {
            if ((keyStorageFlags & X509Certificate.KeyStorageFlagsAll) != keyStorageFlags)
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(keyStorageFlags));

            Interop.Crypt32.PfxCertStoreFlags pfxCertStoreFlags = 0;
            if ((keyStorageFlags & X509KeyStorageFlags.UserKeySet) == X509KeyStorageFlags.UserKeySet)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_KEYSET;
            else if ((keyStorageFlags & X509KeyStorageFlags.MachineKeySet) == X509KeyStorageFlags.MachineKeySet)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_MACHINE_KEYSET;

            if ((keyStorageFlags & X509KeyStorageFlags.Exportable) == X509KeyStorageFlags.Exportable)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_EXPORTABLE;
            if ((keyStorageFlags & X509KeyStorageFlags.UserProtected) == X509KeyStorageFlags.UserProtected)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.CRYPT_USER_PROTECTED;

            // If a user is asking for an Ephemeral key they should be willing to test their code to find out
            // that it will no longer import into CAPI. This solves problems of legacy CSPs being
            // difficult to do SHA-2 RSA signatures with, simplifies the story for UWP, and reduces the
            // complexity of pointer interpretation.
            if ((keyStorageFlags & X509KeyStorageFlags.EphemeralKeySet) == X509KeyStorageFlags.EphemeralKeySet)
                pfxCertStoreFlags |= Interop.Crypt32.PfxCertStoreFlags.PKCS12_NO_PERSIST_KEY | Interop.Crypt32.PfxCertStoreFlags.PKCS12_ALWAYS_CNG_KSP;

            // In .NET Framework loading a PFX then adding the key to the Windows Certificate Store would
            // enable a native application compiled against CAPI to find that private key and interoperate with it.
            //
            // For .NET Core this behavior is being retained.

            return pfxCertStoreFlags;
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
