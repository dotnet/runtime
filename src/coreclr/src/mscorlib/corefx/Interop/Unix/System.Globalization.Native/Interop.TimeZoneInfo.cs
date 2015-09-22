﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Ansi)] // readlink requires char*
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReadLink(string filePath, StringBuilder result, uint resultCapacity);
    }
}
