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

    public static SafeMemoryMappedFileHandle CreateFileMapping(
            SafeFileHandle hFile,
            ref Kernel32.SECURITY_ATTRIBUTES securityAttributes,
            int pageProtection,
            long maximumSize,
            string? name)
    {
        // split the long into two ints
        int capacityHigh, capacityLow;
        Kernel32.SplitLong(maximumSize, out capacityHigh, out capacityLow);

        return Kernel32.CreateFileMapping(hFile, ref securityAttributes, pageProtection, capacityHigh, capacityLow, name);
    }

    public static SafeMemoryMappedFileHandle CreateFileMapping(
            IntPtr hFile,
            ref Kernel32.SECURITY_ATTRIBUTES securityAttributes,
            int pageProtection,
            long maximumSize,
            string? name)
    {
        // split the long into two ints
        int capacityHigh, capacityLow;
        Kernel32.SplitLong(maximumSize, out capacityHigh, out capacityLow);

        return Kernel32.CreateFileMapping(hFile, ref securityAttributes, pageProtection, capacityHigh, capacityLow, name);
    }

    public static SafeMemoryMappedViewHandle MapViewOfFile(
            SafeMemoryMappedFileHandle hFileMappingObject,
            int desiredAccess,
            long fileOffset,
            UIntPtr numberOfBytesToMap)
    {
        // split the long into two ints
        int offsetHigh, offsetLow;
        Kernel32.SplitLong(fileOffset, out offsetHigh, out offsetLow);

        return Kernel32.MapViewOfFile(hFileMappingObject, desiredAccess, offsetHigh, offsetLow, numberOfBytesToMap);
    }

    public static SafeMemoryMappedFileHandle OpenFileMapping(
            int desiredAccess,
            bool inheritHandle,
            string name)
    {
        return Kernel32.OpenFileMapping(desiredAccess, inheritHandle, name);
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
