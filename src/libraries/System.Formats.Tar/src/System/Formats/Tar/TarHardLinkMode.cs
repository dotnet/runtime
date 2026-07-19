// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Tar
{
    /// <summary>
    /// Specifies how hard links are handled when writing tar entries from disk.
    /// </summary>
    public enum TarHardLinkMode
    {
        /// <summary>
        /// When multiple file paths refer to the same underlying file (hard links),
        /// the first occurrence is written as a regular file entry and subsequent
        /// occurrences are written as <see cref="TarEntryType.HardLink"/> entries.
        /// </summary>
        PreserveLink,

        /// <summary>
        /// Hard-linked files are each written as separate regular file entries with
        /// their full content copied independently.
        /// </summary>
        CopyContents
    }
}
