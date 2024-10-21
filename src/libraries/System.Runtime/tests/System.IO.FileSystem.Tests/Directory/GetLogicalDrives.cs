// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.Tests
{
    public class Directory_GetLogicalDrives
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
            Assert.All(drives, d => Assert.Matches(@"^[A-Z]:\\$", d));
        }
    }
}
