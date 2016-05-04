// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed class KeyAgreeRecipientInfoPalWindows : KeyAgreeRecipientInfoPal
    {
        internal KeyAgreeRecipientInfoPalWindows(SafeHandle pCmsgCmsRecipientInfoMemory, int index, int subIndex)
            : base()
        {
            _pCmsgCmsRecipientInfoMemory = pCmsgCmsRecipientInfoMemory;
            Index = index;
            SubIndex = subIndex;
        }

        public sealed override int Version
        {
            get
            {
                unsafe
                {
                    return WithCmsgCmsRecipientInfo<int>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            return recipient->dwVersion;
                        });
                }
            }
        }

        public sealed override SubjectIdentifier RecipientIdentifier
        {
            get
            {
                unsafe
                {
                    return WithCmsgCmsRecipientInfo<SubjectIdentifier>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            CMSG_RECIPIENT_ENCRYPTED_KEY_INFO* pEncryptedKeyInfo = recipient->rgpRecipientEncryptedKeys[SubIndex];
                            return pEncryptedKeyInfo->RecipientId.ToSubjectIdentifier();
                        });
                }
            }
        }

        public sealed override AlgorithmIdentifier KeyEncryptionAlgorithm
        {
            get
            {
                unsafe
                {
                    return WithCmsgCmsRecipientInfo<AlgorithmIdentifier>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            return recipient->KeyEncryptionAlgorithm.ToAlgorithmIdentifier();
                        });
                }
            }
        }

        public sealed override byte[] EncryptedKey
        {
            get
            {
                unsafe
                {
                    return WithCmsgCmsRecipientInfo<byte[]>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            CMSG_RECIPIENT_ENCRYPTED_KEY_INFO* pEncryptedKeyInfo = recipient->rgpRecipientEncryptedKeys[SubIndex];
                            return pEncryptedKeyInfo->EncryptedKey.ToByteArray();
                        });
                }
            }
        }

        public sealed override SubjectIdentifierOrKey OriginatorIdentifierOrKey
        {
            get
            {
                unsafe
                {
                    return WithCmsgCmsRecipientInfo<SubjectIdentifierOrKey>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            CMsgKeyAgreeOriginatorChoice originatorChoice = recipient->dwOriginatorChoice;
                            switch (originatorChoice)
                            {
                                case CMsgKeyAgreeOriginatorChoice.CMSG_KEY_AGREE_ORIGINATOR_CERT:
                                    return recipient->OriginatorCertId.ToSubjectIdentifierOrKey();

                                case CMsgKeyAgreeOriginatorChoice.CMSG_KEY_AGREE_ORIGINATOR_PUBLIC_KEY:
                                    return recipient->OriginatorPublicKeyInfo.ToSubjectIdentifierOrKey();

                                default:
                                    throw new CryptographicException(SR.Format(SR.Cryptography_Cms_Invalid_Originator_Identifier_Choice, originatorChoice));
                            }
                        });
                }
            }
        }

        public sealed override DateTime Date
        {
            get
            {
                if (RecipientIdentifier.Type != SubjectIdentifierType.SubjectKeyIdentifier)
                    throw new InvalidOperationException(SR.Cryptography_Cms_Key_Agree_Date_Not_Available);

                unsafe
                {
                    return WithCmsgCmsRecipientInfo<DateTime>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            CMSG_RECIPIENT_ENCRYPTED_KEY_INFO* pEncryptedKeyInfo = recipient->rgpRecipientEncryptedKeys[SubIndex];
                            long date = (((long)(uint)(pEncryptedKeyInfo->Date.dwHighDateTime)) << 32) | ((long)(uint)(pEncryptedKeyInfo->Date.dwLowDateTime));
                            DateTime dateTime = DateTime.FromFileTimeUtc(date);
                            return dateTime;
                        });
                }
            }
        }

        public sealed override CryptographicAttributeObject OtherKeyAttribute
        {
            get
            {
                if (RecipientIdentifier.Type != SubjectIdentifierType.SubjectKeyIdentifier)
                    throw new InvalidOperationException(SR.Cryptography_Cms_Key_Agree_Date_Not_Available);

                unsafe
                {
                    return WithCmsgCmsRecipientInfo<CryptographicAttributeObject>(
                        delegate (CMSG_KEY_AGREE_RECIPIENT_INFO* recipient)
                        {
                            CMSG_RECIPIENT_ENCRYPTED_KEY_INFO* pEncryptedKeyInfo = recipient->rgpRecipientEncryptedKeys[SubIndex];
                            CRYPT_ATTRIBUTE_TYPE_VALUE* pCryptAttributeTypeValue = pEncryptedKeyInfo->pOtherAttr;
                            if (pCryptAttributeTypeValue == null)
                                return null;

                            string oidValue = pCryptAttributeTypeValue->pszObjId.ToStringAnsi();
                            Oid oid = Oid.FromOidValue(oidValue, OidGroup.All);
                            byte[] rawData = pCryptAttributeTypeValue->Value.ToByteArray();
                            Pkcs9AttributeObject pkcs9AttributeObject = new Pkcs9AttributeObject(oid, rawData);
                            AsnEncodedDataCollection values = new AsnEncodedDataCollection(pkcs9AttributeObject);
                            return new CryptographicAttributeObject(oid, values);
                        });
                }
            }
        }

        internal int Index { get; }
        internal int SubIndex { get; }

        // Provides access to the native CMSG_KEY_TRANS_RECIPIENT_INFO* structure. This helper is structured as taking a delegate to 
        // help avoid the easy trap of forgetting to prevent the underlying memory block from being GC'd early.
        internal T WithCmsgCmsRecipientInfo<T>(KeyAgreeReceiver<T> receiver)
        {
            unsafe
            {
                CMSG_CMS_RECIPIENT_INFO* pRecipientInfo = (CMSG_CMS_RECIPIENT_INFO*)(_pCmsgCmsRecipientInfoMemory.DangerousGetHandle());
                CMSG_KEY_AGREE_RECIPIENT_INFO* pKeyAgree = pRecipientInfo->KeyAgree;
                T value = receiver(pKeyAgree);
                GC.KeepAlive(_pCmsgCmsRecipientInfoMemory);
                return value;
            }
        }

        internal unsafe delegate T KeyAgreeReceiver<T>(CMSG_KEY_AGREE_RECIPIENT_INFO* recipient);

        // This is the backing store for the CMSG_CMS_RECIPIENT_INFO* structure for this RecipientInfo. CMSG_CMS_RECIPIENT_INFO is full of interior
        // pointers so we store in a native heap block to keep it pinned. 
        private readonly SafeHandle _pCmsgCmsRecipientInfoMemory;
    }
}

