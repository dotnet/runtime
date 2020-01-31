// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        [DllImport(Libraries.NtDll, ExactSpelling = true)]
        internal static extern unsafe uint NtQueryInformationProcess(SafeProcessHandle ProcessHandle, int ProcessInformationClass, void* ProcessInformation, uint ProcessInformationLength, out uint ReturnLength);
    }
}
