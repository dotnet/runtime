// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class NtDll
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/ms680600(v=vs.85).aspx
        [DllImport(Libraries.NtDll, ExactSpelling = true)]
        public static extern unsafe uint RtlNtStatusToDosError(
            int Status);
    }
}
