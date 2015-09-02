// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal const int FNM_NOMATCH = 1; // constant from fnmatch.h

        internal enum FnMatchFlags : int
        {
            FNM_NONE = 0,
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int FnMatch(string pattern, string path, FnMatchFlags flags);
    }
}
