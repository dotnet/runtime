// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

using BOOL = System.Int32;
using DWORD = System.UInt32;
using ULONG = System.UInt32;

internal static partial class Interop
{
    // From WinBase.h
    internal const uint COPY_FILE_FAIL_IF_EXISTS = 0x00000001;

    internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

    internal const int FILE_TYPE_DISK = 0x0001;
    internal const int FILE_TYPE_CHAR = 0x0002;
    internal const int FILE_TYPE_PIPE = 0x0003;

    internal const uint SEM_FAILCRITICALERRORS = 1;

    // From WinDef.h
    internal const int MAX_PATH = 260;
    internal const int MAX_DIRECTORY_PATH = 248; // cannot create directories greater than 248 characters


    // Error codes from WinError.h
    internal const int ERROR_SUCCESS = 0x0;
    internal const int ERROR_INVALID_FUNCTION = 0x1;
    internal const int ERROR_FILE_NOT_FOUND = 0x2;
    internal const int ERROR_PATH_NOT_FOUND = 0x3;
    internal const int ERROR_ACCESS_DENIED = 0x5;
    internal const int ERROR_INVALID_HANDLE = 0x6;
    internal const int ERROR_INVALID_DATA = 0xD;
    internal const int ERROR_INVALID_DRIVE = 0xF;
    internal const int ERROR_NO_MORE_FILES = 0x12;
    internal const int ERROR_NOT_READY = 0x15;
    internal const int ERROR_SHARING_VIOLATION = 0x20;
    internal const int ERROR_FILE_EXISTS = 0x50;
    internal const int ERROR_INVALID_PARAMETER = 0x57;
    internal const int ERROR_BROKEN_PIPE = 0x6D;
    internal const int ERROR_INVALID_NAME = 0x7B;
    internal const int ERROR_NEGATIVE_SEEK = 0x83;
    internal const int ERROR_DIR_NOT_EMPTY = 0x91;
    internal const int ERROR_BAD_PATHNAME = 0xA1;
    internal const int ERROR_ALREADY_EXISTS = 0xB7;
    internal const int ERROR_FILENAME_EXCED_RANGE = 0xCE;  // filename too long.
    internal const int ERROR_PIPE_NOT_CONNECTED = 0xE9;
    internal const int ERROR_OPERATION_ABORTED = 0x3E3;  // 995; For IO Cancellation
    internal const int ERROR_NOT_FOUND = 0x490;          // 1168; For IO Cancellation

    // Constants from WinNT.h
    internal const int FILE_ATTRIBUTE_READONLY = 0x00000001;
    internal const int FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    internal const int FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

    internal const uint GENERIC_READ = 0x80000000;
    internal const uint GENERIC_WRITE = 0x40000000;

    internal const uint IO_REPARSE_TAG_FILE_PLACEHOLDER = 0x80000015;
    internal const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;

    internal const int SECURITY_ANONYMOUS = ((int)SECURITY_IMPERSONATION_LEVEL.SecurityAnonymous << 16);
    internal const int SECURITY_SQOS_PRESENT = 0x00100000;

    internal struct FILE_BASIC_INFO
    {
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public long ChangeTime;
        public uint FileAttributes;
    }

    internal enum FILE_INFO_BY_HANDLE_CLASS : uint
    {
        FileBasicInfo = 0x0u,
        FileStandardInfo = 0x1u,
        FileNameInfo = 0x2u,
        FileRenameInfo = 0x3u,
        FileDispositionInfo = 0x4u,
        FileAllocationInfo = 0x5u,
        FileEndOfFileInfo = 0x6u,
        FileStreamInfo = 0x7u,
        FileCompressionInfo = 0x8u,
        FileAttributeTagInfo = 0x9u,
        FileIdBothDirectoryInfo = 0xAu,
        FileIdBothDirectoryRestartInfo = 0xBu,
        FileIoPriorityHintInfo = 0xCu,
        FileRemoteProtocolInfo = 0xDu,
        FileFullDirectoryInfo = 0xEu,
        FileFullDirectoryRestartInfo = 0xFu,
        FileStorageInfo = 0x10u,
        FileAlignmentInfo = 0x11u,
        FileIdInfo = 0x12u,
        FileIdExtdDirectoryInfo = 0x13u,
        FileIdExtdDirectoryRestartInfo = 0x14u,
        MaximumFileInfoByHandleClass = 0x15u,
    }

    internal partial struct FILE_STANDARD_INFO
    {
        public long AllocationSize;
        public long EndOfFile;
        public uint NumberOfLinks;
        [MarshalAs(UnmanagedType.U1)]
        public bool DeletePending;
        [MarshalAs(UnmanagedType.U1)]
        public bool Directory;
    }

    internal struct FILE_TIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;

        public FILE_TIME(long fileTime)
        {
            dwLowDateTime = (uint)fileTime;
            dwHighDateTime = (uint)(fileTime >> 32);
        }

