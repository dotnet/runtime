// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal static bool DisableFileLocking { get; } = OperatingSystem.IsBrowser() // #40065: Emscripten does not support file locking
            || AppContextConfigHelper.GetBooleanConfig("System.IO.DisableFileLocking", "DOTNET_SYSTEM_IO_DISABLEFILELOCKING", defaultValue: false);

        // not using bool? as it's not thread safe
        private volatile NullableBool _canSeek = NullableBool.Undefined;
        private volatile NullableBool _supportsRandomAccess = NullableBool.Undefined;
        private bool _deleteOnClose;
        private bool _isLocked;

        public SafeFileHandle() : this(ownsHandle: true)
        {
        }

        private SafeFileHandle(bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(new IntPtr(-1));
        }

        public bool IsAsync { get; private set; }

        internal bool CanSeek => !IsClosed && GetCanSeek();

        internal bool SupportsRandomAccess
        {
            get
            {
                NullableBool supportsRandomAccess = _supportsRandomAccess;
                if (supportsRandomAccess == NullableBool.Undefined)
                {
                    _supportsRandomAccess = supportsRandomAccess = GetCanSeek() ? NullableBool.True : NullableBool.False;
                }

                return supportsRandomAccess == NullableBool.True;
            }
            set
            {
                Debug.Assert(value == false); // We should only use the setter to disable random access.
                _supportsRandomAccess = value ? NullableBool.True : NullableBool.False;
            }
        }

#pragma warning disable CA1822
        internal ThreadPoolBoundHandle? ThreadPoolBinding => null;

        internal void EnsureThreadPoolBindingInitialized() { /* nop */ }

        internal bool TryGetCachedLength(out long cachedLength)
        {
            cachedLength = -1;
            return false;
        }
