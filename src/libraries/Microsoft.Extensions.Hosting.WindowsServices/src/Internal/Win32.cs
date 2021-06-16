// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Hosting.WindowsServices.Internal
{
    internal static class Win32
    {
        // https://docs.microsoft.com/en-us/windows/desktop/api/tlhelp32/nf-tlhelp32-createtoolhelp32snapshot
        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr CreateToolhelp32Snapshot(SnapshotFlags dwFlags, uint th32ProcessID);

        // https://docs.microsoft.com/en-us/windows/desktop/api/tlhelp32/nf-tlhelp32-process32first
        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool Process32First([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        // https://docs.microsoft.com/en-us/windows/desktop/api/tlhelp32/nf-tlhelp32-process32next
        [DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool Process32Next([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr hObject);

        internal static Process GetParentProcess()
        {
            var snapshotHandle = IntPtr.Zero;
            try
            {
                // Get a list of all processes
                snapshotHandle = CreateToolhelp32Snapshot(SnapshotFlags.Process, 0);

                PROCESSENTRY32 procEntry = default(PROCESSENTRY32);
                procEntry.dwSize = Marshal.SizeOf(typeof(PROCESSENTRY32));
                if (Process32First(snapshotHandle, ref procEntry))
                {
                    var currentProcessId = Process.GetCurrentProcess().Id;
                    do
                    {
                        if (currentProcessId == procEntry.th32ProcessID)
                        {
                            return Process.GetProcessById((int)procEntry.th32ParentProcessID);
                        }
                    }
                    while (Process32Next(snapshotHandle, ref procEntry));
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                CloseHandle(snapshotHandle);
            }

            return null;
        }

        [Flags]
        private enum SnapshotFlags : uint
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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PROCESSENTRY32
        {
            internal const int MAX_PATH = 260;
            internal int dwSize;
            internal int cntUsage;
            internal int th32ProcessID;
            internal IntPtr th32DefaultHeapID;
            internal int th32ModuleID;
            internal int cntThreads;
            internal int th32ParentProcessID;
            internal int pcPriClassBase;
            internal int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
            internal string szExeFile;
        }
    }
}
