// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        // printf takes a variable number of arguments, which is difficult to represent in C#.
        // Instead, since we only have a small and fixed number of call sites, we declare
        // an overload for each of the specific argument sets we need.

        [DllImport(Libraries.Libc)]
        internal static extern unsafe int printf(string format, string arg1);
    }
}