        public long ToTicks()
        {
            return ((long)dwHighDateTime << 32) + dwLowDateTime;
        }
    }

    internal enum FINDEX_INFO_LEVELS : uint
    {
        FindExInfoStandard = 0x0u,
        FindExInfoBasic = 0x1u,
        FindExInfoMaxInfoLevel = 0x2u,
    }

    internal enum FINDEX_SEARCH_OPS : uint
    {
        FindExSearchNameMatch = 0x0u,
        FindExSearchLimitToDirectories = 0x1u,
        FindExSearchLimitToDevices = 0x2u,
        FindExSearchMaxSearchOp = 0x3u,
    }

    internal enum GET_FILEEX_INFO_LEVELS : uint
    {
        GetFileExInfoStandard = 0x0u,
        GetFileExMaxInfoLevel = 0x1u,
    }

    internal struct SECURITY_ATTRIBUTES
    {
        public uint nLength;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    internal enum SECURITY_IMPERSONATION_LEVEL : uint
    {
        SecurityAnonymous = 0x0u,
        SecurityIdentification = 0x1u,
        SecurityImpersonation = 0x2u,
        SecurityDelegation = 0x3u,
    }

    internal struct WIN32_FILE_ATTRIBUTE_DATA
    {
        internal int fileAttributes;
        internal uint ftCreationTimeLow;
        internal uint ftCreationTimeHigh;
        internal uint ftLastAccessTimeLow;
        internal uint ftLastAccessTimeHigh;
        internal uint ftLastWriteTimeLow;
        internal uint ftLastWriteTimeHigh;
        internal uint fileSizeHigh;
        internal uint fileSizeLow;

        [System.Security.SecurityCritical]
        internal void PopulateFrom(WIN32_FIND_DATA findData)
        {
            // Copy the information to data
            fileAttributes = (int)findData.dwFileAttributes;
            ftCreationTimeLow = findData.ftCreationTime.dwLowDateTime;
            ftCreationTimeHigh = findData.ftCreationTime.dwHighDateTime;
            ftLastAccessTimeLow = findData.ftLastAccessTime.dwLowDateTime;
            ftLastAccessTimeHigh = findData.ftLastAccessTime.dwHighDateTime;
            ftLastWriteTimeLow = findData.ftLastWriteTime.dwLowDateTime;
            ftLastWriteTimeHigh = findData.ftLastWriteTime.dwHighDateTime;
            fileSizeHigh = findData.nFileSizeHigh;
            fileSizeLow = findData.nFileSizeLow;
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    [BestFitMapping(false)]
    internal unsafe struct WIN32_FIND_DATA
    {
        public uint dwFileAttributes;
        public FILE_TIME ftCreationTime;
        public FILE_TIME ftLastAccessTime;
        public FILE_TIME ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    internal static partial class mincore
    {
        // Disallow access to all non-file devices from methods that take
        // a String.  This disallows DOS devices like "con:", "com1:", 
        // "lpt1:", etc.  Use this to avoid security problems, like allowing
        // a web client asking a server for "http://server/com1.aspx" and
        // then causing a worker process to hang.
        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle SafeCreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    ref SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            SafeFileHandle handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode,
                                ref securityAttrs, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile);

            if (!handle.IsInvalid)
            {
                int fileType = Interop.mincore.GetFileType(handle);
                if (fileType != Interop.FILE_TYPE_DISK)
                {
                    handle.Dispose();
                    throw new NotSupportedException(SR.NotSupported_FileStreamOnNonFiles);
                }
            }

            return handle;
        }

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "CreateDirectoryW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool CreateDirectory(
                    String path, ref SECURITY_ATTRIBUTES lpSecurityAttributes);

        [DllImport("api-ms-win-core-file-l1-1-0.dll")]
        internal static extern int GetFileType(SafeFileHandle handle);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlushFileBuffers(SafeFileHandle hFile);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern bool SetEndOfFile(SafeFileHandle hFile);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern bool SetFilePointerEx(SafeFileHandle hFile, long liDistanceToMove, out long lpNewFilePointer, uint dwMoveMethod);

        internal static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);  // WinBase.h

        // Note there are two different ReadFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndRead method called by that callback), 
        // and pass in an address for the numBytesRead parameter.

#if USE_OVERLAPPED
        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, IntPtr numBytesRead_mustBeZero, NativeOverlapped* overlapped);
#endif

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        unsafe internal static extern int ReadFile(SafeFileHandle handle, byte* bytes, int numBytesToRead, out int numBytesRead, IntPtr mustBeZero);

        // Note there are two different WriteFile prototypes - this is to use 
        // the type system to force you to not trip across a "feature" in 
        // Win32's async IO support.  You can't do the following three things
        // simultaneously: overlapped IO, free the memory for the overlapped 
        // struct in a callback (or an EndWrite method called by that callback),
        // and pass in an address for the numBytesRead parameter.

