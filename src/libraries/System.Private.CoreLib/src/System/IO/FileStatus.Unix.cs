// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal partial struct FileStatus
    {
        private const int NanosecondsPerTick = 100;

        private const int InitializedExistsDir = -3;  // target is directory.
        private const int InitializedExistsFile = -2; // target is file.
        private const int InitializedNotExists = -1;  // entry does not exist.
        private const int Uninitialized = 0;          // uninitialized, '0' to make default(FileStatus) uninitialized.

        // Tracks the initialization state.
        // < 0 : initialized succesfully. Value is InitializedNotExists, InitializedExistsFile or InitializedExistsDir.
        //   0 : uninitialized.
        // > 0 : initialized with error. Value is raw errno.
        private int _state;

        // The last cached lstat information about the file.
        // Must only be used after calling EnsureCachesInitialized and checking EntryExists is true.
        private Interop.Sys.FileStatus _fileCache;

        private bool EntryExists => _state <= InitializedExistsFile;

        private bool IsDir => _state == InitializedExistsDir;

        // Check if the main path (without following symlinks) has the hidden attribute set.
        private bool HasHiddenFlag
        {
            get
            {
                Debug.Assert(_state != Uninitialized); // Use this after EnsureCachesInitialized has been called.

                return EntryExists && (_fileCache.UserFlags & (uint)Interop.Sys.UserFlags.UF_HIDDEN) == (uint)Interop.Sys.UserFlags.UF_HIDDEN;
            }
        }

        // Checks if the main path (without following symlinks) has the read-only attribute set.
        private bool HasReadOnlyFlag
        {
            get
            {
                Debug.Assert(_state != Uninitialized); // Use this after EnsureCachesInitialized has been called.

                if (!EntryExists)
                {
                    return false;
                }

#if TARGET_BROWSER
                var mode = (Interop.Sys.Permissions)(_fileCache.Mode & (int)Interop.Sys.Permissions.Mask);
                bool isUserReadOnly = (mode & Interop.Sys.Permissions.S_IRUSR) != 0 && // has read permission
                                      (mode & Interop.Sys.Permissions.S_IWUSR) == 0;   // but not write permission
                return isUserReadOnly;
#else
                if (_isReadOnlyCache == 0)
                {
                    return false;
                }
                else if (_isReadOnlyCache == 1)
                {
                    return true;
                }
                else
                {
                    bool isReadOnly = IsModeReadOnlyCore();
                    _isReadOnlyCache = isReadOnly ? 1 : 0;
                    return isReadOnly;
                }
#endif
            }
        }

#if !TARGET_BROWSER
        // HasReadOnlyFlag cache.
        // Must only be used after calling EnsureCachesInitialized.
        private int _isReadOnlyCache;

        private bool IsModeReadOnlyCore()
        {
            var mode = (Interop.Sys.Permissions)(_fileCache.Mode & (int)Interop.Sys.Permissions.Mask);

            bool isUserReadOnly = (mode & Interop.Sys.Permissions.S_IRUSR) != 0 && // has read permission
                                  (mode & Interop.Sys.Permissions.S_IWUSR) == 0;   // but not write permission
            bool isGroupReadOnly = (mode & Interop.Sys.Permissions.S_IRGRP) != 0 && // has read permission
                                    (mode & Interop.Sys.Permissions.S_IWGRP) == 0;   // but not write permission
            bool isOtherReadOnly = (mode & Interop.Sys.Permissions.S_IROTH) != 0 && // has read permission
                                    (mode & Interop.Sys.Permissions.S_IWOTH) == 0;   // but not write permission

            // If they are all the same, no need to check user/group.
            if ((isUserReadOnly == isGroupReadOnly) && (isGroupReadOnly == isOtherReadOnly))
            {
                return isUserReadOnly;
            }

            if (_fileCache.Uid == Interop.Sys.GetEUid())
            {
                // User owns the file.
                return isUserReadOnly;
            }

            // System files often have the same permissions for group and other (umask 022).
            if (isGroupReadOnly == isUserReadOnly)
            {
                return isGroupReadOnly;
            }

            if (Interop.Sys.IsMemberOfGroup(_fileCache.Gid))
            {
                // User belongs to group that owns the file.
                return isGroupReadOnly;
            }
            else
            {
                // Other permissions.
                return isOtherReadOnly;
            }
        }
#endif

        // Checks if the main path is a symbolic link
        private bool HasSymbolicLinkFlag
        {
            get
            {
                Debug.Assert(_state != Uninitialized); // Use this after EnsureCachesInitialized has been called.

                return EntryExists && (_fileCache.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK;
            }
        }

        // Sets the cache initialization flags to 0, which means the caches are now uninitialized
        internal void InvalidateCaches()
        {
            _state = Uninitialized;
        }

        internal bool IsReadOnly(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);
            return HasReadOnlyFlag;
        }

        internal bool IsFileSystemEntryHidden(ReadOnlySpan<char> path, ReadOnlySpan<char> fileName)
        {
            // Because this is called for FileSystemEntry we can assume the entry exists and
            // avoid initialization in some cases.
            if (IsNameHidden(fileName))
            {
                return true;
            }
            if (!Interop.Sys.SupportsHiddenFlag)
            {
                return false;
            }

            EnsureCachesInitialized(path, continueOnError: true);
            return HasHiddenFlag;
        }

        internal static bool IsNameHidden(ReadOnlySpan<char> fileName) => fileName.Length > 0 && fileName[0] == '.';

        // Returns true if the path points to a directory, or if the path is a symbolic link
        // that points to a directory
        internal bool IsDirectory(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);
            return IsDir;
        }

        internal bool IsSymbolicLink(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);
            return HasSymbolicLinkFlag;
        }

        internal FileAttributes GetAttributes(ReadOnlySpan<char> path, ReadOnlySpan<char> fileName, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);

            if (!EntryExists)
                return (FileAttributes)(-1);

            FileAttributes attributes = default;

            if (HasReadOnlyFlag)
                attributes |= FileAttributes.ReadOnly;

            if (HasSymbolicLinkFlag)
                attributes |= FileAttributes.ReparsePoint;

            if (IsDir) // Refresh caches this
                attributes |= FileAttributes.Directory;

            if (IsNameHidden(fileName) || HasHiddenFlag)
                attributes |= FileAttributes.Hidden;

            return attributes != default ? attributes : FileAttributes.Normal;
        }

        internal void SetAttributes(string path, FileAttributes attributes, bool asDirectory)
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

            EnsureCachesInitialized(path);

            if (!EntryExists)
                FileSystemInfo.ThrowNotFound(path);

            if (Interop.Sys.CanSetHiddenFlag)
            {
                if ((attributes & FileAttributes.Hidden) != 0 && (_fileCache.UserFlags & (uint)Interop.Sys.UserFlags.UF_HIDDEN) == 0)
                {
                    // If Hidden flag is set and cached file status does not have the flag set then set it
                    Interop.CheckIo(Interop.Sys.LChflags(path, (_fileCache.UserFlags | (uint)Interop.Sys.UserFlags.UF_HIDDEN)), path, asDirectory);
                }
                else if (HasHiddenFlag)
                {
                    // If Hidden flag is not set and cached file status does have the flag set then remove it
                    Interop.CheckIo(Interop.Sys.LChflags(path, (_fileCache.UserFlags & ~(uint)Interop.Sys.UserFlags.UF_HIDDEN)), path, asDirectory);
                }
            }

            // The only thing we can reasonably change is whether the file object is readonly by changing permissions.

            int newMode = _fileCache.Mode;
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
            if (newMode != _fileCache.Mode)
            {
                Interop.CheckIo(Interop.Sys.ChMod(path, newMode), path, asDirectory);
            }

            InvalidateCaches();
        }

        internal bool GetExists(ReadOnlySpan<char> path, bool asDirectory)
        {
            EnsureCachesInitialized(path, continueOnError: true);
            return EntryExists && asDirectory == IsDir;
        }

        internal DateTimeOffset GetCreationTime(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);

            if (!EntryExists)
                return new DateTimeOffset(DateTime.FromFileTimeUtc(0));

            if ((_fileCache.Flags & Interop.Sys.FileStatusFlags.HasBirthTime) != 0)
                return UnixTimeToDateTimeOffset(_fileCache.BirthTime, _fileCache.BirthTimeNsec);

            // fall back to the oldest time we have in between change and modify time
            if (_fileCache.MTime < _fileCache.CTime ||
                (_fileCache.MTime == _fileCache.CTime && _fileCache.MTimeNsec < _fileCache.CTimeNsec))
                return UnixTimeToDateTimeOffset(_fileCache.MTime, _fileCache.MTimeNsec);

            return UnixTimeToDateTimeOffset(_fileCache.CTime, _fileCache.CTimeNsec);
        }

        internal DateTimeOffset GetLastAccessTime(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);

            if (!EntryExists)
                return new DateTimeOffset(DateTime.FromFileTimeUtc(0));

            return UnixTimeToDateTimeOffset(_fileCache.ATime, _fileCache.ATimeNsec);
        }

        internal void SetLastAccessTime(string path, DateTimeOffset time, bool asDirectory)
            => SetAccessOrWriteTime(path, time, isAccessTime: true, asDirectory);

        internal DateTimeOffset GetLastWriteTime(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            EnsureCachesInitialized(path, continueOnError);

            if (!EntryExists)
                return new DateTimeOffset(DateTime.FromFileTimeUtc(0));

            return UnixTimeToDateTimeOffset(_fileCache.MTime, _fileCache.MTimeNsec);
        }

        internal void SetLastWriteTime(string path, DateTimeOffset time, bool asDirectory)
            => SetAccessOrWriteTime(path, time, isAccessTime: false, asDirectory);

        private static DateTimeOffset UnixTimeToDateTimeOffset(long seconds, long nanoseconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).AddTicks(nanoseconds / NanosecondsPerTick);
        }

        private unsafe void SetAccessOrWriteTimeCore(string path, DateTimeOffset time, bool isAccessTime, bool checkCreationTime, bool asDirectory)
        {
            // This api is used to set creation time on non OSX platforms, and as a fallback for OSX platforms.
            // The reason why we use it to set 'creation time' is the below comment:
            // Unix provides APIs to update the last access time (atime) and last modification time (mtime).
            // There is no API to update the CreationTime.
            // Some platforms (e.g. Linux) don't store a creation time. On those platforms, the creation time
            // is synthesized as the oldest of last status change time (ctime) and last modification time (mtime).
            // We update the LastWriteTime (mtime).
            // This triggers a metadata change for FileSystemWatcher NotifyFilters.CreationTime.
            // Updating the mtime, causes the ctime to be set to 'now'. So, on platforms that don't store a
            // CreationTime, GetCreationTime will return the value that was previously set (when that value
            // wasn't in the future).

            // force a refresh so that we have an up-to-date times for values not being overwritten
            InvalidateCaches();
            EnsureCachesInitialized(path);

            if (!EntryExists)
                FileSystemInfo.ThrowNotFound(path);

            // we use utimes()/utimensat() to set the accessTime and writeTime
            Interop.Sys.TimeSpec* buf = stackalloc Interop.Sys.TimeSpec[2];

            long seconds = time.ToUnixTimeSeconds();
            long nanoseconds = UnixTimeSecondsToNanoseconds(time, seconds);

#if TARGET_BROWSER
            buf[0].TvSec = seconds;
            buf[0].TvNsec = nanoseconds;
            buf[1].TvSec = seconds;
            buf[1].TvNsec = nanoseconds;
#else
            if (isAccessTime)
            {
                buf[0].TvSec = seconds;
                buf[0].TvNsec = nanoseconds;
                buf[1].TvSec = _fileCache.MTime;
                buf[1].TvNsec = _fileCache.MTimeNsec;
            }
            else
            {
                buf[0].TvSec = _fileCache.ATime;
                buf[0].TvNsec = _fileCache.ATimeNsec;
                buf[1].TvSec = seconds;
                buf[1].TvNsec = nanoseconds;
            }
#endif
            Interop.CheckIo(Interop.Sys.UTimensat(path, buf), path, asDirectory);

            // On OSX-like platforms, when the modification time is less than the creation time (including
            // when the modification time is already less than but access time is being set), the creation
            // time is set to the modification time due to the api we're currently using; this is not
            // desirable behaviour since it is inconsistent with windows behaviour and is not logical to
            // the programmer (ie. we'd have to document it), so these api calls revert the creation time
            // when it shouldn't be set (since we're setting modification time and access time here).
            // checkCreationTime is only true on OSX-like platforms.
            // allowFallbackToLastWriteTime is ignored on non OSX-like platforms.
            bool updateCreationTime = checkCreationTime && (_fileCache.Flags & Interop.Sys.FileStatusFlags.HasBirthTime) != 0 &&
                                        (buf[1].TvSec < _fileCache.BirthTime || (buf[1].TvSec == _fileCache.BirthTime && buf[1].TvNsec < _fileCache.BirthTimeNsec));

            InvalidateCaches();

            if (updateCreationTime)
            {
                Interop.Error error = SetCreationTimeCore(path, _fileCache.BirthTime, _fileCache.BirthTimeNsec);
                if (error != Interop.Error.SUCCESS && error != Interop.Error.ENOTSUP)
                {
                    Interop.CheckIo(error, path, asDirectory);
                }
            }
        }

        internal long GetLength(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            // For symbolic links, on Windows, Length returns zero and not the target file size.
            // On Unix, it returns the length of the path stored in the link.

            EnsureCachesInitialized(path, continueOnError);
            return EntryExists ? _fileCache.Size : 0;
        }

        // Tries to refresh the lstat cache (_fileCache).
        // This method should not throw. Instead, we store the results, and we will throw when the user attempts to access any of the properties when there was a failure
        internal void RefreshCaches(ReadOnlySpan<char> path)
        {
            path = Path.TrimEndingDirectorySeparator(path);

#if !TARGET_BROWSER
            _isReadOnlyCache = -1;
#endif

            int rv = Interop.Sys.LStat(path, out _fileCache);
            if (rv < 0)
            {
                Interop.ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();

                if (errorInfo.Error == Interop.Error.ENOENT || // A component of the path does not exist, or path is an empty string
                    errorInfo.Error == Interop.Error.ENOTDIR)  // A component of the path prefix of path is not a directory
                {
                    _state = InitializedNotExists;
                }
                else
                {
                    Debug.Assert(errorInfo.RawErrno > 0); // Expect a positive integer
                    _state = errorInfo.RawErrno; // Initialized with error.
                }

                return;
            }

            // Check if the main path is a directory, or a link to a directory.
            int fileType = _fileCache.Mode & Interop.Sys.FileTypes.S_IFMT;
            bool isDirectory = fileType == Interop.Sys.FileTypes.S_IFDIR ||
                               (fileType == Interop.Sys.FileTypes.S_IFLNK
                                && Interop.Sys.Stat(path, out Interop.Sys.FileStatus target) == 0
                                && (target.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR);

            _state = isDirectory ? InitializedExistsDir : InitializedExistsFile;
        }

        // Checks if the file cache is uninitialized and refreshes it's value.
        // If it failed, and continueOnError is set to true, this method will throw.
        internal void EnsureCachesInitialized(ReadOnlySpan<char> path, bool continueOnError = false)
        {
            if (_state == Uninitialized)
            {
                RefreshCaches(path);
            }

            if (!continueOnError)
            {
                ThrowOnCacheInitializationError(path);
            }
        }

        // Throws if any of the caches has an error number saved in it
        private void ThrowOnCacheInitializationError(ReadOnlySpan<char> path)
        {
            // Lstat should always be initialized by Refresh
            int errno = _state;
            if (errno > 0)
            {
                InvalidateCaches();
                throw Interop.GetExceptionForIoErrno(new Interop.ErrorInfo(errno), new string(path));
            }
        }

        private static long UnixTimeSecondsToNanoseconds(DateTimeOffset time, long seconds)
        {
            const long TicksPerMillisecond = 10000;
            const long TicksPerSecond = TicksPerMillisecond * 1000;
            return (time.UtcDateTime.Ticks - DateTimeOffset.UnixEpoch.Ticks - seconds * TicksPerSecond) * NanosecondsPerTick;
        }
    }
}
