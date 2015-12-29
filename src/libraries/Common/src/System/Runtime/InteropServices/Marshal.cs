// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    // Stand-in type for low-level assemblies to obtain Win32 errors
    internal static class Marshal
    {
        public static int GetLastWin32Error()
        {
            return Interop.mincore.GetLastError();
        }
    }
}
