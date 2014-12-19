// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO
{
    /// <devdoc>
    ///   <para>Contains constants for controlling file sharing options while
    ///   opening files.  You can specify what access othter processes trying
    ///   to open the same file concurrently can have.
    ///   
    /// Note these values currently match the values for FILE_SHARE_READ,
    /// FILE_SHARE_WRITE, and FILE_SHARE_DELETE in winnt.h</para>
    /// <devdoc>
    [Flags]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum FileShare
    {
        /// <devdoc>
        ///   <para>No sharing.  Any request to open the file (by this process or another
        ///   process will fail until the file is closed.<para>
        /// </devdoc>
        None = 0x00,

        /// <devdoc>
        ///   <para>Allows subsequent opening of the file for reading.  If this flag is not
        ///   specified, any request to open the file for reading (by this process or
        ///   another process) will fail until the file is closed.<para>
        /// </devdoc>
        Read = 0x01,

        /// <devdoc>
        ///   <para>Allows subsequent opening of the file for writing.  If this flag is not
        ///   specified, any request to open the file for writing (by this process or
        ///   another process) will fail until the file is closed.<para>
        /// </devdoc>
        Write = 0x02,

        /// <devdoc>
        ///   <para>Allows subsequent opening of the file for writing or reading.  If this flag
        ///   is not specified, any request to open the file for writing or reading (by
        ///   this process or another process) will fail until the file is closed.<para>
        /// </devdoc>
        ReadWrite = 0x03,

        /// <devdoc>
        ///   <para>Open the file, but allow someone else to delete the file.<para>
        /// </devdoc>
        Delete = 0x04,

        /// <devdoc>
        ///   <para>Whether the file handle should be inheritable by child processes.
        ///   Note this is not directly supported like this by Win32.<para>
        /// </devdoc>
        Inheritable = 0x10,
    }
}