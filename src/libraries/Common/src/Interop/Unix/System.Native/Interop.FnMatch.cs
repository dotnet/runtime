// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FnMatch", SetLastError = true)]
        internal static extern int FnMatch(string pattern, string path, FnMatchFlags flags);
    }
}
