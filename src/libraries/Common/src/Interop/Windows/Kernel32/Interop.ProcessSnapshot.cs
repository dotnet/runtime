// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static unsafe partial class Kernel32
    {
        [Flags]
        public enum PSS_CAPTURE_FLAGS : uint
        {
            PSS_CAPTURE_THREADS = 0x00000080,
        }

        public enum PSS_QUERY_INFORMATION_CLASS
        {
            PSS_QUERY_THREAD_INFORMATION = 5,
        }

        public enum PSS_WALK_INFORMATION_CLASS
        {
            PSS_WALK_THREADS = 3,
            PSS_WALK_THREAD_NAME = 4
        }

        [Flags]
        public enum PSS_THREAD_FLAGS
        {
            PSS_THREAD_FLAGS_NONE = 0x0000,
            PSS_THREAD_FLAGS_TERMINATED = 0x0001
        }

        public readonly struct HPSS
        {
            public readonly nint Value;
            public bool IsValid => Value is not 0;
        }

        public readonly struct HPSSWALK
        {
            public readonly nint Value;
            public bool IsValid => Value is not 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSS_THREAD_INFORMATION
        {
            public int ThreadsCaptured;
            public int ContextLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PSS_THREAD_ENTRY
        {
            public int ExitStatus;
            public IntPtr TebBaseAddress;
            public int ProcessId;
            public uint ThreadId;
            public UIntPtr AffinityMask;
            public int Priority;
            public int BasePriority;

            public IntPtr LastSyscallFirstArgument;
            public short LastSyscallNumber;

            public ulong CreateTime;
            public ulong ExitTime;
            public ulong KernelTime;
            public ulong UserTime;

            public IntPtr Win32StartAddress;
            public ulong CaptureTime;

            public PSS_THREAD_FLAGS Flags;
            public short SuspendCount;
            public short SizeOfContextRecord;

            public IntPtr ContextRecord;
        }

        [LibraryImport(Libraries.Kernel32)]
        internal static partial int PssCaptureSnapshot(SafeProcessHandle ProcessHandle, PSS_CAPTURE_FLAGS CaptureFlags, int ThreadContextFlags, out HPSS SnapshotHandle);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial int PssFreeSnapshot(IntPtr ProcessHandle, HPSS SnapshotHandle);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial int PssQuerySnapshot(HPSS SnapshotHandle, PSS_QUERY_INFORMATION_CLASS InformationClass, void* Buffer, int BufferLength);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial int PssWalkMarkerCreate(IntPtr Allocator, out HPSSWALK WalkMarkerHandle);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial int PssWalkMarkerFree(HPSSWALK WalkMarkerHandle);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial int PssWalkSnapshot(HPSS SnapshotHandle, PSS_WALK_INFORMATION_CLASS InformationClass, HPSSWALK WalkMarkerHandle, void* Buffer, int BufferLength);
    }
}
