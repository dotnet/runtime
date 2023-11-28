// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.FileProviders.Physical
{
    /// <summary>
    /// Represents a directory on a physical filesystem
    /// </summary>
    public class PhysicalDirectoryInfo : IFileInfo, IDirectoryContents
    {
        private readonly DirectoryInfo _info;
        private IEnumerable<IFileInfo>? _entries;
        private readonly ExclusionFilters _filters;

        /// <summary>
        /// Initializes an instance of <see cref="PhysicalDirectoryInfo"/> that wraps an instance of <see cref="System.IO.DirectoryInfo"/>
        /// </summary>
        /// <param name="info">The directory</param>
        public PhysicalDirectoryInfo(DirectoryInfo info)
        {
            _info = info;
        }

        internal PhysicalDirectoryInfo(DirectoryInfo info, ExclusionFilters filters)
        {
            _info = info;
            _filters = filters;
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

        /// <inheritdoc/>
        public IEnumerator<IFileInfo> GetEnumerator()
        {
            EnsureInitialized();
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            EnsureInitialized();
            return _entries.GetEnumerator();
        }

        [MemberNotNull(nameof(_entries))]
        private void EnsureInitialized()
        {
            try
            {
                _entries = _info
                    .EnumerateFileSystemInfos()
                    .Where(info => !FileSystemInfoHelper.IsExcluded(info, _filters))
                    .Select<FileSystemInfo, IFileInfo>(info => info switch
                    {
                        FileInfo file => new PhysicalFileInfo(file),
                        DirectoryInfo dir => new PhysicalDirectoryInfo(dir),
                        // shouldn't happen unless BCL introduces new implementation of base type
                        _ => throw new InvalidOperationException(SR.UnexpectedFileSystemInfo)
                    });
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException or IOException)
            {
                _entries = Enumerable.Empty<IFileInfo>();
            }
        }
    }
}
