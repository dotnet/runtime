// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal static unsafe bool CryptDecodeObject(CryptDecodeObjectStructType lpszStructType, IntPtr pbEncoded, int cbEncoded, void* pvStructInfo, ref int pcbStructInfo)
        {
            return CryptDecodeObject(MsgEncodingType.All, (IntPtr)lpszStructType, pbEncoded, cbEncoded, 0, pvStructInfo, ref pcbStructInfo);
        }

        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe bool CryptDecodeObject(
            MsgEncodingType dwCertEncodingType,
            IntPtr lpszStructType,
            IntPtr pbEncoded,
            int cbEncoded,
            int dwFlags,
            void* pvStructInfo,
            ref int pcbStructInfo);
    }
}
