// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal struct FileStatus
    {
        private const int NanosecondsPerTick = 100;

        // The last cached stat information about the file
        private Interop.Sys.FileStatus _fileStatus;

        // -1 if _fileStatus isn't initialized, 0 if _fileStatus was initialized with no
        // errors, or the errno error code.
        private int _fileStatusInitialized;

        // We track intent of creation to know whether or not we want to (1) create a
        // DirectoryInfo around this status struct or (2) actually are part of a DirectoryInfo.
        internal bool InitiallyDirectory { get; private set; }

        // Is a directory as of the last refresh
        internal bool _isDirectory;

        // Exists as of the last refresh
        private bool _exists;

        internal static void Initialize(
            ref FileStatus status,
            bool isDirectory)
        {
            status.InitiallyDirectory = isDirectory;
            status._fileStatusInitialized = -1;
        }

        internal void Invalidate() => _fileStatusInitialized = -1;

        internal bool IsReadOnly(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureStatInitialized(path, continueOnError);
#if TARGET_BROWSER
            const Interop.Sys.Permissions readBit = Interop.Sys.Permissions.S_IRUSR;
            const Interop.Sys.Permissions writeBit = Interop.Sys.Permissions.S_IWUSR;
#else
            Interop.Sys.Permissions readBit, writeBit;

            if (_fileStatus.Uid == Interop.Sys.GetEUid())
            {
                // User effectively owns the file
                readBit = Interop.Sys.Permissions.S_IRUSR;
                writeBit = Interop.Sys.Permissions.S_IWUSR;
            }
            else if (_fileStatus.Gid == Interop.Sys.GetEGid())
            {
                // User belongs to a group that effectively owns the file
                readBit = Interop.Sys.Permissions.S_IRGRP;
                writeBit = Interop.Sys.Permissions.S_IWGRP;
            }
            else
            {
                // Others permissions
                readBit = Interop.Sys.Permissions.S_IROTH;
                writeBit = Interop.Sys.Permissions.S_IWOTH;
            }
#endif

            return ((_fileStatus.Mode & (int)readBit) != 0 && // has read permission
                (_fileStatus.Mode & (int)writeBit) == 0);     // but not write permission
        }

        public FileAttributes GetAttributes(ReadOnlySpan<char> path, ReadOnlySpan<char> fileName)
        {
            // IMPORTANT: Attribute logic must match the logic in FileSystemEntry

            EnsureStatInitialized(path);

            if (!_exists)
                return (FileAttributes)(-1);

            FileAttributes attributes = default;

            if (IsReadOnly(path))
                attributes |= FileAttributes.ReadOnly;

            if ((_fileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK)
                attributes |= FileAttributes.ReparsePoint;

            if (_isDirectory)
                attributes |= FileAttributes.Directory;

            // If the filename starts with a period or has UF_HIDDEN flag set, it's hidden.
            if (fileName.Length > 0 && (fileName[0] == '.' || (_fileStatus.UserFlags & (uint)Interop.Sys.UserFlags.UF_HIDDEN) == (uint)Interop.Sys.UserFlags.UF_HIDDEN))
                attributes |= FileAttributes.Hidden;

            return attributes != default ? attributes : FileAttributes.Normal;
        }

        public void SetAttributes(string path, FileAttributes attributes)
        {
            // Validate that only flags from the attribute are being provided.  This is an
            // approximation for the validation done by the Win32 function.
            const FileAttributes allValidFlags =
                FileAttributes.Archive | FileAttributes.Compressed | FileAttributes.Device |
                FileAttributes.Directory | FileAttributes.Encrypted | FileAttributes.Hidden |
                FileAttributes.IntegrityStream | FileAttributes.Normal | FileAttributes.NoScrubData |
                FileAttributes.NotContentIndexed | FileAttributes.Offline | FileAttributes.ReadOnly |
                FileAttributes.ReparsePoint | FileAttributes.SparseFile | FileAttributes.System |
                FileAttributes.Temporary;
            if ((attributes & ~allValidFlags) != 0)
            {
                // Using constant string for argument to match historical throw
                throw new ArgumentException(SR.Arg_InvalidFileAttrs, "Attributes");
            }

            EnsureStatInitialized(path);

            if (!_exists)
                FileSystemInfo.ThrowNotFound(path);

            if (Interop.Sys.CanSetHiddenFlag)
            {
                if ((attributes & FileAttributes.Hidden) != 0)
                {
                    if ((_fileStatus.UserFlags & (uint)Interop.Sys.UserFlags.UF_HIDDEN) == 0)
                    {
                        // If Hidden flag is set and cached file status does not have the flag set then set it
                        Interop.CheckIo(Interop.Sys.LChflags(path, (_fileStatus.UserFlags | (uint)Interop.Sys.UserFlags.UF_HIDDEN)), path, InitiallyDirectory);
                    }
                }
                else
                {
                    if ((_fileStatus.UserFlags & (uint)Interop.Sys.UserFlags.UF_HIDDEN) == (uint)Interop.Sys.UserFlags.UF_HIDDEN)
                    {
                        // If Hidden flag is not set and cached file status does have the flag set then remove it
                        Interop.CheckIo(Interop.Sys.LChflags(path, (_fileStatus.UserFlags & ~(uint)Interop.Sys.UserFlags.UF_HIDDEN)), path, InitiallyDirectory);
                    }
                }
            }

            // The only thing we can reasonably change is whether the file object is readonly by changing permissions.

            int newMode = _fileStatus.Mode;
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                // Take away all write permissions from user/group/everyone
                newMode &= ~(int)(Interop.Sys.Permissions.S_IWUSR | Interop.Sys.Permissions.S_IWGRP | Interop.Sys.Permissions.S_IWOTH);
            }
            else if ((newMode & (int)Interop.Sys.Permissions.S_IRUSR) != 0)
            {
                // Give write permission to the owner if the owner has read permission
                newMode |= (int)Interop.Sys.Permissions.S_IWUSR;
            }

            // Change the permissions on the file
            if (newMode != _fileStatus.Mode)
            {
                Interop.CheckIo(Interop.Sys.ChMod(path, newMode), path, InitiallyDirectory);
            }

            _fileStatusInitialized = -1;
        }

        internal bool GetExists(ReadOnlySpan<char> path)
        {
            if (_fileStatusInitialized == -1)
                Refresh(path);

            return _exists && InitiallyDirectory == _isDirectory;
        }

        internal DateTimeOffset GetCreationTime(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureStatInitialized(path, continueOnError);
            if (!_exists)
                return DateTimeOffset.FromFileTime(0);

            if ((_fileStatus.Flags & Interop.Sys.FileStatusFlags.HasBirthTime) != 0)
                return UnixTimeToDateTimeOffset(_fileStatus.BirthTime, _fileStatus.BirthTimeNsec);

            // fall back to the oldest time we have in between change and modify time
            if (_fileStatus.MTime < _fileStatus.CTime ||
                (_fileStatus.MTime == _fileStatus.CTime && _fileStatus.MTimeNsec < _fileStatus.CTimeNsec))
                return UnixTimeToDateTimeOffset(_fileStatus.MTime, _fileStatus.MTimeNsec);

            return UnixTimeToDateTimeOffset(_fileStatus.CTime, _fileStatus.CTimeNsec);
        }

        internal void SetCreationTime(string path, DateTimeOffset time)
        {
            // Unix provides APIs to update the last access time (atime) and last modification time (mtime).
            // There is no API to update the CreationTime.
            // Some platforms (e.g. Linux) don't store a creation time. On those platforms, the creation time
            // is synthesized as the oldest of last status change time (ctime) and last modification time (mtime).
            // We update the LastWriteTime (mtime).
            // This triggers a metadata change for FileSystemWatcher NotifyFilters.CreationTime.
            // Updating the mtime, causes the ctime to be set to 'now'. So, on platforms that don't store a
            // CreationTime, GetCreationTime will return the value that was previously set (when that value
            // wasn't in the future).
            SetLastWriteTime(path, time);
        }

        internal DateTimeOffset GetLastAccessTime(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureStatInitialized(path, continueOnError);
            if (!_exists)
                return DateTimeOffset.FromFileTime(0);
            return UnixTimeToDateTimeOffset(_fileStatus.ATime, _fileStatus.ATimeNsec);
        }

        internal void SetLastAccessTime(string path, DateTimeOffset time) => SetAccessOrWriteTime(path, time, isAccessTime: true);

        internal DateTimeOffset GetLastWriteTime(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureStatInitialized(path, continueOnError);
            if (!_exists)
                return DateTimeOffset.FromFileTime(0);
            return UnixTimeToDateTimeOffset(_fileStatus.MTime, _fileStatus.MTimeNsec);
        }

        internal void SetLastWriteTime(string path, DateTimeOffset time) => SetAccessOrWriteTime(path, time, isAccessTime: false);

        private DateTimeOffset UnixTimeToDateTimeOffset(long seconds, long nanoseconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).AddTicks(nanoseconds / NanosecondsPerTick).ToLocalTime();
        }

        private unsafe void SetAccessOrWriteTime(string path, DateTimeOffset time, bool isAccessTime)
        {
            // force a refresh so that we have an up-to-date times for values not being overwritten
            _fileStatusInitialized = -1;
            EnsureStatInitialized(path);

            // we use utimes()/utimensat() to set the accessTime and writeTime
            Interop.Sys.TimeSpec* buf = stackalloc Interop.Sys.TimeSpec[2];

            long seconds = time.ToUnixTimeSeconds();

            const long TicksPerMillisecond = 10000;
            const long TicksPerSecond = TicksPerMillisecond * 1000;
            long nanoseconds = (time.UtcDateTime.Ticks - DateTimeOffset.UnixEpoch.Ticks - seconds * TicksPerSecond) * NanosecondsPerTick;

            if (isAccessTime)
            {
                buf[0].TvSec = seconds;
                buf[0].TvNsec = nanoseconds;
                buf[1].TvSec = _fileStatus.MTime;
                buf[1].TvNsec = _fileStatus.MTimeNsec;
            }
            else
            {
                buf[0].TvSec = _fileStatus.ATime;
                buf[0].TvNsec = _fileStatus.ATimeNsec;
                buf[1].TvSec = seconds;
                buf[1].TvNsec = nanoseconds;
            }

            Interop.CheckIo(Interop.Sys.UTimensat(path, buf), path, InitiallyDirectory);

            _fileStatusInitialized = -1;
        }

        internal long GetLength(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureStatInitialized(path, continueOnError);
            return _fileStatus.Size;
        }

        public void Refresh(ReadOnlySpan<char> path)
        {
            // This should not throw, instead we store the result so that we can throw it
            // when someone actually accesses a property.

            // Use lstat to get the details on the object, without following symlinks.
            // If it is a symlink, then subsequently get details on the target of the symlink,
            // storing those results separately.  We only report failure if the initial
            // lstat fails, as a broken symlink should still report info on exists, attributes, etc.
            _isDirectory = false;
            path = Path.TrimEndingDirectorySeparator(path);
            int result = Interop.Sys.LStat(path, out _fileStatus);
            if (result < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                // This should never set the error if the file can't be found.
                // (see the Windows refresh passing returnErrorOnNotFound: false).
                if (errorInfo.Error == Interop.Error.ENOENT
                    || errorInfo.Error == Interop.Error.ENOTDIR)
                {
                    _fileStatusInitialized = 0;
                    _exists = false;
                }
                else
                {
                    _fileStatusInitialized = errorInfo.RawErrno;
                }
                return;
            }

            _exists = true;

            // IMPORTANT: Is directory logic must match the logic in FileSystemEntry
            _isDirectory = (_fileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;

            // If we're a symlink, attempt to check the target to see if it is a directory
            if ((_fileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK &&
                Interop.Sys.Stat(path, out Interop.Sys.FileStatus targetStatus) >= 0)
            {
                _isDirectory = (targetStatus.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;
            }

            _fileStatusInitialized = 0;
        }

        internal void EnsureStatInitialized(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            if (_fileStatusInitialized == -1)
            {
                Refresh(path);
            }

            if (_fileStatusInitialized != 0 && !continueOnError)
            {
                int errno = _fileStatusInitialized;
                _fileStatusInitialized = -1;
                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(errno), new string(path));
            }
        }
    }
}
