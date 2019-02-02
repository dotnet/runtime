// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static unsafe int GetEnvironmentVariable(string lpName, Span<char> buffer)
        {
            fixed (char* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                return GetEnvironmentVariable(lpName, bufferPtr, buffer.Length);
            }
        }

        [DllImport(Libraries.Kernel32, EntryPoint = "GetEnvironmentVariableW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern unsafe int GetEnvironmentVariable(string lpName, char* lpBuffer, int nSize);
    }
}
