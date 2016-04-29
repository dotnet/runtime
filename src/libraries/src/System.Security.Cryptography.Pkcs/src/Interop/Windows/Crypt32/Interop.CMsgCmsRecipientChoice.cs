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
        internal enum CMsgCmsRecipientChoice : int
        {
            CMSG_KEY_TRANS_RECIPIENT = 1,
            CMSG_KEY_AGREE_RECIPIENT = 2,
            CMSG_MAIL_LIST_RECIPIENT = 3,
        }
    }
}

