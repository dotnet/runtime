// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetNameInfo")]
        internal static extern unsafe int GetNameInfo(
            byte* address, 
            uint addressLength,
            byte isIpv6,
            byte* host, 
            uint hostLength, 
            byte* service, 
            uint serviceLength, 
            GetNameInfoFlags flags);
    }
}
