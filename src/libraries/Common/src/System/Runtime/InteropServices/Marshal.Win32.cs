// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    // Stand-in type for low-level assemblies to obtain Win32 errors
    internal static class Marshal
    {
        public static int GetLastWin32Error()
        {
            return Interop.Kernel32.GetLastError();
        }
    }
}
