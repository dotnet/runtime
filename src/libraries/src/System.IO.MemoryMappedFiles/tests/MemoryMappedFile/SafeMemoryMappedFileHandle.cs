// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;
using Xunit;

public class get_SafeMemoryMappedFileHandle
{
    private int _iCountErrors = 0;
    private int _iCountTestcases = 0;

    [Fact]
    public static void SafeMemoryMappedFileHandleAccessorTestCases()
    {
        bool bResult = false;
        get_SafeMemoryMappedFileHandle test = new get_SafeMemoryMappedFileHandle();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("FAiL! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main.ToString());
        }

        Assert.True(bResult, "One or more test cases failed.");
    }

    public bool runTest()
    {
        try
        {
            // pagefile backed
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("MMF_SMMFH0", 100))
            {
                _iCountTestcases++;
                SafeMemoryMappedFileHandle handle = mmf.SafeMemoryMappedFileHandle;
                if (handle.IsInvalid)
                {
                    _iCountErrors++;
                    Console.WriteLine("ERROR, Loc101: Handle is invalid");
                }
            }

            // file backed
            String fileText = "Non-empty file for MMF testing.";
            File.WriteAllText("test2.txt", fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("test2.txt", FileMode.Open, "MMF_SMMFH1"))
            {
                _iCountTestcases++;
                SafeMemoryMappedFileHandle handle = mmf.SafeMemoryMappedFileHandle;
                if (handle.IsInvalid)
                {
                    _iCountErrors++;
                    Console.WriteLine("ERROR, Loc102: Handle is invalid");
                }
            }

            /// END TEST CASES

            if (_iCountErrors == 0)
            {
                Console.WriteLine("Pass. iCountTestcases==" + _iCountTestcases);
                return true;
            }
            else
            {
                Console.WriteLine("Fail! iCountErrors==" + _iCountErrors);
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR999: Unexpected exception in runTest, {0}", ex);
            return false;
        }
    }
}
