// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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