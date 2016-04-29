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
        internal enum ErrorCode : int
        {
            S_OK = 0,
            CRYPT_E_INVALID_MSG_TYPE = unchecked((int)0x80091004),
            CRYPT_E_ATTRIBUTES_MISSING = unchecked((int)0x8009100f),
            CRYPT_E_UNKNOWN_ALGO = unchecked((int)0x80091002),
            CRYPT_E_RECIPIENT_NOT_FOUND = unchecked((int)0x8009100b),
            CRYPT_E_NOT_FOUND = unchecked((int)0x80092004),
            E_NOTIMPL = unchecked((int)0x80004001),
        }
    }
}

