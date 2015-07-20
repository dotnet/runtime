// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.IO.FileSystem.DriveInfoTests
{
    public partial class DriveInfoUnixTests
    {
        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void TestConstructor()
        {
            Assert.All(
                new[] { "", "\0", "\0/" },
                driveName => Assert.Throws<ArgumentException>("driveName", () => { new DriveInfo(driveName); }));

            Assert.Throws<ArgumentNullException>("driveName", () => { new DriveInfo(null); });

            Assert.Equal("/", new DriveInfo("/").Name);
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void TestGetDrives()
        {
            var drives = DriveInfo.GetDrives();
            Assert.NotNull(drives);
            Assert.True(drives.Length > 0, "Expected at least one drive");
            Assert.All(drives, d => Assert.NotNull(d));
            Assert.Contains(drives, d => d.Name == "/");
            Assert.All(drives, d =>
            {
                // None of these should throw
                DriveType dt = d.DriveType;
                bool isReady = d.IsReady;
                DirectoryInfo di = d.RootDirectory;
            });
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void TestInvalidDriveName()
        {
            var invalidDrive = new DriveInfo("NonExistentDriveName");
            Assert.Throws<DriveNotFoundException>(() => { var df = invalidDrive.DriveFormat; });
            Assert.Throws<DriveNotFoundException>(() => { var size = invalidDrive.TotalFreeSpace; });
            Assert.Throws<DriveNotFoundException>(() => { var size = invalidDrive.TotalSize; });
            Assert.Equal(DriveType.NoRootDirectory, invalidDrive.DriveType);
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
        public void TestProperties()
        {
            var root = new DriveInfo("/");
            Assert.Equal("/", root.Name);
            Assert.Equal("/", root.RootDirectory.FullName);
            Assert.Equal(DriveType.Fixed, root.DriveType);
            Assert.True(root.IsReady);
            Assert.True(root.AvailableFreeSpace > 0);
            Assert.True(root.TotalFreeSpace > 0);
            Assert.True(root.TotalSize > 0);
        }
    }
}