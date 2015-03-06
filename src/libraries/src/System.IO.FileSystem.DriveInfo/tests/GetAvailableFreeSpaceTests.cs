// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Xunit;

namespace System.IO.FileSystem.DriveInfoTests
{
    public class Get_AvailableFreeSpace
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetDiskFreeSpaceEx(String drive, out long freeBytesForUser, out long totalBytes, out long freeBytes);

        [Fact]
        public void Test01()
        {
            bool win32Result;
            Dictionary<string, long> win32Values;
            long fbUser;
            long tbUser;
            long fbTotal;

            DriveInfo[] drives;
            long size;
            long modifiedSize;
            StreamWriter writer;
            FileInfo fi;
            Boolean writable;
            DriveInfo drive;
            List<String> nonDriveLetters;
            List<String> driveLetters;

            //We will test this API by first getting the drives ourselves via Win32 GetDiskFreeSpaceEx
            //This is not ideal since the managed API also calls this API! But there is no really good way to test this otherwise
            //We will also verify manually for the first time these values


            drives = DriveInfo.GetDrives();

            //The GetDiskFreeSpaceEx function retrieves information about the amount of space available on a disk volume: the total amount of space, 
            //the total amount of free space, and the total amount of free space available to the user associated with the calling thread

            win32Values = new Dictionary<string, long>();
            for (int i = 0; i < drives.Length; i++)
            {
                fbUser = -1;
                //We don't want the disk not ready blocking dialog box
                if (drives[i].IsReady)
                {
                    win32Result = GetDiskFreeSpaceEx(drives[i].Name, out fbUser, out tbUser, out fbTotal);
                    if (!win32Result)
                        Assert.False(true, String.Format("Could not get drive size information: {0}", drives[i].Name));
                }
                win32Values.Add(drives[i].Name, fbUser);
            }

            //Scenario 1: Confirm with PInvoke
            for (int i = 0; i < drives.Length; i++)
            {
                if (!drives[i].IsReady)
                    continue;

                //I think we should not allow the OS to throw the blocking dialog box if the drive is not ready. Waiting on that
                if ((long)win32Values[drives[i].Name] != -1)
                {
                    size = drives[i].AvailableFreeSpace;
                    if (size != (long)win32Values[drives[i].Name])
                    {
                        //This can change if there are process IO operations that happen for this user account
                        const double validRatio = 0.9;
                        if ((double)Math.Min(size, (long)win32Values[drives[i].Name]) / (double)Math.Max(size, (long)win32Values[drives[i].Name]) < validRatio)
                        {
                            Assert.False(true, string.Format("Error, Wrong result returned. Expected: {1}, Returned: {2}", i, win32Values[drives[i].Name], size));
                        }
                    }
                }
                else
                {
                    Assert.Throws<IOException>(() => { size = drives[i].AvailableFreeSpace; });
                }
            }

            //Scenario 2: Modify size (by adding and removing files externally) and check
            for (int i = 0; i < drives.Length; i++)
            {
                //We will add a random file to these drives and see if that changes anything - should not at all!
                if ((long)win32Values[drives[i].Name] != -1)
                {
                    //We are not using Path.GetTempFileName because that will limit us to only one drive
                    String tempFileName = "AvailableFreeSpaceTests_";
                    int fileCount = -1;
                    while (fileCount++ < 1000 && File.Exists(drives[i].Name + tempFileName + fileCount)) ;
                    if (fileCount >= 1000)
                    {
                        Assert.False(true, "Error, Some issue with a temp file");
                        break;
                    }
                    tempFileName = drives[i].Name + tempFileName + fileCount;
                    //We need to make sure that this drive is writeable
                    writer = null;
                    writable = true;
                    try
                    {
                        //Will throw UnauthorizedAccessException for non-writeable cases
                        writer = File.CreateText(tempFileName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        writable = false;
                    }

                    if (writable)
                    {
                        writer.Write(new String('a', 5000));
                        if (writer != null)
                            writer.Dispose();

                        fi = new FileInfo(tempFileName);
                        modifiedSize = fi.Length;

                        size = drives[i].AvailableFreeSpace;
                        if (size == (long)win32Values[drives[i].Name])
                        {
                            Assert.False(true, string.Format("Error, Wrong result returned. Expected: {1}, Returned: {2}", i, ((long)win32Values[drives[i].Name] - modifiedSize), size));
                        }
                        if (writer != null)
                            writer.Dispose();
                    }

                    //We are going to delete the file
                    if (File.Exists(tempFileName))
                        File.Delete(tempFileName);

                    //We will recheck the size again after deleting the file
                    size = drives[i].AvailableFreeSpace;
                    if (size != (long)win32Values[drives[i].Name])
                    {
                        //This can change if there are process IO operations that happen for this user account
                        const double validRatio = 0.9;
                        if ((double)Math.Min(size, (long)win32Values[drives[i].Name]) / (double)Math.Max(size, (long)win32Values[drives[i].Name]) < validRatio)
                        {
                            Assert.False(true, string.Format("Error, Wrong result returned. Expected: {1}, Returned: {2}", i, win32Values[drives[i].Name], size));
                        }
                    }
                }
            }

            //Scenario 3: Checking for drives that do not exist
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
                Assert.Throws<DriveNotFoundException>(() => { size = drive.AvailableFreeSpace; });
            }
        }
    }
}
