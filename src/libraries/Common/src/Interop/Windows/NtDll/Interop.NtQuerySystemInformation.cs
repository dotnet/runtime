// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [DllImport(Libraries.NtDll, ExactSpelling = true)]
        internal static extern unsafe uint NtQuerySystemInformation(int SystemInformationClass, void* SystemInformation, uint SystemInformationLength, uint* ReturnLength);

        internal const uint STATUS_INFO_LENGTH_MISMATCH = 0xC0000004;
    }
}
