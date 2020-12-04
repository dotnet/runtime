// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        // From SYSTEM_INFORMATION_CLASS
        // Use for NtQuerySystemInformation
        internal const int SystemProcessInformation = 5;

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SYSTEM_PROCESS_INFORMATION
        {
            internal uint NextEntryOffset;
            internal uint NumberOfThreads;
            private fixed byte Reserved1[48];
            internal Interop.UNICODE_STRING ImageName;
            internal int BasePriority;
            internal IntPtr UniqueProcessId;
            private readonly UIntPtr Reserved2;
            internal uint HandleCount;
            internal uint SessionId;
            private readonly UIntPtr Reserved3;
            internal UIntPtr PeakVirtualSize;  // SIZE_T
            internal UIntPtr VirtualSize;
            private readonly uint Reserved4;
            internal UIntPtr PeakWorkingSetSize;  // SIZE_T
            internal UIntPtr WorkingSetSize;  // SIZE_T
            private readonly UIntPtr Reserved5;
            internal UIntPtr QuotaPagedPoolUsage;  // SIZE_T
            private readonly UIntPtr Reserved6;
            internal UIntPtr QuotaNonPagedPoolUsage;  // SIZE_T
            internal UIntPtr PagefileUsage;  // SIZE_T
            internal UIntPtr PeakPagefileUsage;  // SIZE_T
            internal UIntPtr PrivatePageCount;  // SIZE_T
            private fixed long Reserved7[6];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SYSTEM_THREAD_INFORMATION
        {
            private fixed long Reserved1[3];
            private readonly uint Reserved2;
            internal IntPtr StartAddress;
            internal CLIENT_ID ClientId;
            internal int Priority;
            internal int BasePriority;
            private readonly uint Reserved3;
            internal uint ThreadState;
            internal uint WaitReason;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CLIENT_ID
        {
            internal IntPtr UniqueProcess;
            internal IntPtr UniqueThread;
        }
    }
}
