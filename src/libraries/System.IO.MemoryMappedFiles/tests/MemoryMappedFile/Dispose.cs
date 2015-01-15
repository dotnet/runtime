// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using Xunit;

[Collection("Dispose")]
public class MMF_Dispose
{
    private int _iCountErrors = 0;
    private int _iCountTestcases = 0;

    [Fact]
    public static void DisposeTestCases()
    {
        bool bResult = false;
        MMF_Dispose test = new MMF_Dispose();

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
            ////////////////////////////////////////////////////////////////////////
            // Dispose()
            ////////////////////////////////////////////////////////////////////////

            MemoryMappedFile mmf = MemoryMappedFile.CreateNew("Dispose_mapname101", 100);
            MemoryMappedViewStream viewStream = mmf.CreateViewStream();
            MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor();
            mmf.Dispose();

            // new ViewStream cannot be created from a disposed MMF
            try
            {
                _iCountTestcases++;
                mmf.CreateViewStream();
                _iCountErrors++;
                Console.WriteLine("ERROR, Loc001a: No exception thrown, expected ObjectDisposeException");
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _iCountErrors++;
                Console.WriteLine("ERROR, Loc001b: Unexpected exception, {0}", ex);
            }

            // new ViewAccessor cannot be created from a disposed MMF
            try
            {
                _iCountTestcases++;
                mmf.CreateViewAccessor();
                _iCountErrors++;
                Console.WriteLine("ERROR, Loc002a: No exception thrown, expected ObjectDisposeException");
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                _iCountErrors++;
                Console.WriteLine("ERROR, Loc002b: Unexpected exception, {0}", ex);
            }

            // existing views can still be used
            try
            {
                _iCountTestcases++;
                StreamWriter writer = new StreamWriter(viewStream);
                writer.Write('a');
                writer.Flush();

                _iCountTestcases++;
                Char result = viewAccessor.ReadChar(0);
                if (result != 'a')
                {
                    _iCountErrors++;
                    Console.WriteLine("ERROR, Loc010a: ViewAccessor read/write was wrong.  expected 'a', got '{0}'", result);
                }
            }
            catch (Exception ex)
            {
                _iCountErrors++;
                Console.WriteLine("ERROR, Loc010c: Unexpected exception, {0}", ex);
            }

            // Dispose twice succeeds
            try
            {
                _iCountTestcases++;
                mmf.Dispose();
            }
            catch (Exception ex)
            {
                _iCountErrors++;
                Console.WriteLine("ERROR, Loc010c: Unexpected exception, {0}", ex);
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
