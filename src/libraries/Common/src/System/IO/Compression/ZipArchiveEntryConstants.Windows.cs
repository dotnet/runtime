// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static partial class ZipArchiveEntryConstants
    {
        /// <summary>
        /// The default external file attributes are used to support zip archives on multiple platforms.
        /// Since Windows doesn't use file permissions, there's no default value needed.
        /// </summary>
        internal const UnixFileMode DefaultFileEntryPermissions = UnixFileMode.None;

        /// <summary>
        /// The default external directory attributes are used to support zip archives on multiple platforms.
        /// Since Windows doesn't use file permissions, there's no default value needed.
        /// </summary>
        internal const UnixFileMode DefaultDirectoryEntryPermissions = UnixFileMode.None;
    }
}
