// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class CaseInsensitiveComparer_Object
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        CaseInsensitiveComparer comparer;
        String str1;
        String str2;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            //[]vanila - the ctor simply sets the ComapreInfo to the current culture - CultureInfo.CurrentCulture.CompareInfo
            //There is no easy way to test this other than make sure that string comparison is case insenstive

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

            //[]we will consider nulls 
            iCountTestcases++;
            if (comparer.Compare(5, null) <= 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_83sdg! wrong value returned, " + comparer.Compare(5, null));
            }

            if (comparer.Compare(null, 5) >= 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_94375sdg! wrong value returned, " + comparer.Compare(null, 5));
            }

            if (comparer.Compare(null, null) != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_94375sdg! wrong value returned, " + comparer.Compare(null, null));
            }

            //[]we will consider nulls with Strings
            iCountTestcases++;
            str1 = "Hello";
            str2 = null;
            if (comparer.Compare(str1, str2) <= 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_948732dsg! wrong value returned, " + comparer.Compare(str1, str2));
            }

            str1 = null;
            str2 = "Hello";
            if (comparer.Compare(str1, str2) >= 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_948732dsg! wrong value returned, " + comparer.Compare(str1, str2));
            }

            str1 = null;
            str2 = null;
            if (comparer.Compare(str1, str2) != 0)
            {
                iCountErrors++;
                Console.WriteLine("Err_948732dsg! wrong value returned, " + comparer.Compare(str1, str2));
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
    public static void ExecuteCaseInsensitiveComparer_Object()
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
    }}


