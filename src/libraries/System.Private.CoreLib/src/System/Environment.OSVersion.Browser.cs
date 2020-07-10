// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    public static partial class Environment
    {
        private static OperatingSystem GetOSVersion()
        {
            return new OperatingSystem(PlatformID.Other, new Version(1, 0, 0, 0));
        }
    }
}
