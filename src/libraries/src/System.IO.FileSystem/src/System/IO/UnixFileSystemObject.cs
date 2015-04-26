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
            private Interop.libcoreclr.fileinfo _fileinfo;

            /// <summary>
            /// Whether we've successfully cached a stat structure.
            /// -1 if we need to refresh _fileinfo, 0 if we've successfully cached one,
            /// or any other value that serves as an errno error code from the
            /// last time we tried and failed to refresh _fileinfo.
            /// </summary>
            private int _fileinfoInitialized = -1;

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
                    if (IsLink)
                    {
                        attrs |= FileAttributes.ReparsePoint;
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
                        throw new ArgumentException(SR.Arg_InvalidFileAttrs);
                    }

                    // The only thing we can reasonably change is whether the file object is readonly,
                    // just changing its permissions accordingly.
                    EnsureStatInitialized();
                    IsReadOnlyAssumesInitialized = (value & FileAttributes.ReadOnly) != 0;
                    _fileinfoInitialized = -1;
                }
            }

            /// <summary>Gets whether stat reported this system object as a directory.</summary>
            private bool IsDirectoryAssumesInitialized
            {
                get
                {
                    return (_fileinfo.mode & Interop.libcoreclr.FileTypes.S_IFMT) == Interop.libcoreclr.FileTypes.S_IFDIR;
                }
            }

            private bool IsLink
            {
                get
                {
                    return (_fileinfo.mode & Interop.libcoreclr.FileTypes.S_IFMT) == Interop.libcoreclr.FileTypes.S_IFLNK;
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
                    Interop.libc.Permissions readBit, writeBit;
                    if (_fileinfo.uid == Interop.libc.geteuid())      // does the user effectively own the file?
                    {
                        readBit  = Interop.libc.Permissions.S_IRUSR;
                        writeBit = Interop.libc.Permissions.S_IWUSR;
                    }
                    else if (_fileinfo.gid == Interop.libc.getegid()) // does the user belong to a group that effectively owns the file?
                    {
                        readBit  = Interop.libc.Permissions.S_IRGRP;
                        writeBit = Interop.libc.Permissions.S_IWGRP;
                    }
                    else                                              // everyone else
                    {
                        readBit  = Interop.libc.Permissions.S_IROTH;
                        writeBit = Interop.libc.Permissions.S_IWOTH;
                    }

                    return
                        (_fileinfo.mode & (int)readBit) != 0 && // has read permission
                        (_fileinfo.mode & (int)writeBit) == 0;  // but not write permission
                }
                set
                {
                    int newMode = _fileinfo.mode;
                    if (value) // true if going from writable to readable, false if going from readable to writable
                    {
                        // Take away all write permissions from user/group/everyone
                        newMode &= ~(int)(Interop.libc.Permissions.S_IWUSR | Interop.libc.Permissions.S_IWGRP | Interop.libc.Permissions.S_IWOTH);
                    }
                    else if ((newMode & (int)Interop.libc.Permissions.S_IRUSR) != 0)
                    {
                        // Give write permission to the owner if the owner has read permission
                        newMode |= (int)Interop.libc.Permissions.S_IWUSR;
                    }

                    // Change the permissions on the file
                    if (newMode != _fileinfo.mode)
                    {
                        while (Interop.CheckIo(Interop.libc.chmod(_fullPath, newMode), _fullPath, _isDirectory)) ;
                    }
                }
            }

            public bool Exists
            {
                get
                {
                    if (_fileinfoInitialized == -1)
                    {
                        Refresh();
                    }
                    return
                        _fileinfoInitialized == 0 && // avoid throwing if Refresh failed; instead just return false
                        _isDirectory == IsDirectoryAssumesInitialized;
                }
            }

            public DateTimeOffset CreationTime
            {
                get
                {
                    EnsureStatInitialized();
                    return (_fileinfo.flags & (uint)Interop.libcoreclr.FileInformationFlags.HasBTime) != 0 ?
                        DateTimeOffset.FromUnixTimeSeconds(_fileinfo.btime) :
                        default(DateTimeOffset);
                }
                set { } // Not supported
            }

            public DateTimeOffset LastAccessTime
            {
                get
                {
                    EnsureStatInitialized();
                    return DateTimeOffset.FromUnixTimeSeconds(_fileinfo.atime);
                }
                set { SetAccessWriteTimes((IntPtr)value.ToUnixTimeSeconds(), null); }
            }

            public DateTimeOffset LastWriteTime
            {
                get
                {
                    EnsureStatInitialized();
                    return DateTimeOffset.FromUnixTimeSeconds(_fileinfo.mtime);
                }
                set { SetAccessWriteTimes(null, (IntPtr)value.ToUnixTimeSeconds()); }
            }

            private void SetAccessWriteTimes(IntPtr? accessTime, IntPtr? writeTime)
            {
                _fileinfoInitialized = -1; // force a refresh so that we have an up-to-date times for values not being overwritten
                EnsureStatInitialized();
                Interop.libc.utimbuf buf;
                buf.actime = accessTime ?? new IntPtr(_fileinfo.atime);
                buf.modtime = writeTime ?? new IntPtr(_fileinfo.mtime);
                while (Interop.CheckIo(Interop.libc.utime(_fullPath, ref buf), _fullPath, _isDirectory)) ;
                _fileinfoInitialized = -1;
            }

            public long Length
            {
                get
                {
                    EnsureStatInitialized();
                    return _fileinfo.size;
                }
            }

            public void Refresh()
            {
                // This should not throw, instead we store the result so that we can throw it
                // when someone actually accesses a property.
                int result;
                while (true)
                {
                    result = Interop.libcoreclr.GetFileInformationFromPath(_fullPath, out _fileinfo);
                    if (result >= 0)
                    {
                        _fileinfoInitialized = 0;
                    }
                    else
                    {
                        int errno = Marshal.GetLastWin32Error();
                        if (errno == Interop.Errors.EINTR)
                        {
                            continue;
                        }
                        _fileinfoInitialized = errno;
                    }
                    break;
                }
            }

            private void EnsureStatInitialized()
            {
                if (_fileinfoInitialized == -1)
                {
                    Refresh();
                }

                if (_fileinfoInitialized != 0)
                {
                    int errno = _fileinfoInitialized;
                    _fileinfoInitialized = -1;

                    bool failedBecauseOfDirectory = _isDirectory;
                    if (!failedBecauseOfDirectory && errno == Interop.Errors.ENOENT)
                    {
                        // Windows distinguishes between whether the directory or the file isn't found,
                        // and throws a different exception in these cases.  We attempt to approximate that
                        // here; there is a race condition here, where something could change between
                        // when the error occurs and our checks, but it's the best we can do, and the
                        // worst case in such a race condition (which could occur if the file system is
                        // being manipulated concurrently with these checks) is that we throw a
                        // FileNotFoundException instead of DirectoryNotFoundexception.
                        failedBecauseOfDirectory = !Directory.Exists(Path.GetDirectoryName(_fullPath));
                    }

                    throw Interop.GetExceptionForIoErrno(errno, _fullPath, failedBecauseOfDirectory);
                }
            }
        }
    }
}
