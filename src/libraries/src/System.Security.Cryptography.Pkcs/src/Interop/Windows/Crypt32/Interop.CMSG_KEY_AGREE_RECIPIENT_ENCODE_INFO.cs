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
        internal unsafe struct CMSG_KEY_AGREE_RECIPIENT_ENCODE_INFO
        {
            internal int cbSize;
            internal CRYPT_ALGORITHM_IDENTIFIER KeyEncryptionAlgorithm;
            internal void* pvKeyEncryptionAuxInfo;
            internal CRYPT_ALGORITHM_IDENTIFIER KeyWrapAlgorithm;
            internal IntPtr pvKeyWrapAuxInfo;
            internal IntPtr hCryptProv;
            internal int dwKeySpec;
            internal CmsKeyAgreeKeyChoice dwKeyChoice;

            // This is actually a union between a CRYPT_ALGORITHM_IDENTIFIER* and a CERT_ID* (pSenderId), but the pSenderId option is never used so we won't bother declaring it.
            internal CRYPT_ALGORITHM_IDENTIFIER* pEphemeralAlgorithm;

            internal DATA_BLOB UserKeyingMaterial;
            internal int cRecipientEncryptedKeys;
            internal CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO** rgpRecipientEncryptedKeys;
        }
    }
}

