// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace TestLibrary
{
    public class XPlatformUtils
    {
        public static string GetStandardNativeLibraryFileName(string simpleName)
        {
            if (Utilities.IsWindows)
            {
                return simpleName + ".dll";
            }
            if (Utilities.IsMacOSX)
            {
                return "lib" + simpleName + ".dylib";
            }
            return "lib" + simpleName + ".so";
        }
    }
}
