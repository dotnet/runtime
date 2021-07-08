// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Serialization;

#if MS_IO_REDIST
namespace Microsoft.IO
#else
namespace System.IO
#endif
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
