// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetLongPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = false)]
        internal static extern int GetLongPathName(string path, [Out]StringBuilder longPathBuffer, int bufferLength);
    }
}
