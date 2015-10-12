// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [Flags]
        internal enum GetNameInfoFlags : int
        {
            NI_NAMEREQD     = 0x1,
            NI_NUMERICHOST  = 0x2,
        }
        
        [DllImport(Libraries.SystemNative)]
        internal static unsafe extern int GetNameInfo(
            byte* address, 
            uint addressLength,
            bool isIpv6,
            byte* host, 
            uint hostLength, 
            byte* service, 
            uint serviceLength, 
            GetNameInfoFlags flags);
    }
}
