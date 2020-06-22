// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion()
        {
            Version version = Interop.libobjc.GetOperatingSystemVersion();

            // For compatibility reasons with Mono, PlatformID.Unix is returned on MacOSX. PlatformID.MacOSX
            // is hidden from the editor and shouldn't be used.
            return new OperatingSystem(PlatformID.Unix, version);
        }
    }
}
