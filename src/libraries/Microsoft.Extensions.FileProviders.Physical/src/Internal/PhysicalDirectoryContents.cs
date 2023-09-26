// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileProviders.Physical;

namespace Microsoft.Extensions.FileProviders.Internal
{
    /// <summary>
    /// Represents the contents of a physical file directory
    /// </summary>
    public class PhysicalDirectoryContents : IDirectoryContents
    {
        private readonly PhysicalDirectoryInfo _info;

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

            _info = new PhysicalDirectoryInfo(new DirectoryInfo(directory), filters);
        }

        /// <inheritdoc/>
        public bool Exists => _info.Exists;

        /// <inheritdoc/>
        public IEnumerator<IFileInfo> GetEnumerator() => _info.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _info.GetEnumerator();
    }
}
