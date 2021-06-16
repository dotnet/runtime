// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libobjc
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct NSOperatingSystemVersion
        {
            public nint majorVersion;
            public nint minorVersion;
            public nint patchVersion;
        }

        [DllImport(Libraries.libobjc)]
        private static extern IntPtr objc_getClass(string className);
        [DllImport(Libraries.libobjc)]
        private static extern IntPtr sel_getUid(string selector);
        [DllImport(Libraries.libobjc, EntryPoint = "objc_msgSend")]
        private static extern IntPtr intptr_objc_msgSend(IntPtr basePtr, IntPtr selector);

        internal static Version GetOperatingSystemVersion()
        {
            int major = 0;
            int minor = 0;
            int patch = 0;

            IntPtr processInfo = intptr_objc_msgSend(objc_getClass("NSProcessInfo"), sel_getUid("processInfo"));

            if (processInfo != IntPtr.Zero)
            {
#if TARGET_ARM64
                NSOperatingSystemVersion osVersion = NSOperatingSystemVersion_objc_msgSend(processInfo, sel_getUid("operatingSystemVersion"));
#else
                NSOperatingSystemVersion_objc_msgSend_stret(out NSOperatingSystemVersion osVersion, processInfo, sel_getUid("operatingSystemVersion"));
#endif
                checked
                {
                    major = (int)osVersion.majorVersion;
                    minor = (int)osVersion.minorVersion;
                    patch = (int)osVersion.patchVersion;
                }
            }

            if (major == 10 && minor == 16)
            {
                // We get "compat" version for 11.0 unless we build with updated SDK.
                // Hopefully that will be before 11.x comes out
                // For now, this maps 10.16 to 11.0.
                major = 11;
                minor = 0;
            }

            return new Version(major, minor, patch);
        }

        [DllImport(Libraries.libobjc, EntryPoint = "objc_msgSend")]
        private static extern NSOperatingSystemVersion NSOperatingSystemVersion_objc_msgSend(IntPtr basePtr, IntPtr selector);

        [DllImport(Libraries.libobjc, EntryPoint = "objc_msgSend_stret")]
        private static extern void NSOperatingSystemVersion_objc_msgSend_stret(out NSOperatingSystemVersion osVersion, IntPtr basePtr, IntPtr selector);
    }
}
