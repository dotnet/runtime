// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        // printf takes a variable number of arguments, which is difficult to represent in C#.
        // Instead, since we only have a small and fixed number of call sites, we declare
        // an overload for each of the specific argument sets we need.

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern unsafe int PrintF(string format, string arg1);
    }
}
