// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [LibraryImport(Libraries.NtDll)]
        internal static unsafe partial uint NtQuerySystemInformation(int SystemInformationClass, void* SystemInformation, uint SystemInformationLength, uint* ReturnLength);

        internal const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
    }
}
