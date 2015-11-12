// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    private static partial class Libraries
    {
        internal const string LibCoreClr = "libcoreclr";       // CoreCLR runtime

        // Shims
        internal const string SystemNative = "System.Native";
        internal const string HttpNative = "System.Net.Http.Native";
        internal const string CryptoNative = "System.Security.Cryptography.Native";
        internal const string GlobalizationNative = "System.Globalization.Native";
        internal const string CompressionNative = "System.IO.Compression.Native";
    }
}
