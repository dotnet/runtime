// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.Win32.SafeHandles;
using Xunit;

public class MMVS_get_SafeMemoryMappedViewHandle
{
    private int _iCountErrors = 0;
    private int _iCountTestcases = 0;

    [Fact]
    public static void SafeMemoryMappedViewHandleAccessorTestCases()
    {
        bool bResult = false;
        MMVS_get_SafeMemoryMappedViewHandle test = new MMVS_get_SafeMemoryMappedViewHandle();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main.ToString());
        }

        Assert.True(bResult, "One or more test cases failed.");
    }

    public bool runTest()
    {
        try
        {
            // pagefile backed
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, 100))
            {
                using (MemoryMappedViewStream viewStream = mmf.CreateViewStream())
                {
                    _iCountTestcases++;
                    SafeMemoryMappedViewHandle handle = viewStream.SafeMemoryMappedViewHandle;
                }
            }

            // file backed
            String fileText = "Non-empty file for MMF testing.";
            File.WriteAllText("SMMVH_test2.txt", fileText);
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile("SMMVH_test2.txt", FileMode.Open))
            {
                using (MemoryMappedViewStream viewStream = mmf.CreateViewStream())
                {
                    _iCountTestcases++;
                    SafeMemoryMappedViewHandle handle = viewStream.SafeMemoryMappedViewHandle;
                }
            }

            /// END TEST CASES

            if (_iCountErrors == 0)
            {
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
