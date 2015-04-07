// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Security;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll, CharSet = CharSet.Unicode, EntryPoint = "RtlZeroMemory")]
        internal static extern void ZeroMemory(SafeBSTRHandle address, uint length);

        [DllImport(Libraries.NtDll, CharSet = CharSet.Unicode, EntryPoint = "RtlZeroMemory")]
        internal static extern void ZeroMemory(IntPtr address, UIntPtr length);
    }
}
