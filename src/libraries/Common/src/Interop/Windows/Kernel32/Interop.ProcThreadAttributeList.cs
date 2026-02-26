// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal const int PROC_THREAD_ATTRIBUTE_HANDLE_LIST = 0x00020002;
        internal const int PROC_THREAD_ATTRIBUTE_JOB_LIST = 0x0002000D;
        internal const int EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

        [StructLayout(LayoutKind.Sequential)]
        internal struct STARTUPINFOEX
        {
            internal STARTUPINFO StartupInfo;
            internal LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList;
        }

        internal struct LPPROC_THREAD_ATTRIBUTE_LIST
        {
            internal IntPtr AttributeList;
        }

        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CreateProcess(
            char* lpApplicationName,
            char* lpCommandLine,
            ref SECURITY_ATTRIBUTES procSecAttrs,
            ref SECURITY_ATTRIBUTES threadSecAttrs,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            int dwCreationFlags,
            char* lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFOEX lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation
        );

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool InitializeProcThreadAttributeList(
            LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList,
            int dwAttributeCount,
            int dwFlags,
            ref nuint lpSize);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool UpdateProcThreadAttribute(
            LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList,
            int dwFlags,
            IntPtr attribute,
            void* lpValue,
            nuint cbSize,
            void* lpPreviousValue,
            nuint lpReturnSize);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial void DeleteProcThreadAttributeList(LPPROC_THREAD_ATTRIBUTE_LIST lpAttributeList);
    }
}
