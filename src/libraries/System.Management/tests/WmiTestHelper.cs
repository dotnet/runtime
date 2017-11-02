// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace System.Management.Tests
{
    public static class WmiTestHelper
    {
        private static readonly bool s_isElevated = AdminHelpers.IsProcessElevated();
        private static readonly bool s_isWmiSupported = PlatformDetection.IsWindows && PlatformDetection.IsNotWindowsNanoServer && !PlatformDetection.IsUap;
        private static readonly string s_systemDriveId = Path.GetPathRoot(Environment.GetEnvironmentVariable("SystemDrive"));

        // Use the environment variable below to do manual runs against remote boxes: just ensure that the credentials running the tests have
        // rights to query the box (use wbemtest tool to validate).
        private static readonly string s_targetMachine = Environment.GetEnvironmentVariable("WmiTestTargetMachine") ?? Environment.MachineName;
        private static readonly object[][] s_scopeRoots = new[]
        {
            new [] { $@"\\{s_targetMachine}\" },
            new [] { @"\\.\" },
            new [] { string.Empty }
        };

        public static string Namespace => "root/WmiEBvt";
        public static string SystemDriveId => s_systemDriveId;
        public static bool IsWmiSupported => s_isWmiSupported;
        public static bool IsElevatedAndSupportsWmi => s_isElevated && IsWmiSupported;
        public static string TargetMachine => s_targetMachine;
        public static IEnumerable<object[]> ScopeRoots => s_scopeRoots;
    }
}
