// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.LibraryLoader, ExactSpelling = true, SetLastError = true)]
        public static extern unsafe bool FreeLibrary([In] IntPtr hModule);
    }
}
