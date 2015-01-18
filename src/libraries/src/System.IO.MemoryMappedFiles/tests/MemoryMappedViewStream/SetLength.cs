// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

public class MMVS_SetLength
{
    private int _iCountErrors = 0;
    private int _iCountTestcases = 0;

    [Fact]
    public static void SetLengthTestCases()
    {
        bool bResult = false;
        MMVS_SetLength test = new MMVS_SetLength();

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
            ////////////////////////////////////////////////////////////////////////
            // SetLength()
            ////////////////////////////////////////////////////////////////////////

            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("MMVS_SetLength0", 100))
            {
                using (MemoryMappedViewStream viewStream = mmf.CreateViewStream())
                {
                    long length = viewStream.Length;

                    // throws NotSupportedException
                    _iCountTestcases++;
                    try
                    {
                        viewStream.SetLength(1000);
                        _iCountErrors++;
                        Console.WriteLine("ERROR, No exception thrown, expected NotSupportedException");
                    }
                    catch (NotSupportedException)
                    {
                        // Expected
                    }
                    catch (Exception ex)
                    {
                        _iCountErrors++;
                        Console.WriteLine("ERROR, Unexpected exception, {0}", ex);
                    }

                    // length was unchanged
                    _iCountTestcases++;
                    if (viewStream.Length != length)
                    {
                        _iCountErrors++;
                        Console.WriteLine("ERROR, Length was wrong, expected {0}, got {1}",
                                  length, viewStream.Length);
                    }
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
