// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

internal partial class Interop
{
    public static void CheckForAvailableVirtualMemory(ulong nativeSize)
    {
        // this cannot be implemented on UAP because we cannot get the total 
        // available Virtual Memory.
    }

    public static SafeMemoryMappedFileHandle CreateFileMapping(
            SafeFileHandle hFile,
            ref Kernel32.SECURITY_ATTRIBUTES securityAttributes,
            int pageProtection,
            long maximumSize,
            string name)
    {
        return Interop.Kernel32.CreateFileMappingFromApp(hFile, ref securityAttributes, pageProtection, maximumSize, name);
    }

    public static SafeMemoryMappedFileHandle CreateFileMapping(
            IntPtr hFile,
            ref Kernel32.SECURITY_ATTRIBUTES securityAttributes,
            int pageProtection,
            long maximumSize,
            string name)
    {
        return Interop.Kernel32.CreateFileMappingFromApp(hFile, ref securityAttributes, pageProtection, maximumSize, name);
    }

    public static SafeMemoryMappedViewHandle MapViewOfFile(
            SafeMemoryMappedFileHandle hFileMappingObject,
            int desiredAccess,
            long fileOffset,
            UIntPtr numberOfBytesToMap)
    {
        return Interop.Kernel32.MapViewOfFileFromApp(hFileMappingObject, desiredAccess, fileOffset, numberOfBytesToMap);
    }
    public static SafeMemoryMappedFileHandle OpenFileMapping(
            int desiredAccess,
            bool inheritHandle,
            string name)
    {
        return Interop.mincore.OpenFileMappingFromApp(desiredAccess, inheritHandle, name);
    }
    public static IntPtr VirtualAlloc(
            SafeHandle baseAddress,
            UIntPtr size,
            int allocationType,
            int protection)
    {
        return Interop.mincore.VirtualAllocFromApp(baseAddress, size, allocationType, protection);
    }
}