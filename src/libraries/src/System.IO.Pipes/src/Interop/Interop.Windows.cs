// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

internal static partial class Interop
{
    private const String KERNEL32DLL = "kernel32.dll";
    private const String HANDLEDLL = "api-ms-win-core-handle-l1-1-0.dll";
    private const String PROCESSTHREADSDLL = "api-ms-win-core-processthreads-l1-1-0.dll";
    private const String FILEDLL = "api-ms-win-core-file-l1-1-0.dll";
    private const String NAMEDPIPEDLL = "api-ms-win-core-namedpipe-l1-1-0.dll";
    private const String IODLL = "api-ms-win-core-io-l1-1-0.dll";

    internal static readonly IntPtr NULL = IntPtr.Zero;

    //
    // Win32 IO
    //
    internal const int CREDUI_MAX_USERNAME_LENGTH = 513;

    // WinError.h codes:

    internal const int ERROR_SUCCESS = 0x0;
    internal const int ERROR_FILE_NOT_FOUND = 0x2;
    internal const int ERROR_PATH_NOT_FOUND = 0x3;
    internal const int ERROR_ACCESS_DENIED = 0x5;
    internal const int ERROR_INVALID_HANDLE = 0x6;

    // Can occurs when filled buffers are trying to flush to disk, but disk IOs are not fast enough. 
    // This happens when the disk is slow and event traffic is heavy. 
    // Eventually, there are no more free (empty) buffers and the event is dropped.
    internal const int ERROR_NOT_ENOUGH_MEMORY = 0x8;

    internal const int ERROR_INVALID_DRIVE = 0xF;
    internal const int ERROR_NO_MORE_FILES = 0x12;
    internal const int ERROR_NOT_READY = 0x15;
    internal const int ERROR_BAD_LENGTH = 0x18;
    internal const int ERROR_SHARING_VIOLATION = 0x20;
    internal const int ERROR_LOCK_VIOLATION = 0x21;  // 33
    internal const int ERROR_HANDLE_EOF = 0x26;  // 38
    internal const int ERROR_FILE_EXISTS = 0x50;
    internal const int ERROR_INVALID_PARAMETER = 0x57;  // 87
    internal const int ERROR_BROKEN_PIPE = 0x6D;  // 109
    internal const int ERROR_INSUFFICIENT_BUFFER = 0x7A;  // 122
    internal const int ERROR_INVALID_NAME = 0x7B;
    internal const int ERROR_BAD_PATHNAME = 0xA1;
    internal const int ERROR_ALREADY_EXISTS = 0xB7;
    internal const int ERROR_ENVVAR_NOT_FOUND = 0xCB;
    internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long
    internal const int ERROR_PIPE_BUSY = 0xE7;  // 231
    internal const int ERROR_NO_DATA = 0xE8;  // 232
    internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;  // 233
    internal const int ERROR_MORE_DATA = 0xEA;
    internal const int ERROR_NO_MORE_ITEMS = 0x103;  // 259
    internal const int ERROR_PIPE_CONNECTED = 0x217;  // 535
    internal const int ERROR_PIPE_LISTENING = 0x218;  // 536
    internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
    internal const int ERROR_IO_PENDING = 0x3E5;  // 997
    internal const int ERROR_NOT_FOUND = 0x490;  // 1168      

    // The event size is larger than the allowed maximum (64k - header).
    internal const int ERROR_ARITHMETIC_OVERFLOW = 0x216;  // 534

    internal const int ERROR_RESOURCE_LANG_NOT_FOUND = 0x717;  // 1815


    // Event log specific codes:

    internal const int ERROR_EVT_MESSAGE_NOT_FOUND = 15027;
    internal const int ERROR_EVT_MESSAGE_ID_NOT_FOUND = 15028;
    internal const int ERROR_EVT_UNRESOLVED_VALUE_INSERT = 15029;
    internal const int ERROR_EVT_UNRESOLVED_PARAMETER_INSERT = 15030;
    internal const int ERROR_EVT_MAX_INSERTS_REACHED = 15031;
    internal const int ERROR_EVT_MESSAGE_LOCALE_NOT_FOUND = 15033;
    internal const int ERROR_MUI_FILE_NOT_FOUND = 15100;


