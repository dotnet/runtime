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
    public class GetDrives
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int GetLogicalDrives();

        [Fact]
        public void Test01()
        {
            DriveInfo[] drives;
            int win32Result;
            uint mask;
            BitArray bits;
            List<char> driveLetters;
            Char letter;

            //We will test this API by first getting the drives ourselves via Win32 GetLogicalDrives
            //This is kinds of a non-testable API since the managed API also calls this API!


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
            driveLetters = new List<char>();
            for (int i = 0; i < bits.Length; i++)
            {
                letter = (Char)(65 + i);
                if (bits[i])
                    driveLetters.Add(letter);
            }

            //Scenario 1: Vanilla ensure that it does return all the drives.
            drives = DriveInfo.GetDrives();

            if (drives.Length != driveLetters.Count)
            {
                Assert.False(true, string.Format("Error, Returned Drives number wrong. Expected: {0}, Returned: {1}", driveLetters.Count, drives.Length));
            }

            //Now we make sure of the returned values
            for (int i = 0; i < drives.Length; i++)
            {
                if (!driveLetters.Contains(drives[i].Name[0]))
                {
                    Assert.False(true, string.Format("Error, Drive letter does not match one in Win32. Returned: {0}", drives[i].Name));
                }
                else
                    driveLetters.Remove(drives[i].Name[0]);
            }
        }
    }
}
