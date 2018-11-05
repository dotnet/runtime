// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    /// <summary>
    /// Wraps an instance of <see cref="System.IO.DirectoryInfo" /> and provides implementation of
    /// <see cref="DirectoryInfoBase" />.
    /// </summary>
    public class DirectoryInfoWrapper : DirectoryInfoBase
    {
        private readonly DirectoryInfo _directoryInfo;
        private readonly bool _isParentPath;

        /// <summary>
        /// Initializes an instance of <see cref="DirectoryInfoWrapper" />.
        /// </summary>
        /// <param name="directoryInfo">The <see cref="DirectoryInfo" />.</param>
        public DirectoryInfoWrapper(DirectoryInfo directoryInfo)
            : this(directoryInfo, isParentPath: false)
        { }

        private DirectoryInfoWrapper(DirectoryInfo directoryInfo, bool isParentPath)
        {
            _directoryInfo = directoryInfo;
            _isParentPath = isParentPath;
        }

        /// <inheritdoc />
        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            if (_directoryInfo.Exists)
            {
                foreach (var fileSystemInfo in _directoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly))
                {
                    var directoryInfo = fileSystemInfo as DirectoryInfo;
                    if (directoryInfo != null)
                    {
                        yield return new DirectoryInfoWrapper(directoryInfo);
                    }
                    else
                    {
                        yield return new FileInfoWrapper((FileInfo)fileSystemInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Returns an instance of <see cref="DirectoryInfoBase" /> that represents a subdirectory.
        /// </summary>
        /// <remarks>
        /// If <paramref name="name" /> equals '..', this returns the parent directory.
        /// </remarks>
        /// <param name="name">The directory name</param>
        /// <returns>The directory</returns>
        public override DirectoryInfoBase GetDirectory(string name)
        {
            var isParentPath = string.Equals(name, "..", StringComparison.Ordinal);

            if (isParentPath)
            {
                return new DirectoryInfoWrapper(
                    new DirectoryInfo(Path.Combine(_directoryInfo.FullName, name)),
                    isParentPath);
            }
            else
            {
                var dirs = _directoryInfo.GetDirectories(name);

                if (dirs.Length == 1)
                {
                    return new DirectoryInfoWrapper(dirs[0], isParentPath);
                }
                else if (dirs.Length == 0)
                {
                    return null;
                }
                else
                {
                    // This shouldn't happen. The parameter name isn't supposed to contain wild card.
                    throw new InvalidOperationException(
                        string.Format("More than one sub directories are found under {0} with name {1}.",
                            _directoryInfo.FullName, name));
                }
            }
        }

        /// <inheritdoc />
        public override FileInfoBase GetFile(string name)
            => new FileInfoWrapper(new FileInfo(Path.Combine(_directoryInfo.FullName, name)));

        /// <inheritdoc />
        public override string Name => _isParentPath ? ".." : _directoryInfo.Name;

        /// <summary>
        /// Returns the full path to the directory.
        /// </summary>
        /// <remarks>
        /// Equals the value of <seealso cref="System.IO.FileSystemInfo.FullName" />.
        /// </remarks>
        public override string FullName => _directoryInfo.FullName;

        /// <summary>
        /// Returns the parent directory.
        /// </summary>
        /// <remarks>
        /// Equals the value of <seealso cref="System.IO.DirectoryInfo.Parent" />.
        /// </remarks>
        public override DirectoryInfoBase ParentDirectory
            => new DirectoryInfoWrapper(_directoryInfo.Parent);
    }
}
