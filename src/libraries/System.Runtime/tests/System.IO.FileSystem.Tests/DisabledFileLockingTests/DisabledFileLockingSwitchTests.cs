// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.IO.Tests
{
    public class DisabledFileLockingSwitchTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/114951", typeof(PlatformDetection), nameof(PlatformDetection.IsSingleFile))]
        public static void ConfigSwitchIsHonored()
        {
            Assert.Equal(OperatingSystem.IsWindows(), PlatformDetection.IsFileLockingEnabled);
        }
    }
}
