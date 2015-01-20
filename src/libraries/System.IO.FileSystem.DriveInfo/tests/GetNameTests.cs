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
    public class Get_Name
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();

        [Fact]
        public void Test01()
        {
            String[] driveLetterCombinaions =
            {
                String.Empty, ":", String.Format("{0}{1}", ":", "\\"),
                String.Format("{0}{1}", ":", "/")
            };

            DriveInfo drive;
            String path;
            String tempPath;
            int win32Result;
            uint mask;
            BitArray bits;
            List<Char> driveLetters;
            List<Char> nonDriveLetters;
            Char letter;

            //We will test this API by first getting the drives ourselves. There are 3 ways to get the drives
            //1) Directory.GetLogicalDrives which returns a String[]
            //2) DriveInfo.GetDrives() which returns a DriveInfo[]. This API calls 1)
            //3) Win32 GetLogicalDrives, which 1) calls to get the values
            //We will use the 3rd one here


            //Win32 call. If the function succeeds, the return value is a bitmask representing the currently available disk drives. 
            //Bit position 0 (the least-significant bit) is drive A, bit position 1 is drive B, bit position 2 is drive C, and so on.
            win32Result = GetLogicalDrives();
            mask = (uint)win32Result;
            if (mask == 0)
            {
                Assert.False(true, "Test Failed, No drives in this machine or error calling Win32!");
            }

            //There are many ways to read this bit mask. Old style C way would be to shift the bits and check for 1. But we go the modern FX way!
            bits = new BitArray(new Int32[] { (int)mask });
            driveLetters = new List<Char>();
            nonDriveLetters = new List<Char>();
            for (int i = 0; i < bits.Length; i++)
            {
                letter = (Char)(65 + i);
                if (bits[i])
                    driveLetters.Add(letter);
                else
                {
                    if (Char.IsLetter(letter))
                        nonDriveLetters.Add(letter);
                }
            }

            //Scenario 1: Vanilla - valid drive letters and valid paths to the root
            //Scenario 2: Variation of the drive letters: c, c:, c:\
            //Scenario 3: DriveAltCharacters like / in Windows
            foreach (char c in driveLetters)
            {
                //compromising clarity for brevity since the new testcase guidelines encourages brevity. Combining the following testcases in a loop
                //DriveInfo ctor with path with only the letter - "C"
                //DriveInfo ctor with path with the letter and the : - "C:"
                //DriveInfo ctor with path with the letter and the :\ - "C:\"
                //DriveInfo ctor with path with the letter and the :/ - "C:/"
                foreach (String suffix in driveLetterCombinaions)
                {
                    //Uppercase letters
                    path = c.ToString() + suffix;
                    drive = new DriveInfo(path);
                    //If we pass the AltDirectorySeparatorChar in the ctor, the library will change this to DirectorySeparatorChar
                    if (!drive.Name.StartsWith(path.Replace("/", "\\")))
                    {
                        Assert.False(true, string.Format("Error, Wrong value returned. Expected: {1}, Returned: {2}", suffix, path, drive.Name));
                    }

                    //Lowercase
                    path = c.ToString().ToLower() + suffix;
                    drive = new DriveInfo(path);
                    //If we pass the AltDirectorySeparatorChar in the ctor, the library will change this to DirectorySeparatorChar
                    if (!drive.Name.ToLower().StartsWith(path.Replace("/", "\\")))
                    {
                        Assert.False(true, string.Format("Error, Wrong value returned. Expected: {1}, Returned: {2}", suffix, path, drive.Name));
                    }
                }

                //We allow any path to use including invalid paths
                tempPath = c.ToString().ToUpper() + String.Format("{0}{1}", ":", "\\");
                path = tempPath + @"bar1\bar2";
                drive = new DriveInfo(path);
                if (!path.StartsWith(drive.Name))
                {
                    Assert.False(true, string.Format("Error, Wrong value returned. Expected: {0}, Returned: {1}", path, drive.Name));
                }

                path = tempPath + @"bar1\bar2\..\bar1bar2";
                drive = new DriveInfo(path);
                if (!path.StartsWith(drive.Name))
                {
                    Assert.False(true, string.Format("Error, Wrong value returned. Expected: {0}, Returned: {1}", path, drive.Name));
                }

                //And invalid characters too
                foreach (Char invalidCh in Path.GetInvalidPathChars())
                {
                    path = tempPath + invalidCh.ToString();
                    Assert.Throws<ArgumentException>(() => { drive = new DriveInfo(path); });
                }
            }

            //invalid drive letter
            foreach (char c in nonDriveLetters)
            {
                drive = new DriveInfo(c.ToString());
                if (!drive.Name.StartsWith(c.ToString()))
                {
                    Assert.False(true, string.Format("Error, Exception not thrown: {0}", c));
                }
            }


            //4) Parm validation: null, empty, UNC shares
            Assert.Throws<ArgumentNullException>(() => { drive = new DriveInfo(null); });
            Assert.Throws<ArgumentException>(() => { drive = new DriveInfo(String.Empty); });
            Assert.Throws<ArgumentException>(() => { drive = new DriveInfo(@"\\user\public"); });
        }
    }
}
