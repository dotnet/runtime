// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net
{
    [Flags]
    public enum DecompressionMethods
    {
        None = 0,
        GZip = 0x1,
        Deflate = 0x2,
        Brotli = 0x4,
        All = ~None
    }
}
