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

        internal ThreadPoolBoundHandle? ThreadPoolBinding => null;

        internal void EnsureThreadPoolBindingInitialized() { /* nop */ }

        /// <summary>Opens the specified file with the requested flags and mode.</summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="flags">The flags with which to open the file.</param>
        /// <param name="mode">The mode for opening the file.</param>
        /// <returns>A SafeFileHandle for the opened file.</returns>
        private static SafeFileHandle Open(string path, Interop.Sys.OpenFlags flags, int mode)
        {
            Debug.Assert(path != null);
            SafeFileHandle handle = Interop.Sys.Open(path, flags, mode);
            handle._path = path;

            if (handle.IsInvalid)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                handle.Dispose();

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

            // Make sure it's not a directory; we do this after opening it once we have a file descriptor
            // to avoid race conditions.
            //
            // We can omit the check when write access is requested. open will have failed with EISDIR.
            if ((flags & (Interop.Sys.OpenFlags.O_WRONLY | Interop.Sys.OpenFlags.O_RDWR)) == 0)
            {
                Interop.Sys.FileStatus status;
                if (Interop.Sys.FStat(handle, out status) != 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    handle.Dispose();
                    throw Interop.GetExceptionForIoErrno(error, path);
                }
                if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
                {
                    handle.Dispose();
                    throw Interop.GetExceptionForIoErrno(Interop.Error.EACCES.Info(), path, isDirectory: true);
                }

                if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFREG)
                {
                    // we take advantage of the information provided by the fstat syscall
                    // and for regular files (most common case)
                    // avoid one extra sys call for determining whether file can be seeked
                    handle._canSeek = NullableBool.True;
                    Debug.Assert(Interop.Sys.LSeek(handle, 0, Interop.Sys.SeekWhence.SEEK_CUR) >= 0);
                }
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

            // If DeleteOnClose was requested when constructed, delete the file now.
            // (Unix doesn't directly support DeleteOnClose, so we mimic it here.)
            if (_deleteOnClose)
            {
                // Since we still have the file open, this will end up deleting
                // it (assuming we're the only link to it) once it's closed, but the
                // name will be removed immediately.
                Debug.Assert(_path is not null);
                Interop.Sys.Unlink(_path); // ignore errors; it's valid that the path may no longer exist
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

        internal static SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            // Translate the arguments into arguments for an open call.
            Interop.Sys.OpenFlags openFlags = PreOpenConfigurationFromOptions(mode, access, share, options);

            // If the file gets created a new, we'll select the permissions for it.  Most Unix utilities by default use 666 (read and
            // write for all), so we do the same (even though this doesn't match Windows, where by default it's possible to write out
            // a file and then execute it). No matter what we choose, it'll be subject to the umask applied by the system, such that the
            // actual permissions will typically be less than what we select here.
            const Interop.Sys.Permissions OpenPermissions =
                Interop.Sys.Permissions.S_IRUSR | Interop.Sys.Permissions.S_IWUSR |
                Interop.Sys.Permissions.S_IRGRP | Interop.Sys.Permissions.S_IWGRP |
                Interop.Sys.Permissions.S_IROTH | Interop.Sys.Permissions.S_IWOTH;

            SafeFileHandle safeFileHandle = Open(fullPath, openFlags, (int)OpenPermissions);
            try
            {
                safeFileHandle.Init(fullPath, mode, access, share, options, preallocationSize);

                return safeFileHandle;
            }
            catch (Exception)
            {
                safeFileHandle.Dispose();

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
                case FileMode.Truncate: // We truncate the file after getting the lock
                    break;

                case FileMode.Append: // Append is the same as OpenOrCreate, except that we'll also separately jump to the end later
                case FileMode.OpenOrCreate:
                case FileMode.Create: // We truncate the file after getting the lock
                    flags |= Interop.Sys.OpenFlags.O_CREAT;
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

        private void Init(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
        {
            IsAsync = (options & FileOptions.Asynchronous) != 0;
            _deleteOnClose = (options & FileOptions.DeleteOnClose) != 0;

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

            if (mode == FileMode.Create || mode == FileMode.Truncate)
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

            // If preallocationSize has been provided for a creatable and writeable file
            if (FileStreamHelpers.ShouldPreallocate(preallocationSize, access, mode))
            {
                int fallocateResult = Interop.Sys.PosixFAllocate(this, 0, preallocationSize);
                if (fallocateResult != 0)
                {
                    Dispose();
                    Interop.Sys.Unlink(path!); // remove the file to mimic Windows behaviour (atomic operation)

                    Debug.Assert(fallocateResult == -1 || fallocateResult == -2);
                    throw new IOException(SR.Format(
                        fallocateResult == -1 ? SR.IO_DiskFull_Path_AllocationSize : SR.IO_FileTooLarge_Path_AllocationSize,
                        path,
                        preallocationSize));
                }
            }
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

        private enum NullableBool
        {
            Undefined = 0,
            False = -1,
            True = 1
        }
    }
}
