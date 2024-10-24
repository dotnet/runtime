// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion()
        {
            Version version = new Version(Interop.Sys.iOSSupportVersion());

            int major = version.Major;
            int minor = version.Minor;
            // Normalize the build component to 0 if undefined
            // to match iOS behavior
            int build = version.Build < 0 ? 0 : version.Build;

            // The revision component is always set to -1,
            // as it is not specified on MacCatalyst or iOS
            version = new Version(major, minor, build);
            return new OperatingSystem(PlatformID.Unix, version);
        }
    }
}
