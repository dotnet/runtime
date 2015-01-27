// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc)]
        internal static extern int fnmatch(string pattern, string str, FnmatchFlags flags);

        [Flags]
        internal enum FnmatchFlags
        {
            None = 0,
            FNM_PATHNAME    = (1 << 0),
            FNM_FILE_NAME   = FNM_PATHNAME,
            FNM_NOESCAPE    = (1 << 1),
            FNM_PERIOD      = (1 << 2),
            FNM_LEADING_DIR = (1 << 3),
            FNM_CASEFOLD    = (1 << 4),
            FNM_EXTMATCH    = (1 << 5)
        }
    }
}