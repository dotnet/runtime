// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
