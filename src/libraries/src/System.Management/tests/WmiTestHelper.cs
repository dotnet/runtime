// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Management.Tests
{
    public static class WmiTestHelper
    {
        private static readonly bool s_isElevated = AdminHelpers.IsProcessElevated();
        private static readonly bool s_isWmiSupported = PlatformDetection.IsWindows && PlatformDetection.IsNotWindowsNanoServer && !PlatformDetection.IsUap;
        private static readonly string s_systemDriveId = Path.GetPathRoot(Environment.GetEnvironmentVariable("SystemDrive"));

        public static string Namespace => "root/WmiEBvt";
        public static string SystemDriveId => s_systemDriveId;
        public static bool IsWmiSupported => s_isWmiSupported;
        public static bool IsElevatedAndSupportsWmi => s_isElevated && IsWmiSupported;
    }
}
