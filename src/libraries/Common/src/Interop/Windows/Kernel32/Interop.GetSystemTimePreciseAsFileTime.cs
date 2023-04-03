// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#pragma warning disable CS3016 // Arrays as attribute arguments is not CLS-compliant
        // https://learn.microsoft.com/windows/win32/api/sysinfoapi/nf-sysinfoapi-getsystemtimepreciseasfiletime
        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static unsafe partial void GetSystemTimePreciseAsFileTime(
            // [NativeTypeName("LPFILETIME")]
            ulong* lpSystemTimeAsFileTime);
    }
#pragma warning restore CS3016 // Arrays as attribute arguments is not CLS-compliant
}
