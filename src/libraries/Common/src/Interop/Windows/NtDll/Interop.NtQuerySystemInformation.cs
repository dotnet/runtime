// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll, CharSet = CharSet.Unicode)]
        internal static extern int NtQuerySystemInformation(int query, IntPtr dataPtr, int size, out int returnedSize);

        internal const int NtQuerySystemProcessInformation = 5;
        internal const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
    }
}
