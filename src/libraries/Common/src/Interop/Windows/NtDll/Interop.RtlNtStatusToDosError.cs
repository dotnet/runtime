// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms680600(v=vs.85).aspx
        [LibraryImport(Libraries.NtDll)]
        public static partial uint RtlNtStatusToDosError(int Status);
    }
}
