// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "GetComputerNameW")]
        private static unsafe partial int GetComputerName(char* lpBuffer, uint* nSize);

        // maximum length of the NETBIOS name (not including NULL)
        private const int MAX_COMPUTERNAME_LENGTH = 15;

        internal static string? GetComputerName()
        {
            Span<char> buffer = stackalloc char[MAX_COMPUTERNAME_LENGTH + 1];
            uint length = (uint)buffer.Length;
            unsafe
            {
                fixed (char* lpBuffer = &MemoryMarshal.GetReference(buffer))
                {
                    return GetComputerName(lpBuffer, &length) != 0 ?
                        buffer.Slice(0, (int)length).ToString() :
                        null;
                }
            }
        }
    }
}
