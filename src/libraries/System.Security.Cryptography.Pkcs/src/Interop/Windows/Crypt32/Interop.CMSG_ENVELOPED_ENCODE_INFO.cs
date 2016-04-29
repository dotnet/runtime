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
        internal unsafe struct CMSG_ENVELOPED_ENCODE_INFO
        {
            internal int cbSize;
            internal IntPtr hCryptProv;
            internal CRYPT_ALGORITHM_IDENTIFIER ContentEncryptionAlgorithm;
            internal IntPtr pvEncryptionAuxInfo;
            internal int cRecipients;

            // The following array may only be used for transport recipients identified
            // by their IssuereAndSerialNumber. If rgpRecipients != NULL, then,
            // the rgCmsRecipients must be NULL.
            internal IntPtr rgpRecipients;

            // If rgCmsRecipients != NULL, then, the above rgpRecipients must be
            // NULL.
            internal CMSG_RECIPIENT_ENCODE_INFO* rgCmsRecipients;
            internal int cCertEncoded;
            internal DATA_BLOB* rgCertEncoded;
            internal int cCrlEncoded;
            internal DATA_BLOB* rgCrlEncoded;
            internal int cAttrCertEncoded;
            internal DATA_BLOB* rgAttrCertEncoded;
            internal int cUnprotectedAttr;
            internal CRYPT_ATTRIBUTE* rgUnprotectedAttr;
        }
    }
}

