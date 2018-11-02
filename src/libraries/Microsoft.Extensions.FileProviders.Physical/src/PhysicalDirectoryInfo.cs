// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// Represents a directory on a physical filesystem
    /// </summary>
    public class PhysicalDirectoryInfo : IFileInfo
    {
        private readonly DirectoryInfo _info;

        /// <summary>
        /// Initializes an instance of <see cref="PhysicalDirectoryInfo"/> that wraps an instance of <see cref="System.IO.DirectoryInfo"/>
        /// </summary>
        /// <param name="info">The directory</param>
        public PhysicalDirectoryInfo(DirectoryInfo info)
        {
            _info = info;
        }

        /// <inheritdoc />
        public bool Exists => _info.Exists;

        /// <summary>
        /// Always equals -1.
        /// </summary>
        public long Length => -1;

        /// <inheritdoc />
        public string PhysicalPath => _info.FullName;

        /// <inheritdoc />
        public string Name => _info.Name;

        /// <summary>
        /// The time when the directory was last written to.
        /// </summary>
        public DateTimeOffset LastModified => _info.LastWriteTimeUtc;

        /// <summary>
        /// Always true.
        /// </summary>
        public bool IsDirectory => true;

        /// <summary>
        /// Always throws an exception because read streams are not support on directories.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always thrown</exception>
        /// <returns>Never returns</returns>
        public Stream CreateReadStream()
        {
            throw new InvalidOperationException("Cannot create a stream for a directory.");
        }
    }
}
