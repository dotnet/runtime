// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.ServiceCore, CharSet = CharSet.Unicode, SetLastError = true)]
        internal extern static bool EnumDependentServices(
            IntPtr serviceHandle,
            int serviceState,
            IntPtr bufferOfENUM_SERVICE_STATUS,
            int bufSize,
            ref int bytesNeeded,
            ref int numEnumerated);
    }
}
