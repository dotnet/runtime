// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        unsafe internal static bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer)
        {
            buffer.length = sizeof(MEMORYSTATUSEX);
            return GlobalMemoryStatusExNative(ref buffer);
        }

        [DllImport(Libraries.Kernel32, SetLastError = true, EntryPoint = "GlobalMemoryStatusEx")]
        private static extern bool GlobalMemoryStatusExNative(ref MEMORYSTATUSEX lpBuffer);
    }
}
