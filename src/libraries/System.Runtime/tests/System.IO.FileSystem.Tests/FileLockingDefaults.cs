// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public class FileLockingDefaults
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.iOS | TestPlatforms.tvOS)]
        public void FileLockingDisabledByDefault()
        {
            Assert.False(PlatformDetection.IsFileLockingEnabled);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.MacCatalyst)]
        public void FileLockingEnabledByDefault()
        {
            Assert.True(PlatformDetection.IsFileLockingEnabled);
        }
    }
}
