// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    internal sealed partial class Win32FileSystem : FileSystem
    {
        internal const int GENERIC_READ = unchecked((int)0x80000000);

        public override int MaxPath { get { return Interop.Kernel32.MAX_PATH; } }
        public override int MaxDirectoryPath { get { return Interop.Kernel32.MAX_DIRECTORY_PATH; } }

        public override void CopyFile(string sourceFullPath, string destFullPath, bool overwrite)
        {
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
            int errorCode = Interop.Kernel32.CopyFile(sourceFullPath, destFullPath, !overwrite);

            if (errorCode != Interop.Errors.ERROR_SUCCESS)
            {
                string fileName = destFullPath;

                if (errorCode != Interop.Errors.ERROR_FILE_EXISTS)
                {
                    // For a number of error codes (sharing violation, path 
                    // not found, etc) we don't know if the problem was with
                    // the source or dest file.  Try reading the source file.
                    using (SafeFileHandle handle = Interop.Kernel32.UnsafeCreateFile(sourceFullPath, GENERIC_READ, FileShare.Read, ref secAttrs, FileMode.Open, 0, IntPtr.Zero))
                    {
                        if (handle.IsInvalid)
                            fileName = sourceFullPath;
                    }

                    if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
                    {
                        if (DirectoryExists(destFullPath))
                            throw new IOException(SR.Format(SR.Arg_FileIsDirectory_Name, destFullPath), Interop.Errors.ERROR_ACCESS_DENIED);
                    }
                }

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fileName);
            }
        }

        public override void ReplaceFile(string sourceFullPath, string destFullPath, string destBackupFullPath, bool ignoreMetadataErrors)
        {
            int flags = Interop.Kernel32.REPLACEFILE_WRITE_THROUGH;
            if (ignoreMetadataErrors)
            {
                flags |= Interop.Kernel32.REPLACEFILE_IGNORE_MERGE_ERRORS;
            }

            if (!Interop.Kernel32.ReplaceFile(destFullPath, sourceFullPath, destBackupFullPath, flags, IntPtr.Zero, IntPtr.Zero))
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        [System.Security.SecuritySafeCritical]
        public override void CreateDirectory(string fullPath)
        {
            if (PathInternal.IsDirectoryTooLong(fullPath))
                throw new PathTooLongException(SR.IO_PathTooLong);

            // We can save a bunch of work if the directory we want to create already exists.  This also
            // saves us in the case where sub paths are inaccessible (due to ERROR_ACCESS_DENIED) but the
            // final path is accessible and the directory already exists.  For example, consider trying
            // to create c:\Foo\Bar\Baz, where everything already exists but ACLS prevent access to c:\Foo
            // and c:\Foo\Bar.  In that case, this code will think it needs to create c:\Foo, and c:\Foo\Bar
            // and fail to due so, causing an exception to be thrown.  This is not what we want.
            if (DirectoryExists(fullPath))
                return;

            List<string> stackDir = new List<string>();

            // Attempt to figure out which directories don't exist, and only
            // create the ones we need.  Note that InternalExists may fail due
            // to Win32 ACL's preventing us from seeing a directory, and this
            // isn't threadsafe.

            bool somepathexists = false;

            int length = fullPath.Length;

            // We need to trim the trailing slash or the code will try to create 2 directories of the same name.
            if (length >= 2 && PathHelpers.EndsInDirectorySeparator(fullPath))
                length--;

            int lengthRoot = PathInternal.GetRootLength(fullPath);

            if (length > lengthRoot)
            {
                // Special case root (fullpath = X:\\)
                int i = length - 1;
                while (i >= lengthRoot && !somepathexists)
                {
                    String dir = fullPath.Substring(0, i + 1);

                    if (!DirectoryExists(dir)) // Create only the ones missing
                        stackDir.Add(dir);
                    else
                        somepathexists = true;

                    while (i > lengthRoot && !PathInternal.IsDirectorySeparator(fullPath[i])) i--;
                    i--;
                }
            }

            int count = stackDir.Count;

            // If we were passed a DirectorySecurity, convert it to a security
            // descriptor and set it in he call to CreateDirectory.
            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default(Interop.Kernel32.SECURITY_ATTRIBUTES);

            bool r = true;
            int firstError = 0;
            String errorString = fullPath;

            // If all the security checks succeeded create all the directories
            while (stackDir.Count > 0)
            {
                String name = stackDir[stackDir.Count - 1];
                stackDir.RemoveAt(stackDir.Count - 1);

                r = Interop.Kernel32.CreateDirectory(name, ref secAttrs);
                if (!r && (firstError == 0))
                {
                    int currentError = Marshal.GetLastWin32Error();
                    // While we tried to avoid creating directories that don't
                    // exist above, there are at least two cases that will 
                    // cause us to see ERROR_ALREADY_EXISTS here.  InternalExists 
                    // can fail because we didn't have permission to the 
                    // directory.  Secondly, another thread or process could
                    // create the directory between the time we check and the
                    // time we try using the directory.  Thirdly, it could
                    // fail because the target does exist, but is a file.
                    if (currentError != Interop.Errors.ERROR_ALREADY_EXISTS)
                        firstError = currentError;
                    else
                    {
                        // If there's a file in this directory's place, or if we have ERROR_ACCESS_DENIED when checking if the directory already exists throw.
                        if (File.InternalExists(name) || (!DirectoryExists(name, out currentError) && currentError == Interop.Errors.ERROR_ACCESS_DENIED))
                        {
                            firstError = currentError;
                            errorString = name;
                        }
                    }
                }
            }

            // We need this check to mask OS differences
            // Handle CreateDirectory("X:\\") when X: doesn't exist. Similarly for n/w paths.
            if ((count == 0) && !somepathexists)
            {
                String root = Directory.InternalGetDirectoryRoot(fullPath);
                if (!DirectoryExists(root))
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, root);
                return;
            }

            // Only throw an exception if creating the exact directory we 
            // wanted failed to work correctly.
            if (!r && (firstError != 0))
                throw Win32Marshal.GetExceptionForWin32Error(firstError, errorString);
        }

        public override void DeleteFile(System.String fullPath)
        {
            bool r = Interop.Kernel32.DeleteFile(fullPath);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    return;
                else
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        public override bool DirectoryExists(string fullPath)
        {
            int lastError = Interop.Errors.ERROR_SUCCESS;
            return DirectoryExists(fullPath, out lastError);
        }

        private bool DirectoryExists(String path, out int lastError)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            lastError = FillAttributeInfo(path, ref data, false, true);

            return (lastError == 0) && (data.fileAttributes != -1)
                    && ((data.fileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0);
        }

        public override IEnumerable<string> EnumeratePaths(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            return Win32FileSystemEnumerableFactory.CreateFileNameIterator(fullPath, fullPath, searchPattern,
                (searchTarget & SearchTarget.Files) == SearchTarget.Files,
                (searchTarget & SearchTarget.Directories) == SearchTarget.Directories,
                searchOption);
        }

        public override IEnumerable<FileSystemInfo> EnumerateFileSystemInfos(string fullPath, string searchPattern, SearchOption searchOption, SearchTarget searchTarget)
        {
            switch (searchTarget)
            {
                case SearchTarget.Directories:
                    return Win32FileSystemEnumerableFactory.CreateDirectoryInfoIterator(fullPath, fullPath, searchPattern, searchOption);
                case SearchTarget.Files:
                    return Win32FileSystemEnumerableFactory.CreateFileInfoIterator(fullPath, fullPath, searchPattern, searchOption);
                case SearchTarget.Both:
                    return Win32FileSystemEnumerableFactory.CreateFileSystemInfoIterator(fullPath, fullPath, searchPattern, searchOption);
                default:
                    throw new ArgumentException(SR.ArgumentOutOfRange_Enum, nameof(searchTarget));
            }
        }

        // Returns 0 on success, otherwise a Win32 error code.  Note that
        // classes should use -1 as the uninitialized state for dataInitialized.
        [System.Security.SecurityCritical]  // auto-generated
        internal static int FillAttributeInfo(String path, ref Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data, bool tryagain, bool returnErrorOnNotFound)
        {
            int errorCode = 0;
            if (tryagain) // someone has a handle to the file open, or other error
            {
                Interop.Kernel32.WIN32_FIND_DATA findData;
                findData = new Interop.Kernel32.WIN32_FIND_DATA();

                // Remove trailing slash since this can cause grief to FindFirstFile. You will get an invalid argument error
                String tempPath = path.TrimEnd(PathHelpers.DirectorySeparatorChars);

                // For floppy drives, normally the OS will pop up a dialog saying
                // there is no disk in drive A:, please insert one.  We don't want that.
                // SetErrorMode will let us disable this, but we should set the error
                // mode back, since this may have wide-ranging effects.
                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    bool error = false;
                    SafeFindHandle handle = Interop.Kernel32.FindFirstFile(tempPath, ref findData);
                    try
                    {
                        if (handle.IsInvalid)
                        {
                            error = true;
                            errorCode = Marshal.GetLastWin32Error();

                            if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND ||
                                errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND ||
                                errorCode == Interop.Errors.ERROR_NOT_READY)  // floppy device not ready
                            {
                                if (!returnErrorOnNotFound)
                                {
                                    // Return default value for backward compatibility
                                    errorCode = 0;
                                    data.fileAttributes = -1;
                                }
                            }
                            return errorCode;
                        }
                    }
                    finally
                    {
                        // Close the Win32 handle
                        try
                        {
                            handle.Dispose();
                        }
                        catch
                        {
                            // if we're already returning an error, don't throw another one. 
                            if (!error)
                            {
                                throw Win32Marshal.GetExceptionForLastWin32Error();
                            }
                        }
                    }
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
                }

                // Copy the information to data
                data.PopulateFrom(ref findData);
            }
            else
            {
                // For floppy drives, normally the OS will pop up a dialog saying
                // there is no disk in drive A:, please insert one.  We don't want that.
                // SetErrorMode will let us disable this, but we should set the error
                // mode back, since this may have wide-ranging effects.
                bool success = false;
                uint oldMode = Interop.Kernel32.SetErrorMode(Interop.Kernel32.SEM_FAILCRITICALERRORS);
                try
                {
                    success = Interop.Kernel32.GetFileAttributesEx(path, Interop.Kernel32.GET_FILEEX_INFO_LEVELS.GetFileExInfoStandard, ref data);
                }
                finally
                {
                    Interop.Kernel32.SetErrorMode(oldMode);
                }

                if (!success)
                {
                    errorCode = Marshal.GetLastWin32Error();
                    if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND &&
                        errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND &&
                        errorCode != Interop.Errors.ERROR_NOT_READY)  // floppy device not ready
                    {
                        // In case someone latched onto the file. Take the perf hit only for failure
                        return FillAttributeInfo(path, ref data, true, returnErrorOnNotFound);
                    }
                    else
                    {
                        if (!returnErrorOnNotFound)
                        {
                            // Return default value for backward compatibility
                            errorCode = 0;
                            data.fileAttributes = -1;
                        }
                    }
                }
            }

            return errorCode;
        }

        public override bool FileExists(System.String fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            int errorCode = FillAttributeInfo(fullPath, ref data, false, true);

            return (errorCode == 0) && (data.fileAttributes != -1)
                    && ((data.fileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0);
        }

        public override FileAttributes GetAttributes(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            int errorCode = FillAttributeInfo(fullPath, ref data, false, true);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            return (FileAttributes)data.fileAttributes;
        }

        public override string GetCurrentDirectory()
        {
            StringBuilder sb = StringBuilderCache.Acquire(Interop.Kernel32.MAX_PATH + 1);
            if (Interop.Kernel32.GetCurrentDirectory(sb.Capacity, sb) == 0)
                throw Win32Marshal.GetExceptionForLastWin32Error();
            String currentDirectory = sb.ToString();
            // Note that if we have somehow put our command prompt into short
            // file name mode (i.e. by running edlin or a DOS grep, etc), then
            // this will return a short file name.
            if (currentDirectory.IndexOf('~') >= 0)
            {
                int r = Interop.Kernel32.GetLongPathName(currentDirectory, sb, sb.Capacity);
                if (r == 0 || r >= Interop.Kernel32.MAX_PATH)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    if (r >= Interop.Kernel32.MAX_PATH)
                        errorCode = Interop.Errors.ERROR_FILENAME_EXCED_RANGE;
                    if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND &&
                        errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND &&
                        errorCode != Interop.Errors.ERROR_INVALID_FUNCTION &&  // by design - enough said.
                        errorCode != Interop.Errors.ERROR_ACCESS_DENIED)
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode);
                }
                currentDirectory = sb.ToString();
            }
            StringBuilderCache.Release(sb);

            return currentDirectory;
        }

        public override DateTimeOffset GetCreationTime(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            int errorCode = FillAttributeInfo(fullPath, ref data, false, false);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            long dt = ((long)(data.ftCreationTimeHigh) << 32) | ((long)data.ftCreationTimeLow);
            return DateTimeOffset.FromFileTime(dt);
        }

        public override IFileSystemObject GetFileSystemInfo(string fullPath, bool asDirectory)
        {
            return asDirectory ?
                (IFileSystemObject)new DirectoryInfo(fullPath, null) :
                (IFileSystemObject)new FileInfo(fullPath, null);
        }

        public override DateTimeOffset GetLastAccessTime(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            int errorCode = FillAttributeInfo(fullPath, ref data, false, false);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            long dt = ((long)(data.ftLastAccessTimeHigh) << 32) | ((long)data.ftLastAccessTimeLow);
            return DateTimeOffset.FromFileTime(dt);
        }

        public override DateTimeOffset GetLastWriteTime(string fullPath)
        {
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            int errorCode = FillAttributeInfo(fullPath, ref data, false, false);
            if (errorCode != 0)
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);

            long dt = ((long)data.ftLastWriteTimeHigh << 32) | ((long)data.ftLastWriteTimeLow);
            return DateTimeOffset.FromFileTime(dt);
        }

        public override void MoveDirectory(string sourceFullPath, string destFullPath)
        {
            if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath))
            {
                int errorCode = Marshal.GetLastWin32Error();

                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    throw Win32Marshal.GetExceptionForWin32Error(Interop.Errors.ERROR_PATH_NOT_FOUND, sourceFullPath);

                // This check was originally put in for Win9x (unfortunately without special casing it to be for Win9x only). We can't change the NT codepath now for backcomp reasons.
                if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED) // WinNT throws IOException. This check is for Win9x. We can't change it for backcomp.
                    throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, sourceFullPath), Win32Marshal.MakeHRFromErrorCode(errorCode));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }

        public override void MoveFile(string sourceFullPath, string destFullPath)
        {
            if (!Interop.Kernel32.MoveFile(sourceFullPath, destFullPath))
            {
                throw Win32Marshal.GetExceptionForLastWin32Error();
            }
        }

        public override FileStream Open(string fullPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, FileStream parent)
        {
            return new FileStream(fullPath, mode, access, share, bufferSize, options);
        }

        [System.Security.SecurityCritical]
        private static SafeFileHandle OpenHandle(string fullPath, bool asDirectory)
        {
            String root = fullPath.Substring(0, PathInternal.GetRootLength(fullPath));
            if (root == fullPath && root[1] == Path.VolumeSeparatorChar)
            {
                // intentionally not fullpath, most upstack public APIs expose this as path.
                throw new ArgumentException(SR.Arg_PathIsVolume, "path");
            }

            Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = default(Interop.Kernel32.SECURITY_ATTRIBUTES);
            SafeFileHandle handle = Interop.Kernel32.SafeCreateFile(
                fullPath,
                (int)Interop.Kernel32.GenericOperations.GENERIC_WRITE,
                FileShare.ReadWrite | FileShare.Delete,
                ref secAttrs,
                FileMode.Open,
                asDirectory ? (int)Interop.Kernel32.FileOperations.FILE_FLAG_BACKUP_SEMANTICS : (int)FileOptions.None,
                IntPtr.Zero
            );

            if (handle.IsInvalid)
            {
                int errorCode = Marshal.GetLastWin32Error();

                // NT5 oddity - when trying to open "C:\" as a File,
                // we usually get ERROR_PATH_NOT_FOUND from the OS.  We should
                // probably be consistent w/ every other directory.
                if (!asDirectory && errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && fullPath.Equals(Directory.GetDirectoryRoot(fullPath)))
                    errorCode = Interop.Errors.ERROR_ACCESS_DENIED;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
            return handle;
        }
        public override void RemoveDirectory(string fullPath, bool recursive)
        {
            // Do not recursively delete through reparse points.  Perhaps in a 
            // future version we will add a new flag to control this behavior, 
            // but for now we're much safer if we err on the conservative side.
            // This applies to symbolic links and mount points.
            Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA data = new Interop.Kernel32.WIN32_FILE_ATTRIBUTE_DATA();
            int errorCode = FillAttributeInfo(fullPath, ref data, false, true);
            if (errorCode != 0)
            {
                // Ensure we throw a DirectoryNotFoundException.
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }

            if (((FileAttributes)data.fileAttributes & FileAttributes.ReparsePoint) != 0)
                recursive = false;

            // We want extended syntax so we can delete "extended" subdirectories and files
            // (most notably ones with trailing whitespace or periods)
            RemoveDirectoryHelper(PathInternal.EnsureExtendedPrefix(fullPath), recursive, true);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private static void RemoveDirectoryHelper(string fullPath, bool recursive, bool throwOnTopLevelDirectoryNotFound)
        {
            bool r;
            int errorCode;
            Exception ex = null;

            // Do not recursively delete through reparse points.  Perhaps in a 
            // future version we will add a new flag to control this behavior, 
            // but for now we're much safer if we err on the conservative side.
            // This applies to symbolic links and mount points.
            // Note the logic to check whether fullPath is a reparse point is
            // in Delete(String, String, bool), and will set "recursive" to false.
            // Note that Win32's DeleteFile and RemoveDirectory will just delete
            // the reparse point itself.

            if (recursive)
            {
                Interop.Kernel32.WIN32_FIND_DATA data = new Interop.Kernel32.WIN32_FIND_DATA();

                // Open a Find handle
                using (SafeFindHandle hnd = Interop.Kernel32.FindFirstFile(Directory.EnsureTrailingDirectorySeparator(fullPath) + "*", ref data))
                {
                    if (hnd.IsInvalid)
                        throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);

                    do
                    {
                        bool isDir = (0 != (data.dwFileAttributes & Interop.Kernel32.FileAttributes.FILE_ATTRIBUTE_DIRECTORY));
                        if (isDir)
                        {
                            // Skip ".", "..".
                            if (data.cFileName.Equals(".") || data.cFileName.Equals(".."))
                                continue;

                            // Recurse for all directories, unless they are 
                            // reparse points.  Do not follow mount points nor
                            // symbolic links, but do delete the reparse point 
                            // itself.
                            bool shouldRecurse = (0 == (data.dwFileAttributes & (int)FileAttributes.ReparsePoint));
                            if (shouldRecurse)
                            {
                                string newFullPath = Path.Combine(fullPath, data.cFileName);
                                try
                                {
                                    RemoveDirectoryHelper(newFullPath, recursive, false);
                                }
                                catch (Exception e)
                                {
                                    if (ex == null)
                                        ex = e;
                                }
                            }
                            else
                            {
                                // Check to see if this is a mount point, and
                                // unmount it.
                                if (data.dwReserved0 == Interop.Kernel32.IOReparseOptions.IO_REPARSE_TAG_MOUNT_POINT)
                                {
                                    // Use full path plus a trailing '\'
                                    String mountPoint = Path.Combine(fullPath, data.cFileName + PathHelpers.DirectorySeparatorCharAsString);
                                    if (!Interop.Kernel32.DeleteVolumeMountPoint(mountPoint))
                                    {
                                         errorCode = Marshal.GetLastWin32Error();
                                    
                                        if (errorCode != Interop.Errors.ERROR_SUCCESS && 
                                            errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
                                        {
                                            try
                                            {
                                                throw Win32Marshal.GetExceptionForWin32Error(errorCode, data.cFileName);
                                            }
                                            catch (Exception e)
                                            {
                                                if (ex == null)
                                                    ex = e;
                                            }
                                        }
                                    }
                                }

                                // RemoveDirectory on a symbolic link will
                                // remove the link itself.
                                String reparsePoint = Path.Combine(fullPath, data.cFileName);
                                r = Interop.Kernel32.RemoveDirectory(reparsePoint);
                                if (!r)
                                {
                                    errorCode = Marshal.GetLastWin32Error();
                                    if (errorCode != Interop.Errors.ERROR_PATH_NOT_FOUND)
                                    {
                                        try
                                        {
                                            throw Win32Marshal.GetExceptionForWin32Error(errorCode, data.cFileName);
                                        }
                                        catch (Exception e)
                                        {
                                            if (ex == null)
                                                ex = e;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            String fileName = Path.Combine(fullPath, data.cFileName);
                            r = Interop.Kernel32.DeleteFile(fileName);
                            if (!r)
                            {
                                errorCode = Marshal.GetLastWin32Error();
                                if (errorCode != Interop.Errors.ERROR_FILE_NOT_FOUND)
                                {
                                    try
                                    {
                                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, data.cFileName);
                                    }
                                    catch (Exception e)
                                    {
                                        if (ex == null)
                                            ex = e;
                                    }
                                }
                            }
                        }
                    } while (Interop.Kernel32.FindNextFile(hnd, ref data));
                    // Make sure we quit with a sensible error.
                    errorCode = Marshal.GetLastWin32Error();
                }

                if (ex != null)
                    throw ex;
                if (errorCode != 0 && errorCode != Interop.Errors.ERROR_NO_MORE_FILES)
                    throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }

            r = Interop.Kernel32.RemoveDirectory(fullPath);

            if (!r)
            {
                errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND) // A dubious error code.
                    errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
                // This check was originally put in for Win9x (unfortunately without special casing it to be for Win9x only). We can't change the NT codepath now for backcomp reasons.
                if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
                    throw new IOException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, fullPath));

                // don't throw the DirectoryNotFoundException since this is a subdir and 
                // there could be a race condition between two Directory.Delete callers
                if (errorCode == Interop.Errors.ERROR_PATH_NOT_FOUND && !throwOnTopLevelDirectoryNotFound)
                    return;

                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        public override void SetAttributes(string fullPath, FileAttributes attributes)
        {
            SetAttributesInternal(fullPath, attributes);
        }

        private static void SetAttributesInternal(string fullPath, FileAttributes attributes)
        {
            bool r = Interop.Kernel32.SetFileAttributes(fullPath, (int)attributes);
            if (!r)
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_INVALID_PARAMETER)
                    throw new ArgumentException(SR.Arg_InvalidFileAttrs, nameof(attributes));
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        public override void SetCreationTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            SetCreationTimeInternal(fullPath, time, asDirectory);
        }

        private static void SetCreationTimeInternal(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            using (SafeFileHandle handle = OpenHandle(fullPath, asDirectory))
            {
                bool r = Interop.Kernel32.SetFileTime(handle, creationTime: time.ToFileTime());
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
                }
            }
        }

        public override void SetCurrentDirectory(string fullPath)
        {
            if (!Interop.Kernel32.SetCurrentDirectory(fullPath))
            {
                // If path doesn't exist, this sets last error to 2 (File 
                // not Found).  LEGACY: This may potentially have worked correctly
                // on Win9x, maybe.
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_FILE_NOT_FOUND)
                    errorCode = Interop.Errors.ERROR_PATH_NOT_FOUND;
                throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
            }
        }

        public override void SetLastAccessTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            SetLastAccessTimeInternal(fullPath, time, asDirectory);
        }

        private static void SetLastAccessTimeInternal(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            using (SafeFileHandle handle = OpenHandle(fullPath, asDirectory))
            {
                bool r = Interop.Kernel32.SetFileTime(handle, lastAccessTime: time.ToFileTime());
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
                }
            }
        }

        public override void SetLastWriteTime(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            SetLastWriteTimeInternal(fullPath, time, asDirectory);
        }

        private static void SetLastWriteTimeInternal(string fullPath, DateTimeOffset time, bool asDirectory)
        {
            using (SafeFileHandle handle = OpenHandle(fullPath, asDirectory))
            {
                bool r = Interop.Kernel32.SetFileTime(handle, lastWriteTime: time.ToFileTime());
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error(fullPath);
                }
            }
        }

        public override string[] GetLogicalDrives()
        {
            return DriveInfoInternal.GetLogicalDrives();
        }
    }
}
