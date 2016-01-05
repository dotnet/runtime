// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum SysConfName
        {
            _SC_CLK_TCK = 1,
            _SC_PAGESIZE = 2,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SysConf", SetLastError = true)]
        internal static extern long SysConf(SysConfName name);
    }
}
