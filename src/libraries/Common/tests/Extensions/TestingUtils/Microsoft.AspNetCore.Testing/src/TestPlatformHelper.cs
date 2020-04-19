// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestPlatformHelper
    {
        public static bool IsMono =>
            Type.GetType("Mono.Runtime") != null;

        public static bool IsWindows =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsLinux =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static bool IsMac =>
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    }
}
