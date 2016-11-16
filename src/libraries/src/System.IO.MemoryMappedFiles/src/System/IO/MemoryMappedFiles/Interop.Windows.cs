// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal partial class Interop
{
    public static unsafe void CheckForAvailableVirtualMemory(ulong nativeSize)
    {
        Interop.Kernel32.MEMORYSTATUSEX memStatus;
        memStatus.dwLength = (uint)sizeof(Interop.Kernel32.MEMORYSTATUSEX);
        Interop.Kernel32.GlobalMemoryStatusEx(out memStatus);
        ulong totalVirtual = memStatus.ullTotalVirtual;
        if (nativeSize >= totalVirtual)
        {
            throw new IOException(SR.IO_NotEnoughMemory);
        }
    }

    public static SafeMemoryMappedFileHandle CreateFileMapping(
            SafeFileHandle hFile,
            ref Kernel32.SECURITY_ATTRIBUTES securityAttributes,
            int pageProtection,
            long maximumSize,
            string name)
    {
        // split the long into two ints
        int capacityHigh, capacityLow;
        SplitLong(maximumSize, out capacityHigh, out capacityLow);

        return Interop.Kernel32.CreateFileMapping(hFile, ref securityAttributes, pageProtection, capacityHigh, capacityLow, name);
    }

    public static SafeMemoryMappedFileHandle CreateFileMapping(
            IntPtr hFile,
            ref Kernel32.SECURITY_ATTRIBUTES securityAttributes,
            int pageProtection,
            long maximumSize,
            string name)
    {
        // split the long into two ints
        int capacityHigh, capacityLow;
        SplitLong(maximumSize, out capacityHigh, out capacityLow);

        return Interop.Kernel32.CreateFileMapping(hFile, ref securityAttributes, pageProtection, capacityHigh, capacityLow, name);
    }

    public static SafeMemoryMappedViewHandle MapViewOfFile(
            SafeMemoryMappedFileHandle hFileMappingObject,
            int desiredAccess,
            long fileOffset,
            UIntPtr numberOfBytesToMap)
    {
        // split the long into two ints
        int offsetHigh, offsetLow;
        SplitLong(fileOffset, out offsetHigh, out offsetLow);

        return Interop.Kernel32.MapViewOfFile(hFileMappingObject, desiredAccess, offsetHigh, offsetLow, numberOfBytesToMap);
    }

    public static SafeMemoryMappedFileHandle OpenFileMapping(
            int desiredAccess,
            bool inheritHandle,
            string name)
    {
        return Interop.Kernel32.OpenFileMapping(desiredAccess, inheritHandle, name);
    }

    public static IntPtr VirtualAlloc(
            SafeHandle baseAddress,
            UIntPtr size,
            int allocationType,
            int protection)
    {
        return Interop.Kernel32.VirtualAlloc(baseAddress, size, allocationType, protection);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SplitLong(long number, out int high, out int low)
    {
        high = unchecked((int)(number >> 32));
        low = unchecked((int)(number & 0x00000000FFFFFFFFL));
    }
}