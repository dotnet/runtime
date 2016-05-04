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
        internal static unsafe bool CryptEncodeObject(MsgEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[] pbEncoded, ref int pcbEncoded)
        {
            return CryptEncodeObject(dwCertEncodingType, (IntPtr)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe bool CryptEncodeObject(MsgEncodingType dwCertEncodingType, IntPtr lpszStructType, void* pvStructInfo, [Out] byte[] pbEncoded, [In, Out] ref int pcbEncoded);
    }
}