    internal const int SECURITY_SQOS_PRESENT = 0x00100000;
    internal const int SECURITY_ANONYMOUS = 0 << 16;
    internal const int SECURITY_IDENTIFICATION = 1 << 16;
    internal const int SECURITY_IMPERSONATION = 2 << 16;
    internal const int SECURITY_DELEGATION = 3 << 16;

    internal const int GENERIC_READ = unchecked((int)0x80000000);
    internal const int GENERIC_WRITE = 0x40000000;

    internal const int STD_INPUT_HANDLE = -10;
    internal const int STD_OUTPUT_HANDLE = -11;
    internal const int STD_ERROR_HANDLE = -12;

    internal const int DUPLICATE_SAME_ACCESS = 0x00000002;

    internal const int PIPE_ACCESS_INBOUND = 1;
    internal const int PIPE_ACCESS_OUTBOUND = 2;
    internal const int PIPE_ACCESS_DUPLEX = 3;
    internal const int PIPE_TYPE_BYTE = 0;
    internal const int PIPE_TYPE_MESSAGE = 4;
    internal const int PIPE_READMODE_BYTE = 0;
    internal const int PIPE_READMODE_MESSAGE = 2;
    internal const int PIPE_UNLIMITED_INSTANCES = 255;

    internal const int FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
    internal const int FILE_SHARE_READ = 0x00000001;
    internal const int FILE_SHARE_WRITE = 0x00000002;
    internal const int FILE_ATTRIBUTE_NORMAL = 0x00000080;

    internal const int FILE_FLAG_OVERLAPPED = 0x40000000;

    internal const int OPEN_EXISTING = 3;

    // From WinBase.h
    internal const int FILE_TYPE_DISK = 0x0001;
    internal const int FILE_TYPE_CHAR = 0x0002;
    internal const int FILE_TYPE_PIPE = 0x0003;

    // Memory mapped file constants
    internal const int MEM_COMMIT = 0x1000;
    internal const int MEM_RESERVE = 0x2000;
    internal const int INVALID_FILE_SIZE = -1;
    internal const int PAGE_READWRITE = 0x04;
    internal const int PAGE_READONLY = 0x02;
    internal const int PAGE_WRITECOPY = 0x08;
    internal const int PAGE_EXECUTE_READ = 0x20;
    internal const int PAGE_EXECUTE_READWRITE = 0x40;

    internal const int FILE_MAP_COPY = 0x0001;
    internal const int FILE_MAP_WRITE = 0x0002;
    internal const int FILE_MAP_READ = 0x0004;
    internal const int FILE_MAP_EXECUTE = 0x0020;

    // From WinBase.h
    internal const int SEM_FAILCRITICALERRORS = 1;

    internal static partial class mincore
    {
        // 
        // Pipe
        //

