// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Mime
{
    internal sealed class Base64WriteStateInfo : WriteStateInfoBase
    {
        internal Base64WriteStateInfo() { }

        internal Base64WriteStateInfo(int bufferSize, byte[] header, byte[] footer, int maxLineLength, int mimeHeaderLength) :
            base(bufferSize, header, footer, maxLineLength, mimeHeaderLength)
        {
        }

        internal int Padding { get; set; }
        internal byte LastBits { get; set; }
    }
}
