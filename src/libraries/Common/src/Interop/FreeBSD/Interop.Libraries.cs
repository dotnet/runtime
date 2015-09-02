// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    private static partial class Libraries
    {
        /// <summary>
        /// We aren't OS X so don't have an INODE64 suffix to entry points
        /// </summary>
        internal const string INODE64SUFFIX = "";

        internal const string LibRt = "librt";  // POSIX Realtime Extensions library
        internal const string LibCurl = "libcurl";             // Curl HTTP client library	
    }
}
