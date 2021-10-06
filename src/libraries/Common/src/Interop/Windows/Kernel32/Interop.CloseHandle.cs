// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
                                                // Disabled since CloseHandle is a QCall in some scenarios and DllImportGenerator doesn't support QCalls.
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);
    }
}
