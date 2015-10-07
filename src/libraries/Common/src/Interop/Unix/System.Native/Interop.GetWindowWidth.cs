// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WinSize
        {
            internal ushort Row;
            internal ushort Col;
            internal ushort XPixel;
            internal ushort YPixel;
        };

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        private static extern int GetWindowSize(out WinSize winSize);

        internal static int GetWindowWidth()
        {
            WinSize winsize;

            if (GetWindowSize(out winsize) == 0)
            {
                return winsize.Col;
            }

            return -1;
        }
    }
}
