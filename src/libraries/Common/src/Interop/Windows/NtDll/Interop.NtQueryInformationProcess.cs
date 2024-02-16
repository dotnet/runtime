// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [LibraryImport(Libraries.NtDll)]
        internal static unsafe partial uint NtQueryInformationProcess(SafeProcessHandle ProcessHandle, int ProcessInformationClass, void* ProcessInformation, uint ProcessInformationLength, out uint ReturnLength);
    }
}
