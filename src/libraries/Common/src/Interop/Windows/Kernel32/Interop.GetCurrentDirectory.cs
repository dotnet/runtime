// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [GeneratedDllImport(Libraries.Kernel32, EntryPoint = "GetCurrentDirectoryW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static partial uint GetCurrentDirectory(uint nBufferLength, ref char lpBuffer);
    }
}
