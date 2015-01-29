// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int pathconf(string path, int name);

        internal static class PathConfNames
        {
            internal const int _PC_NAME_MAX = 3;
            internal const int _PC_PATH_MAX = 4;
        }

        internal static int DEFAULT_PC_NAME_MAX = 255;
        internal static int DEFAULT_PC_PATH_MAX = 4096;
    }
}
