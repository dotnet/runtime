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
    public class Set_VolumeLabel
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetVolumeInformation(String drive, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen);

        [Fact]
        public void Test01()
        {
            DriveInfo[] drives;
            Dictionary<string, string> originalVolumeInfo;
            String tempVolumeName;
            Boolean volumeSettable;
            int chCount = 0;

            DriveInfo drive;
            List<string> nonDriveLetters;
            List<string> driveLetters;

            //This is both a settable and gettable property. We test the get part of it here. There is a Win32 API that gives this information.
            //But since this is settable, we can also use that to make sure that this property works as expected. We will make sure that we will leave
            //the drives with the original volume labels

            //Scenario 1 - Confirm with Pinvoke	
            drives = DriveInfo.GetDrives();
            originalVolumeInfo = new Dictionary<string, string>();

            for (int i = 0; i < drives.Length; i++)
            {
                const int volNameLen = 50;
                StringBuilder volumeName = new StringBuilder(volNameLen);
                const int fileSystemNameLen = 50;
                StringBuilder fileSystemName = new StringBuilder(fileSystemNameLen);
                int serialNumber, maxFileNameLen, fileSystemFlags;
                bool r = GetVolumeInformation(drives[i].Name, volumeName, volNameLen, out serialNumber, out maxFileNameLen, out fileSystemFlags, fileSystemName, fileSystemNameLen);
                if (!r)
                {
                    if (drives[i].IsReady)
                    {
                        Assert.False(true, string.Format("Unexpected error returned from Win32 fn. Drive Name: {1}, Volume: {2}", i, drives[i].Name, drives[i].VolumeLabel));
                    }
                }
                else
                {
                    if (volumeName.ToString() != drives[i].VolumeLabel)
                    {
                        Assert.False(true, string.Format("Error, Wrong volume label returned. Expected: {1}, Returned: {2}", i, volumeName.ToString(), drives[i].VolumeLabel));
                    }
                    originalVolumeInfo.Add(drives[i].Name, drives[i].VolumeLabel);
                }
            }

            //Scenario 2: Change the volume label and check
            tempVolumeName = "BCLTest";

            for (int i = 0; i < drives.Length; i++)
            {
                if (drives[i].IsReady)
                {
                    volumeSettable = true;
                    try
                    {
                        drives[i].VolumeLabel = tempVolumeName;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        volumeSettable = false;
                    }
                    if (volumeSettable)
                    {
                        if (String.Compare(drives[i].VolumeLabel, tempVolumeName, StringComparison.CurrentCultureIgnoreCase) != 0)
                        {
                            Assert.False(true, string.Format("Error, Wrong volume label returned. Expected: {1}, Returned: {2}", i, tempVolumeName, drives[i].VolumeLabel));
                        }
                    }
                }
            }

            //Scenario 3: Invalid string values. This is really mute since we dont do any error checking in our code. We pass the down to Win32 and report
            //Any error conditions. And we dont want to test the Win32 API here :-)
            for (int i = 0; i < drives.Length; i++)
            {
                if (drives[i].IsReady && drives[i].DriveType != DriveType.CDRom && drives[i].DriveType != DriveType.Network)
                {
                    volumeSettable = true;
                    try
                    {
                        drives[i].VolumeLabel = null;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        volumeSettable = false;
                    }
                    if (volumeSettable && drives[i].VolumeLabel != String.Empty)
                    {
                        Assert.False(true, string.Format("Error, Wrong result returned, <{0}>", drives[i].VolumeLabel));
                    }
                }
                if (drives[i].IsReady && (drives[i].DriveType == DriveType.CDRom || drives[i].DriveType == DriveType.Network))
                {
                    Assert.Throws<UnauthorizedAccessException>(() => { drives[i].VolumeLabel = null; });
                }
                if (!drives[i].IsReady)
                {
                    try
                    {
                        drives[i].VolumeLabel = null;
                    }
                    catch (IOException)
                    {
                    }
                    catch (System.UnauthorizedAccessException)
                    {
                    }
                }

                if (drives[i].IsReady && (drives[i].DriveType != DriveType.CDRom) && (drives[i].DriveType != DriveType.Network))
                {
                    //There is a char limit for every file system when setting a volume label
                    switch (drives[i].DriveFormat)
                    {
                        case "FAT":
                            chCount = 12;
                            break;
                        case "FAT32":
                            chCount = 12;
                            break;
                        case "NTFS":
                            chCount = 33;
                            break;
                        default:
                            chCount = 50;
                            Assert.False(true, "Test Failed");
                            break;
                    }

                    String sT = new String('a', chCount);
                    try
                    {
                        drives[i].VolumeLabel = sT;
                        Assert.False(true, "Expected IOException.");
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }

            //Scenario 4: We will revert to the original volume value and check
            for (int i = 0; i < drives.Length; i++)
            {
                if (originalVolumeInfo.ContainsKey(drives[i].Name))
                {
                    try
                    {
                        drives[i].VolumeLabel = (String)originalVolumeInfo[drives[i].Name];
                    }
                    catch (UnauthorizedAccessException) { }
                }

                if (drives[i].IsReady)
                {
                    if (String.Compare((String)originalVolumeInfo[drives[i].Name], drives[i].VolumeLabel, StringComparison.CurrentCultureIgnoreCase) != 0)
                    {
                        Assert.False(true, string.Format("Error, Wrong volume label returned. Expected: {1}, Returned: {2}", i, (String)originalVolumeInfo[drives[i].Name], drives[i].VolumeLabel));
                    }
                }
            }

            //Scenario 5: Checking for drives that do not exist
            drives = DriveInfo.GetDrives();

            driveLetters = new List<string>();
            for (int i = 0; i < drives.Length; i++)
                driveLetters.Add(drives[i].Name[0].ToString().ToUpper());
            nonDriveLetters = new List<string>();

            for (int i = 0; i < 26; i++)
                if (!driveLetters.Contains(((Char)(65 + i)).ToString()))
                    nonDriveLetters.Add(((Char)(65 + i)).ToString());
            foreach (String letter in nonDriveLetters)
            {
                drive = new DriveInfo(letter);
                Assert.Throws<DriveNotFoundException>(() => { drive.VolumeLabel = "NOCANDO"; });
            }
        }
    }
}


