// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "GetNamedSecurityInfoW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial uint GetSecurityInfoByName(
            string name,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);
    }
}
