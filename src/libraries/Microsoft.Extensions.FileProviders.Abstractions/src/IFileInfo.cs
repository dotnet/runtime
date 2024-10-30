// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Represents a file in the given file provider.
    /// </summary>
    public interface IFileInfo
    {
        /// <summary>
        /// Gets a value that indicates if the resource exists in the underlying storage system.
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Gets the length of the file in bytes, or -1 for a directory or nonexistent file.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets the path to the file, including the file name. Returns <see langword="null"/> if the file is not directly accessible.
        /// </summary>
        string? PhysicalPath { get; }

        /// <summary>
        /// Gets the name of the file or directory, not including any path.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the time when the file was last modified.
        /// </summary>
        DateTimeOffset LastModified { get; }

        /// <summary>
        /// Gets a value that indicates whether <c>TryGetDirectoryContents</c> has enumerated a subdirectory.
        /// </summary>
        bool IsDirectory { get; }

        /// <summary>
        /// Returns file contents as a read-only stream.
        /// </summary>
        /// <returns>The file stream.</returns>
        /// <remarks>The caller should dispose the stream when complete.</remarks>
        Stream CreateReadStream();
    }
}
