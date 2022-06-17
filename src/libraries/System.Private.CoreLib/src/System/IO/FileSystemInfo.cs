// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Versioning;

namespace System.IO
{
    public abstract partial class FileSystemInfo : MarshalByRefObject, ISerializable
    {
        // FullPath and OriginalPath are documented fields
        protected string FullPath = null!;          // fully qualified path of the file or directory
        protected string OriginalPath = null!;      // path passed in by the user

        internal string _name = null!; // Fields initiated in derived classes

        private string? _linkTarget;
        private bool _linkTargetIsValid;

        protected FileSystemInfo(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        internal void Invalidate()
        {
            _linkTargetIsValid = false;
            InvalidateCore();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // Full path of the directory/file
        public virtual string FullName => FullPath;

        public string Extension
        {
            get
            {
                int length = FullPath.Length;
                for (int i = length; --i >= 0;)
                {
                    char ch = FullPath[i];
                    if (ch == '.')
                        return FullPath.Substring(i, length - i);
                    if (PathInternal.IsDirectorySeparator(ch) || ch == Path.VolumeSeparatorChar)
                        break;
                }
                return string.Empty;
            }
        }

        public virtual string Name => _name;

        // Whether a file/directory exists
        public virtual bool Exists
        {
            get
            {
                try
                {
                    return ExistsCore;
                }
                catch
                {
                    return false;
                }
            }
        }

        // Delete a file/directory
        public abstract void Delete();

        public DateTime CreationTime
        {
            get => CreationTimeUtc.ToLocalTime();
            set => CreationTimeUtc = value.ToUniversalTime();
        }

        public DateTime CreationTimeUtc
        {
            get => CreationTimeCore.UtcDateTime;
            set => CreationTimeCore = File.GetUtcDateTimeOffset(value);
        }

        public DateTime LastAccessTime
        {
            get => LastAccessTimeUtc.ToLocalTime();
            set => LastAccessTimeUtc = value.ToUniversalTime();
        }

        public DateTime LastAccessTimeUtc
        {
            get => LastAccessTimeCore.UtcDateTime;
            set => LastAccessTimeCore = File.GetUtcDateTimeOffset(value);
        }

        public DateTime LastWriteTime
        {
            get => LastWriteTimeUtc.ToLocalTime();
            set => LastWriteTimeUtc = value.ToUniversalTime();
        }

        public DateTime LastWriteTimeUtc
        {
            get => LastWriteTimeCore.UtcDateTime;
            set => LastWriteTimeCore = File.GetUtcDateTimeOffset(value);
        }

        /// <summary>
        /// If this <see cref="FileSystemInfo"/> instance represents a link, returns the link target's path.
        /// If a link does not exist in <see cref="FullName"/>, or this instance does not represent a link, returns <see langword="null"/>.
        /// </summary>
        public string? LinkTarget
        {
            get
            {
                if (_linkTargetIsValid)
                {
                    return _linkTarget;
                }

                _linkTarget = FileSystem.GetLinkTarget(FullPath, this is DirectoryInfo);
                _linkTargetIsValid = true;
                return _linkTarget;
            }
        }

        /// <summary>Gets or sets the Unix file mode for the current file or directory.</summary>
        /// <value><see cref="T:System.IO.UnixFileMode" /> of the current <see cref="T:System.IO.FileSystemInfo" />.</value>
        /// <exception cref="T:System.ArgumentException">The caller attempts to set an invalid file mode.</exception>
        /// <exception cref="T:System.UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="T:System.IO.PathTooLongException">The specified path exceeds the system-defined maximum length.</exception>
        /// <exception cref="T:System.IO.DirectoryNotFoundException">The specified path is invalid. Only thrown when setting the property value.</exception>
        /// <exception cref="T:System.IO.FileNotFoundException">The specified file doesn't exist. Only thrown when setting the property value.</exception>
        /// <exception cref="T:System.IO.IOException"><see cref="M:System.IO.FileSystemInfo.Refresh" /> cannot initialize the data.</exception>
        ///
        /// <remarks>
        /// The value may be cached when either the value itself or other <see cref="T:System.IO.FileSystemInfo" /> properties are accessed. To get the latest value, call the <see cref="M:System.IO.FileSystemInfo.Refresh" /> method.
        ///
        /// If the path doesn't exist as of the last cached state, the return value is `(UnixFileMode)(-1)`. <see cref="System.IO.FileNotFoundException"/> or <see cref="System.IO.DirectoryNotFoundException"/> can only be thrown when setting the value.
        /// </remarks>
        public UnixFileMode UnixFileMode
        {
            get => UnixFileModeCore;
            [UnsupportedOSPlatform("windows")]
            set => UnixFileModeCore = value;
        }

        /// <summary>
        /// Creates a symbolic link located in <see cref="FullName"/> that points to the specified <paramref name="pathToTarget"/>.
        /// </summary>
        /// <param name="pathToTarget">The path of the symbolic link target.</param>
        /// <exception cref="ArgumentNullException"><paramref name="pathToTarget"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="pathToTarget"/> is empty.
        /// -or-
        /// This instance was not created passing an absolute path.
        /// -or-
        /// <paramref name="pathToTarget"/> contains invalid path characters.</exception>
        /// <exception cref="IOException">A file or directory already exists in the location of <see cref="FullName"/>.
        /// -or-
        /// An I/O error occurred.</exception>
        public void CreateAsSymbolicLink(string pathToTarget)
        {
            FileSystem.VerifyValidPath(pathToTarget, nameof(pathToTarget));
            FileSystem.CreateSymbolicLink(OriginalPath, pathToTarget, this is DirectoryInfo);
            Invalidate();
        }

        /// <summary>
        /// Gets the target of the specified link.
        /// </summary>
        /// <param name="returnFinalTarget"><see langword="true"/> to follow links to the final target; <see langword="false"/> to return the immediate next link.</param>
        /// <returns>A <see cref="FileSystemInfo"/> instance if the link exists, independently if the target exists or not; <see langword="null"/> if this file or directory is not a link.</returns>
        /// <exception cref="IOException">The file or directory does not exist.
        /// -or-
        /// The link's file system entry type is inconsistent with that of its target.
        /// -or-
        /// Too many levels of symbolic links.</exception>
        /// <remarks>When <paramref name="returnFinalTarget"/> is <see langword="true"/>, the maximum number of symbolic links that are followed are 40 on Unix and 63 on Windows.</remarks>
        public FileSystemInfo? ResolveLinkTarget(bool returnFinalTarget) =>
            FileSystem.ResolveLinkTarget(FullPath, returnFinalTarget, this is DirectoryInfo);

        /// <summary>
        /// Returns the original path. Use FullName or Name properties for the full path or file/directory name.
        /// </summary>
        public override string ToString() => OriginalPath ?? string.Empty;
    }
}
