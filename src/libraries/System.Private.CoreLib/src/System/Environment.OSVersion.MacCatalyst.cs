// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion()
        {
            Version version = new Version(Interop.Sys.iOSSupportVersion());
            return new OperatingSystem(PlatformID.Unix, version);
        }
    }
}
