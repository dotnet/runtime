// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// Represents a file on a physical filesystem
    /// </summary>
    public class PhysicalFileInfo : IFileInfo
    {
        private readonly FileInfo _info;

        /// <summary>
        /// Initializes an instance of <see cref="PhysicalFileInfo"/> that wraps an instance of <see cref="System.IO.FileInfo"/>
        /// </summary>
        /// <param name="info">The <see cref="System.IO.FileInfo"/></param>
        public PhysicalFileInfo(FileInfo info)
        {
            _info = info;
        }

        /// <inheritdoc />
        public bool Exists => _info.Exists;

        /// <inheritdoc />
        public long Length => _info.Length;

        /// <inheritdoc />
        public string PhysicalPath => _info.FullName;

        /// <inheritdoc />
        public string Name => _info.Name;

        /// <inheritdoc />
        public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsDirectory => false;

        /// <inheritdoc />
        public Stream CreateReadStream()
        {
            // We are setting buffer size to 1 to prevent FileStream from allocating it's internal buffer
            // 0 causes constructor to throw
            int bufferSize = 1;
            return new FileStream(
                PhysicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}
