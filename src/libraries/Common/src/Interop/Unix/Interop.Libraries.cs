// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    private static partial class Libraries
    {
        internal const string CryptoInterop = "System.Security.Cryptography.Native";
        internal const string IOInterop = "System.IO.Native";
        internal const string Libc = "libc";                   // C library
        internal const string LibCoreClr= "libcoreclr";        // CoreCLR runtime
        internal const string LibCrypto = "libcrypto";         // OpenSSL crypto library
        internal const string LibCurl = "libcurl";             // Curl HTTP client library
        internal const string Zlib = "libz";                   // zlib compression library
    }
}
