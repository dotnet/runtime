// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    public partial class Directory_GetLogicalDrives
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Valid drive strings on Unix
        public void GetsValidDriveStrings_Unix()
        {
            string[] drives = Directory.GetLogicalDrives();
            Assert.NotEmpty(drives);
            Assert.All(drives, d => Assert.NotNull(d));
            Assert.Contains(drives, d => d == "/");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Valid drive strings on Windows
        public void GetsValidDriveStrings_Windows()
        {
            string[] drives = Directory.GetLogicalDrives();
            Assert.NotEmpty(drives);
            Assert.All(drives, d =>
            {
                const string driveRootSuffix = ":\\";
                Assert.Equal(driveRootSuffix.Length + 1, d.Length);
                Assert.InRange(d[0], 'A', 'Z');
                Assert.EndsWith(driveRootSuffix, d);
            });
        }
    }
}
