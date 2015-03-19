// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Wow64, SetLastError = true)]
        internal static extern bool IsWow64Process(SafeProcessHandle hProcess, ref bool Wow64Process);
    }
}
