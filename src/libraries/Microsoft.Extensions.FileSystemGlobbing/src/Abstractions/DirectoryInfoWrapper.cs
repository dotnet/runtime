// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// Initializes a new instance of the <see cref="DirectoryInfoWrapper" /> class.
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
                IEnumerable<FileSystemInfo> fileSystemInfos;
                try
                {
                    fileSystemInfos = _directoryInfo.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
                }
                catch (DirectoryNotFoundException)
                {
                    yield break;
                }

                foreach (FileSystemInfo fileSystemInfo in fileSystemInfos)
                {
                    if (fileSystemInfo is DirectoryInfo directoryInfo)
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
        /// <returns>Instance of <see cref="DirectoryInfoBase" /> even if the directory does not exist</returns>
        public override DirectoryInfoBase GetDirectory(string name)
        {
            bool isParentPath = string.Equals(name, "..", StringComparison.Ordinal);

            return new DirectoryInfoWrapper(
                new DirectoryInfo(Path.Combine(_directoryInfo.FullName, name)),
                isParentPath);
        }

        /// <summary>
        /// Returns an instance of <see cref="FileInfoBase" /> that represents a file in the directory
        /// </summary>
        /// <param name="name">The file name</param>
        /// <returns>Instance of <see cref="FileInfoBase" /> even if the file does not exist</returns>
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
        public override DirectoryInfoBase? ParentDirectory
            => new DirectoryInfoWrapper(_directoryInfo.Parent!);
    }
}
