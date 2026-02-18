// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO
{
    /// <summary>
    /// Specifies the type of a file.
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// The file type is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// The file is a regular file.
        /// </summary>
        RegularFile,

        /// <summary>
        /// The file is a pipe (FIFO).
        /// </summary>
        Pipe,

        /// <summary>
        /// The file is a socket.
        /// </summary>
        Socket,

        /// <summary>
        /// The file is a character device.
        /// </summary>
        CharacterDevice,

        /// <summary>
        /// The file is a directory.
        /// </summary>
        Directory,

        /// <summary>
        /// The file is a symbolic link.
        /// </summary>
        SymbolicLink,

        /// <summary>
        /// The file is a block device.
        /// </summary>
        [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
        BlockDevice
    }
}