        [DllImport(PROCESSTHREADSDLL, SetLastError = true)]
        [SecurityCritical]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport(HANDLEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DuplicateHandle(IntPtr hSourceProcessHandle,
            SafePipeHandle hSourceHandle, IntPtr hTargetProcessHandle, out SafePipeHandle lpTargetHandle,
            uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        [DllImport(FILEDLL)]
        [SecurityCritical]
        internal static extern int GetFileType(SafePipeHandle handle);

        [DllImport(NAMEDPIPEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CreatePipe(out SafePipeHandle hReadPipe,
            out SafePipeHandle hWritePipe, ref Interop.SECURITY_ATTRIBUTES pipeSecAttrs, int nSize);


        [DllImport(FILEDLL, EntryPoint = "CreateFile", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        [SecurityCritical]
        internal static extern SafePipeHandle CreateNamedPipeClient(String lpFileName,
            int dwDesiredAccess, System.IO.FileShare dwShareMode,
            ref Interop.SECURITY_ATTRIBUTES secAttrs, System.IO.FileMode dwCreationDisposition,
            int dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport(NAMEDPIPEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        unsafe internal static extern bool ConnectNamedPipe(SafePipeHandle handle, NativeOverlapped* overlapped);

        [DllImport(NAMEDPIPEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ConnectNamedPipe(SafePipeHandle handle, IntPtr overlapped);

        [DllImport(NAMEDPIPEDLL, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "WaitNamedPipeW")]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WaitNamedPipe(String name, int timeout);

        [DllImport(KERNEL32DLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeHandleState(SafePipeHandle hNamedPipe, out int lpState,
            IntPtr lpCurInstances, IntPtr lpMaxCollectionCount, IntPtr lpCollectDataTimeout,
            IntPtr lpUserName, int nMaxUserNameSize);

        [DllImport(KERNEL32DLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeHandleState(SafePipeHandle hNamedPipe, IntPtr lpState,
            out int lpCurInstances, IntPtr lpMaxCollectionCount, IntPtr lpCollectDataTimeout,
            IntPtr lpUserName, int nMaxUserNameSize);

        [DllImport(KERNEL32DLL, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "GetNamedPipeHandleStateW")]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeHandleState(SafePipeHandle hNamedPipe, IntPtr lpState,
            IntPtr lpCurInstances, IntPtr lpMaxCollectionCount, IntPtr lpCollectDataTimeout,
            StringBuilder lpUserName, int nMaxUserNameSize);

        [DllImport(KERNEL32DLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeInfo(SafePipeHandle hNamedPipe,
          out int lpFlags,
          IntPtr lpOutBufferSize,
          IntPtr lpInBufferSize,
          IntPtr lpMaxInstances
        );

        [DllImport(KERNEL32DLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeInfo(SafePipeHandle hNamedPipe,
          IntPtr lpFlags,
          out int lpOutBufferSize,
          IntPtr lpInBufferSize,
          IntPtr lpMaxInstances
        );

        [DllImport(KERNEL32DLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetNamedPipeInfo(SafePipeHandle hNamedPipe,
          IntPtr lpFlags,
          IntPtr lpOutBufferSize,
          out int lpInBufferSize,
          IntPtr lpMaxInstances
        );

        [DllImport(NAMEDPIPEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe extern bool SetNamedPipeHandleState(
          SafePipeHandle hNamedPipe,
          int* lpMode,
          IntPtr lpMaxCollectionCount,
          IntPtr lpCollectDataTimeout
        );

        [DllImport(NAMEDPIPEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DisconnectNamedPipe(SafePipeHandle hNamedPipe);

        [DllImport(FILEDLL, SetLastError = true)]
        [SecurityCritical]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlushFileBuffers(SafePipeHandle hNamedPipe);

        [DllImport(NAMEDPIPEDLL, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "CreateNamedPipeW")]
        [SecurityCritical]
        internal static extern SafePipeHandle CreateNamedPipe(string pipeName,
            int openMode, int pipeMode, int maxInstances,
            int outBufferSize, int inBufferSize, int defaultTimeout,
            ref Interop.SECURITY_ATTRIBUTES securityAttributes);

        // Note there are two different ReadFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndRead method called by that callback), 
        // and pass in an address for the numBytesRead parameter.  
        // <STRIP> See Windows Bug 105512 for details.  -- </STRIP>

        [DllImport(FILEDLL, SetLastError = true)]
        [SecurityCritical]
        unsafe internal static extern int ReadFile(SafePipeHandle handle, byte* bytes, int numBytesToRead,
            IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);

        [DllImport(FILEDLL, SetLastError = true)]
        [SecurityCritical]
        unsafe internal static extern int ReadFile(SafePipeHandle handle, byte* bytes, int numBytesToRead,
            out int numBytesRead, IntPtr mustBeZero);

        // Note there are two different WriteFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.  
        // <STRIP> See Windows Bug 105512 for details.  -- </STRIP>

        [DllImport(FILEDLL, SetLastError = true)]
        [SecurityCritical]
        internal static unsafe extern int WriteFile(SafePipeHandle handle, byte* bytes, int numBytesToWrite,
            IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);

        [DllImport(FILEDLL, SetLastError = true)]
        [SecurityCritical]
        internal static unsafe extern int WriteFile(SafePipeHandle handle, byte* bytes, int numBytesToWrite,
            out int numBytesWritten, IntPtr mustBeZero);

        [DllImport(IODLL, SetLastError = true)]
        internal static unsafe extern bool CancelIoEx(SafeHandle handle, NativeOverlapped* lpOverlapped);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
}
