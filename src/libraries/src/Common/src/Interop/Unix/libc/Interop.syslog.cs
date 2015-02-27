// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc)]
        internal static extern void syslog(int priority, string format, string arg1);

        internal const int LOG_DEBUG = 7;
        internal const int LOG_USER = (1 << 3);
    }
}
