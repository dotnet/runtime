// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Pkcs;

using Microsoft.Win32.SafeHandles;

using static Interop.Crypt32;

namespace Internal.Cryptography.Pal.Windows
{
    internal sealed partial class DecryptorPalWindows : DecryptorPal
    {
        private static RecipientInfoCollection CreateRecipientInfos(SafeCryptMsgHandle hCryptMsg)
        {
            int numRecipients;
            int cbRecipientsCount = sizeof(int);
            if (!Interop.Crypt32.CryptMsgGetParam(hCryptMsg, CryptMsgParamType.CMSG_CMS_RECIPIENT_COUNT_PARAM, 0, out numRecipients, ref cbRecipientsCount))
                throw Marshal.GetLastPInvokeError().ToCryptographicException();

            List<RecipientInfo> recipientInfos = new List<RecipientInfo>(numRecipients);
            for (int index = 0; index < numRecipients; index++)
            {
                // Do not dispose this safehandle. The RecipientInfoPal objects we create hold on to these and they get freed through garbage collection.
                SafeHandle pCmsgCmsRecipientInfoMemory = hCryptMsg.GetMsgParamAsMemory(CryptMsgParamType.CMSG_CMS_RECIPIENT_INFO_PARAM, index);
                IEnumerable<RecipientInfo> recipientInfosForThisIndex = ToRecipientInfosForThisIndex(pCmsgCmsRecipientInfoMemory, index);
                recipientInfos.AddRange(recipientInfosForThisIndex);
            }

            return new RecipientInfoCollection(recipientInfos);
        }

        private static IEnumerable<RecipientInfo> ToRecipientInfosForThisIndex(SafeHandle pCmsgCmsRecipientInfoMemory, int index)
        {
            bool mustRelease = false;
            pCmsgCmsRecipientInfoMemory.DangerousAddRef(ref mustRelease);
            try
            {
                unsafe
                {
                    CMSG_CMS_RECIPIENT_INFO* pCMsgCmsRecipientInfo = (CMSG_CMS_RECIPIENT_INFO*)(pCmsgCmsRecipientInfoMemory.DangerousGetHandle());
                    switch (pCMsgCmsRecipientInfo->dwRecipientChoice)
                    {
                        case CMsgCmsRecipientChoice.CMSG_KEY_TRANS_RECIPIENT:
                            return new KeyTransRecipientInfo[] { new KeyTransRecipientInfo(new KeyTransRecipientInfoPalWindows(pCmsgCmsRecipientInfoMemory, index)) };

                        case CMsgCmsRecipientChoice.CMSG_KEY_AGREE_RECIPIENT:
                            {
                                CMSG_KEY_AGREE_RECIPIENT_INFO* pCmsKeyAgreeRecipientInfo = pCMsgCmsRecipientInfo->KeyAgree;
                                int numKeys = pCmsKeyAgreeRecipientInfo->cRecipientEncryptedKeys;
                                KeyAgreeRecipientInfo[] recipients = new KeyAgreeRecipientInfo[numKeys];
                                for (int subIndex = 0; subIndex < numKeys; subIndex++)
                                {
                                    recipients[subIndex] = new KeyAgreeRecipientInfo(new KeyAgreeRecipientInfoPalWindows(pCmsgCmsRecipientInfoMemory, index, subIndex));
                                }
                                return recipients;
                            }

                        default:
                            throw ErrorCode.E_NOTIMPL.ToCryptographicException();
                    }
                }
            }
            finally
            {
                if (mustRelease)
                {
                    pCmsgCmsRecipientInfoMemory.DangerousRelease();
                }
            }
        }
    }
}
