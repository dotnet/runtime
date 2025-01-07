// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Libraries.Crypt32, EntryPoint = "CryptDecodeObject", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CryptDecodeObjectPointer(
            CertEncodingType dwCertEncodingType,
            [MarshalAs(UnmanagedType.LPStr)] string lpszStructType,
            byte[] pbEncoded, int cbEncoded,
            CryptDecodeObjectFlags dwFlags,
            void* pvStructInfo,
            ref int pcbStructInfo);
    }
}
