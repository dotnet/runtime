// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            throw new InvalidOperationException(SR.CannotCreateStream);
        }
    }
}
