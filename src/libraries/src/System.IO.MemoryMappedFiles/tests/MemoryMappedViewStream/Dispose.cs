// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

public class MMVS_Dispose
{
    [Fact]
    public static void DisposeTestCase()
    {
        bool bResult = false;
        MMVS_Dispose test = new MMVS_Dispose();

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
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, Int16.MaxValue))
            {
                // Dispose twice
                MemoryMappedViewStream view1 = mmf.CreateViewStream();
                view1.Dispose();
                view1.Dispose();
                view1.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERR999: Unexpected exception in runTest, {0}", ex);
            return false;
        }
        return true;
    }
}
