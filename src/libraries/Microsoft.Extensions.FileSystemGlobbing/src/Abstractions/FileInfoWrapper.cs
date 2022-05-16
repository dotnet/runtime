// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    /// <summary>
    /// Wraps an instance of <see cref="System.IO.FileInfo" /> to provide implementation of <see cref="FileInfoBase" />.
    /// </summary>
    public class FileInfoWrapper : FileInfoBase
    {
        private readonly FileInfo _fileInfo;

        /// <summary>
        /// Initializes instance of <see cref="FileInfoWrapper" /> to wrap the specified object <see cref="System.IO.FileInfo" />.
        /// </summary>
        /// <param name="fileInfo">The <see cref="System.IO.FileInfo" /></param>
        public FileInfoWrapper(FileInfo fileInfo)
        {
            ThrowHelper.ThrowIfNull(fileInfo);

            _fileInfo = fileInfo;
        }

        /// <summary>
        /// The file name. (Overrides <see cref="FileSystemInfoBase.Name" />).
        /// </summary>
        /// <remarks>
        /// Equals the value of <see cref="System.IO.FileInfo.Name" />.
        /// </remarks>
        public override string Name => _fileInfo.Name;

        /// <summary>
        /// The full path of the file. (Overrides <see cref="FileSystemInfoBase.FullName" />).
        /// </summary>
        /// <remarks>
        /// Equals the value of <see cref="System.IO.FileSystemInfo.Name" />.
        /// </remarks>
        public override string FullName => _fileInfo.FullName;

        /// <summary>
        /// The directory containing the file. (Overrides <see cref="FileSystemInfoBase.ParentDirectory" />).
        /// </summary>
        /// <remarks>
        /// Equals the value of <see cref="System.IO.FileInfo.Directory" />.
        /// </remarks>
        public override DirectoryInfoBase? ParentDirectory
            => new DirectoryInfoWrapper(_fileInfo.Directory!);
    }
}
