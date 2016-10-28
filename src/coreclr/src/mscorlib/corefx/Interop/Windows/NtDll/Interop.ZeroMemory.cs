// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
