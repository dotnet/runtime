// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Xunit;
using System.Text;

namespace System.IO.FileSystem.DriveInfoTests
{
    public class DriveInfoWindowsTests
    {
        [Theory]
        [InlineData(":", null)]
        [InlineData("://", null)]
        [InlineData(@":\", null)]
        [InlineData(":/", null)]
        [InlineData(@":\\", null)]
        [InlineData("Az", null)]
        [InlineData("1", null)]
        [InlineData("a1", null)]
        [InlineData(@"\\share", null)]
        [InlineData(@"\\", null)]
        [InlineData("c ", null)]
        [InlineData("", "path")]
        [InlineData(" c", null)]
        public void Ctor_InvalidPath_ThrowsArgumentException(string driveName, string paramName)
        {
            AssertExtensions.Throws<ArgumentException>(paramName, null, () => new DriveInfo(driveName));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestConstructor()
        {
            string[] variableInput = { "{0}", "{0}", "{0}:", "{0}:", @"{0}:\", @"{0}:\\", "{0}://" };

            // Test Null
            Assert.Throws<ArgumentNullException>(() => { new DriveInfo(null); });

            // Test Valid DriveLetter
            var validDriveLetter = GetValidDriveLettersOnMachine().First();
            foreach (var input in variableInput)
            {
                string name = string.Format(input, validDriveLetter);
                DriveInfo dInfo = new DriveInfo(name);
                Assert.Equal(string.Format(@"{0}:\", validDriveLetter), dInfo.Name);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestGetDrives()
        {
            var validExpectedDrives = GetValidDriveLettersOnMachine();
            var validActualDrives = DriveInfo.GetDrives();

            // Test count
            Assert.Equal(validExpectedDrives.Count(), validActualDrives.Count());

            for (int i = 0; i < validActualDrives.Count(); i++)
            {
                // Test if the driveletter is correct
                Assert.Contains(validActualDrives[i].Name[0], validExpectedDrives);
            }
        }

        [Fact]
        public void TestDriveProperties_AppContainer()
        {
            DriveInfo validDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            bool isReady = validDrive.IsReady;
            Assert.NotNull(validDrive.Name);
            Assert.NotNull(validDrive.RootDirectory.Name);

            if (PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<UnauthorizedAccessException>(() => validDrive.AvailableFreeSpace);
                Assert.Throws<UnauthorizedAccessException>(() => validDrive.DriveFormat);
                Assert.Throws<UnauthorizedAccessException>(() => validDrive.TotalFreeSpace);
                Assert.Throws<UnauthorizedAccessException>(() => validDrive.TotalSize);
                Assert.Throws<UnauthorizedAccessException>(() => validDrive.VolumeLabel);
            }
            else
            {
                Assert.NotNull(validDrive.DriveFormat);
                Assert.True(validDrive.AvailableFreeSpace > 0);
                Assert.True(validDrive.TotalFreeSpace > 0);
                Assert.True(validDrive.TotalSize > 0);
                Assert.NotNull(validDrive.VolumeLabel);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Accessing drive format is not permitted inside an AppContainer.")]
        public void TestDriveFormat()
        {
            DriveInfo validDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            const int volNameLen = 50;
            StringBuilder volumeName = new StringBuilder(volNameLen);
            const int fileSystemNameLen = 50;
            StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
            int serialNumber, maxFileNameLen, fileSystemFlags;
            bool r = GetVolumeInformation(validDrive.Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
            var fileSystem = fileSystemName.ToString();

            if (r)
            {
                Assert.Equal(fileSystem, validDrive.DriveFormat);
            }
            else
            {
                Assert.Throws<IOException>(() => validDrive.DriveFormat);
            }

            // Test Invalid drive
            var invalidDrive = new DriveInfo(GetInvalidDriveLettersOnMachine().First().ToString());
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.DriveFormat);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestDriveType()
        {
            var validDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            var expectedDriveType = GetDriveType(validDrive.Name);
            Assert.Equal((DriveType)expectedDriveType, validDrive.DriveType);

            // Test Invalid drive
            var invalidDrive = new DriveInfo(GetInvalidDriveLettersOnMachine().First().ToString());
            Assert.Equal(invalidDrive.DriveType, DriveType.NoRootDirectory);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "GetDiskFreeSpaceEx blocked in AC")]
        public void TestValidDiskSpaceProperties()
        {
            bool win32Result;
            long fbUser = -1;
            long tbUser;
            long fbTotal;
            DriveInfo drive;

            drive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            if (drive.IsReady)
            {
                win32Result = GetDiskFreeSpaceEx(drive.Name, out fbUser, out tbUser, out fbTotal);
                Assert.True(win32Result);

                if (fbUser != drive.AvailableFreeSpace)
                    Assert.True(drive.AvailableFreeSpace >= 0);

                // valid property getters shouldn't throw
                string name = drive.Name;
                string format = drive.DriveFormat;
                Assert.Equal(name, drive.ToString());

                // totalsize should not change for a fixed drive.
                Assert.Equal(tbUser, drive.TotalSize);

                if (fbTotal != drive.TotalFreeSpace)
                    Assert.True(drive.TotalFreeSpace >= 0);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void TestInvalidDiskProperties()
        {
            string invalidDriveName = GetInvalidDriveLettersOnMachine().First().ToString();
            var invalidDrive = new DriveInfo(invalidDriveName);

            Assert.Throws<DriveNotFoundException>(() => invalidDrive.AvailableFreeSpace);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.DriveFormat);
            Assert.Equal(DriveType.NoRootDirectory, invalidDrive.DriveType);
            Assert.False(invalidDrive.IsReady);
            Assert.Equal(invalidDriveName + ":\\", invalidDrive.Name);
            Assert.Equal(invalidDriveName + ":\\", invalidDrive.ToString());
            Assert.Equal(invalidDriveName + ":\\", invalidDrive.RootDirectory.FullName);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.TotalFreeSpace);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.TotalSize);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.VolumeLabel);
            Assert.Throws<DriveNotFoundException>(() => invalidDrive.VolumeLabel = null);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetVolumeLabel_Returns_CorrectLabel()
        {
            void DoDriveCheck()
            {
                // Get Volume Label - valid drive
                int serialNumber, maxFileNameLen, fileSystemFlags;
                int volNameLen = 50;
                int fileNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                StringBuilder fileSystemName = new StringBuilder(fileNameLen);

                DriveInfo validDrive = DriveInfo.GetDrives().First(d => d.DriveType == DriveType.Fixed);
                bool volumeInformationSuccess = GetVolumeInformation(validDrive.Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileNameLen);

                if (volumeInformationSuccess)
                {
                    Assert.Equal(volumeName.ToString(), validDrive.VolumeLabel);
                }
                else // if we can't compare the volumeName, we should at least check that getting it doesn't throw
                {
                    var name = validDrive.VolumeLabel;
                }
            };
            
            if (PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<UnauthorizedAccessException>(() => DoDriveCheck());
            }
            else 
            {
                DoDriveCheck();
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void SetVolumeLabel_Roundtrips()
        {
            DriveInfo drive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            // Inside an AppContainer access to VolumeLabel is denied.
            if (PlatformDetection.IsInAppContainer)
            {
                Assert.Throws<UnauthorizedAccessException>(() => drive.VolumeLabel);
                return;
            }

            string currentLabel = drive.VolumeLabel;
            try
            {
                drive.VolumeLabel = currentLabel; // shouldn't change the state of the drive regardless of success
            }
            catch (UnauthorizedAccessException) { }
            Assert.Equal(drive.VolumeLabel, currentLabel);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void VolumeLabelOnNetworkOrCdRom_Throws()
        {
            // Test setting the volume label on a Network or CD-ROM
            var noAccessDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Network || d.DriveType == DriveType.CDRom);
            foreach (var adrive in noAccessDrive)
            {
                if (adrive.IsReady)
                {
                    Exception e = Assert.ThrowsAny<Exception>(() => { adrive.VolumeLabel = null; });
                    Assert.True(
                        e is UnauthorizedAccessException || 
                        e is IOException ||
                        e is SecurityException);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();

        [DllImport("kernel32.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool GetVolumeInformation(string drive, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "GetDriveTypeW", CharSet = CharSet.Unicode)]
        internal static extern int GetDriveType(string drive);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetDiskFreeSpaceEx(string drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

        private IEnumerable<char> GetValidDriveLettersOnMachine()
        {
            uint mask = (uint)GetLogicalDrives();
            Assert.NotEqual<uint>(mask, 0);

            var bits = new BitArray(new int[] { (int)mask });
            for (int i = 0; i < bits.Length; i++)
            {
                var letter = (char)('A' + i);
                if (bits[i])
                    yield return letter;
            }
        }

        private IEnumerable<char> GetInvalidDriveLettersOnMachine()
        {
            uint mask = (uint)GetLogicalDrives();
            Assert.NotEqual<uint>(mask, 0);

            var bits = new BitArray(new int[] { (int)mask });
            for (int i = 0; i < bits.Length; i++)
            {
                var letter = (char)('A' + i);
                if (!bits[i])
                {
                    if (char.IsLetter(letter))
                        yield return letter;
                }
            }
        }
    }
}
