// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "CreateFile", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern SafePipeHandle CreateNamedPipeClient(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES secAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);
    }
}
