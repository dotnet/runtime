// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed partial class DecryptorPalWindows : DecryptorPal
    {
        public sealed unsafe override ContentInfo? TryDecrypt(
            RecipientInfo recipientInfo,
            X509Certificate2? cert,
            AsymmetricAlgorithm? privateKey,
            X509Certificate2Collection originatorCerts,
            X509Certificate2Collection extraStore,
            out Exception? exception)
        {
            Debug.Assert((cert != null) ^ (privateKey != null));

            if (privateKey != null)
            {
                RSA? key = privateKey as RSA;

                if (key == null)
                {
                    exception = new CryptographicException(SR.Cryptography_Cms_Ktri_RSARequired);
                    return null;
                }

                ContentInfo contentInfo = _hCryptMsg.GetContentInfo();
                byte[]? cek = AnyOS.ManagedPkcsPal.ManagedKeyTransPal.DecryptCekCore(
                    cert,
                    key,
                    recipientInfo.EncryptedKey,
                    recipientInfo.KeyEncryptionAlgorithm.Oid.Value,
                    recipientInfo.KeyEncryptionAlgorithm.Parameters,
                    out exception);

                // Pin CEK to prevent it from getting copied during heap compaction.
                fixed (byte* pinnedCek = cek)
                {
                    try
                    {
                        if (exception != null)
                        {
                            return null;
                        }

                        return AnyOS.ManagedPkcsPal.ManagedDecryptorPal.TryDecryptCore(
                            cek!,
                            contentInfo.ContentType.Value!,
                            contentInfo.Content,
                            _contentEncryptionAlgorithm,
                            out exception);
                    }
                    finally
                    {
                        if (cek != null)
                        {
                            Array.Clear(cek, 0, cek.Length);
                        }
                    }
                }
            }

            Debug.Assert(recipientInfo != null);
            Debug.Assert(cert != null);
            Debug.Assert(originatorCerts != null);
            Debug.Assert(extraStore != null);

            CryptKeySpec keySpec;
            exception = TryGetKeySpecForCertificate(cert, out keySpec);
            if (exception != null)
                return null;

            // .NET Framework compat: We pass false for "silent" here (thus allowing crypto providers to display UI.)
            const bool Silent = false;
            // Note: Using CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG rather than CRYPT_ACQUIRE_PREFER_NCRYPT_KEY_FLAG
            // because wrapping an NCrypt wrapper over CAPI keys unconditionally causes some legacy features
            // (such as RC4 support) to break.
            const bool PreferNCrypt = false;
            using (SafeProvOrNCryptKeyHandle? hKey = PkcsPalWindows.GetCertificatePrivateKey(cert, Silent, PreferNCrypt, out _, out exception))
            {
                if (hKey == null)
                    return null;

                RecipientInfoType type = recipientInfo.Type;
                switch (type)
                {
                    case RecipientInfoType.KeyTransport:
                        exception = TryDecryptTrans((KeyTransRecipientInfo)recipientInfo, hKey, keySpec);
                        break;

                    case RecipientInfoType.KeyAgreement:
                        exception = TryDecryptAgree((KeyAgreeRecipientInfo)recipientInfo, hKey, keySpec, originatorCerts, extraStore);
                        break;

                    default:
                        // Since only the framework can construct RecipientInfo's, we're at fault if we get here. So it's okay to assert and throw rather than
                        // returning to the caller.
                        Debug.Fail($"Unexpected RecipientInfoType: {type}");
                        throw new NotSupportedException();
                }

                if (exception != null)
                    return null;

                // If we got here, we successfully decrypted. Return the decrypted content.
                return _hCryptMsg.GetContentInfo();
            }
        }

        private static Exception? TryGetKeySpecForCertificate(X509Certificate2 cert, out CryptKeySpec keySpec)
        {
            using (SafeCertContextHandle hCertContext = cert.CreateCertContextHandle())
            {
                int cbSize = 0;

                if (Interop.Crypt32.CertGetCertificateContextProperty(
                    hCertContext,
                    CertContextPropId.CERT_NCRYPT_KEY_HANDLE_PROP_ID,
                    null,
                    ref cbSize))
                {
                    keySpec = CryptKeySpec.CERT_NCRYPT_KEY_SPEC;
                    return null;
                }

                if (!Interop.Crypt32.CertGetCertificateContextProperty(hCertContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, null, ref cbSize))
                {
                    ErrorCode errorCode = (ErrorCode)(Marshal.GetLastWin32Error());
                    keySpec = default(CryptKeySpec);
                    return errorCode.ToCryptographicException();
                }

                byte[] pData = new byte[cbSize];
                unsafe
                {
                    fixed (byte* pvData = pData)
                    {
                        if (!Interop.Crypt32.CertGetCertificateContextProperty(hCertContext, CertContextPropId.CERT_KEY_PROV_INFO_PROP_ID, pData, ref cbSize))
                        {
                            ErrorCode errorCode = (ErrorCode)(Marshal.GetLastWin32Error());
                            keySpec = default(CryptKeySpec);
                            return errorCode.ToCryptographicException();
                        }

                        CRYPT_KEY_PROV_INFO* pCryptKeyProvInfo = (CRYPT_KEY_PROV_INFO*)pvData;
                        keySpec = pCryptKeyProvInfo->dwKeySpec;
                        return null;
                    }
                }
            }
        }

        private unsafe Exception? TryDecryptTrans(KeyTransRecipientInfo recipientInfo, SafeProvOrNCryptKeyHandle hKey, CryptKeySpec keySpec)
        {
            KeyTransRecipientInfoPalWindows pal = (KeyTransRecipientInfoPalWindows)(recipientInfo.Pal);

            bool keyAddRefd = false;

            try
            {
                CMSG_CTRL_DECRYPT_PARA decryptPara;
                decryptPara.cbSize = sizeof(CMSG_CTRL_DECRYPT_PARA);
                hKey.DangerousAddRef(ref keyAddRefd);
                decryptPara.hKey = hKey.DangerousGetHandle();
                decryptPara.dwKeySpec = keySpec;
                decryptPara.dwRecipientIndex = pal.Index;

                bool success = Interop.Crypt32.CryptMsgControl(_hCryptMsg, 0, MsgControlType.CMSG_CTRL_DECRYPT, ref decryptPara);
                if (!success)
                    return Marshal.GetHRForLastWin32Error().ToCryptographicException();

                return null;
            }
            finally
            {
                if (keyAddRefd)
                {
                    hKey.DangerousRelease();
                }
            }
        }

        private Exception? TryDecryptAgree(KeyAgreeRecipientInfo keyAgreeRecipientInfo, SafeProvOrNCryptKeyHandle hKey, CryptKeySpec keySpec, X509Certificate2Collection originatorCerts, X509Certificate2Collection extraStore)
        {
            unsafe
            {
                KeyAgreeRecipientInfoPalWindows pal = (KeyAgreeRecipientInfoPalWindows)(keyAgreeRecipientInfo.Pal);
                return pal.WithCmsgCmsRecipientInfo<Exception?>(
                    delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* pKeyAgreeRecipientInfo)
                    {
                        bool keyAddRefd = false;
                        try
                        {
                            CMSG_CTRL_KEY_AGREE_DECRYPT_PARA decryptPara = default(CMSG_CTRL_KEY_AGREE_DECRYPT_PARA);
                            decryptPara.cbSize = sizeof(CMSG_CTRL_KEY_AGREE_DECRYPT_PARA);
                            hKey.DangerousAddRef(ref keyAddRefd);
                            decryptPara.hProv = hKey.DangerousGetHandle();
                            decryptPara.dwKeySpec = keySpec;
                            decryptPara.pKeyAgree = pKeyAgreeRecipientInfo;
                            decryptPara.dwRecipientIndex = pal.Index;
                            decryptPara.dwRecipientEncryptedKeyIndex = pal.SubIndex;
                            CMsgKeyAgreeOriginatorChoice originatorChoice = pKeyAgreeRecipientInfo->dwOriginatorChoice;
                            switch (originatorChoice)
                            {
                                case CMsgKeyAgreeOriginatorChoice.CMSG_KEY_AGREE_ORIGINATOR_CERT:
                                    {
                                        X509Certificate2Collection candidateCerts = new X509Certificate2Collection();
                                        candidateCerts.AddRange(PkcsHelpers.GetStoreCertificates(StoreName.AddressBook, StoreLocation.CurrentUser, openExistingOnly: true));
                                        candidateCerts.AddRange(PkcsHelpers.GetStoreCertificates(StoreName.AddressBook, StoreLocation.LocalMachine, openExistingOnly: true));
                                        candidateCerts.AddRange(originatorCerts);
                                        candidateCerts.AddRange(extraStore);
                                        SubjectIdentifier originatorId = pKeyAgreeRecipientInfo->OriginatorCertId.ToSubjectIdentifier();
                                        X509Certificate2? originatorCert = candidateCerts.TryFindMatchingCertificate(originatorId);
                                        if (originatorCert == null)
                                            return ErrorCode.CRYPT_E_NOT_FOUND.ToCryptographicException();
                                        using (SafeCertContextHandle hCertContext = originatorCert.CreateCertContextHandle())
                                        {
                                            CERT_CONTEXT* pOriginatorCertContext = hCertContext.DangerousGetCertContext();
                                            decryptPara.OriginatorPublicKey = pOriginatorCertContext->pCertInfo->SubjectPublicKeyInfo.PublicKey;

                                            // Do not factor this call out of the switch statement as leaving this "using" block will free up
                                            // native memory that decryptPara points to.
                                            return TryExecuteDecryptAgree(ref decryptPara);
                                        }
                                    }

                                case CMsgKeyAgreeOriginatorChoice.CMSG_KEY_AGREE_ORIGINATOR_PUBLIC_KEY:
                                    {
                                        decryptPara.OriginatorPublicKey = pKeyAgreeRecipientInfo->OriginatorPublicKeyInfo.PublicKey;
                                        return TryExecuteDecryptAgree(ref decryptPara);
                                    }

                                default:
                                    return new CryptographicException(SR.Format(SR.Cryptography_Cms_Invalid_Originator_Identifier_Choice, originatorChoice));
                            }
                        }
                        finally
                        {
                            if (keyAddRefd)
                            {
                                hKey.DangerousRelease();
                            }
                        }
                    });
            }
        }

        private Exception? TryExecuteDecryptAgree(ref CMSG_CTRL_KEY_AGREE_DECRYPT_PARA decryptPara)
        {
            if (!Interop.Crypt32.CryptMsgControl(_hCryptMsg, 0, MsgControlType.CMSG_CTRL_KEY_AGREE_DECRYPT, ref decryptPara))
            {
                ErrorCode errorCode = (ErrorCode)(Marshal.GetHRForLastWin32Error());
                return errorCode.ToCryptographicException();
            }
            return null;
        }
    }
}
