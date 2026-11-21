// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal sealed class SafeJobHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeJobHandle() : base(true) { }

            internal static unsafe SafeJobHandle Create(uint limitFlags)
            {
                JOBOBJECT_EXTENDED_LIMIT_INFORMATION limitInfo = default;
                limitInfo.BasicLimitInformation.LimitFlags = limitFlags;

                SafeJobHandle job = CreateJobObjectW(IntPtr.Zero, IntPtr.Zero);
                if (job.IsInvalid || !SetInformationJobObject(
                    job,
                    JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation,
                    ref limitInfo,
                    (uint)sizeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION)))
                {
                    int error = Marshal.GetLastPInvokeError();
                    job.Dispose();
                    throw new Win32Exception(error);
                }

                return job;
            }

            internal void Terminate()
            {
                if (!TerminateJobObject(this, -1))
                {
                    throw new Win32Exception(Marshal.GetLastPInvokeError());
                }
            }

            protected override bool ReleaseHandle() => Interop.Kernel32.CloseHandle(handle);
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial SafeJobHandle CreateJobObjectW(IntPtr lpJobAttributes, IntPtr lpName);

        internal const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
        internal const uint JOB_OBJECT_LIMIT_BREAKAWAY_OK = 0x00000800;

        internal enum JOBOBJECTINFOCLASS
        {
            JobObjectBasicAccountingInformation = 1,
            JobObjectExtendedLimitInformation = 9
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
        {
            internal long TotalUserTime;
            internal long TotalKernelTime;
            internal long ThisPeriodTotalUserTime;
            internal long ThisPeriodTotalKernelTime;
            internal uint TotalPageFaultCount;
            internal uint TotalProcesses;
            internal uint ActiveProcesses;
            internal uint TotalTerminatedProcesses;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IO_COUNTERS
        {
            internal ulong ReadOperationCount;
            internal ulong WriteOperationCount;
            internal ulong OtherOperationCount;
            internal ulong ReadTransferCount;
            internal ulong WriteTransferCount;
            internal ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            internal long PerProcessUserTimeLimit;
            internal long PerJobUserTimeLimit;
            internal uint LimitFlags;
            internal nuint MinimumWorkingSetSize;
            internal nuint MaximumWorkingSetSize;
            internal uint ActiveProcessLimit;
            internal nuint Affinity;
            internal uint PriorityClass;
            internal uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            internal JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            internal IO_COUNTERS IoInfo;
            internal nuint ProcessMemoryLimit;
            internal nuint JobMemoryLimit;
            internal nuint PeakProcessMemoryUsed;
            internal nuint PeakJobMemoryUsed;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool SetInformationJobObject(SafeJobHandle hJob, JOBOBJECTINFOCLASS JobObjectInfoClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool QueryInformationJobObject(SafeJobHandle hJob, JOBOBJECTINFOCLASS JobObjectInfoClass, out JOBOBJECT_BASIC_ACCOUNTING_INFORMATION lpJobObjectInfo, uint cbJobObjectInfoLength, IntPtr lpReturnLength);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool AssignProcessToJobObject(SafeJobHandle hJob, SafeProcessHandle hProcess);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool TerminateJobObject(SafeJobHandle hJob, int uExitCode);

        internal const int PROC_THREAD_ATTRIBUTE_JOB_LIST = 0x0002000D;
    }
}
