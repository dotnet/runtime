// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "CreateProcessW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CreateProcess(
            string? lpApplicationName,
            char* lpCommandLine,
            ref SECURITY_ATTRIBUTES procSecAttrs,
            ref SECURITY_ATTRIBUTES threadSecAttrs,
            [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
            int dwCreationFlags,
            char* lpEnvironment,
            string? lpCurrentDirectory,
            STARTUPINFOEX* lpStartupInfo,
            PROCESS_INFORMATION* lpProcessInformation
        );

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            internal IntPtr hProcess;
            internal IntPtr hThread;
            internal int dwProcessId;
            internal int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct STARTUPINFO
        {
            internal int cb;
            internal IntPtr lpReserved;
            internal IntPtr lpDesktop;
            internal IntPtr lpTitle;
            internal int dwX;
            internal int dwY;
            internal int dwXSize;
            internal int dwYSize;
            internal int dwXCountChars;
            internal int dwYCountChars;
            internal int dwFillAttribute;
            internal int dwFlags;
            internal short wShowWindow;
            internal short cbReserved2;
            internal IntPtr lpReserved2;
            internal IntPtr hStdInput;
            internal IntPtr hStdOutput;
            internal IntPtr hStdError;
        }

        internal const int PROC_THREAD_ATTRIBUTE_HANDLE_LIST = 0x00020002;
        internal const int EXTENDED_STARTUPINFO_PRESENT = 0x00080000;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct STARTUPINFOEX
        {
            internal STARTUPINFO StartupInfo;
            internal void* lpAttributeList;
        }

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool InitializeProcThreadAttributeList(
            void* lpAttributeList,
            int dwAttributeCount,
            int dwFlags,
            ref nuint lpSize);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool UpdateProcThreadAttribute(
            void* lpAttributeList,
            int dwFlags,
            IntPtr attribute,
            void* lpValue,
            nuint cbSize,
            void* lpPreviousValue,
            nuint* lpReturnSize);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static unsafe partial void DeleteProcThreadAttributeList(void* lpAttributeList);
    }
}
