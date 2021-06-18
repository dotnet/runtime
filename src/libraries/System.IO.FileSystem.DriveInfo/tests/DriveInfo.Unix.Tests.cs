// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace System.IO.FileSystem.DriveInfoTests
{
    public partial class DriveInfoUnixTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void TestConstructor()
        {
            Assert.All(
                new[] { "", "\0", "\0/" },
                driveName => AssertExtensions.Throws<ArgumentException>("driveName", () => { new DriveInfo(driveName); }));

            AssertExtensions.Throws<ArgumentNullException>("driveName", () => { new DriveInfo(null); });

            Assert.Equal("/", new DriveInfo("/").Name);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
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
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Browser)]
        public void PropertiesOfInvalidDrive()
        {
            string invalidDriveName = "NonExistentDriveName";
            var invalidDrive = new DriveInfo(invalidDriveName);

            Assert.Throws<DriveNotFoundException>(() =>invalidDrive.AvailableFreeSpace);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.DriveFormat);
            Assert.Equal(DriveType.NoRootDirectory, invalidDrive.DriveType);
            Assert.False(invalidDrive.IsReady);
            Assert.Equal(invalidDriveName, invalidDrive.Name);
            Assert.Equal(invalidDriveName, invalidDrive.ToString());
            Assert.Equal(invalidDriveName, invalidDrive.RootDirectory.Name);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.TotalFreeSpace);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.TotalSize);
            Assert.Equal(invalidDriveName, invalidDrive.VolumeLabel);   // VolumeLabel is equivalent to Name on Unix
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Android)]
        public void PropertiesOfValidDrive()
        {
            var root = new DriveInfo("/");
            var format = root.DriveFormat;
            Assert.Equal(PlatformDetection.IsBrowser ? DriveType.Unknown : DriveType.Fixed, root.DriveType);
            Assert.True(root.IsReady);
            Assert.Equal("/", root.Name);
            Assert.Equal("/", root.ToString());
            Assert.Equal("/", root.RootDirectory.FullName);
            Assert.Equal("/", root.VolumeLabel);

            if (PlatformDetection.IsBrowser)
            {
                Assert.True(root.AvailableFreeSpace == 0);
                Assert.True(root.TotalFreeSpace == 0);
                Assert.True(root.TotalSize == 0);
            }
            else
            {
                Assert.True(root.AvailableFreeSpace > 0);
                Assert.True(root.TotalFreeSpace > 0);
                Assert.True(root.TotalSize > 0);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void SetVolumeLabel_Throws_PlatformNotSupportedException()
        {
            var root = new DriveInfo("/");
            Assert.Throws<PlatformNotSupportedException>(() => root.VolumeLabel = root.Name);
        }
    }
}
