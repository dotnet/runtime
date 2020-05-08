// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using nint = System.IntPtr;

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion()
        {
            int major = 0;
            int minor = 0;
            int patch = 0;

            IntPtr processInfo = objc_msgSend(objc_getClass("NSProcessInfo"), sel_getUid("processInfo"));
            if (processInfo != IntPtr.Zero)
            {
                NSOperatingSystemVersion osVersion = get_operatingSystemVersion(processInfo, sel_getUid("operatingSystemVersion"));
                major = osVersion.majorVersion.ToInt32();
                minor = osVersion.minorVersion.ToInt32();
                patch = osVersion.patchVersion.ToInt32();
            }

            // For compatibility reasons with Mono, PlatformID.Unix is returned on MacOSX. PlatformID.MacOSX
            // is hidden from the editor and shouldn't be used.
            return new OperatingSystem(PlatformID.Unix, new Version(major, minor, patch));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NSOperatingSystemVersion
        {
            public nint majorVersion;
            public nint minorVersion;
            public nint patchVersion;
        }

        [DllImport("libobjc.dylib")]
        private static extern IntPtr objc_getClass(string className);
        [DllImport("libobjc.dylib")]
        private static extern IntPtr sel_getUid(string selector);
        [DllImport("libobjc.dylib")]
        private static extern IntPtr objc_msgSend(IntPtr basePtr, IntPtr selector);
        [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend_stret")]
        private static extern NSOperatingSystemVersion get_operatingSystemVersion(IntPtr basePtr, IntPtr selector);
    }
}
