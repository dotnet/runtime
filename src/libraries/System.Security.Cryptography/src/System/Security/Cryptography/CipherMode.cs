// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    // This enum represents supported cipher chaining modes:
    //  cipher block chaining (CBC),
    //  electronic code book (ECB),
    //  cipher feedback (CFB),
    //  ciphertext-stealing (CTS).
    // Not all implementations will support all modes.
    public enum CipherMode
    {
        CBC = 1,
        [UnsupportedOSPlatform("browser")]
        ECB = 2,
        [EditorBrowsable(EditorBrowsableState.Never)]
        [UnsupportedOSPlatformAttribute("browser")]
        OFB = 3,
        [UnsupportedOSPlatform("browser")]
        CFB = 4,
        [UnsupportedOSPlatform("browser")]
        CTS = 5
    }
}
