// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.Extensions.FileProviders.Internal
{
    /// <summary>
    /// Represents the contents of a physical file directory
    /// </summary>
    public class PhysicalDirectoryContents : IDirectoryContents
    {
        private IEnumerable<IFileInfo>? _entries;
        private readonly string _directory;
        private readonly ExclusionFilters _filters;

        /// <summary>
        /// Initializes an instance of <see cref="PhysicalDirectoryContents"/>
        /// </summary>
        /// <param name="directory">The directory</param>
        public PhysicalDirectoryContents(string directory)
            : this(directory, ExclusionFilters.Sensitive)
        { }

        /// <summary>
        /// Initializes an instance of <see cref="PhysicalDirectoryContents"/>
        /// </summary>
        /// <param name="directory">The directory</param>
        /// <param name="filters">Specifies which files or directories are excluded from enumeration.</param>
        public PhysicalDirectoryContents(string directory, ExclusionFilters filters)
        {
            ThrowHelper.ThrowIfNull(directory);

            _directory = directory;
            _filters = filters;
        }

        /// <inheritdoc />
        public bool Exists => Directory.Exists(_directory);

        /// <inheritdoc />
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
                _entries = new DirectoryInfo(_directory)
                    .EnumerateFileSystemInfos()
                    .Where(info => !FileSystemInfoHelper.IsExcluded(info, _filters))
                    .Select<FileSystemInfo, IFileInfo>(info =>
                    {
                        if (info is FileInfo file)
                        {
                            return new PhysicalFileInfo(file);
                        }
                        else if (info is DirectoryInfo dir)
                        {
                            return new PhysicalDirectoryInfo(dir);
                        }
                        // shouldn't happen unless BCL introduces new implementation of base type
                        throw new InvalidOperationException(SR.UnexpectedFileSystemInfo);
                    });
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is IOException)
            {
                _entries = Enumerable.Empty<IFileInfo>();
            }
        }
    }
}
