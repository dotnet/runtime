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
    public class Get_DriveType
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetDriveType(String drive);

        [Fact]
        public void Test01()
        {
            DriveInfo[] drives;
            int win32Result;
            DriveInfo drive;
            List<String> nonDriveLetters;
            List<String> driveLetters;

            //Scenario 1: Vanilla - Confirm with PInvoke
            drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                win32Result = GetDriveType(drives[i].Name);
                if ((DriveType)win32Result != drives[i].DriveType)
                {
                    Assert.False(true, string.Format("Error, unexpected result returned: {0}, Expected: {1}", drives[i].DriveType, (DriveType)win32Result));
                }
            }

            //Scenario 2: Checking for drives that do not exist
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
                if (drive.DriveType != DriveType.NoRootDirectory)
                {
                    Assert.False(true, "Error, Exception expected");
                }
            }
        }
    }
}
