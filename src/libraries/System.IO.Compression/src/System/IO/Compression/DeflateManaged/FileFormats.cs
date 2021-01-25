// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Compression
{
    internal interface IFileFormatReader
    {
        bool ReadHeader(InputBuffer input);
        bool ReadFooter(InputBuffer input);
        void UpdateWithBytesRead(byte[] buffer, int offset, int bytesToCopy);
        void Validate();
    }
}
