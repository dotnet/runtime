// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Runtime.InteropServices;

partial class Interop
{
    partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal unsafe static extern int GetLongPathNameW(char* path, char* longPathBuffer, int bufferLength);

        [DllImport(Libraries.CoreFile_L1, SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern int GetLongPathNameW(string path, [Out]StringBuilder longPathBuffer, int bufferLength);
    }
}
