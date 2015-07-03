// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;

partial class Interop
{
    partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal unsafe static extern int GetFullPathNameW(char* path, int numBufferChars, char* buffer, IntPtr mustBeZero);

        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern int GetFullPathNameW(string path, int numBufferChars, [Out]StringBuilder buffer, IntPtr mustBeZero);
    }
}
