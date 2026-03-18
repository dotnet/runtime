// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.FileSystemGlobbing.Abstractions
{
    /// <summary>
    /// Shared abstraction for files and directories
    /// </summary>
    public abstract class FileSystemInfoBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemInfoBase" /> class.
        /// </summary>
        protected FileSystemInfoBase() { }

        /// <summary>
        /// Gets the name of the file or directory.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the full path of the file or directory.
        /// </summary>
        public abstract string FullName { get; }

        /// <summary>
        /// Gets the parent directory for the current file or directory.
        /// </summary>
        public abstract DirectoryInfoBase? ParentDirectory { get; }
    }
}
