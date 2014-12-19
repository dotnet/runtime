// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace System.IO
{
    internal sealed partial class UnixFileSystem
    {
        /// <summary>Provides an IFileSystemObject implementation for Unix systems.</summary>
        internal sealed class UnixFileSystemObject : IFileSystemObject
        {
            /// <summary>The full path to the system object.</summary>
            private string _fullPath;

            /// <summary>Whether this path is interpreted to be a directory.</summary>
            private bool _isDirectory;

            /// <summary>The last cached stat information about the file.</summary>
            private Interop.structStat _stat;

            /// <summary>
            /// Whether we've successfully cached a stat structure.
            /// -1 if we need to refresh _stat, 0 if we've successfully cached one,
            /// or any other value that serves as an errno error code from the
            /// last time we tried and failed to refresh _stat.
            /// </summary>
            private int _statInitialized = -1;

            public UnixFileSystemObject(string fullPath, bool asDirectory)
            {
                _fullPath = fullPath;
                _isDirectory = asDirectory;
            }

            public FileAttributes Attributes
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
                    if (Path.GetFileName(_fullPath).StartsWith("."))
                    {
                        attrs |= FileAttributes.Hidden;
                    }

                    return attrs != default(FileAttributes) ?
                        attrs :
                        FileAttributes.Normal;
                }
                set
                {
                    // We can't change directory or hidden; the only thing we can reasonably change
                    // is whether the file object is readonly, just changing its permissions accordingly.
                    EnsureStatInitialized();
                    IsReadOnlyAssumesInitialized = (value & FileAttributes.ReadOnly) != 0;
                }
            }

            /// <summary>Gets whether stat reported this system object as a directory.</summary>
            private bool IsDirectoryAssumesInitialized
            {
                get
                {
                    return (_stat.st_mode & (int)Interop.FileTypes.S_IFMT) == (int)Interop.FileTypes.S_IFDIR;
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
                    Interop.Permissions readBit, writeBit;
                    if (_stat.st_uid == Interop.geteuid())      // does the user effectively own the file?
                    {
                        readBit = Interop.Permissions.S_IRUSR;
                        writeBit = Interop.Permissions.S_IWUSR;
                    }
                    else if (_stat.st_gid == Interop.getegid()) // does the user belong to a group that effectively owns the file?
                    {
                        readBit = Interop.Permissions.S_IRGRP;
                        writeBit = Interop.Permissions.S_IWGRP;
                    }
                    else                                        // everyone else
                    {
                        readBit = Interop.Permissions.S_IROTH;
                        writeBit = Interop.Permissions.S_IWOTH;
                    }

                    return
                        (_stat.st_mode & (int)readBit) != 0 && // has read permission
                        (_stat.st_mode & (int)writeBit) == 0;  // but not write permission
                }
                set
                {
                    bool isReadOnly = IsReadOnlyAssumesInitialized;
                    if (value == isReadOnly)
                    {
                        return;
                    }

                    int newMode = _stat.st_mode;
                    if (value) // true if going from writable to readable, false if going from readable to writable
                    {
                        // Take away all write permissions from user/group/everyone
                        newMode &= ~(int)(Interop.Permissions.S_IWUSR | Interop.Permissions.S_IWGRP | Interop.Permissions.S_IWOTH);
                    }
                    else if ((newMode & (int)Interop.Permissions.S_IRUSR) != 0)
                    {
                        // Give write permission to the owner if the owner has read permission
                        newMode |= (int)Interop.Permissions.S_IWUSR;
                    }

                    // Change the permissions on the file
                    if (newMode != _stat.st_mode)
                    {
                        while (Interop.CheckIo(Interop.chmod(_fullPath, newMode), _fullPath, _isDirectory)) ;
                    }
                }
            }

            public bool Exists
            {
                get
                {
                    if (_statInitialized == -1)
                    {
                        Refresh();
                    }
                    return
                        _statInitialized == 0 && // avoid throwing if Refresh failed; instead just return false
                        _isDirectory == IsDirectoryAssumesInitialized;
                }
            }

            public DateTimeOffset CreationTime
            {
                // Not supported.
                get { return default(DateTimeOffset); }
                set { } // Not supported. Only some systems support "birthtime" from stat.
            }

            public DateTimeOffset LastAccessTime
            {
                get
                {
                    EnsureStatInitialized();
                    return DateTimeOffset.FromUnixTimeSeconds((long)_stat.st_atime);
                }
                set { SetAccessWriteTimes((IntPtr)value.ToUnixTimeSeconds(), null); }
            }

            public DateTimeOffset LastWriteTime
            {
                get
                {
                    EnsureStatInitialized();
                    return DateTimeOffset.FromUnixTimeSeconds((long)_stat.st_mtime);
                }
                set { SetAccessWriteTimes(null, (IntPtr)value.ToUnixTimeSeconds()); }
            }

            private void SetAccessWriteTimes(IntPtr? accessTime, IntPtr? writeTime)
            {
                _statInitialized = -1; // force a refresh so that we have an up-to-date times for values not being overwritten
                EnsureStatInitialized();
                Interop.utimbuf buf;
                buf.actime = accessTime ?? _stat.st_atime;
                buf.modtime = writeTime ?? _stat.st_mtime;
                while (Interop.CheckIo(Interop.utime(_fullPath, ref buf), _fullPath, _isDirectory)) ;
                _statInitialized = -1;
            }

            public long Length
            {
                get
                {
                    EnsureStatInitialized();
                    return _stat.st_size;
                }
            }

            public void Refresh()
            {
                // This should not throw, instead we store the result so that we can throw it
                // when someone actually accesses a property.
                int result;
                while (true)
                {
                    result = Interop.stat(_fullPath, out _stat);
                    if (result >= 0)
                    {
                        _statInitialized = 0;
                    }
                    else
                    {
                        int errno = Marshal.GetLastWin32Error();
                        if (errno == (int)Interop.Errors.EINTR)
                        {
                            continue;
                        }
                        _statInitialized = errno;
                    }
                    break;
                }
            }

            private void EnsureStatInitialized()
            {
                if (_statInitialized == -1)
                {
                    Refresh();
                }

                if (_statInitialized != 0)
                {
                    int errno = _statInitialized;
                    _statInitialized = -1;
                    throw Interop.GetExceptionForIoErrno(errno, _fullPath, _isDirectory);
                }
            }
        }
    }
}
