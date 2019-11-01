// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    public enum ZipArchiveMode
    {
        /// <summary>
        /// Only reading entries from the archive is permitted.
        /// If the underlying file or stream is seekable, then files will be read from the archive on-demand as they are requested.
        /// If the underlying file or stream is not seekable, the entire archive will be held in memory.
        /// Requires that the underlying file or stream is readable.
        /// </summary>
        Read,
        /// <summary>
        /// Only supports the creation of new archives.
        /// Only writing to newly created entries in the archive is permitted.
        /// Each entry in the archive can only be opened for writing once.
        /// If only one entry is written to at a time, data will be written to the underlying stream or file as soon as it is available.
        /// The underlying stream must be writable, but need not be seekable.
        /// </summary>
        Create,
        /// <summary>
        /// Reading and writing from entries in the archive is permitted.
        /// Requires that the contents of the entire archive be held in memory.
        /// The underlying file or stream must be readable, writable and seekable.
        /// No data will be written to the underlying file or stream until the archive is disposed.
        /// </summary>
        Update
    }
}
