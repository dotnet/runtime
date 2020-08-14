// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net
{
    // sspi.h
    [StructLayout(LayoutKind.Sequential)]
    internal struct SecPkgContext_Sizes
    {
        public readonly int cbMaxToken;
        public readonly int cbMaxSignature;
        public readonly int cbBlockSize;
        public readonly int cbSecurityTrailer;
    }
}
