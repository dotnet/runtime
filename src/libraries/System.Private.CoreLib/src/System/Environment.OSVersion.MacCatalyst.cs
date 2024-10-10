// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion()
        {
            Version version = Version.Parse(Interop.Sys.iOSSupportVersion());

            // Check if build and revision are -1 and default them to 0
            int major = version.Major;
            int minor = version.Minor;
            int build = version.Build >= 0 ? version.Build : 0;
            int revision = version.Revision >= 0 ? version.Revision : 0;

            version = new Version(major, minor, build, revision);

            return new OperatingSystem(PlatformID.Unix, version);
        }
    }
}
