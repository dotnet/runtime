// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net
{
    // sspi.h
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecPkgContext_StreamSizes
    {
        public int cbHeader;
        public int cbTrailer;
        public int cbMaximumMessage;
        public int cBuffers;
        public int cbBlockSize;
    }
}
