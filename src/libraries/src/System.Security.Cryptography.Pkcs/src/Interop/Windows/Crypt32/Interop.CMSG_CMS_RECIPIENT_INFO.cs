// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CMSG_CMS_RECIPIENT_INFO
        {
            internal CMsgCmsRecipientChoice dwRecipientChoice;
            // union {
            //  PCMSG_KEY_TRANS_RECIPIENT_INFO pKeyTrans;   // CMSG_KEY_TRANS_RECIPIENT
            //  PCMSG_KEY_AGREE_RECIPIENT_INFO pKeyAgree;   // CMSG_KEY_AGREE_RECIPIENT
            //  PCMSG_MAIL_LIST_RECIPIENT_INFO pMailList;   // CMSG_MAIL_LIST_RECIPIENT
            // }
            //
            private void* pRecipientInfo;  // Do NOT add an underscore - this name still maps to a C++ Win32 header definition.

            internal unsafe CMSG_KEY_TRANS_RECIPIENT_INFO* KeyTrans
            {
                get
                {
                    Debug.Assert(dwRecipientChoice == CMsgCmsRecipientChoice.CMSG_KEY_TRANS_RECIPIENT);
                    return (CMSG_KEY_TRANS_RECIPIENT_INFO*)pRecipientInfo;
                }
            }

            internal unsafe CMSG_KEY_AGREE_RECIPIENT_INFO* KeyAgree
            {
                get
                {
                    Debug.Assert(dwRecipientChoice == CMsgCmsRecipientChoice.CMSG_KEY_AGREE_RECIPIENT);
                    return (CMSG_KEY_AGREE_RECIPIENT_INFO*)pRecipientInfo;
                }
            }
        }
    }
}
