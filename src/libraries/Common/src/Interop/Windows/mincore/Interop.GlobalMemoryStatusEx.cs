// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.SystemInfo_L1_1)]
        internal extern static int GlobalMemoryStatusEx(out MEMORYSTATUSEX lpBuffer);
    }
}
