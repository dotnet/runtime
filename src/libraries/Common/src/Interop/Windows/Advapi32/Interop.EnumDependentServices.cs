// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Libraries.Advapi32, EntryPoint = "EnumDependentServicesW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EnumDependentServices(
            SafeServiceHandle serviceHandle,
            int serviceState,
            IntPtr bufferOfENUM_SERVICE_STATUS,
            int bufSize,
            ref int bytesNeeded,
            ref int numEnumerated);
    }
}
