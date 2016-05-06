// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed partial class PkcsPalWindows : PkcsPal
    {
        internal PkcsPalWindows()
        {
        }

        public sealed override DecryptorPal Decode(byte[] encodedMessage, out int version, out ContentInfo contentInfo, out AlgorithmIdentifier contentEncryptionAlgorithm, out X509Certificate2Collection originatorCerts, out CryptographicAttributeObjectCollection unprotectedAttributes)
        {
            return DecryptorPalWindows.Decode(encodedMessage, out version, out contentInfo, out contentEncryptionAlgorithm, out originatorCerts, out unprotectedAttributes);
        }

        public sealed override byte[] EncodeOctetString(byte[] octets)
        {
            unsafe
            {
                fixed (byte* pOctets = octets)
                {
                    DATA_BLOB blob = new DATA_BLOB((IntPtr)pOctets, (uint)(octets.Length));
                    return Interop.Crypt32.CryptEncodeObjectToByteArray(CryptDecodeObjectStructType.X509_OCTET_STRING, &blob);
                }
            }
        }

        public sealed override byte[] DecodeOctetString(byte[] encodedOctets)
        {
            using (SafeHandle sh = Interop.Crypt32.CryptDecodeObjectToMemory(CryptDecodeObjectStructType.X509_OCTET_STRING, encodedOctets))
            {
                unsafe
                {
                    DATA_BLOB blob = *(DATA_BLOB*)(sh.DangerousGetHandle());
                    return blob.ToByteArray();
                }
            }
        }

        public sealed override byte[] EncodeUtcTime(DateTime utcTime)
        {
            long ft = utcTime.ToFileTimeUtc();
            unsafe
            {
                return Interop.Crypt32.CryptEncodeObjectToByteArray(CryptDecodeObjectStructType.PKCS_UTC_TIME, &ft);
            }
        }

        public sealed override DateTime DecodeUtcTime(byte[] encodedUtcTime)
        {
            long signingTime = 0;
            unsafe
            {
                fixed (byte* pEncodedUtcTime = encodedUtcTime)
                {
                    int cbSize = sizeof(long);
                    if (!Interop.Crypt32.CryptDecodeObject(CryptDecodeObjectStructType.PKCS_UTC_TIME, (IntPtr)pEncodedUtcTime, encodedUtcTime.Length, &signingTime, ref cbSize))
                        throw Marshal.GetLastWin32Error().ToCryptographicException();
                }
            }
            return DateTime.FromFileTimeUtc(signingTime);
        }

        public sealed override string DecodeOid(byte[] encodedOid)
        {
            using (SafeHandle sh = Interop.Crypt32.CryptDecodeObjectToMemory(CryptDecodeObjectStructType.X509_OBJECT_IDENTIFIER, encodedOid))
            {
                unsafe
                {
                    IntPtr pOidValue = *(IntPtr*)(sh.DangerousGetHandle());
                    string contentType = pOidValue.ToStringAnsi();
                    return contentType;
                }
            }
        }

        public sealed override Oid GetEncodedMessageType(byte[] encodedMessage)
        {
            using (SafeCryptMsgHandle hCryptMsg = Interop.Crypt32.CryptMsgOpenToDecode(MsgEncodingType.All, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
            {
                if (hCryptMsg == null || hCryptMsg.IsInvalid)
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                if (!Interop.Crypt32.CryptMsgUpdate(hCryptMsg, encodedMessage, encodedMessage.Length, fFinal: true))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                int msgTypeAsInt;
                int cbSize = sizeof(int);
                if (!Interop.Crypt32.CryptMsgGetParam(hCryptMsg, CryptMsgParamType.CMSG_TYPE_PARAM, 0, out msgTypeAsInt, ref cbSize))
                    throw Marshal.GetLastWin32Error().ToCryptographicException();

                CryptMsgType msgType = (CryptMsgType)msgTypeAsInt;

                switch (msgType)
                {
                    case CryptMsgType.CMSG_DATA:
                        return Oid.FromOidValue(Oids.Pkcs7Data, OidGroup.ExtensionOrAttribute);

                    case CryptMsgType.CMSG_SIGNED:
                        return Oid.FromOidValue(Oids.Pkcs7Signed, OidGroup.ExtensionOrAttribute);

                    case CryptMsgType.CMSG_ENVELOPED:
                        return Oid.FromOidValue(Oids.Pkcs7Enveloped, OidGroup.ExtensionOrAttribute);

                    case CryptMsgType.CMSG_SIGNED_AND_ENVELOPED:
                        return Oid.FromOidValue(Oids.Pkcs7SignedEnveloped, OidGroup.ExtensionOrAttribute);

                    case CryptMsgType.CMSG_HASHED:
                        return Oid.FromOidValue(Oids.Pkcs7Hashed, OidGroup.ExtensionOrAttribute);

                    case CryptMsgType.CMSG_ENCRYPTED:
                        return Oid.FromOidValue(Oids.Pkcs7Encrypted, OidGroup.ExtensionOrAttribute);

                    default:
                        throw ErrorCode.CRYPT_E_INVALID_MSG_TYPE.ToCryptographicException();
                }
            }
        }

        public sealed override void AddCertsFromStoreForDecryption(X509Certificate2Collection certs)
        {
            certs.AddRange(Helpers.GetStoreCertificates(StoreName.My, StoreLocation.CurrentUser, openExistingOnly: true));
            certs.AddRange(Helpers.GetStoreCertificates(StoreName.My, StoreLocation.LocalMachine, openExistingOnly: true));
        }

        public sealed override Exception CreateRecipientsNotFoundException()
        {
            return ErrorCode.CRYPT_E_RECIPIENT_NOT_FOUND.ToCryptographicException();
        }

        public sealed override Exception CreateRecipientInfosAfterEncryptException()
        {
            return ErrorCode.CRYPT_E_INVALID_MSG_TYPE.ToCryptographicException();
        }

        public sealed override Exception CreateDecryptAfterEncryptException()
        {
            return ErrorCode.CRYPT_E_INVALID_MSG_TYPE.ToCryptographicException();
        }

        public sealed override Exception CreateDecryptTwiceException()
        {
            return ErrorCode.CRYPT_E_INVALID_MSG_TYPE.ToCryptographicException();
        }

        public sealed override byte[] GetSubjectKeyIdentifier(X509Certificate2 certificate)
        {
            using (SafeCertContextHandle hCertContext = certificate.CreateCertContextHandle())
            {
                byte[] ski = hCertContext.GetSubjectKeyIdentifer();
                return ski;
            }
        }
    }
}
