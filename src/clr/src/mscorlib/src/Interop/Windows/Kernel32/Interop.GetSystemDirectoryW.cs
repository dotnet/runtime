// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern unsafe int GetSystemDirectoryW(char* lpBuffer, int uSize);

        internal static unsafe int GetSystemDirectoryW(Span<char> buffer)
        {
            fixed (char* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                return GetSystemDirectoryW(bufferPtr, buffer.Length);
            }
        }
    }
}
