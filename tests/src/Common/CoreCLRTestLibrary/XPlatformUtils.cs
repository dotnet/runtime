// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace TestLibrary
{
    public class XPlatformUtils
    {
#if PLATFORM_WINDOWS
        public const string NativeLibraryPrefix = "";
        public const string NativeLibrarySuffix = ".dll";
#else
        public const string NativeLibraryPrefix = "lib";
#if PLATFORM_OSX
        public const string NativeLibrarySuffix = ".dylib";
#else
        public const string NativeLibrarySuffix = ".so";
#endif
#endif

        public static string GetStandardNativeLibraryFileName(string simpleName)
        {
            return NativeLibraryPrefix + simpleName + NativeLibrarySuffix;
        }
    }
}
