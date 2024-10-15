// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class DisabledFileLockingSwitchTests
    {
        [Fact]
        public static void ConfigSwitchIsHonored()
        {
            Assert.Equal(OperatingSystem.IsWindows(), PlatformDetection.IsFileLockingEnabled);
        }
    }
}
