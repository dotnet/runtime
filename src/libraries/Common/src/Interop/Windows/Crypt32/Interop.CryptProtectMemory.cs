// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        internal const uint CRYPTPROTECTMEMORY_BLOCK_SIZE = 16;
        internal const uint CRYPTPROTECTMEMORY_SAME_PROCESS = 0;

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CryptProtectMemory(SafeBuffer pData, uint cbData, uint dwFlags);

        [GeneratedDllImport(Libraries.Crypt32, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static partial bool CryptUnprotectMemory(SafeBuffer pData, uint cbData, uint dwFlags);
    }
}
