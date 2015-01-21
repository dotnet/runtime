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
    public class Get_IsReady
    {
        [Fact]
        public void Test01()
        {
            DriveInfo[] drives;
            StreamWriter writer;
            Boolean writable;

            DriveInfo drive;
            List<String> nonDriveLetters;
            List<String> driveLetters;

            //There is no easy way to test this fully. The FX tests this internally using Directory.InternalExists which in turn uses a significant
            //code for specific OSs and Win32 functions. We attempt to test this indirectly. We will get all the drives and call IsReady on it. If it 
            //succeds, we will try to write a file to it.
            drives = DriveInfo.GetDrives();
            for (int i = 0; i < drives.Length; i++)
            {
                //Added test to verify that if a drive is removable, has no media, and Dir is called
                //an exception is thrown instead of a Windows dialog being displayed.
                //
                if (drives[i].DriveType == DriveType.Removable && !drives[i].IsReady)
                {
                    try
                    {
                        string[] mydir = Directory.GetFiles(drives[i].Name);
                        Assert.False(true, string.Format("Test Failed, Expected IOException or UnauthorizedAccessException"));
                    }
                    catch (System.IO.IOException)
                    {
                    }
                    catch (System.UnauthorizedAccessException)
                    {
                    }
                    catch (System.Exception ex)
                    {
                        Assert.False(true, string.Format("Test Failed, threw unexpected exception: {0}", ex.GetType().ToString()));
                    }
                }

                String tempFileName = drives[i].Name + "_get_IsReady";
                if (drives[i].IsReady)
                {
                    if (File.Exists(tempFileName))
                        File.Delete(tempFileName);
                    //We need to make sure that this drive is writeable
                    writer = null;
                    writable = true;
                    try
                    {
                        writer = File.CreateText(tempFileName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //Will throw UnauthorizedAccessException for CD-ROM's
                        writable = false;
                    }
                    catch (IOException)
                    {
                        //Write protected disks (floppy)
                        writable = false;
                    }

                    if (writable)
                    {
                        writer.Write(new String('a', 5000));
                        if (writer != null)
                            writer.Dispose();
                        if (!File.Exists(tempFileName))
                        {
                            Assert.False(true, string.Format("Error, File: {0} does not exist", tempFileName));
                        }
                    }
                }
                else
                {
                    try
                    {
                        writer = File.CreateText(tempFileName);
                        Assert.False(true, string.Format("Error, Able to write to a non-available drive: {0}", tempFileName));
                    }
                    catch (IOException)
                    {
                    }
                    catch (System.UnauthorizedAccessException)
                    {
                    }
                    if (File.Exists(tempFileName))
                    {
                        Assert.False(true, string.Format("Error, File: {0} does exist", tempFileName));
                    }
                }
                //We are going to delete the file
                if (File.Exists(tempFileName))
                    File.Delete(tempFileName);
            }

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
                if (drive.IsReady)
                {
                    Assert.False(true, "Error, Exception expected");
                }
            }
        }
    }
}
