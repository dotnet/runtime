// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Net.Mime
{
    internal static class EncodedStreamFactory
    {
        //RFC 2822: no encoded-word line should be longer than 76 characters not including the soft CRLF
        //since the header length is unknown (if there even is one) we're going to be slightly more conservative
        //and cut off at 70.  This will also prevent any other folding behavior from being triggered anywhere
        //in the code
        internal const int DefaultMaxLineLength = 70;

        //default buffer size for encoder
        private const int InitialBufferSize = 1024;

        //use for encoding headers
        internal static IEncodableStream GetEncoderForHeader(Encoding encoding, bool useBase64Encoding, int headerTextLength)
        {
            byte[] header = CreateHeader(encoding, useBase64Encoding);
            byte[] footer = s_footer;

            WriteStateInfoBase writeState;
            if (useBase64Encoding)
            {
                writeState = new Base64WriteStateInfo(InitialBufferSize, header, footer, DefaultMaxLineLength, headerTextLength);
                return new Base64Stream((Base64WriteStateInfo)writeState);
            }

            writeState = new WriteStateInfoBase(InitialBufferSize, header, footer, DefaultMaxLineLength, headerTextLength);
            return new QEncodedStream(writeState);
        }

        //Create the header for what type of byte encoding is going to be used
        //based on the encoding type and if base64 encoding should be forced
        //sample header: =?utf-8?B?
        private static byte[] CreateHeader(Encoding encoding, bool useBase64Encoding) =>
            Encoding.ASCII.GetBytes("=?" + encoding.HeaderName + "?" + (useBase64Encoding ? "B?" : "Q?"));

        //The footer that marks the end of a quoted string of some sort
        private static readonly byte[] s_footer = new byte[] { (byte)'?', (byte)'=' };
    }
}
