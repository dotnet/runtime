// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal static partial class ZipArchiveEntryConstants
    {
        /// <summary>
        /// The default external file attributes are used to support zip archives on multiple platforms.
        /// </summary>
        internal const uint DefaultFileExternalAttributes =
            (Interop.Sys.FileTypes.S_IFREG |
                (uint)(UnixFileMode.UserRead | UnixFileMode.UserWrite
                    | UnixFileMode.GroupRead
                    | UnixFileMode.OtherRead)
            ) << 16;

        /// <summary>
        /// The default external directory attributes are used to support zip archives on multiple platforms.
        /// Directories on Unix require the execute permissions to get into them.
        /// </summary>
        internal const uint DefaultDirectoryExternalAttributes =
            (Interop.Sys.FileTypes.S_IFDIR |
                (uint)(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                    | UnixFileMode.OtherRead | UnixFileMode.OtherExecute)
            ) << 16;
    }
}
