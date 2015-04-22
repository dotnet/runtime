// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static class SysConfNames
        {
            internal const int _SC_CLK_TCK  = 3;
            internal const int _SC_PAGESIZE = 29;
        }
    }
}
