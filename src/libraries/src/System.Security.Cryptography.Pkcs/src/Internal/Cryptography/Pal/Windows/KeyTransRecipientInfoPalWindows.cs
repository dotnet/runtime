// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed class KeyTransRecipientInfoPalWindows : KeyTransRecipientInfoPal
    {
        internal KeyTransRecipientInfoPalWindows(SafeHandle pCmsgCmsRecipientInfoMemory, int index)
            : base()
        {
            _pCmsgCmsRecipientInfoMemory = pCmsgCmsRecipientInfoMemory;
            Index = index;
        }

        public sealed override int Version
        {
            get
            {
                unsafe
                {
                    return WithCmsgCmsRecipientInfo(
                        delegate (CMSG_KEY_TRANS_RECIPIENT_INFO* recipient)
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
                    return WithCmsgCmsRecipientInfo(
                        delegate (CMSG_KEY_TRANS_RECIPIENT_INFO* recipient)
                        {
                            SubjectIdentifier subjectIdentifier = recipient->RecipientId.ToSubjectIdentifier();
                            return subjectIdentifier;
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
                    return WithCmsgCmsRecipientInfo(
                        delegate (CMSG_KEY_TRANS_RECIPIENT_INFO* recipient)
                        {
                            AlgorithmIdentifier algorithmIdentifier = recipient->KeyEncryptionAlgorithm.ToAlgorithmIdentifier();
                            return algorithmIdentifier;
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
                    return WithCmsgCmsRecipientInfo(
                        delegate (CMSG_KEY_TRANS_RECIPIENT_INFO* recipient)
                        {
                            return recipient->EncryptedKey.ToByteArray();
                        });
                }
            }
        }

        internal int Index { get; }

        // Provides access to the native CMSG_KEY_TRANS_RECIPIENT_INFO* structure. This helper is structured as taking a delegate to 
        // help avoid the easy trap of forgetting to prevent the underlying memory block from being GC'd early.
        private T WithCmsgCmsRecipientInfo<T>(KeyTransReceiver<T> receiver)
        {
            unsafe
            {
                CMSG_CMS_RECIPIENT_INFO* pRecipientInfo = (CMSG_CMS_RECIPIENT_INFO*)(_pCmsgCmsRecipientInfoMemory.DangerousGetHandle());
                CMSG_KEY_TRANS_RECIPIENT_INFO* pKeyTrans = pRecipientInfo->KeyTrans;
                T value = receiver(pKeyTrans);
                GC.KeepAlive(_pCmsgCmsRecipientInfoMemory);
                return value;
            }
        }

        private unsafe delegate T KeyTransReceiver<T>(CMSG_KEY_TRANS_RECIPIENT_INFO* recipient);

        // This is the backing store for the CMSG_CMS_RECIPIENT_INFO* structure for this RecipientInfo. CMSG_CMS_RECIPIENT_INFO is full of interior
        // pointers so we store in a native heap block to keep it pinned. 
        private readonly SafeHandle _pCmsgCmsRecipientInfoMemory;
    }
}

