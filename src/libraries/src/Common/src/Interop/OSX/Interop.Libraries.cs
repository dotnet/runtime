// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

internal static partial class Interop
{
    private static partial class Libraries
    {
        /// <summary>
        /// Some functions in OS X have the __DARWIN_INODE64(<function_name>) decoration, which means
        /// that the function has the prefix $INODE64 natively, so we need to apply that decoration
        /// onto the end of the entry point name to PInvoke into the right function. We use this const
        /// so that other *nix systems that have the same function prototype but don't have the INODE64, since
        /// they aren't OS X, can use the same declaration and not redeclare it.
        /// </summary>
        internal const string INODE64SUFFIX = "$INODE64";

        internal const string CoreFoundationLibrary = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
        internal const string CoreServicesLibrary   = "/System/Library/Frameworks/CoreServices.framework/CoreServices";
        internal const string libproc = "libproc";
        internal const string LibSystemKernel = "/usr/lib/system/libsystem_kernel";
        internal const string LibCurl = "libcurl";             // Curl HTTP client library
    }
}
