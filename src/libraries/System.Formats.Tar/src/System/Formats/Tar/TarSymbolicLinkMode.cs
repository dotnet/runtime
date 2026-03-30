// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Specifies how symbolic links are handled when reading or writing tar entries.
    /// </summary>
    public enum TarSymbolicLinkMode
    {
        /// <summary>
        /// Symbolic links are preserved as <see cref="TarEntryType.SymbolicLink"/> entries.
        /// </summary>
        PreserveLink,

        /// <summary>
        /// Symbolic links are replaced by the content of their target.
        /// When writing, the target file's content is written as a regular file entry.
        /// When extracting, the target file's content is copied to the destination instead of creating a symbolic link.
        /// </summary>
        CopyContents,

        /// <summary>
        /// Symbolic link entries are skipped.
        /// </summary>
        Skip,
    }
}
