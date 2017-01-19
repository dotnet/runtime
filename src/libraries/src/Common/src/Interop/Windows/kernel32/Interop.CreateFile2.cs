// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, EntryPoint = "CreateFile2", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern SafeFileHandle CreateFile2(
            string lpFileName,
            int dwDesiredAccess,
            System.IO.FileShare dwShareMode,
            System.IO.FileMode dwCreationDisposition,
            [In] ref CREATEFILE2_EXTENDED_PARAMETERS parameters);

        internal struct CREATEFILE2_EXTENDED_PARAMETERS
        {
            internal uint dwSize;
            internal uint dwFileAttributes;
            internal uint dwFileFlags;
            internal uint dwSecurityQosFlags;
            internal IntPtr lpSecurityAttributes;
            internal IntPtr hTemplateFile;
        }
    }
}
