// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    /// <summary>
    /// Represents a directory
    /// </summary>
    public abstract class DirectoryInfoBase : FileSystemInfoBase
    {
        /// <summary>
        /// Enumerates all files and directories in the directory.
        /// </summary>
        /// <returns>Collection of files and directories</returns>
        public abstract IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos();

        /// <summary>
        /// Returns an instance of <see cref="DirectoryInfoBase" /> that represents a subdirectory
        /// </summary>
        /// <param name="path">The directory name</param>
        /// <returns>Instance of <see cref="DirectoryInfoBase" /> even if directory does not exist</returns>
        public abstract DirectoryInfoBase GetDirectory(string path);

        /// <summary>
        /// Returns an instance of <see cref="FileInfoBase" /> that represents a file in the directory
        /// </summary>
        /// <param name="path">The file name</param>
        /// <returns>Instance of <see cref="FileInfoBase" /> even if file does not exist</returns>
        public abstract FileInfoBase GetFile(string path);
    }
}
