// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Interop.Libraries.SecurityProvider, EntryPoint = "SetSecurityInfo", CallingConvention = CallingConvention.Winapi, 
            SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern /*DWORD*/ uint SetSecurityInfoByHandle(SafeHandle handle, /*DWORD*/ uint objectType, /*DWORD*/ uint securityInformation,
            byte[] owner, byte[] group, byte[] dacl, byte[] sacl);
    }
}