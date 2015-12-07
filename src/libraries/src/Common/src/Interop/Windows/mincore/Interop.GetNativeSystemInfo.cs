// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.SystemInfo_L1_2)]
        internal extern static void GetNativeSystemInfo(out SYSTEM_INFO lpSystemInfo);
    }
}
