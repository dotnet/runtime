// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi,
            SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern /*DWORD*/ uint SetSecurityInfoByHandle(SafeHandle handle, /*DWORD*/ uint objectType, /*DWORD*/ uint securityInformation,
            byte[] owner, byte[] group, byte[] dacl, byte[] sacl);
    }
}
