// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, EntryPoint = "GetComputerNameW", ExactSpelling = true)]
        private static extern int GetComputerName(ref char lpBuffer, ref uint nSize);

        // maximum length of the NETBIOS name (not including NULL)
        private const int MAX_COMPUTERNAME_LENGTH = 15;

        internal static string? GetComputerName()
        {
            Span<char> buffer = stackalloc char[MAX_COMPUTERNAME_LENGTH + 1];
            uint length = (uint)buffer.Length;

            return GetComputerName(ref MemoryMarshal.GetReference(buffer), ref length) != 0 ?
                buffer.Slice(0, (int)length).ToString() :
                null;
        }
    }
}
