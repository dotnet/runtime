// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static unsafe int GetEnvironmentVariable(string lpName, Span<char> buffer)
        {
            fixed (char* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                return GetEnvironmentVariable(lpName, bufferPtr, buffer.Length);
            }
        }

        [DllImport(Libraries.Kernel32, EntryPoint = "GetEnvironmentVariableW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern unsafe int GetEnvironmentVariable(string lpName, char* lpBuffer, int nSize);
    }
}
