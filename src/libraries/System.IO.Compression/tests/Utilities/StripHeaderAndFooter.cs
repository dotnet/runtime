// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections.Generic;

class StripHeaderAndFooter
{
    //' delegate void ExceptionCode();

    public static MemoryStream Strip(Stream inputStream)
    {
        //We will try to strip the header and footer for any gzip compressed stream here
        //This is needed for Deflate testing

        var outputStream = new MemoryStream();

        bool fText, fCRC, fextra, fname, fComment;
        byte flag;
        int len;
        int value;

        //Header
        //See RFC 1952 - ftp://swrinde.nde.swri.edu/pub/png/documents/zlib/rfc-gzip.html
        // or http://madhaus.utcs.utoronto.ca/links/ftp/doc/rfc/rfc1952.txt

        //skip the first 3 bytes - ID and CM
        SkipBytes(inputStream, 3);

        //flag
        flag = (byte)inputStream.ReadByte();
        fText = ((flag & 1) != 0);
        fCRC = ((flag & 2) != 0);
        fextra = ((flag & 4) != 0);
        fname = ((flag & 8) != 0);
        fComment = ((flag & 16) != 0);

        //MTIME, XFL and OS
        SkipBytes(inputStream, 6);

        if (fextra)
        {
            len = inputStream.ReadByte();
            len |= (inputStream.ReadByte() << 8);

            SkipBytes(inputStream, len);
        }

        if (fname)
        {
            ReadUntilZero(inputStream);
        }

        if (fComment)
        {
            ReadUntilZero(inputStream);
        }

        if (fCRC)
        {
            SkipBytes(inputStream, 2);
        }

        //body
        //We will now write the body to the output file

        List<byte> bitlist = new List<byte>();

        while ((value = inputStream.ReadByte()) != -1)
        {
            bitlist.Add((byte)value);
            //				outputStream.WriteByte((Byte)value);
        }

        //			outputStream.Close();
        inputStream.Dispose();

        //Footer
        //The correct way to find a footer would be to read the compressed blocks but we try a different approach
        //All we know about it is that it is the last 8 bytes and we will read the last 8 bytes as the footer
        //We can confirm this by comparing the size of the decompressed file with the size specified in the footer
        //To do this, we need to decompress the file
        //@TODO!! - is it worth it??
        //			outputStream = new MemoryStream(outputFileName, FileMode.Create, FileAccess.Write);
        //			inputStream = new MemoryStream(inputFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

        //			outputStream.Flush();
        byte[] bits = new byte[bitlist.Count - 8];
        bitlist.CopyTo(0, bits, 0, bitlist.Count - 8);
        outputStream.Write(bits, 0, bits.Length);

        outputStream.Position = 0;

        return outputStream;
    }

    static void SkipBytes(Stream inputStream, int count)
    {
        //This will skip this many bytes
        for (int i = 0; i < count; i++)
            inputStream.ReadByte();
    }

    static void ReadUntilZero(Stream inputStream)
    {
        while (inputStream.ReadByte() != 0) ;
    }
}
