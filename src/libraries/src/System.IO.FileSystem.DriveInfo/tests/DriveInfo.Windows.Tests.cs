// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using System.Text;

namespace System.IO.FileSystem.DriveInfoTests
{
    public class DriveInfoTests
    {
        [Fact]
        public void TestConstructor()
        {
            string[] invalidInput = { ":", "://", @":\", ":/", @":\\", "Az", "1", "a1", @"\\share", @"\\", "c ", string.Empty, " c" };
            string[] variableInput = { "{0}", "{0}", "{0}:", "{0}:", @"{0}:\", @"{0}:\\", "{0}://" };

            // Test Invalid input
            foreach (var input in invalidInput)
            {
                Assert.Throws<ArgumentException>(() => { new DriveInfo(input); });
            }

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
        public void TestDriveFormat()
        {
            var validDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
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
                Assert.Throws<IOException>(() => { var df = validDrive.DriveFormat; });
            }

            // Test Invalid drive
            var invalidDrive = new DriveInfo(GetInvalidDriveLettersOnMachine().First().ToString());
            Assert.Throws<DriveNotFoundException>(() => { var df = invalidDrive.DriveFormat; });
        }

        [Fact]
        public void TestDriveType()
        {
            var validDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            var expectedDriveType = GetDriveType(validDrive.Name);
            Assert.Equal((DriveType)expectedDriveType, validDrive.DriveType);

            // Test Invalid drive
            var invalidDrive = new DriveInfo(GetInvalidDriveLettersOnMachine().First().ToString());
            Assert.Equal(invalidDrive.DriveType, DriveType.NoRootDirectory);
        }

        [Fact, OuterLoop]
        public void TestVolumeLabel()
        {
            // Get Volume Label - valid drive
            var validDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed).First();
            const int volNameLen = 50;
            StringBuilder volumeName = new StringBuilder(volNameLen);
            const int fileSystemNameLen = 50;
            StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
            int serialNumber, maxFileNameLen, fileSystemFlags;
            bool r = GetVolumeInformation(validDrive.Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
            if (r)
            {
                Assert.Equal(volumeName.ToString(), validDrive.VolumeLabel);
            }

            // Set Volume Label - valid drive
            string newLabel = "DriveInfoTests";
            string originalLabel = validDrive.VolumeLabel;

            if (validDrive.IsReady)
            {
                bool haveAccess = true;
                try
                {
                    validDrive.VolumeLabel = newLabel;
                    Assert.Equal(newLabel, validDrive.VolumeLabel);
                }
                catch (UnauthorizedAccessException) { haveAccess = false; }
                finally
                {
                    if (haveAccess)
                        validDrive.VolumeLabel = originalLabel;
                }
            }

            // Test UnauthorizedAccess on Network or CD-ROM
            var noAccessDrive = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Network || d.DriveType == DriveType.CDRom);
            foreach (var adrive in noAccessDrive)
            {
                if (adrive.IsReady)
                    Assert.Throws<UnauthorizedAccessException>(() => { adrive.VolumeLabel = null; });
            }

            // Test Invalid drive
            var invalidDrive = new DriveInfo(GetInvalidDriveLettersOnMachine().First().ToString());
            Assert.Throws<DriveNotFoundException>(() => { var vl = invalidDrive.VolumeLabel; });
            Assert.Throws<DriveNotFoundException>(() => { invalidDrive.VolumeLabel = null; });
        }

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();

        [DllImport("api-ms-win-core-file-l1-1-0.dll", EntryPoint = "GetVolumeInformationW", CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false)]
        internal static extern bool GetVolumeInformation(string drive, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen);

        [DllImport("api-ms-win-core-file-l1-1-0.dll", SetLastError = true)]
        internal static extern int GetDriveType(string drive);

        private IEnumerable<char> GetValidDriveLettersOnMachine()
        {
            uint mask = (uint)GetLogicalDrives();
            if (mask == 0)
            {
                Assert.False(true, "Test Failed, Error calling Win32 GetLogicalDrive.");
            }

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
            if (mask == 0)
            {
                Assert.False(true, "Test Failed, Error calling Win32 GetLogicalDrive.");
            }

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
