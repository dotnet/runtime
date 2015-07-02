// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Runtime.InteropServices;

partial class Interop
{
    partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern uint GetTempFileNameW(string tmpPath, string prefix, uint uniqueIdOrZero, [Out]StringBuilder tmpFileName);
    }
}
