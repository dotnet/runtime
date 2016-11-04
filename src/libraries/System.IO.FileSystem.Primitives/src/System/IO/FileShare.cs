// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   Contains constants for controlling file sharing options while
    ///   opening files.  You can specify what access other processes trying
    ///   to open the same file concurrently can have.
    ///   
    ///   Note these values currently match the values for FILE_SHARE_READ,
    ///   FILE_SHARE_WRITE, and FILE_SHARE_DELETE in winnt.h
    /// </devdoc>
    [Serializable]
    [Flags]
    public enum FileShare
    {
        /// <devdoc>
        ///   No sharing.  Any request to open the file (by this process or another
        ///   process will fail until the file is closed.
        /// </devdoc>
        None = 0x00,

        /// <devdoc>
        ///   Allows subsequent opening of the file for reading.  If this flag is not
        ///   specified, any request to open the file for reading (by this process or
        ///   another process) will fail until the file is closed.
        /// </devdoc>
        Read = 0x01,

        /// <devdoc>
        ///   Allows subsequent opening of the file for writing.  If this flag is not
        ///   specified, any request to open the file for writing (by this process or
        ///   another process) will fail until the file is closed.
        /// </devdoc>
        Write = 0x02,

        /// <devdoc>
        ///   Allows subsequent opening of the file for writing or reading.  If this flag
        ///   is not specified, any request to open the file for writing or reading (by
        ///   this process or another process) will fail until the file is closed.
        /// </devdoc>
        ReadWrite = 0x03,

        /// <devdoc>
        ///   Open the file, but allow someone else to delete the file.
        /// </devdoc>
        Delete = 0x04,

        /// <devdoc>
        ///   Whether the file handle should be inheritable by child processes.
        ///   Note this is not directly supported like this by Win32.
        /// </devdoc>
        Inheritable = 0x10,
    }
}
