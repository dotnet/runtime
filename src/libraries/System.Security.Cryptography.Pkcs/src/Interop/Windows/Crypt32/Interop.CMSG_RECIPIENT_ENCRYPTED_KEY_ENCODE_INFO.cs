// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CMSG_RECIPIENT_ENCRYPTED_KEY_ENCODE_INFO
        {
            internal int cbSize;
            internal CRYPT_BIT_BLOB RecipientPublicKey;
            internal CERT_ID RecipientId;
            internal FILETIME Date;
            internal CRYPT_ATTRIBUTE_TYPE_VALUE* pOtherAttr;
        }
    }
}

