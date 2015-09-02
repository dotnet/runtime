// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    private static partial class Libraries
    {
        /// <summary>
        /// We aren't OS X so we don't have a suffix
        /// </summary>
        internal const string INODE64SUFFIX = "";

        internal const string LibRt = "librt"; // POSIX Realtime Extensions library
        internal const string LibCurl = "libcurl.so.4";             // Curl HTTP client library
    }
}
