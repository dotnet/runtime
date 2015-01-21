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
    public class Get_RootDirectory
    {
        [Fact]
        public void Test01()
        {
            DriveInfo[] drives;
            DirectoryInfo dir;

            DriveInfo drive;
            List<String> nonDriveLetters;
            List<String> driveLetters;

            //Scenario 1: Vanilla - check the return DirectoryInfo object for correctness
            drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                // avoid empty floppy drives popping up a dialog and then hanging the test
                if (drives[i].DriveType == DriveType.Removable || !drives[i].IsReady)
                    continue;

                dir = drives[i].RootDirectory;

                if (dir.Parent != null)
                {
                    Assert.False(true, string.Format("Error, unexpected result returned: {0}, Expected: {1}", dir.Parent, "null"));
                }

                if (String.Compare(dir.FullName, drives[i].Name) != 0)
                {
                    Assert.False(true, string.Format("Error, unexpected result returned: {0}, Expected: {1}", dir.FullName, drives[i].Name));
                }

                if (!dir.Exists)
                {
                    Assert.Throws<System.IO.IOException>(() => { dir.GetDirectories(); });
                }
                else
                {
                    try
                    {
                        //this can throw an UnauthorizedAccessException if its not accessible
                        dir.GetDirectories();
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("threw UnauthorizedAccessException. {0} {1} {2}", dir.FullName, drives[i].DriveType, drives[i].DriveFormat);
                    }
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
                if (drive.RootDirectory.Name[0] != letter[0])
                {
                    Assert.False(true, "Error, Exception expected");
                }
            }
        }
    }
}
