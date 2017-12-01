// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal static int DEFAULT_PC_NAME_MAX = 255;

        internal enum PathConfName : int
        {
            PC_LINK_MAX         = 1,
            PC_MAX_CANON        = 2,
            PC_MAX_INPUT        = 3,
            PC_NAME_MAX         = 4,
            PC_PATH_MAX         = 5,
            PC_PIPE_BUF         = 6,
            PC_CHOWN_RESTRICTED = 7,
            PC_NO_TRUNC         = 8,
            PC_VDISABLE         = 9,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_PathConf", SetLastError = true)]
        private static extern int PathConf(string path, PathConfName name);
    }
}
