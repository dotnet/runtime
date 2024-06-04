// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "ConvertSecurityDescriptorToStringSecurityDescriptorW",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool ConvertSdToStringSd(
            byte[] securityDescriptor,
            /* DWORD */ uint requestedRevision,
            uint securityInformation,
            out IntPtr resultString,
            ref uint resultStringLength);
    }
}
