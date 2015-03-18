// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.ProcessEnvironment, EntryPoint = "GetCurrentDirectoryW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern int GetCurrentDirectory(int nBufferLength, [Out]StringBuilder lpBuffer);
    }
}
