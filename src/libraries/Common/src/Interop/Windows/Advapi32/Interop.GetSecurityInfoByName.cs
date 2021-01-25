// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "GetNamedSecurityInfoW", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern /*DWORD*/ uint GetSecurityInfoByName(string name, /*DWORD*/ uint objectType, /*DWORD*/ uint securityInformation,
            out IntPtr sidOwner, out IntPtr sidGroup, out IntPtr dacl, out IntPtr sacl, out IntPtr securityDescriptor);
    }
}
