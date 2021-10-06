// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#if DLLIMPORTGENERATOR_ENABLED
        [GeneratedDllImport(Interop.Libraries.Advapi32, EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW",
            CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial bool ConvertStringSdToSd(
#else
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "ConvertStringSecurityDescriptorToSecurityDescriptorW",
            CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern bool ConvertStringSdToSd(
#endif
            string stringSd,
            /* DWORD */ uint stringSdRevision,
            out IntPtr resultSd,
            ref uint resultSdLength);
    }
}
