// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.IO
{
    partial class FileSystemInfo : IFileSystemObject
    {
        /// <summary>The last cached stat information about the file.</summary>
        private Interop.Sys.FileStatus _fileStatus;

        /// <summary>
        /// Whether we've successfully cached a stat structure.
        /// -1 if we need to refresh _fileStatus, 0 if we've successfully cached one,
        /// or any other value that serves as an errno error code from the
        /// last time we tried and failed to refresh _fileStatus.
        /// </summary>
        private int _fileStatusInitialized = -1;

        internal IFileSystemObject FileSystemObject
        {
            get { return this; }
        }

        internal void Invalidate()
        {
            _fileStatusInitialized = -1;
        }

        FileAttributes IFileSystemObject.Attributes
        {
            get
            {
                EnsureStatInitialized();

                FileAttributes attrs = default(FileAttributes);

                if (IsDirectoryAssumesInitialized)
                {
                    attrs |= FileAttributes.Directory;
                }
                if (IsReadOnlyAssumesInitialized)
                {
                    attrs |= FileAttributes.ReadOnly;
                }
                if (IsLink)
                {
                    attrs |= FileAttributes.ReparsePoint;
                }
                if (Path.GetFileName(FullPath).StartsWith("."))
                {
                    attrs |= FileAttributes.Hidden;
                }

                return attrs != default(FileAttributes) ?
                    attrs :
                    FileAttributes.Normal;
            }
            set
            {
                // Validate that only flags from the attribute are being provided.  This is an
                // approximation for the validation done by the Win32 function.
                const FileAttributes allValidFlags =
                    FileAttributes.Archive | FileAttributes.Compressed | FileAttributes.Device |
                    FileAttributes.Directory | FileAttributes.Encrypted | FileAttributes.Hidden |
                    FileAttributes.Hidden | FileAttributes.IntegrityStream | FileAttributes.Normal |
                    FileAttributes.NoScrubData | FileAttributes.NotContentIndexed | FileAttributes.Offline |
                    FileAttributes.ReadOnly | FileAttributes.ReparsePoint | FileAttributes.SparseFile |
                    FileAttributes.System | FileAttributes.Temporary;
                if ((value & ~allValidFlags) != 0)
                {
                    throw new ArgumentException(SR.Arg_InvalidFileAttrs, "value");
                }

                // The only thing we can reasonably change is whether the file object is readonly,
                // just changing its permissions accordingly.
                EnsureStatInitialized();
                IsReadOnlyAssumesInitialized = (value & FileAttributes.ReadOnly) != 0;
                _fileStatusInitialized = -1;
            }
        }

        /// <summary>Gets whether stat reported this system object as a directory.</summary>
        private bool IsDirectoryAssumesInitialized
        {
            get
            {
                return (_fileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR;
            }
        }

        private bool IsLink
        {
            get
            {
                return (_fileStatus.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFLNK;
            }
        }

        /// <summary>
        /// Gets or sets whether the file is read-only.  This is based on the read/write/execute
        /// permissions of the object.
        /// </summary>
        private bool IsReadOnlyAssumesInitialized
        {
            get
            {
                Interop.Sys.Permissions readBit, writeBit;
                if (_fileStatus.Uid == Interop.Sys.GetEUid())      // does the user effectively own the file?
                {
                    readBit  = Interop.Sys.Permissions.S_IRUSR;
                    writeBit = Interop.Sys.Permissions.S_IWUSR;
                }
                else if (_fileStatus.Gid == Interop.Sys.GetEGid()) // does the user belong to a group that effectively owns the file?
                {
                    readBit  = Interop.Sys.Permissions.S_IRGRP;
                    writeBit = Interop.Sys.Permissions.S_IWGRP;
                }
                else                                              // everyone else
                {
                    readBit  = Interop.Sys.Permissions.S_IROTH;
                    writeBit = Interop.Sys.Permissions.S_IWOTH;
                }

                return
                    (_fileStatus.Mode & (int)readBit) != 0 && // has read permission
                    (_fileStatus.Mode & (int)writeBit) == 0;  // but not write permission
            }
            set
            {
                int newMode = _fileStatus.Mode;
                if (value) // true if going from writable to readable, false if going from readable to writable
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
                    bool isDirectory = this is DirectoryInfo;
                    while (Interop.CheckIo(Interop.Sys.ChMod(FullPath, newMode), FullPath, isDirectory)) ;
                }
            }
        }

        bool IFileSystemObject.Exists
        {
            get
            {
                if (_fileStatusInitialized == -1)
                {
                    Refresh();
                }
                return
                    _fileStatusInitialized == 0 && // avoid throwing if Refresh failed; instead just return false
                    (this is DirectoryInfo) == IsDirectoryAssumesInitialized;
            }
        }

        DateTimeOffset IFileSystemObject.CreationTime
        {
            get
            {
                EnsureStatInitialized();
                return (_fileStatus.Flags & Interop.Sys.FileStatusFlags.HasBirthTime) != 0 ?
                    DateTimeOffset.FromUnixTimeSeconds(_fileStatus.BirthTime).ToLocalTime() :
                    default(DateTimeOffset);
            }
            set
            {
                // The ctime in Unix can be interpreted differently by different formats so there isn't
                // a reliable way to set this; however, we can't just do nothing since the FileSystemWatcher
                // specifically looks for this call to make a Metatdata Change, so we should set the
                // LastAccessTime of the file to cause the metadata change we need.
                LastAccessTime = LastAccessTime;
            }
        }

        DateTimeOffset IFileSystemObject.LastAccessTime
        {
            get
            {
                EnsureStatInitialized();
                return DateTimeOffset.FromUnixTimeSeconds(_fileStatus.ATime).ToLocalTime();
            }
            set { SetAccessWriteTimes(value.ToUnixTimeSeconds(), null); }
        }

        DateTimeOffset IFileSystemObject.LastWriteTime
        {
            get
            {
                EnsureStatInitialized();
                return DateTimeOffset.FromUnixTimeSeconds(_fileStatus.MTime).ToLocalTime();
            }
            set { SetAccessWriteTimes(null, value.ToUnixTimeSeconds()); }
        }

        private void SetAccessWriteTimes(long? accessTime, long? writeTime)
        {
            _fileStatusInitialized = -1; // force a refresh so that we have an up-to-date times for values not being overwritten
            EnsureStatInitialized();
            Interop.Sys.UTimBuf buf;
            buf.AcTime = accessTime ?? _fileStatus.ATime;
            buf.ModTime = writeTime ?? _fileStatus.MTime;
            bool isDirectory = this is DirectoryInfo;
            while (Interop.CheckIo(Interop.Sys.UTime(FullPath, ref buf), FullPath, isDirectory)) ;
            _fileStatusInitialized = -1;
        }

        long IFileSystemObject.Length
        {
            get
            {
                EnsureStatInitialized();
                return _fileStatus.Size;
            }
        }

        void IFileSystemObject.Refresh()
        {
            // This should not throw, instead we store the result so that we can throw it
            // when someone actually accesses a property.
            int result;
            while (true)
            {
                result = Interop.Sys.Stat(FullPath, out _fileStatus);
                if (result >= 0)
                {
                    _fileStatusInitialized = 0;
                }
                else
                {
                    var errorInfo = Interop.Sys.GetLastErrorInfo();
                    if (errorInfo.Error == Interop.Error.EINTR)
                    {
                        continue;
                    }
                    _fileStatusInitialized = errorInfo.RawErrno;
                }
                break;
            }
        }

        private void EnsureStatInitialized()
        {
            if (_fileStatusInitialized == -1)
            {
                Refresh();
            }

            if (_fileStatusInitialized != 0)
            {
                int errno = _fileStatusInitialized;
                _fileStatusInitialized = -1;
                var errorInfo =  new Interop.ErrorInfo(errno);

                // Windows distinguishes between whether the directory or the file isn't found,
                // and throws a different exception in these cases.  We attempt to approximate that
                // here; there is a race condition here, where something could change between
                // when the error occurs and our checks, but it's the best we can do, and the
                // worst case in such a race condition (which could occur if the file system is
                // being manipulated concurrently with these checks) is that we throw a
                // FileNotFoundException instead of DirectoryNotFoundexception.

                // directoryError is true only if a FileNotExists error was provided and the parent
                // directory of the file represented by _fullPath is nonexistent
                bool directoryError = (errorInfo.Error == Interop.Error.ENOENT && !Directory.Exists(Path.GetDirectoryName(PathHelpers.TrimEndingDirectorySeparator(FullPath)))); // The destFile's path is invalid
                throw Interop.GetExceptionForIoErrno(errorInfo, FullPath, directoryError);
            }
        }
    }
}
