// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    public static unsafe void CheckForAvailableVirtualMemory(ulong nativeSize)
    {
        Kernel32.MEMORYSTATUSEX memoryStatus = default;
        memoryStatus.dwLength = (uint)sizeof(Kernel32.MEMORYSTATUSEX);
        if (Kernel32.GlobalMemoryStatusEx(ref memoryStatus))
        {
            ulong totalVirtual = memoryStatus.ullTotalVirtual;
            if (nativeSize >= totalVirtual)
            {
                throw new IOException(SR.IO_NotEnoughMemory);
            }
        }
    }

    public static SafeMemoryMappedViewHandle MapViewOfFile(
            SafeMemoryMappedFileHandle hFileMappingObject,
            int desiredAccess,
            long fileOffset,
            UIntPtr numberOfBytesToMap)
    {
        return Kernel32.MapViewOfFile(
            hFileMappingObject,
            desiredAccess,
            dwFileOffsetHigh: (int)(fileOffset >> 32), // Split the fileOffset long into two ints
            dwFileOffsetLow: (int)fileOffset,
            numberOfBytesToMap);
    }

    public static IntPtr VirtualAlloc(
            SafeHandle baseAddress,
            UIntPtr size,
            int allocationType,
            int protection)
    {
        return Kernel32.VirtualAlloc(baseAddress, size, allocationType, protection);
    }
}
