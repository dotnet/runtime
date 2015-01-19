// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.IO.FileSystem.DriveInfoTests
{
    public class Get_DriveFormat
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetVolumeInformation(String drive, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen);

        [Fact]
        public void Test01()
        {
            DriveInfo[] drives;
            String fileSystem;
            String drFormat;
            DriveInfo drive;
            List<String> nonDriveLetters;
            List<String> driveLetters;

            //Scenario 1: Vanilla - Confirm with PInvoke. We are doing the same as FX API but no good way to test this automatically. We checked the
            //Values manually at first pass. Need to add some more manual tests separately
            drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                if (!drives[i].IsReady)
                    continue;
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;
                bool r = GetVolumeInformation(drives[i].Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);

                fileSystem = fileSystemName.ToString();

                if (r)
                {
                    if (fileSystem != drives[i].DriveFormat)
                    {
                        Assert.False(true, string.Format("Error, unexpected result returned: {0}, Expected: {1}", drives[i].DriveFormat, fileSystem));
                    }
                }
                else
                {
                    Assert.Throws<IOException>(() => { drFormat = drives[i].DriveFormat; });
                }
            }

            //Scenario 2 - for invalid drives
            drives = DriveInfo.GetDrives();

            driveLetters = new List<String>();
            for (int i = 0; i < drives.Length; i++)
                driveLetters.Add(drives[i].Name[0].ToString().ToUpper());
            nonDriveLetters = new List<String>();

            for (int i = 0; i < 26; i++)
                if (!driveLetters.Contains(((Char)(65 + i)).ToString()))
                    nonDriveLetters.Add(((Char)(65 + i)).ToString());
            foreach (String letter in nonDriveLetters)
            {
                drive = new DriveInfo(letter);
                Assert.Throws<DriveNotFoundException>(() => { drFormat = drive.DriveFormat; });
            }
        }
    }
}
