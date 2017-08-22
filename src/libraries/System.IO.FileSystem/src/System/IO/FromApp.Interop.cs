// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;

#pragma warning disable BCL0015
internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool CopyFileFromApp(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        internal static int CopyFile(string src, string dst, bool failIfExists)
        {
            src = PathInternal.EnsureExtendedPrefixOverMaxPath(src);
            dst = PathInternal.EnsureExtendedPrefixOverMaxPath(dst);
            if (!CopyFileFromApp(src, dst, failIfExists))
            {
                return Marshal.GetLastWin32Error();
            }
            return Errors.ERROR_SUCCESS;
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool CreateDirectoryFromApp(string lpPathName, ref SECURITY_ATTRIBUTES lpSecurityAttributes);

        internal static bool CreateDirectory(string path, ref SECURITY_ATTRIBUTES lpSecurityAttributes)
        {
            // We always want to add for CreateDirectory to get around the legacy 248 character limitation
            path = PathInternal.EnsureExtendedPrefix(path);
            return CreateDirectoryFromApp(path, ref lpSecurityAttributes);
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool DeleteFileFromApp(string lpFileName);

        internal static bool DeleteFile(string path)
        {
            path = PathInternal.EnsureExtendedPrefixOverMaxPath(path);
            return DeleteFileFromApp(path);
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern SafeFindHandle FindFirstFileExFromApp(string lpFileName, FINDEX_INFO_LEVELS fInfoLevelId, ref WIN32_FIND_DATA lpFindFileData, FINDEX_SEARCH_OPS fSearchOp, IntPtr lpSearchFilter, int dwAdditionalFlags);

        internal static SafeFindHandle FindFirstFile(string fileName, ref WIN32_FIND_DATA data)
        {
            fileName = PathInternal.EnsureExtendedPrefixOverMaxPath(fileName);

            // use FindExInfoBasic since we don't care about short name and it has better perf
            return FindFirstFileExFromApp(fileName, FINDEX_INFO_LEVELS.FindExInfoBasic, ref data, FINDEX_SEARCH_OPS.FindExSearchNameMatch, IntPtr.Zero, 0);
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool GetFileAttributesExFromApp(string lpFileName, GET_FILEEX_INFO_LEVELS fInfoLevelId, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation);

        internal static bool GetFileAttributesEx(string name, GET_FILEEX_INFO_LEVELS fileInfoLevel, ref WIN32_FILE_ATTRIBUTE_DATA lpFileInformation)
        {
            name = PathInternal.EnsureExtendedPrefixOverMaxPath(name);
            return GetFileAttributesExFromApp(name, fileInfoLevel, ref lpFileInformation);
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool MoveFileFromApp(string lpExistingFileName, string lpNewFileName);

        internal static bool MoveFile(string src, string dst)
        {
            src = PathInternal.EnsureExtendedPrefixOverMaxPath(src);
            dst = PathInternal.EnsureExtendedPrefixOverMaxPath(dst);
            return MoveFileFromApp(src, dst);
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool RemoveDirectoryFromApp(string lpPathName);

        internal static bool RemoveDirectory(string path)
        {
            path = PathInternal.EnsureExtendedPrefixOverMaxPath(path);
            return RemoveDirectoryFromApp(path);
        }

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool ReplaceFileFromApp(
            string lpReplacedFileName, string lpReplacementFileName, string lpBackupFileName,
            int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved);

        internal static bool ReplaceFile(
            string replacedFileName, string replacementFileName, string backupFileName,
            int dwReplaceFlags, IntPtr lpExclude, IntPtr lpReserved)
        {
            replacedFileName = PathInternal.EnsureExtendedPrefixOverMaxPath(replacedFileName);
            replacementFileName = PathInternal.EnsureExtendedPrefixOverMaxPath(replacementFileName);
            backupFileName = PathInternal.EnsureExtendedPrefixOverMaxPath(backupFileName);

            return ReplaceFileFromApp(
                replacedFileName, replacementFileName, backupFileName,
                dwReplaceFlags, lpExclude, lpReserved);
        }

        internal const int REPLACEFILE_WRITE_THROUGH = 0x1;
        internal const int REPLACEFILE_IGNORE_MERGE_ERRORS = 0x2;

        [DllImport("FileApiInterop.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool SetFileAttributesFromApp(string lpFileName, int dwFileAttributes);

        internal static bool SetFileAttributes(string name, int attr)
        {
            name = PathInternal.EnsureExtendedPrefixOverMaxPath(name);
            return SetFileAttributesFromApp(name, attr);
        }

        [DllImport("FileApiInterop.dll", EntryPoint = "CreateFile2FromApp", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern SafeFileHandle CreateFile2(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            FileMode dwCreationDisposition,
            [In] ref CREATEFILE2_EXTENDED_PARAMETERS parameters);

        internal static unsafe SafeFileHandle UnsafeCreateFile(
            string lpFileName,
            int dwDesiredAccess,
            FileShare dwShareMode,
            ref SECURITY_ATTRIBUTES securityAttrs,
            FileMode dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile)
        {
            CREATEFILE2_EXTENDED_PARAMETERS parameters;
            parameters.dwSize = (uint)Marshal.SizeOf<CREATEFILE2_EXTENDED_PARAMETERS>();

            parameters.dwFileAttributes = (uint)dwFlagsAndAttributes & 0x0000FFFF;
            parameters.dwSecurityQosFlags = (uint)dwFlagsAndAttributes & 0x000F0000;
            parameters.dwFileFlags = (uint)dwFlagsAndAttributes & 0xFFF00000;

            parameters.hTemplateFile = hTemplateFile;
            fixed (SECURITY_ATTRIBUTES* lpSecurityAttributes = &securityAttrs)
            {
                parameters.lpSecurityAttributes = (IntPtr)lpSecurityAttributes;
                return CreateFile2(lpFileName, dwDesiredAccess, dwShareMode, dwCreationDisposition, ref parameters);
            }
        }
    }
}
#pragma warning restore BCL0015
