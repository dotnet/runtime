// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Compression
{
    internal interface IFileFormatWriter
    {
        byte[] GetHeader();
        void UpdateWithBytesRead(byte[] buffer, int offset, int bytesToCopy);
        byte[] GetFooter();
    }

    internal interface IFileFormatReader
    {
        bool ReadHeader(InputBuffer input);
        bool ReadFooter(InputBuffer input);
        void UpdateWithBytesRead(byte[] buffer, int offset, int bytesToCopy);
        void Validate();

        /// <summary>
        /// A reader corresponds to an expected file format and contains methods
        /// to read header/footer data from a file of that format. If the Zlib library
        /// is instead being used and the file format is supported, we can simply pass
        /// a supported WindowSize and let Zlib do the header/footer parsing for us.
        ///
        /// This Property allows getting of a ZLibWindowSize that can be used in place
        /// of manually parsing the raw data stream.
        /// </summary>
        /// <return>
        /// For raw data, return -8..-15
        /// For GZip header detection and decoding, return 16..31
        /// For GZip and Zlib header detection and decoding, return 32..47
        /// </return>
        /// <remarks>
        /// The windowBits parameter for inflation must be greater than or equal to the
        /// windowBits parameter used in deflation.
        /// </remarks>
        /// <remarks>
        /// If the incorrect header information is used, zlib inflation will likely throw a
        /// Z_DATA_ERROR exception.
        ///</remarks>
        int ZLibWindowSize { get; }
    }
}
