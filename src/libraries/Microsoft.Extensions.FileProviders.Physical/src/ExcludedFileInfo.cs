// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders.Physical
{
    public class ExcludedFileInfo : IFileInfo
    {
        private readonly FileInfo _info;

        /// <summary>
        /// Initializes an instance of <see cref="ExcludedFileInfo"/>.
        /// </summary>
        /// <param name="info">The <see cref="System.IO.FileInfo"/> of the file that could was excluded.</param>
        /// <param name="matchingFilter">The <see cref="ExclusionFilters"/> the file matched with.</param>
        public ExcludedFileInfo(FileInfo info, ExclusionFilters matchingFilter)
        {
            _info = info;
            MatchingFilter = matchingFilter;
        }

        /// <summary>
        /// The <see cref="ExclusionFilters"/> the file matched with.
        /// </summary>
        public ExclusionFilters MatchingFilter { get; }

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

        /// <summary>
        /// Always throws. A stream cannot be created for non-existing file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Always thrown.</exception>
        /// <returns>Does not return</returns>
        public Stream CreateReadStream()
        {
            throw new FileExcludedException(SR.Format(SR.FileExcluded, Name), MatchingFilter);
        }
    }
}
