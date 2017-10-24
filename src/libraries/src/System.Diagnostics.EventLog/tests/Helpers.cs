// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.Tests
{
    internal class Helpers
    {
        public static bool IsElevatedAndSupportsEventLogs { get => AdminHelpers.IsProcessElevated() && SupportsEventLogs; }
        public static bool SupportsEventLogs { get => PlatformDetection.IsNotWindowsNanoServer && !PlatformDetection.IsWindows7; }
    }
}
