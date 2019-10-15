// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        // printf takes a variable number of arguments, which is difficult to represent in C#.
        // Instead, since we only have a small and fixed number of call sites, we declare
        // an overload for each of the specific argument sets we need.

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_PrintF", SetLastError = true)]
        internal static extern unsafe int PrintF(string format, string arg1);
    }
}
