// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.CoreFile_L1, EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern int GetDriveType(string drive);

    }
}
