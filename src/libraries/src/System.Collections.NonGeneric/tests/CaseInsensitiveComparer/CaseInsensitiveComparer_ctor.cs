// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class CaseInsensitiveComparer_ctor
{

    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        CaseInsensitiveComparer comparer;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////

            //[]vanilla - the ctor simply sets the CompareInfo to the current culture - CultureInfo.CurrentCulture.CompareInfo
            //There is no easy way to test this other than make sure that string comparison is case insensitive

            iCountTestcases++;

            comparer = new CaseInsensitiveComparer();

            if (comparer.Compare("hello", "HELLO") != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, comparer.Compare("hello", "HELLO"));
            }

            //same strings should work ok
            if (comparer.Compare("hello", "hello") != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_93745sdg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, comparer.Compare("hello", "hello"));
            }

            //different strings should return false
            if (comparer.Compare("hello", "mello") == 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, comparer.Compare("hello", "mello"));
            }


            //[]Other data types should work as is
            iCountTestcases++;
            if (comparer.Compare(5, 5) != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_347tsfg! wrong value returned");
            }

            if (comparer.Compare(5, 10) == 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_973425sdg! wrong value returned");
            }

            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics

        if (iCountErrors == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    [Fact]
    public static void ExecuteCaseInsensitiveComparer_ctor()
    {
        bool bResult = false;
        var test = new CaseInsensitiveComparer_ctor();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.True(bResult);
    }
}


