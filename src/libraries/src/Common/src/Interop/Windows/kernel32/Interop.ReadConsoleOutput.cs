// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CHAR_INFO
        {
            ushort charData;
            short attributes;
        }

        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "ReadConsoleOutputW")]
        internal static extern unsafe bool ReadConsoleOutput(IntPtr hConsoleOutput, CHAR_INFO* pBuffer, COORD bufferSize, COORD bufferCoord, ref SMALL_RECT readRegion);
    }
}
