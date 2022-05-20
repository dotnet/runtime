// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [Flags]
        internal enum SnapshotFlags : uint
        {
            HeapList = 0x00000001,
            Process = 0x00000002,
            Thread = 0x00000004,
            Module = 0x00000008,
            Module32 = 0x00000010,
            All = (HeapList | Process | Thread | Module),
            Inherit = 0x80000000,
            NoHeaps = 0x40000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct PROCESSENTRY32
        {
            internal int dwSize;
            internal int cntUsage;
            internal int th32ProcessID;
            internal IntPtr th32DefaultHeapID;
            internal int th32ModuleID;
            internal int cntThreads;
            internal int th32ParentProcessID;
            internal int pcPriClassBase;
            internal int dwFlags;
            internal fixed char szExeFile[MAX_PATH];
        }

        // https://docs.microsoft.com/windows/desktop/api/tlhelp32/nf-tlhelp32-createtoolhelp32snapshot
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        // https://docs.microsoft.com/windows/desktop/api/tlhelp32/nf-tlhelp32-process32first
        [LibraryImport(Libraries.Kernel32, EntryPoint = "Process32FirstW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        // https://docs.microsoft.com/windows/desktop/api/tlhelp32/nf-tlhelp32-process32next
        [LibraryImport(Libraries.Kernel32, EntryPoint = "Process32NextW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);
    }
}
