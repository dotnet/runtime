// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

internal static class DllImport
{
#if !uap
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
    internal static extern Win32Handle CreateFile(String lpFileName,
       FileAccess dwDesiredAccess, FileShare dwShareMode,
       IntPtr securityAttrs, CreationDisposition dwCreationDisposition,
       FileAttributes dwFlagsAndAttributes, IntPtr hTemplateFile);
#else
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
    internal static unsafe extern Win32Handle CreateFile2(string lpFileName,
        FileAccess dwDesiredAccess, FileShare dwShareMode,
        CreationDisposition dwCreationDisposition, CREATEFILE2_EXTENDED_PARAMETERS* pCreateExParams);

    [StructLayout(LayoutKind.Sequential)]
    internal struct CREATEFILE2_EXTENDED_PARAMETERS
    {
        internal uint dwSize;
        internal FileAttributes dwFileAttributes;
        internal FileAttributes dwFileFlags;
        internal uint dwSecurityQosFlags;
        internal IntPtr lpSecurityAttributes;
        internal IntPtr hTemplateFile;
    }
#endif

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern unsafe int WriteFile(SafeHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);


    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    internal static extern bool GetHandleInformation(IntPtr handle, out int flags);

    internal const int ERROR_IO_PENDING = 0x000003E5;

    [Flags]
    internal enum FileAccess : uint
    {
        FILE_LIST_DIRECTORY = 0x1,
        GenericRead = 0x80000000,
        GenericWrite = 0x40000000,
        GenericExecute = 0x20000000,
        GenericAll = 0x10000000,
    }

    [Flags]
    internal enum FileShare : uint
    {
        None = 0x00000000,
        Read = 0x00000001,
        Write = 0x00000002,
        Delete = 0x00000004,
    }

    internal enum CreationDisposition : uint
    {
        New = 1,
        CreateAlways = 2,
        OpenExisting = 3,
        OpenAlways = 4,
        TruncateExisting = 5,
    }

    [Flags]
    internal enum FileAttributes : uint
    {
        Readonly = 0x00000001,
        Hidden = 0x00000002,
        System = 0x00000004,
        Directory = 0x00000010,
        Archive = 0x00000020,
        Device = 0x00000040,
        Normal = 0x00000080,
        Temporary = 0x00000100,
        SparseFile = 0x00000200,
        ReparsePoint = 0x00000400,
        Compressed = 0x00000800,
        Offline = 0x00001000,
        NotContentIndexed = 0x00002000,
        Encrypted = 0x00004000,
        Write_Through = 0x80000000,
        Overlapped = 0x40000000,
        NoBuffering = 0x20000000,
        RandomAccess = 0x10000000,
        SequentialScan = 0x08000000,
        DeleteOnClose = 0x04000000,
        BackupSemantics = 0x02000000,
        PosixSemantics = 0x01000000,
        OpenReparsePoint = 0x00200000,
        OpenNoRecall = 0x00100000,
        FirstPipeInstance = 0x00080000
    }
}
