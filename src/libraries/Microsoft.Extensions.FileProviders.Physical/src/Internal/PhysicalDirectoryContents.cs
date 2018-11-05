﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
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
        private IEnumerable<IFileInfo> _entries;
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
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
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
                        throw new InvalidOperationException("Unexpected type of FileSystemInfo");
                    });
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException || ex is IOException)
            {
                _entries = Enumerable.Empty<IFileInfo>();
            }
        }
    }
}
