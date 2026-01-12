// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    /// <summary>
    /// Specifies the compression format for <see cref="ZlibEncoder"/> and <see cref="ZlibDecoder"/>.
    /// </summary>
    public enum ZlibCompressionFormat
    {
        /// <summary>
        /// Raw deflate format without any header or trailer.
        /// </summary>
        /// <remarks>
        /// This format produces the smallest output but provides no error checking.
        /// It is compatible with <see cref="DeflateStream"/>.
        /// </remarks>
        Deflate = 0,

        /// <summary>
        /// ZLib format with a small header and Adler-32 checksum trailer.
        /// </summary>
        /// <remarks>
        /// This format adds a 2-byte header and 4-byte Adler-32 checksum for error detection.
        /// It is compatible with <see cref="ZLibStream"/>.
        /// </remarks>
        ZLib = 1,

        /// <summary>
        /// GZip format with header and CRC-32 checksum trailer.
        /// </summary>
        /// <remarks>
        /// This format adds a larger header with optional metadata and a CRC-32 checksum.
        /// It is compatible with <see cref="GZipStream"/> and the gzip file format.
        /// </remarks>
        GZip = 2
    }
}
