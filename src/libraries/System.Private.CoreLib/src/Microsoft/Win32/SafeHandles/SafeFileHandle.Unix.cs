// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Strategies;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using AsyncResult = System.Threading.UnixHandleAsyncContext.AsyncResult;
using OnCompletedResult = System.Threading.UnixHandleAsyncContext.OnCompletedResult;
using SyncResult = System.Threading.UnixHandleAsyncContext.SyncResult;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // IovStackThreshold matches Linux's UIO_FASTIOV, which is the number of 'struct iovec'
        // that get stackalloced in the Linux kernel.
        private const int IovStackThreshold = 8;

        private const UnixFileMode PermissionMask =
            UnixFileMode.UserRead |
            UnixFileMode.UserWrite |
            UnixFileMode.UserExecute |
            UnixFileMode.GroupRead |
            UnixFileMode.GroupWrite |
            UnixFileMode.GroupExecute |
            UnixFileMode.OtherRead |
            UnixFileMode.OtherWrite |
            UnixFileMode.OtherExecute;

        // If the file gets created a new, we'll select the permissions for it.  Most Unix utilities by default use 666 (read and
        // write for all), so we do the same (even though this doesn't match Windows, where by default it's possible to write out
        // a file and then execute it). No matter what we choose, it'll be subject to the umask applied by the system, such that the
        // actual permissions will typically be less than what we select here.
        internal const UnixFileMode DefaultCreateMode =
            UnixFileMode.UserRead |
            UnixFileMode.UserWrite |
            UnixFileMode.GroupRead |
            UnixFileMode.GroupWrite |
            UnixFileMode.OtherRead |
            UnixFileMode.OtherWrite;

        internal static bool DisableFileLocking { get; } = OperatingSystem.IsBrowser() || OperatingSystem.IsWasi()// #40065: Emscripten does not support file locking
            || AppContextConfigHelper.GetBooleanConfig("System.IO.DisableFileLocking", "DOTNET_SYSTEM_IO_DISABLEFILELOCKING", defaultValue: false);

        // not using bool? as it's not thread safe
        private NullableBool _canSeek /* = NullableBool.Undefined */;
        private NullableBool _supportsRandomAccess /* = NullableBool.Undefined */;
        private bool _deleteOnClose;
        private bool _isLocked;
        private NullableBool _isBlocking;
        private UnixHandleAsyncContext? _asyncContext;
        private ReadOperation? _cachedReadOp;
        private WriteOperation? _cachedWriteOp;

        public SafeFileHandle() : this(ownsHandle: true)
        {
        }

        private SafeFileHandle(bool ownsHandle)
            : base(ownsHandle)
        {
            SetHandle(new IntPtr(-1));
        }

        private SafeFileHandle(FileHandleType type, bool nonBlocking)
            : this(ownsHandle: true)
        {
            _cachedFileType = (int)type;
            _isBlocking = nonBlocking ? NullableBool.False : NullableBool.True;
        }

        public bool IsAsync => !IsBlocking;

        // RegularFile and BlockDevices don't support non-blocking and do support random access.
        // Perform read/write operations on the ThreadPool so that multiple can happen in parallel.
        // Pipe and Socket support non-blocking and do not support random access.
        // Character devices may support non-blocking and may support random access.
        private bool SupportsNonBlocking
            => UnixHandleAsyncContext.IsSupported // We can't handle non-blocking.
                && Type is FileHandleType.Socket or FileHandleType.Pipe or FileHandleType.CharacterDevice;
        private bool UseThreadPoolForAsync
            => !SupportsNonBlocking;

        private bool IsBlocking
        {
            get
            {
                NullableBool isBlocking = _isBlocking;
                if (isBlocking == NullableBool.Undefined)
                {
                    if (!SupportsNonBlocking)
                    {
                        _isBlocking = NullableBool.True;
                        return true;
                    }

                    if (Interop.Sys.Fcntl.GetIsNonBlocking(this, out bool nonBlocking) != 0)
                    {
                        throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                    }

                    _isBlocking = isBlocking = nonBlocking ? NullableBool.False : NullableBool.True;
                }

                return isBlocking == NullableBool.True;
            }
        }

        private void SetHandleNonBlocking()
        {
            Debug.Assert(SupportsNonBlocking);

            if (_isBlocking != NullableBool.False)
            {
                if (Interop.Sys.Fcntl.SetIsNonBlocking(this, 1) != 0)
                {
                    throw Interop.GetExceptionForIoErrno(Interop.Sys.GetLastErrorInfo());
                }
                _isBlocking = NullableBool.False;
            }
        }

        private UnixHandleAsyncContext AsyncContext
        {
            get
            {
                if (_asyncContext == null)
                {
                    SetHandleNonBlocking();
                    Interlocked.CompareExchange(ref _asyncContext, new UnixHandleAsyncContext(this), null);
                }
                return _asyncContext!;
            }
        }

        private ReadOperation RentReadOperation()
            => Interlocked.Exchange(ref _cachedReadOp, null) ?? new ReadOperation(this);

        private WriteOperation RentWriteOperation()
            => Interlocked.Exchange(ref _cachedWriteOp, null) ?? new WriteOperation(this);

        private void ReturnReadOperation(ReadOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedReadOp, op);
        }

        private void ReturnWriteOperation(WriteOperation op)
        {
            op.Reset();
            Volatile.Write(ref _cachedWriteOp, op);
        }

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
                Debug.Assert(!value); // We should only use the setter to disable random access.
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

        private static SafeFileHandle Open(string path, Interop.Sys.OpenFlags flags, int mode, bool failForSymlink, out bool wasSymlink,
                                           Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException)
        {
            wasSymlink = false;
            Debug.Assert(path != null);
            SafeFileHandle handle = Interop.Sys.Open(path, flags, mode);
            handle._path = path;

            if (handle.IsInvalid)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                handle.Dispose();

                if (failForSymlink && error.Error == Interop.Error.ELOOP)
                {
                    wasSymlink = true;
                    return handle;
                }

                if (createOpenException?.Invoke(error, flags, path) is Exception ex)
                {
                    throw ex;
                }

                if (error.Error == Interop.Error.EISDIR)
                {
                    error = Interop.Error.EACCES.Info();
                }

                Interop.CheckIo(error.Error, path);
            }

            return handle;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _asyncContext?.Dispose();
            }
            base.Dispose(disposing);
        }

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

            // Close the descriptor.
            return Interop.Sys.Close(handle) == 0;
        }

        public override bool IsInvalid
        {
            get
            {
                long h = (long)handle;
                return h < 0 || h > int.MaxValue;
            }
        }

        public static partial void CreateAnonymousPipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle, bool asyncRead, bool asyncWrite)
        {
            // Allocate the handles first, so in case of OOM we don't leak any handles.
            SafeFileHandle tempReadHandle = new(FileHandleType.Pipe, nonBlocking: asyncRead);
            SafeFileHandle tempWriteHandle = new(FileHandleType.Pipe, nonBlocking: asyncWrite);

            Interop.Sys.PipeFlags flags = Interop.Sys.PipeFlags.O_CLOEXEC;
            if (asyncRead)
            {
                flags |= Interop.Sys.PipeFlags.O_NONBLOCK_READ;
            }

            if (asyncWrite)
            {
                flags |= Interop.Sys.PipeFlags.O_NONBLOCK_WRITE;
            }

            int readFd, writeFd;
            unsafe
            {
                int* fds = stackalloc int[2];
                if (Interop.Sys.Pipe(fds, flags) != 0)
                {
                    Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                    tempReadHandle.Dispose();
                    tempWriteHandle.Dispose();
                    throw Interop.GetExceptionForIoErrno(error);
                }

                readFd = fds[Interop.Sys.ReadEndOfPipe];
                writeFd = fds[Interop.Sys.WriteEndOfPipe];
            }

            tempReadHandle.SetHandle(readFd);
            tempWriteHandle.SetHandle(writeFd);

            readHandle = tempReadHandle;
            writeHandle = tempWriteHandle;
        }

        // Specialized Open that returns the file length and permissions of the opened file.
        // This information is retrieved from the 'stat' syscall that must be performed to ensure the path is not a directory.
        internal static SafeFileHandle OpenReadOnly(string fullPath, FileOptions options, out long fileLength, out UnixFileMode filePermissions)
        {
            SafeFileHandle handle = Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, options, preallocationSize: 0, DefaultCreateMode, out fileLength, out filePermissions, false, out _, null);
            Debug.Assert(fileLength >= 0);
            return handle;
        }

        internal static SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, UnixFileMode? unixCreateMode = null,
                                            Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException = null)
        {
            return Open(fullPath, mode, access, share, options, preallocationSize, unixCreateMode ?? DefaultCreateMode, out _, out _, false, out _, createOpenException);
        }

        internal static SafeFileHandle? OpenNoFollowSymlink(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, out bool wasSymlink, UnixFileMode? unixCreateMode = null,
                                            Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException = null)
        {
            return Open(fullPath, mode, access, share, options, preallocationSize, unixCreateMode ?? DefaultCreateMode, out _, out _, true, out wasSymlink, createOpenException);
        }

        private static SafeFileHandle Open(string fullPath, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize, UnixFileMode openPermissions,
                                            out long fileLength, out UnixFileMode filePermissions, bool failForSymlink, out bool wasSymlink,
                                            Func<Interop.ErrorInfo, Interop.Sys.OpenFlags, string, Exception?>? createOpenException = null)
        {
            // Translate the arguments into arguments for an open call.
            Interop.Sys.OpenFlags openFlags = PreOpenConfigurationFromOptions(mode, access, share, options, failForSymlink);

            SafeFileHandle? safeFileHandle = null;
            try
            {
                while (true)
                {
                    safeFileHandle = Open(fullPath, openFlags, (int)openPermissions, failForSymlink, out wasSymlink, createOpenException);

                    if (failForSymlink && wasSymlink)
                    {
                        fileLength = default;
                        filePermissions = default;
                        return safeFileHandle;
                    }

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
        /// <param name="failForSymlink">Whether to cause ELOOP error when opening a symlink</param>
        /// <returns>The flags value to be passed to the open system call.</returns>
        private static Interop.Sys.OpenFlags PreOpenConfigurationFromOptions(FileMode mode, FileAccess access, FileShare share, FileOptions options, bool failForSymlink)
        {
            // Translate FileMode.  Most of the values map cleanly to one or more options for open.
            Interop.Sys.OpenFlags flags = default;
            if (failForSymlink)
            {
                flags |= Interop.Sys.OpenFlags.O_NOFOLLOW;
            }
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
            // - Asynchronous: Unix does not support O_NONBLOCK for regular files, only for pipes and sockets.
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
                          out long fileLength, out UnixFileMode filePermissions)
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
                    throw Interop.GetExceptionForIoErrno(Interop.Error.EACCES.Info(), path);
                }

                if ((status.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFREG)
                {
                    // we take advantage of the information provided by the fstat syscall
                    // and for regular files (most common case)
                    // avoid one extra sys call for determining whether file can be seeked
                    _canSeek = NullableBool.True;

                    // we exclude 0-length files from the assert because those may may be pseudofiles
                    // (e.g. /proc/net/route) and these are not seekable on all systems
                    Debug.Assert(status.Size == 0 || Interop.Sys.LSeek(this, 0, Interop.Sys.SeekWhence.SEEK_CUR) >= 0);
                }

                // Cache the file type from the status
                _cachedFileType = (int)MapUnixFileTypeToFileType(status);

                fileLength = status.Size;
                filePermissions = ((UnixFileMode)status.Mode) & PermissionMask;
            }

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
                    throw Interop.GetExceptionForIoErrno(errorInfo, path);
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
            // Enable DeleteOnClose when we've successfully locked the file.
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
                        throw Interop.GetExceptionForIoErrno(errorInfo, path);
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
                    Interop.Sys.Unlink(path);

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

            return Interop.Sys.FileSystemSupportsLocking(this, lockOperation, accessWrite: (access & FileAccess.Write) != 0);
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
                bool unseekable = Type is FileHandleType.Socket or FileHandleType.Pipe;
                _canSeek = canSeek = !unseekable && Interop.Sys.LSeek(this, 0, Interop.Sys.SeekWhence.SEEK_CUR) >= 0 ? NullableBool.True : NullableBool.False;
            }

            return canSeek == NullableBool.True;
        }

        internal FileHandleType GetFileTypeCore()
        {
            int result = Interop.Sys.FStat(this, out Interop.Sys.FileStatus status);
            if (result != 0)
            {
                Interop.ErrorInfo error = Interop.Sys.GetLastErrorInfo();
                throw Interop.GetExceptionForIoErrno(error, Path);
            }

            return MapUnixFileTypeToFileType(status);
        }

        private static FileHandleType MapUnixFileTypeToFileType(Interop.Sys.FileStatus status)
            => (status.Mode & Interop.Sys.FileTypes.S_IFMT) switch
            {
                Interop.Sys.FileTypes.S_IFREG => FileHandleType.RegularFile,
                Interop.Sys.FileTypes.S_IFDIR => FileHandleType.Directory,
                Interop.Sys.FileTypes.S_IFLNK => FileHandleType.SymbolicLink,
                Interop.Sys.FileTypes.S_IFIFO => FileHandleType.Pipe,
                Interop.Sys.FileTypes.S_IFSOCK => FileHandleType.Socket,
                Interop.Sys.FileTypes.S_IFCHR => FileHandleType.CharacterDevice,
                Interop.Sys.FileTypes.S_IFBLK => FileHandleType.BlockDevice,
                _ => FileHandleType.Unknown
            };

        internal long GetFileLength()
        {
            int result = Interop.Sys.FStat(this, out Interop.Sys.FileStatus status);
            FileStreamHelpers.CheckFileCall(result, Path);
            return status.Size;
        }

        internal unsafe int Read(long offset, Span<byte> buffer)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsReadReady(out sequenceNumber);
            if (doSync)
            {
                fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
                {
                    if (TryCompleteReadAt(offset, bufPtr, buffer.Length, out var result, out bool pending))
                    {
                        return CheckFileCall(result.BytesRead, result.ErrorInfo);
                    }
                    if (isBlocking)
                    {
                        Debug.Assert(pending);
                        if (AsyncContext.IsReadReady(out sequenceNumber) && TryCompleteReadAt(offset, bufPtr, buffer.Length, out result, out _))
                        {
                            return CheckFileCall(result.BytesRead, result.ErrorInfo);
                        }
                    }
                }
            }

            ReadOperation op = RentReadOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.Init(offset, bufPtr, buffer.Length);

                SyncResult result = AsyncContext.ReadSync(op, sequenceNumber, timeout: -1);

                if (result == SyncResult.Completed)
                {
                    int readResult = (int)op.BytesRead;
                    Exception? exception = op.Exception;

                    ReturnReadOperation(op);

                    if (exception != null)
                    {
                        throw exception;
                    }
                    return readResult;
                }

                throw new OperationCanceledException();
            }
        }

        internal unsafe ValueTask<int> ReadAsync(long offset, Memory<byte> destination, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => UseThreadPoolForAsync
                ? ReadAsyncThreadPool(offset, destination, cancellationToken, strategy)
                : ReadAsyncPollable(offset, destination, cancellationToken, strategy);

        private ValueTask<int> ReadAsyncThreadPool(long offset, Memory<byte> destination, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            ReadOperation op = RentReadOperation();
            op.Init(offset, destination, cancellationToken, strategy);
            op.QueueToThreadPool();
            return new ValueTask<int>(op, op.Version);
        }

        private unsafe ValueTask<int> ReadAsyncPollable(long offset, Memory<byte> destination, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            int sequenceNumber;
            if (AsyncContext.IsReadReady(out sequenceNumber))
            {
                fixed (byte* bufPtr = &MemoryMarshal.GetReference(destination.Span))
                {
                    if (TryCompleteReadAt(offset, bufPtr, destination.Length, out var readResult, out _))
                    {
                        UpdateFileStreamForAsyncRead(strategy, destination, readResult.BytesRead);
                        return new ValueTask<int>(CheckFileCall(readResult.BytesRead, readResult.ErrorInfo));
                    }
                }
            }

            ReadOperation op = RentReadOperation();
            op.Init(offset, destination, cancellationToken, strategy);

            AsyncResult result = AsyncContext.ReadAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<int>(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                int completedResult = (int)op.BytesRead;
                Exception? exception = op.Exception;
                UpdateFileStreamForAsyncRead(strategy, destination, completedResult);

                ReturnReadOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return new ValueTask<int>(completedResult);
            }

            throw new OperationCanceledException();
        }

        internal unsafe void Write(long offset, ReadOnlySpan<byte> buffer, OSFileStreamStrategy? strategy = null)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsWriteReady(out sequenceNumber);
            while (doSync)
            {
                fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
                {
                    bool completed = TryCompleteWriteAt(offset, bufPtr, buffer.Length, out int bytesWritten, out Interop.ErrorInfo errorInfo);

                    buffer = buffer.Slice(bytesWritten);
                    offset += bytesWritten;

                    if (completed)
                    {
                        strategy?.OnIncompleteOperation(buffer.Length, 0);
                        CheckFileCall(errorInfo);
                        return;
                    }

                    if (!isBlocking)
                    {
                        break;
                    }
                    // The handle changed to non-blocking due to a concurrent operation.
                    isBlocking = false;
                    doSync = AsyncContext.IsWriteReady(out sequenceNumber);
                }
            }

            WriteOperation op = RentWriteOperation();
            fixed (byte* bufPtr = &MemoryMarshal.GetReference(buffer))
            {
                op.Init(offset, bufPtr, buffer.Length, strategy);

                SyncResult result = AsyncContext.WriteSync(op, sequenceNumber, timeout: -1);

                if (result == SyncResult.Completed)
                {
                    strategy?.OnIncompleteOperation(op.SyncRemaining, 0);

                    Exception? exception = op.Exception;

                    ReturnWriteOperation(op);

                    if (exception != null)
                    {
                        throw exception;
                    }
                    return;
                }

                throw new OperationCanceledException();
            }
        }

        internal unsafe ValueTask WriteAsync(long offset, ReadOnlyMemory<byte> source, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => UseThreadPoolForAsync
                ? WriteAsyncThreadPool(offset, source, cancellationToken, strategy)
                : WriteAsyncPollable(offset, source, cancellationToken, strategy);

        private ValueTask WriteAsyncThreadPool(long offset, ReadOnlyMemory<byte> source, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            if (source.IsEmpty)
            {
                return default;
            }

            WriteOperation op = RentWriteOperation();
            op.Init(offset, source, cancellationToken, strategy);
            op.QueueToThreadPool();
            return new ValueTask(op, op.Version);
        }

        private unsafe ValueTask WriteAsyncPollable(long offset, ReadOnlyMemory<byte> source, CancellationToken cancellationToken, OSFileStreamStrategy? strategy)
        {
            if (source.IsEmpty)
            {
                return default;
            }

            int bytesWritten = 0;
            int sequenceNumber;
            if (AsyncContext.IsWriteReady(out sequenceNumber))
            {
                fixed (byte* bufPtr = &MemoryMarshal.GetReference(source.Span))
                {
                    bool completed = TryCompleteWriteAt(offset, bufPtr, source.Length, out bytesWritten, out Interop.ErrorInfo writeResult);

                    source = source.Slice(bytesWritten);
                    offset += bytesWritten;

                    if (completed)
                    {
                        UpdateFileStreamForAsyncWrite(strategy, source);
                        CheckFileCall(writeResult);
                        return default;
                    }
                }
            }

            WriteOperation op = RentWriteOperation();
            op.Init(offset, source, cancellationToken, strategy);

            AsyncResult result = AsyncContext.WriteAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                Exception? exception = op.Exception;
                UpdateFileStreamForAsyncWrite(strategy, op.Remaining);

                ReturnWriteOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return default;
            }

            throw new OperationCanceledException();
        }

        internal long Read(long offset, IReadOnlyList<Memory<byte>> buffers)
        {
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsReadReady(out sequenceNumber);
            if (doSync)
            {
                if (TryCompleteReadAt(offset, buffers, out var readResult, out bool pending))
                {
                    return CheckFileCall(readResult.BytesRead, readResult.ErrorInfo);
                }
                if (isBlocking)
                {
                    Debug.Assert(pending);
                    if (AsyncContext.IsReadReady(out sequenceNumber) && TryCompleteReadAt(offset, buffers, out readResult, out _))
                    {
                        return CheckFileCall(readResult.BytesRead, readResult.ErrorInfo);
                    }
                }
            }

            ReadOperation op = RentReadOperation();
            op.Init(offset, buffers);

            SyncResult result = AsyncContext.ReadSync(op, sequenceNumber, timeout: -1);

            if (result == SyncResult.Completed)
            {
                long readResult = op.BytesRead;
                Exception? exception = op.Exception;

                ReturnReadOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return readResult;
            }

            throw new OperationCanceledException();
        }

        internal ValueTask<long> ReadAsync(long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
            => UseThreadPoolForAsync
                ? ReadAsyncThreadPool(offset, buffers, cancellationToken)
                : ReadAsyncPollable(offset, buffers, cancellationToken);

        private ValueTask<long> ReadAsyncThreadPool(long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
        {
            ReadOperation op = RentReadOperation();
            op.Init(offset, buffers, cancellationToken);
            op.QueueToThreadPool();
            return new ValueTask<long>(op, op.Version);
        }

        private ValueTask<long> ReadAsyncPollable(long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
        {
            int sequenceNumber;
            if (AsyncContext.IsReadReady(out sequenceNumber) &&
                TryCompleteReadAt(offset, buffers, out var readResult, out _))
            {
                return new ValueTask<long>(CheckFileCall(readResult.BytesRead, readResult.ErrorInfo));
            }

            ReadOperation op = RentReadOperation();
            op.Init(offset, buffers, cancellationToken);

            AsyncResult result = AsyncContext.ReadAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask<long>(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                long completedResult = op.BytesRead;
                Exception? exception = op.Exception;

                ReturnReadOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return new ValueTask<long>(completedResult);
            }

            throw new OperationCanceledException();
        }

        internal void Write(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers)
        {
            int bufferIndex = 0;
            int bufferOffset = 0;
            int sequenceNumber = 0;
            bool isBlocking = IsBlocking;

            bool doSync = isBlocking || AsyncContext.IsWriteReady(out sequenceNumber);
            while (doSync)
            {
                if (TryCompleteWriteAt(ref offset, buffers, ref bufferIndex, ref bufferOffset, out Interop.ErrorInfo errorInfo))
                {
                    CheckFileCall(errorInfo);
                    return;
                }

                if (!isBlocking)
                {
                    break;
                }
                // The handle changed to non-blocking due to a concurrent operation.
                isBlocking = false;
                doSync = AsyncContext.IsWriteReady(out sequenceNumber);
            }

            WriteOperation op = RentWriteOperation();
            op.Init(offset, buffers, bufferIndex, bufferOffset);

            SyncResult result = AsyncContext.WriteSync(op, sequenceNumber, timeout: -1);

            if (result == SyncResult.Completed)
            {
                Exception? exception = op.Exception;

                ReturnWriteOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return;
            }

            throw new OperationCanceledException();
        }

        internal ValueTask WriteAsync(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken)
            => UseThreadPoolForAsync
                ? WriteAsyncThreadPool(offset, buffers, cancellationToken)
                : WriteAsyncPollable(offset, buffers, cancellationToken);

        private ValueTask WriteAsyncThreadPool(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken)
        {
            WriteOperation op = RentWriteOperation();
            op.Init(offset, buffers, 0, 0, cancellationToken);
            op.QueueToThreadPool();
            return new ValueTask(op, op.Version);
        }

        private ValueTask WriteAsyncPollable(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken)
        {
            int bufferIndex = 0;
            int bufferOffset = 0;
            int sequenceNumber;
            if (AsyncContext.IsWriteReady(out sequenceNumber))
            {
                if (TryCompleteWriteAt(ref offset, buffers, ref bufferIndex, ref bufferOffset, out Interop.ErrorInfo writeResult))
                {
                    CheckFileCall(writeResult);
                    return default;
                }
            }

            WriteOperation op = RentWriteOperation();
            op.Init(offset, buffers, bufferIndex, bufferOffset, cancellationToken);

            AsyncResult result = AsyncContext.WriteAsync(op, sequenceNumber, cancellationToken);

            if (result == AsyncResult.Pending)
            {
                return new ValueTask(op, op.Version);
            }
            else if (result == AsyncResult.Completed)
            {
                Exception? exception = op.Exception;

                ReturnWriteOperation(op);

                if (exception != null)
                {
                    throw exception;
                }
                return default;
            }

            throw new OperationCanceledException();
        }

        private sealed unsafe class ReadOperation : UnixHandleAsyncContext.Operation, IValueTaskSource<int>, IValueTaskSource<long>
        {
            private readonly SafeFileHandle _owner;
            private ManualResetValueTaskSourceCore<long> _mrvtsc;
            private Memory<byte> _buffer;
            private IReadOnlyList<Memory<byte>>? _buffers;
            private byte* _syncBuffer;
            private int _syncBufferLength;
            private long _offset;
            private bool _runOnThreadPool;
            private ExecutionContext? _executionContext;
            private CancellationToken _cancellationToken;
            private OSFileStreamStrategy? _strategy;

            internal long BytesRead;
            internal Exception? Exception;

            internal ReadOperation(SafeFileHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal void Init(long offset, byte* syncBuffer, int syncBufferLength)
            {
                _offset = offset;
                _syncBuffer = syncBuffer;
                _syncBufferLength = syncBufferLength;
            }

            internal void Init(long offset, Memory<byte> buffer, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            {
                _offset = offset;
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _strategy = strategy;
            }

            internal void Init(long offset, IReadOnlyList<Memory<byte>> buffers)
            {
                _offset = offset;
                _buffers = buffers;
            }

            internal void Init(long offset, IReadOnlyList<Memory<byte>> buffers, CancellationToken cancellationToken)
            {
                _offset = offset;
                _buffers = buffers;
                _cancellationToken = cancellationToken;
            }

            internal void QueueToThreadPool()
            {
                _runOnThreadPool = true;
                _executionContext = ExecutionContext.Capture();
                bool refAdded = false;
                _owner.DangerousAddRef(ref refAdded);
                try
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                }
                catch
                {
                    _owner.DangerousRelease();
                    throw;
                }
            }

            private void ExecuteOnThreadPool()
            {
                bool completed = TryCompleteOperation(_owner);
                Debug.Assert(completed);
                _owner.DangerousRelease();
                OnCompleted(OnCompletedResult.Completed);
            }

            protected override void ExecuteThreadPoolWorkItem()
            {
                if (!_runOnThreadPool)
                {
                    base.ExecuteThreadPoolWorkItem();
                    return;
                }

                if (_executionContext == null || _executionContext.IsDefault)
                {
                    ExecuteOnThreadPool();
                }
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(_executionContext, static x => x.ExecuteOnThreadPool(), this);
                }
            }

            internal void Reset()
            {
                _buffer = default;
                _buffers = null;
                _syncBuffer = null;
                _offset = 0;
                _runOnThreadPool = false;
                _executionContext = null;
                _cancellationToken = default;
                _strategy = null;
                Exception = null;
                _mrvtsc.Reset();
            }

            protected internal override bool TryCompleteOperation(SafeHandle handle)
            {
                (int BytesRead, Interop.ErrorInfo ErrorInfo) readResult;

                if (_buffers != null)
                {
                    if (!_owner.TryCompleteReadAt(_offset, _buffers, out readResult, out _))
                    {
                        return false;
                    }
                }
                else if (_syncBuffer != null)
                {
                    Debug.Assert(_syncBufferLength > 0);
                    if (!_owner.TryCompleteReadAt(_offset, _syncBuffer, _syncBufferLength, out readResult, out _))
                    {
                        return false;
                    }
                }
                else
                {
                    Span<byte> span = _buffer.Span;

                    fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                    {
                        if (!_owner.TryCompleteReadAt(_offset, bufPtr, span.Length, out readResult, out _))
                        {
                            return false;
                        }
                    }
                }

                BytesRead = readResult.BytesRead;
                if (readResult.BytesRead == -1)
                {
                    Exception = Interop.GetExceptionForIoErrno(readResult.ErrorInfo, _owner.Path);
                }
                return true;
            }

            protected internal override void OnCompleted(OnCompletedResult result)
            {
                UpdateFileStreamForAsyncRead(_strategy, _buffer, (int)BytesRead);

                if (result == OnCompletedResult.Completed)
                {
                    if (Exception != null)
                    {
                        _mrvtsc.SetException(Exception);
                    }
                    else
                    {
                        _mrvtsc.SetResult(BytesRead);
                    }
                }
                else if (result == OnCompletedResult.Canceled)
                {
                    _mrvtsc.SetException(new OperationCanceledException(_cancellationToken));
                }
                else
                {
                    Debug.Assert(result == OnCompletedResult.Aborted);
                    _mrvtsc.SetException(new OperationCanceledException());
                }
            }

            private long GetResultAndPool(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) != ValueTaskSourceStatus.Canceled;
                try
                {
                    return _mrvtsc.GetResult(token);
                }
                finally
                {
                    if (canPool)
                    {
                        _owner.ReturnReadOperation(this);
                    }
                }
            }

            ValueTaskSourceStatus IValueTaskSource<int>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<int>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            int IValueTaskSource<int>.GetResult(short token)
                => (int)GetResultAndPool(token);

            ValueTaskSourceStatus IValueTaskSource<long>.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource<long>.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            long IValueTaskSource<long>.GetResult(short token)
                => GetResultAndPool(token);
        }

        private sealed unsafe class WriteOperation : UnixHandleAsyncContext.Operation, IValueTaskSource
        {
            private readonly SafeFileHandle _owner;
            private ManualResetValueTaskSourceCore<bool> _mrvtsc;
            private ReadOnlyMemory<byte> _buffer;
            private IReadOnlyList<ReadOnlyMemory<byte>>? _buffers;
            private byte* _syncBuffer;
            private int _syncRemaining;
            private long _offset;
            private int _bufferIndex;
            private int _bufferOffset;
            private bool _runOnThreadPool;
            private ExecutionContext? _executionContext;
            private CancellationToken _cancellationToken;
            private OSFileStreamStrategy? _strategy;

            internal Exception? Exception;

            internal WriteOperation(SafeFileHandle owner)
                => _owner = owner;

            internal short Version
                => _mrvtsc.Version;

            internal ReadOnlyMemory<byte> Remaining
                => _buffer;

            internal int SyncRemaining
                => _syncRemaining;

            internal void Init(long offset, byte* syncBuffer, int syncRemaining, OSFileStreamStrategy? strategy = null)
            {
                _offset = offset;
                _syncBuffer = syncBuffer;
                _syncRemaining = syncRemaining;
                _strategy = strategy;
            }

            internal void Init(long offset, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            {
                _offset = offset;
                _buffer = buffer;
                _cancellationToken = cancellationToken;
                _strategy = strategy;
            }

            internal void Init(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, int bufferIndex, int bufferOffset)
            {
                _offset = offset;
                _buffers = buffers;
                _bufferIndex = bufferIndex;
                _bufferOffset = bufferOffset;
            }

            internal void Init(long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, int bufferIndex, int bufferOffset, CancellationToken cancellationToken)
            {
                _offset = offset;
                _buffers = buffers;
                _bufferIndex = bufferIndex;
                _bufferOffset = bufferOffset;
                _cancellationToken = cancellationToken;
            }

            internal void QueueToThreadPool()
            {
                _runOnThreadPool = true;
                _executionContext = ExecutionContext.Capture();
                bool refAdded = false;
                _owner.DangerousAddRef(ref refAdded);
                try
                {
                    ThreadPool.UnsafeQueueUserWorkItem(this, preferLocal: true);
                }
                catch
                {
                    _owner.DangerousRelease();
                    throw;
                }
            }

            private void ExecuteOnThreadPool()
            {
                while (!TryCompleteOperation(_owner))
                {
                }
                _owner.DangerousRelease();
                OnCompleted(OnCompletedResult.Completed);
            }

            protected override void ExecuteThreadPoolWorkItem()
            {
                if (!_runOnThreadPool)
                {
                    base.ExecuteThreadPoolWorkItem();
                    return;
                }

                if (_executionContext == null || _executionContext.IsDefault)
                {
                    ExecuteOnThreadPool();
                }
                else
                {
                    ExecutionContext.RunForThreadPoolUnsafe(_executionContext, static x => x.ExecuteOnThreadPool(), this);
                }
            }

            internal void Reset()
            {
                _buffer = default;
                _buffers = null;
                _syncBuffer = null;
                _offset = 0;
                _bufferIndex = 0;
                _bufferOffset = 0;
                _runOnThreadPool = false;
                _executionContext = null;
                _cancellationToken = default;
                _strategy = null;
                Exception = null;
                _mrvtsc.Reset();
            }

            protected internal override bool TryCompleteOperation(SafeHandle handle)
            {
                if (_buffers != null)
                {
                    if (_owner.TryCompleteWriteAt(ref _offset, _buffers, ref _bufferIndex, ref _bufferOffset, out Interop.ErrorInfo errorInfo))
                    {
                        if (errorInfo.Error != Interop.Error.SUCCESS)
                        {
                            Exception = Interop.GetExceptionForIoErrno(errorInfo, _owner.Path);
                        }
                        return true;
                    }
                    Debug.Assert(!_runOnThreadPool, "ThreadPool only used with non-blocking");
                    return false;
                }

                if (_syncBuffer != null)
                {
                    Debug.Assert(_syncRemaining > 0);

                    bool completed = _owner.TryCompleteWriteAt(_offset, _syncBuffer, _syncRemaining, out int bytesWritten, out Interop.ErrorInfo errorInfo);

                    _syncBuffer += bytesWritten;
                    _syncRemaining -= bytesWritten;
                    _offset += bytesWritten;

                    if (completed)
                    {
                        if (errorInfo.Error != Interop.Error.SUCCESS)
                        {
                            Exception = Interop.GetExceptionForIoErrno(errorInfo, _owner.Path);
                        }
                        return true;
                    }

                    Debug.Assert(!_runOnThreadPool, "ThreadPool only used with non-blocking");
                    return false;
                }

                ReadOnlySpan<byte> span = _buffer.Span;
                Debug.Assert(!span.IsEmpty);

                fixed (byte* bufPtr = &MemoryMarshal.GetReference(span))
                {
                    bool completed = _owner.TryCompleteWriteAt(_offset, bufPtr, span.Length, out int bytesWritten, out Interop.ErrorInfo errorInfo);

                    _buffer = _buffer.Slice(bytesWritten);
                    _offset += bytesWritten;

                    if (completed)
                    {
                        if (errorInfo.Error != Interop.Error.SUCCESS)
                        {
                            Exception = Interop.GetExceptionForIoErrno(errorInfo, _owner.Path);
                        }
                        return true;
                    }
                    Debug.Assert(!_runOnThreadPool, "ThreadPool only used with non-blocking");
                    return false;
                }
            }

            protected internal override void OnCompleted(OnCompletedResult result)
            {
                if (_syncBuffer != null)
                {
                    _strategy?.OnIncompleteOperation(_syncRemaining, 0);
                }
                else
                {
                    UpdateFileStreamForAsyncWrite(_strategy, _buffer);
                }

                if (result == OnCompletedResult.Completed)
                {
                    if (Exception != null)
                    {
                        _mrvtsc.SetException(Exception);
                    }
                    else
                    {
                        _mrvtsc.SetResult(default);
                    }
                }
                else if (result == OnCompletedResult.Canceled)
                {
                    _mrvtsc.SetException(new OperationCanceledException(_cancellationToken));
                }
                else
                {
                    Debug.Assert(result == OnCompletedResult.Aborted);
                    _mrvtsc.SetException(new OperationCanceledException());
                }
            }

            ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
                => _mrvtsc.GetStatus(token);

            void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
                => _mrvtsc.OnCompleted(continuation, state, token, flags);

            void IValueTaskSource.GetResult(short token)
            {
                bool canPool = _mrvtsc.GetStatus(token) != ValueTaskSourceStatus.Canceled;
                try
                {
                    _mrvtsc.GetResult(token);
                }
                finally
                {
                    if (canPool)
                    {
                        _owner.ReturnWriteOperation(this);
                    }
                }
            }
        }

        private unsafe bool TryCompleteReadAt(long offset, IReadOnlyList<Memory<byte>> buffers, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, out bool pending)
        {
            if (SupportsRandomAccess)
            {
                if (TryCompleteReadAt(useOffset: true, offset, buffers, out result, out pending))
                {
                    if (result.BytesRead == -1 && ShouldFallBackToNonOffsetSyscall(result.ErrorInfo))
                    {
                        SupportsRandomAccess = false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return TryCompleteReadAt(useOffset: false, offset, buffers, out result, out pending);
        }

        private unsafe bool TryCompleteReadAt(bool useOffset, long offset, IReadOnlyList<Memory<byte>> buffers, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, out bool pending)
        {
            int count = buffers.Count;
            MemoryHandle[] memHandles = new MemoryHandle[count];
            Span<Interop.Sys.IOVector> vectors = count <= IovStackThreshold
                ? stackalloc Interop.Sys.IOVector[IovStackThreshold].Slice(0, count)
                : new Interop.Sys.IOVector[count];

            try
            {
                for (int i = 0; i < count; i++)
                {
                    Memory<byte> buffer = buffers[i];
                    MemoryHandle mh = buffer.Pin();
                    vectors[i] = new Interop.Sys.IOVector { Base = (byte*)mh.Pointer, Count = (UIntPtr)buffer.Length };
                    memHandles[i] = mh;
                }

                fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                {
                    long bytesRead = useOffset
                        ? Interop.Sys.PReadV(this, pinnedVectors, count, offset)
                        : Interop.Sys.ReadV(this, pinnedVectors, count);
                    if (bytesRead < 0)
                    {
                        Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                        if (IsPending(errorInfo))
                        {
                            pending = true;
                            result = default;
                            return false;
                        }
                        pending = false;
                        result = (-1, errorInfo);
                        return true;
                    }

                    pending = false;
                    result = ((int)bytesRead, default);
                    return true;
                }
            }
            finally
            {
                foreach (MemoryHandle mh in memHandles)
                {
                    mh.Dispose();
                }
            }
        }

        private unsafe bool TryCompleteReadAt(long offset, byte* buffer, int length, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, out bool pending)
        {
            if (SupportsRandomAccess)
            {
                if (TryCompleteReadAt(useOffset: true, offset, buffer, length, out result, out pending))
                {
                    if (result.BytesRead == -1 && ShouldFallBackToNonOffsetSyscall(result.ErrorInfo))
                    {
                        SupportsRandomAccess = false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return TryCompleteReadAt(useOffset: false, offset, buffer, length, out result, out pending);
        }

        private unsafe bool TryCompleteReadAt(bool useOffset, long offset, byte* buffer, int length, out (int BytesRead, Interop.ErrorInfo ErrorInfo) result, out bool pending)
        {
            int bytesRead = useOffset
                ? Interop.Sys.PRead(this, buffer, length, offset)
                : Interop.Sys.Read(this, buffer, length);
            if (bytesRead < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
                if (IsPending(errorInfo))
                {
                    pending = true;
                    result = default;
                    return false;
                }
                pending = false;
                result = (-1, errorInfo);
                return true;
            }

            pending = false;
            result = (bytesRead, default);
            return true;
        }

        private unsafe bool TryCompleteWriteAt(ref long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, ref int bufferIndex, ref int bufferOffset, out Interop.ErrorInfo errorInfo)
        {
            if (SupportsRandomAccess)
            {
                if (TryCompleteWriteAt(useOffset: true, ref offset, buffers, ref bufferIndex, ref bufferOffset, out errorInfo))
                {
                    if (errorInfo.Error != Interop.Error.SUCCESS && ShouldFallBackToNonOffsetSyscall(errorInfo))
                    {
                        SupportsRandomAccess = false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return TryCompleteWriteAt(useOffset: false, ref offset, buffers, ref bufferIndex, ref bufferOffset, out errorInfo);
        }

        private unsafe bool TryCompleteWriteAt(bool useOffset, ref long offset, IReadOnlyList<ReadOnlyMemory<byte>> buffers, ref int bufferIndex, ref int bufferOffset, out Interop.ErrorInfo errorInfo)
        {
            Span<Interop.Sys.IOVector> stackVectors = stackalloc Interop.Sys.IOVector[IovStackThreshold];
            while (true)
            {
                // Skip zero-length buffers.
                while (bufferIndex < buffers.Count && buffers[bufferIndex].Length == 0)
                {
                    bufferIndex++;
                }

                if (bufferIndex >= buffers.Count)
                {
                    errorInfo = default;
                    return true;
                }

                int remaining = buffers.Count - bufferIndex;
                MemoryHandle[] memHandles = new MemoryHandle[remaining];
                Span<Interop.Sys.IOVector> vectors = remaining <= IovStackThreshold
                    ? stackVectors.Slice(0, remaining)
                    : new Interop.Sys.IOVector[remaining];

                try
                {
                    long totalToWrite = 0;
                    for (int i = 0; i < remaining; i++)
                    {
                        ReadOnlyMemory<byte> buf = buffers[bufferIndex + i];
                        MemoryHandle mh = buf.Pin();
                        byte* ptr = (byte*)mh.Pointer;
                        int len = buf.Length;
                        if (i == 0 && bufferOffset > 0)
                        {
                            ptr += bufferOffset;
                            len -= bufferOffset;
                        }
                        vectors[i] = new Interop.Sys.IOVector { Base = ptr, Count = (UIntPtr)len };
                        memHandles[i] = mh;
                        totalToWrite += len;
                    }

                    fixed (Interop.Sys.IOVector* pinnedVectors = &MemoryMarshal.GetReference(vectors))
                    {
                        long bytesWritten = useOffset
                            ? Interop.Sys.PWriteV(this, pinnedVectors, remaining, offset)
                            : Interop.Sys.WriteV(this, pinnedVectors, remaining);
                        if (bytesWritten < 0)
                        {
                            errorInfo = Interop.Sys.GetLastErrorInfo();
                            return !IsPending(errorInfo);
                        }

                        errorInfo = default;
                        offset += bytesWritten;

                        if (bytesWritten == totalToWrite)
                        {
                            return true;
                        }

                        long written = bytesWritten;
                        while (written > 0)
                        {
                            int currentLen = buffers[bufferIndex].Length - bufferOffset;
                            if (written >= currentLen)
                            {
                                written -= currentLen;
                                bufferIndex++;
                                bufferOffset = 0;
                            }
                            else
                            {
                                bufferOffset += (int)written;
                                written = 0;
                            }
                        }
                    }
                }
                finally
                {
                    foreach (MemoryHandle mh in memHandles)
                    {
                        mh.Dispose();
                    }
                }
            }
        }

        private unsafe bool TryCompleteWriteAt(long offset, byte* buffer, int length, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            if (SupportsRandomAccess)
            {
                if (TryCompleteWriteAt(useOffset: true, offset, buffer, length, out bytesWritten, out errorInfo))
                {
                    if (errorInfo.Error != Interop.Error.SUCCESS && ShouldFallBackToNonOffsetSyscall(errorInfo))
                    {
                        SupportsRandomAccess = false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }

            return TryCompleteWriteAt(useOffset: false, offset, buffer, length, out bytesWritten, out errorInfo);
        }

        private unsafe bool TryCompleteWriteAt(bool useOffset, long offset, byte* buffer, int length, out int bytesWritten, out Interop.ErrorInfo errorInfo)
        {
            int totalBytesWritten = 0;
            while (true)
            {
                int toWrite = GetNumberOfBytesToWrite(length);
                int written = useOffset
                    ? Interop.Sys.PWrite(this, buffer, toWrite, offset)
                    : Interop.Sys.Write(this, buffer, toWrite);
                if (written < 0)
                {
                    errorInfo = Interop.Sys.GetLastErrorInfo();
                    bytesWritten = totalBytesWritten;
                    return !IsPending(errorInfo);
                }

                totalBytesWritten += written;
                length -= written;
                if (length == 0)
                {
                    errorInfo = default;
                    bytesWritten = totalBytesWritten;
                    return true;
                }

                buffer += written;
                offset += written;
            }
        }

        private static void UpdateFileStreamForAsyncRead(OSFileStreamStrategy? strategy, Memory<byte> destination, int bytesRead)
        {
            if (strategy is not null)
            {
                int bytesRemaining = destination.Length - Math.Max(bytesRead, 0);
                strategy.OnIncompleteOperation(bytesRemaining, 0);
            }
        }

        private static void UpdateFileStreamForAsyncWrite(OSFileStreamStrategy? strategy, ReadOnlyMemory<byte> remaining)
        {
            strategy?.OnIncompleteOperation(remaining.Length, 0);
        }

        private int CheckFileCall(int result, Interop.ErrorInfo errorInfo)
        {
            if (result == -1)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, Path);
            }
            return result;
        }

        private void CheckFileCall(Interop.ErrorInfo errorInfo)
        {
            if (errorInfo.Error != Interop.Error.SUCCESS)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, Path);
            }
        }

        private static bool ShouldFallBackToNonOffsetSyscall(Interop.ErrorInfo errorInfo)
            => errorInfo.Error is Interop.Error.ENXIO or Interop.Error.ESPIPE;

        private static bool IsPending(Interop.ErrorInfo errorInfo)
            => errorInfo.Error is Interop.Error.EAGAIN or Interop.Error.EWOULDBLOCK;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumberOfBytesToWrite(int byteCount)
        {
#if DEBUG
            // In debug only, to assist with testing, simulate writing fewer than the requested number of bytes.
            if (byteCount > 1 &&  // ensure we don't turn the write into a zero-byte write
                byteCount < 512)  // avoid on larger buffers that might have a length used to meet an alignment requirement
            {
                byteCount /= 2;
            }
#endif
            return byteCount;
        }
    }
}