#if USE_OVERLAPPED
        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, IntPtr numBytesWritten_mustBeZero, NativeOverlapped* lpOverlapped);
#endif

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static unsafe extern int WriteFile(SafeFileHandle handle, byte* bytes, int numBytesToWrite, out int numBytesWritten, IntPtr mustBeZero);

#if USE_OVERLAPPED
        [DllImport("api-ms-win-core-io-l1-1-0.dll", SetLastError = true)]
        internal static unsafe extern bool CancelIoEx(SafeFileHandle handle, NativeOverlapped* lpOverlapped);
#endif

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "DeleteFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool DeleteFile(String path);

        // Default values indicate "no change".  Use defaults so that we don't force callsites to be aware of the default values
        internal unsafe static bool SetFileTime(SafeFileHandle hFile, long creationTime = -1, long lastAccessTime = -1,
            long lastWriteTime = -1, long changeTime = -1, uint fileAttributes = 0)
        {
            Interop.FILE_BASIC_INFO basicInfo = new Interop.FILE_BASIC_INFO()
            {
                CreationTime = creationTime,
                LastAccessTime = lastAccessTime,
                LastWriteTime = lastWriteTime,
                ChangeTime = changeTime,
                FileAttributes = fileAttributes
            };

            return Interop.mincore.SetFileInformationByHandle(hFile, Interop.FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, ref basicInfo, (uint)Marshal.SizeOf<Interop.FILE_BASIC_INFO>());
        }

        [DllImport("api-ms-win-core-file-l2-1-0.dll", SetLastError = true)]
        internal static extern bool GetFileInformationByHandleEx(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, out FILE_STANDARD_INFO lpFileInformation, uint dwBufferSize);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern bool SetFileInformationByHandle(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, ref FILE_BASIC_INFO lpFileInformation, uint dwBufferSize);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetFileAttributesExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool GetFileAttributesEx(String name, Interop.GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "SetFileAttributesW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool SetFileAttributes(String name, int attr);

        [DllImport("api-ms-win-core-file-l2-1-0.dll", EntryPoint = "MoveFileExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        private static extern bool MoveFileEx(String src, String dst, uint flags);

        //[DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError=true, CharSet=CharSet.Unicode, BestFitMapping=false)]
        //internal static extern SafeFindHandle FindFirstFile(String fileName, [In, Out] Interop.WIN32_FIND_DATA data);

        internal static bool MoveFile(String src, String dst)
        {
            return MoveFileEx(src, dst, 2 /* MOVEFILE_COPY_ALLOWED */);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static SafeFileHandle UnsafeCreateFile(String lpFileName,
                    int dwDesiredAccess, System.IO.FileShare dwShareMode,
                    ref SECURITY_ATTRIBUTES securityAttrs, System.IO.FileMode dwCreationDisposition,
                    int dwFlagsAndAttributes, IntPtr hTemplateFile)
        {
            SafeFileHandle handle = CreateFile(lpFileName, dwDesiredAccess, dwShareMode,
                                ref securityAttrs, dwCreationDisposition,
                                dwFlagsAndAttributes, hTemplateFile);

            return handle;
        }

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", EntryPoint = "GetCurrentDirectoryW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern int GetCurrentDirectory(
                  int nBufferLength,
                  [Out]StringBuilder lpBuffer);

        [DllImport("api-ms-win-core-processenvironment-l1-1-0.dll", EntryPoint = "SetCurrentDirectoryW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool SetCurrentDirectory(String path);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetLongPathNameW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = false)]
        internal static extern int GetLongPathName(String path, [Out]StringBuilder longPathBuffer, int bufferLength);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "DeleteVolumeMountPointW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool DeleteVolumeMountPoint(String mountPoint);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "RemoveDirectoryW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool RemoveDirectory(String path);

        [DllImport("api-ms-win-core-handle-l1-1-0.dll", SetLastError = true)]
        internal static extern bool CloseHandle(IntPtr handle);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "FindFirstFileExW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern SafeFindHandle FindFirstFileEx(String lpFileName, FINDEX_INFO_LEVELS fInfoLevelId, ref WIN32_FIND_DATA lpFindFileData, FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

        internal static SafeFindHandle FindFirstFile(String fileName, ref Interop.WIN32_FIND_DATA data)
        {
            // use FindExInfoBasic since we don't care about short name and it has better perf
            return FindFirstFileEx(fileName, FINDEX_INFO_LEVELS.FindExInfoBasic, ref data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, 0);
        }

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "FindNextFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false)]
        internal static extern bool FindNextFile(
                    SafeFindHandle hndFindFile,
                    ref WIN32_FIND_DATA lpFindFileData);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal extern static bool FindClose(IntPtr hFindFile);
    }
}
