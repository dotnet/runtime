// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial uint SetSecurityInfoByHandle(
#else
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern /*DWORD*/ uint SetSecurityInfoByHandle(
#endif
            SafeHandle handle,
            /*DWORD*/ uint objectType,
            /*DWORD*/ uint securityInformation,
            byte[]? owner,
            byte[]? group,
            byte[]? dacl,
            byte[]? sacl);
    }
}
