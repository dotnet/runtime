// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Represents a non-existing file.
    /// </summary>
    public class NotFoundFileInfo : IFileInfo
    {
        /// <summary>
        /// Initializes an instance of <see cref="NotFoundFileInfo"/>.
        /// </summary>
        /// <param name="name">The name of the file that could not be found</param>
        public NotFoundFileInfo(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Always false.
        /// </summary>
        public bool Exists => false;

        /// <summary>
        /// Always false.
        /// </summary>
        public bool IsDirectory => false;

        /// <summary>
        /// Returns <see cref="DateTimeOffset.MinValue"/>.
        /// </summary>
        public DateTimeOffset LastModified => DateTimeOffset.MinValue;

        /// <summary>
        /// Always equals -1.
        /// </summary>
        public long Length => -1;

        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Always null.
        /// </summary>
        public string? PhysicalPath => null;

        /// <summary>
        /// Always throws. A stream cannot be created for non-existing file.
        /// </summary>
        /// <exception cref="FileNotFoundException">Always thrown.</exception>
        /// <returns>Does not return</returns>
        [DoesNotReturn]
        public Stream CreateReadStream()
        {
            throw new FileNotFoundException(SR.Format(SR.FileNotExists, Name));
        }
    }
}
