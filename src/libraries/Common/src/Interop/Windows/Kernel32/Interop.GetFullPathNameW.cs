// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
                                                // Disabled since GetFullPathNameW takes 'ref's into buffers (the ref parameters don't represent single characters).
        /// <summary>
        /// WARNING: This method does not implicitly handle long paths. Use GetFullPathName or PathHelper.
        /// </summary>
        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        internal static extern uint GetFullPathNameW(
            ref char lpFileName,
            uint nBufferLength,
            ref char lpBuffer,
            IntPtr lpFilePart);
    }
}