#pragma warning restore CA1822

        private static SafeFileHandle Open(string path, Interop.Sys.OpenFlags flags, int mode,
                                           Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException)
        {
            Debug.Assert(path != null);
            SafeFileHandle handle = Interop.Sys.Open(path, flags, mode);
            handle._path = path;

            if (handle.IsInvalid)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                handle.Dispose();

                if (createOpenException?.Invoke(error, flags, path) is Exception ex)
                {
                    throw ex;
                }

                // If we fail to open the file due to a path not existing, we need to know whether to blame
                // the file itself or its directory.  If we're creating the file, then we blame the directory,
                // otherwise we blame the file.
                //
                // When opening, we need to align with Windows, which considers a missing path to be
                // FileNotFound only if the containing directory exists.

                bool isDirectory = (error.Error == Interop.Error.ENOENT) &&
                    ((flags & Interop.Sys.OpenFlags.O_CREAT) != 0
                    || !DirectoryExists(System.IO.Path.GetDirectoryName(System.IO.Path.TrimEndingDirectorySeparator(path!))!));

                Interop.CheckIo(
                    error.Error,
                    path,
                    isDirectory,
                    errorRewriter: e => (e.Error == Interop.Error.EISDIR) ? Interop.Error.EACCES.Info() : e);
            }

            return handle;
        }

        private static bool DirectoryExists(string fullPath)
        {
            Interop.Sys.FileStatus fileinfo;

            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0)
            {
                return false;
            }

            return ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR);
        }

        // Each thread will have its own copy. This prevents race conditions if the handle had the last error.
        [ThreadStatic]
        internal static Interop.ErrorInfo? t_lastCloseErrorInfo;

        protected override bool ReleaseHandle()
        {
            // If DeleteOnClose was requested when constructed, delete the file now.
            // (Unix doesn't directly support DeleteOnClose, so we mimic it here.)
            // We delete the file before releasing the lock to detect the removal in Init.
            if (_deleteOnClose)
            {
                // Since we still have the file open, this will end up deleting
                // it (assuming we're the only link to it) once it's closed, but the
                // name will be removed immediately.
                Debug.Assert(_path is not null);
                Interop.Sys.Unlink(_path); // ignore errors; it's valid that the path may no longer exist
            }

            // When the SafeFileHandle was opened, we likely issued an flock on the created descriptor in order to add
            // an advisory lock.  This lock should be removed via closing the file descriptor, but close can be
            // interrupted, and we don't retry closes.  As such, we could end up leaving the file locked,
            // which could prevent subsequent usage of the file until this process dies.  To avoid that, we proactively
            // try to release the lock before we close the handle.
            if (_isLocked)
            {
                Interop.Sys.FLock(handle, Interop.Sys.LockOperations.LOCK_UN); // ignore any errors
                _isLocked = false;
            }

            // Close the descriptor. Although close is documented to potentially fail with EINTR, we never want
            // to retry, as the descriptor could actually have been closed, been subsequently reassigned, and
            // be in use elsewhere in the process.  Instead, we simply check whether the call was successful.
            int result = Interop.Sys.Close(handle);
            if (result != 0)
            {
                t_lastCloseErrorInfo = Interop.Sys.GetLastErrorInfo();
            }
            return result == 0;
        }

        public override bool IsInvalid
        {
            get
            {
                long h = (long)handle;
                return h < 0 || h > int.MaxValue;
            }
        }

        // If the file gets created a new, we'll select the permissions for it.  Most Unix utilities by default use 666 (read and
        // write for all), so we do the same (even though this doesn't match Windows, where by default it's possible to write out
        // a file and then execute it). No matter what we choose, it'll be subject to the umask applied by the system, such that the
        // actual permissions will typically be less than what we select here.
        private const Interop.Sys.Permissions DefaultOpenPermissions =
                Interop.Sys.Permissions.S_IRUSR | Interop.Sys.Permissions.S_IWUSR |
                Interop.Sys.Permissions.S_IRGRP | Interop.Sys.Permissions.S_IWGRP |
                Interop.Sys.Permissions.S_IROTH | Interop.Sys.Permissions.S_IWOTH;

        // Specialized Open that returns the file length and permissions of the opened file.
        // This information is retrieved from the 'stat' syscall that must be performed to ensure the path is not a directory.
        internal static SafeFileHandle OpenReadOnly(string fullPath, FileOptions options, out long fileLength, out Interop.Sys.Permissions filePermissions)
        {
            SafeFileHandle handle = Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, options, preallocationSize: 0, DefaultOpenPermissions, out fileLength, out filePermissions, null);
            Debug.Assert(fileLength >= 0);
            return handle;
        }

        internal static SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize,
                                            Interop.Sys.Permissions openPermissions = DefaultOpenPermissions,
                                            Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException = null)
        {
            return Open(fullPath, mode, access, share, options, preallocationSize, openPermissions, out _, out _, createOpenException);
        }

        private static SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize,
                                            Interop.Sys.Permissions openPermissions,
                                            out long fileLength,
                                            out Interop.Sys.Permissions filePermissions,
                                            Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException = null)
        {
            // Translate the arguments into arguments for an open call.
            Interop.Sys.OpenFlags openFlags = PreOpenConfigurationFromOptions(mode, access, share, options);

            SafeFileHandle? safeFileHandle = null;
            try
            {
                while (true)
                {
                    safeFileHandle = Open(fullPath, openFlags, (int)openPermissions, createOpenException);

                    // When Init return false, the path has changed to another file entry, and
                    // we need to re-open the path to reflect that.
                    if (safeFileHandle.Init(fullPath, mode, access, share, options, preallocationSize, out fileLength, out filePermissions))
                    {
                        return safeFileHandle;
                    }
                    else
                    {
                        safeFileHandle.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                safeFileHandle?.Dispose();

                throw;
            }
        }

        /// <summary>Translates the FileMode, FileAccess, and FileOptions values into flags to be passed when opening the file.</summary>
        /// <param name="mode">The FileMode provided to the stream's constructor.</param>
        /// <param name="access">The FileAccess provided to the stream's constructor</param>
        /// <param name="share">The FileShare provided to the stream's constructor</param>
        /// <param name="options">The FileOptions provided to the stream's constructor</param>
        /// <returns>The flags value to be passed to the open system call.</returns>
        private static Interop.Sys.OpenFlags PreOpenConfigurationFromOptions(FileMode mode, FileAccess access, FileShare share, FileOptions options)
        {
            // Translate FileMode.  Most of the values map cleanly to one or more options for open.
            Interop.Sys.OpenFlags flags = default;
            switch (mode)
            {
                default:
                case FileMode.Open: // Open maps to the default behavior for open(...).  No flags needed.
                    break;
                case FileMode.Truncate:
                    if (DisableFileLocking)
                    {
                        // if we don't lock the file, we can truncate it when opening
                        // otherwise we truncate the file after getting the lock
                        flags |= Interop.Sys.OpenFlags.O_TRUNC;
                    }
                    break;

                case FileMode.Append: // Append is the same as OpenOrCreate, except that we'll also separately jump to the end later
                case FileMode.OpenOrCreate:
                    flags |= Interop.Sys.OpenFlags.O_CREAT;
                    break;

                case FileMode.Create:
                    flags |= Interop.Sys.OpenFlags.O_CREAT;
                    if (DisableFileLocking)
                    {
                        flags |= Interop.Sys.OpenFlags.O_TRUNC;
                    }
                    break;

                case FileMode.CreateNew:
                    flags |= (Interop.Sys.OpenFlags.O_CREAT | Interop.Sys.OpenFlags.O_EXCL);
                    break;
            }

            // Translate FileAccess.  All possible values map cleanly to corresponding values for open.
            switch (access)
            {
                case FileAccess.Read:
                    flags |= Interop.Sys.OpenFlags.O_RDONLY;
                    break;

                case FileAccess.ReadWrite:
                    flags |= Interop.Sys.OpenFlags.O_RDWR;
                    break;

                case FileAccess.Write:
                    flags |= Interop.Sys.OpenFlags.O_WRONLY;
                    break;
            }

            // Handle Inheritable, other FileShare flags are handled by Init
            if ((share & FileShare.Inheritable) == 0)
            {
                flags |= Interop.Sys.OpenFlags.O_CLOEXEC;
            }

            // Translate some FileOptions; some just aren't supported, and others will be handled after calling open.
            // - Asynchronous: Handled in ctor, setting _useAsync and SafeFileHandle.IsAsync to true
            // - DeleteOnClose: Doesn't have a Unix equivalent, but we approximate it in Dispose
            // - Encrypted: No equivalent on Unix and is ignored
            // - RandomAccess: Implemented after open if posix_fadvise is available
            // - SequentialScan: Implemented after open if posix_fadvise is available
            // - WriteThrough: Handled here
            if ((options & FileOptions.WriteThrough) != 0)
            {
                flags |= Interop.Sys.OpenFlags.O_SYNC;
            }

            return flags;
        }

        private bool Init(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize,
                          out long fileLength, out Interop.Sys.Permissions filePermissions)
        {
            Interop.Sys.FileStatus status = default;
            bool statusHasValue = false;
            fileLength = -1;
            filePermissions = 0;

            // Make sure our handle is not a directory.
            // We can omit the check when write access is requested. open will have failed with EISDIR.
            if ((access & FileAccess.Write) == 0)
            {
                // Stat the file descriptor to avoid race conditions.
                FStatCheckIO(path, ref status, ref statusHasValue);

                if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Error.EACCES.Info(), path, isDirectory: true);
                }

                if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFREG)
                {
                    // we take advantage of the information provided by the fstat syscall
                    // and for regular files (most common case)
                    // avoid one extra sys call for determining whether file can be seeked
                    _canSeek = NullableBool.True;
                    Debug.Assert(Interop.Sys.LSeek(this, 0, Interop.Sys.SeekWhence.SEEK_CUR) >= 0);
                }

                fileLength = status.Size;
                filePermissions = (Interop.Sys.Permissions)(status.Mode & (int)Interop.Sys.Permissions.Mask);
            }

            IsAsync = (options & FileOptions.Asynchronous) != 0;

            // Lock the file if requested via FileShare.  This is only advisory locking. FileShare.None implies an exclusive
            // lock on the file and all other modes use a shared lock.  While this is not as granular as Windows, not mandatory,
            // and not atomic with file opening, it's better than nothing.
            Interop.Sys.LockOperations lockOperation = (share == FileShare.None) ? Interop.Sys.LockOperations.LOCK_EX : Interop.Sys.LockOperations.LOCK_SH;
            if (CanLockTheFile(lockOperation, access) && !(_isLocked = Interop.Sys.FLock(this, lockOperation | Interop.Sys.LockOperations.LOCK_NB) >= 0))
            {
                // The only error we care about is EWOULDBLOCK, which indicates that the file is currently locked by someone
                // else and we would block trying to access it.  Other errors, such as ENOTSUP (locking isn't supported) or
                // EACCES (the file system doesn't allow us to lock), will only hamper FileStream's usage without providing value,
                // given again that this is only advisory / best-effort.
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (errorInfo.Error == Interop.Error.EWOULDBLOCK)
                {
                    throw Interop.GetExceptionForIoErrno(errorInfo, path, isDirectory: false);
                }
            }

            // On Windows, DeleteOnClose happens when all kernel handles to the file are closed.
            // Unix kernels don't have this feature, and .NET deletes the file when the Handle gets disposed.
            // When the file is opened with an exclusive lock, we can use it to check the file at the path
            // still matches the file we've opened.
            // When the delete is performed by another .NET Handle, it holds the lock during the delete.
            // Since we've just obtained the lock, the file will already be removed/replaced.
            // We limit performing this check to cases where our file was opened with DeleteOnClose with
            // a mode of OpenOrCreate.
            if (_isLocked && ((options & FileOptions.DeleteOnClose) != 0) &&
                share == FileShare.None && mode == FileMode.OpenOrCreate)
            {
                FStatCheckIO(path, ref status, ref statusHasValue);

                Interop.Sys.FileStatus pathStatus;
                if (Interop.Sys.Stat(path, out pathStatus) < 0)
                {
                    // If the file was removed, re-open.
                    // Otherwise throw the error 'stat' gave us (assuming this is the
                    // error 'open' will give us if we'd call it now).
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();

                    if (error.Error == Interop.Error.ENOENT)
                    {
                        return false;
                    }

                    throw Interop.GetExceptionForIoErrno(error, path);
                }
                if (pathStatus.Ino != status.Ino || pathStatus.Dev != status.Dev)
                {
                    // The file was replaced, re-open
                    return false;
                }
            }
            // Enable DeleteOnClose when we've succesfully locked the file.
            // On Windows, the locking happens atomically as part of opening the file.
            _deleteOnClose = (options & FileOptions.DeleteOnClose) != 0;

            // These provide hints around how the file will be accessed.  Specifying both RandomAccess
            // and Sequential together doesn't make sense as they are two competing options on the same spectrum,
            // so if both are specified, we prefer RandomAccess (behavior on Windows is unspecified if both are provided).
            Interop.Sys.FileAdvice fadv =
                (options & FileOptions.RandomAccess) != 0 ? Interop.Sys.FileAdvice.POSIX_FADV_RANDOM :
                (options & FileOptions.SequentialScan) != 0 ? Interop.Sys.FileAdvice.POSIX_FADV_SEQUENTIAL :
                0;
            if (fadv != 0)
            {
                FileStreamHelpers.CheckFileCall(Interop.Sys.PosixFAdvise(this, 0, 0, fadv), path,
                    ignoreNotSupported: true); // just a hint.
            }

            if ((mode == FileMode.Create || mode == FileMode.Truncate) && !DisableFileLocking)
            {
                // Truncate the file now if the file mode requires it. This ensures that the file only will be truncated
                // if opened successfully.
                if (Interop.Sys.FTruncate(this, 0) < 0)
                {
                    Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error != Interop.Error.EBADF && errorInfo.Error != Interop.Error.EINVAL)
                    {
                        // We know the file descriptor is valid and we know the size argument to FTruncate is correct,
                        // so if EBADF or EINVAL is returned, it means we're dealing with a special file that can't be
                        // truncated.  Ignore the error in such cases; in all others, throw.
                        throw Interop.GetExceptionForIoErrno(errorInfo, path, isDirectory: false);
                    }
                }
            }

            if (preallocationSize > 0 && Interop.Sys.FAllocate(this, 0, preallocationSize) < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                // Only throw for errors that indicate there is not enough space.
                if (errorInfo.Error == Interop.Error.EFBIG ||
                    errorInfo.Error == Interop.Error.ENOSPC)
                {
                    Dispose();

                    // Delete the file we've created.
                    Debug.Assert(mode == FileMode.Create || mode == FileMode.CreateNew);
                    Interop.Sys.Unlink(path!);

                    throw new IOException(SR.Format(errorInfo.Error == Interop.Error.EFBIG
                                                        ? SR.IO_FileTooLarge_Path_AllocationSize
                                                        : SR.IO_DiskFull_Path_AllocationSize,
                                            path, preallocationSize));
                }
            }

            return true;
        }

        private bool CanLockTheFile(Interop.Sys.LockOperations lockOperation, FileAccess access)
        {
            Debug.Assert(lockOperation == Interop.Sys.LockOperations.LOCK_EX || lockOperation == Interop.Sys.LockOperations.LOCK_SH);

            if (DisableFileLocking)
            {
                return false;
            }
            else if (lockOperation == Interop.Sys.LockOperations.LOCK_EX)
            {
                return true; // LOCK_EX is always OK
            }
            else if ((access & FileAccess.Write) == 0)
            {
                return true; // LOCK_SH is always OK when reading
            }

            if (!Interop.Sys.TryGetFileSystemType(this, out Interop.Sys.UnixFileSystemTypes unixFileSystemType))
            {
                return false; // assume we should not acquire the lock if we don't know the File System
            }

            switch (unixFileSystemType)
            {
                case Interop.Sys.UnixFileSystemTypes.nfs: // #44546
                case Interop.Sys.UnixFileSystemTypes.smb:
                case Interop.Sys.UnixFileSystemTypes.smb2: // #53182
                case Interop.Sys.UnixFileSystemTypes.cifs:
                    return false; // LOCK_SH is not OK when writing to NFS, CIFS or SMB
                default:
                    return true; // in all other situations it should be OK
            }
        }

        private void FStatCheckIO(string path, ref Interop.Sys.FileStatus status, ref bool statusHasValue)
        {
            if (!statusHasValue)
            {
                if (Interop.Sys.FStat(this, out status) != 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    throw Interop.GetExceptionForIoErrno(error, path);
                }

                statusHasValue = true;
            }
        }

        private bool GetCanSeek()
        {
            Debug.Assert(!IsClosed);
            Debug.Assert(!IsInvalid);

            NullableBool canSeek = _canSeek;
            if (canSeek == NullableBool.Undefined)
            {
                _canSeek = canSeek = Interop.Sys.LSeek(this, 0, Interop.Sys.SeekWhence.SEEK_CUR) >= 0 ? NullableBool.True : NullableBool.False;
            }

            return canSeek == NullableBool.True;
        }

        internal long GetFileLength()
        {
            int result = Interop.Sys.FStat(this, out Interop.Sys.FileStatus status);
            FileStreamHelpers.CheckFileCall(result, Path);
            return status.Size;
        }

        private enum NullableBool
        {
            Undefined = 0,
            False = -1,
            True = 1
        }
    }
}
