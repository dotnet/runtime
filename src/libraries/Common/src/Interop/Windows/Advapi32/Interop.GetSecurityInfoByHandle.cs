// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "GetSecurityInfo", ExactSpelling = true)]
        internal static unsafe partial uint GetSecurityInfoByHandle(
            SafeHandle handle,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            IntPtr* sidOwner,
            IntPtr* sidGroup,
            IntPtr* dacl,
            IntPtr* sacl,
            IntPtr* securityDescriptor);
    }
}
