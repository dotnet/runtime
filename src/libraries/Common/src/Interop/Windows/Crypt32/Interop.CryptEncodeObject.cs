// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal static unsafe bool CryptEncodeObject(MsgEncodingType dwCertEncodingType, CryptDecodeObjectStructType lpszStructType, void* pvStructInfo, byte[]? pbEncoded, ref int pcbEncoded)
        {
            return CryptEncodeObject(dwCertEncodingType, (nint)lpszStructType, pvStructInfo, pbEncoded, ref pcbEncoded);
        }

        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static unsafe partial bool CryptEncodeObject(
            MsgEncodingType dwCertEncodingType,
            nint lpszStructType,
            void* pvStructInfo,
            byte[]? pbEncoded,
            ref int pcbEncoded);
    }
}
