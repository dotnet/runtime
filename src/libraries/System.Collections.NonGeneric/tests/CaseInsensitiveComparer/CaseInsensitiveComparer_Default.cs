// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Globalization;
using Xunit;

public class CaseInsensitiveComparer_Default
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        CaseInsensitiveComparer comparer;
        CaseInsensitiveComparer defaultComparer;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            //[]vanila - Default sets the ComapreInfo to the current culture - CultureInfo.CurrentCulture.CompareInfo
            //There is no easy way to test this other than make sure that string comparison is case insenstive

            iCountTestcases++;
            iCountTestcases++;

            var somePopularCultureNames = new string[] {
                        "cs-CZ","da-DK","de-DE","el-GR","en-US",
                        "es-ES","fi-FI","fr-FR","hu-HU","it-IT",
                        "ja-JP","ko-KR","nb-NO","nl-NL","pl-PL",
                        "pt-BR","pt-PT","ru-RU","sv-SE","tr-TR",
                        "zh-CN","zh-HK","zh-TW" };
            foreach (string cultureName in somePopularCultureNames)
            {
                CultureInfo culture = new CultureInfo(cultureName);
                if (culture == null)
                {
                    continue;
                }
                comparer = new CaseInsensitiveComparer(culture);
                defaultComparer = CaseInsensitiveComparer.Default;

                if (defaultComparer.Compare("hello", "HELLO") != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93745sdg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, defaultComparer.Compare("hello", "HELLO"));
                }

                //same strings should work ok
                if (defaultComparer.Compare("hello", "hello") != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93745sdg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, defaultComparer.Compare("hello", "hello"));
                }

                //different strings should return false
                if (defaultComparer.Compare("hello", "mello") == 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, defaultComparer.Compare("hello", "mello"));
                }

                //What would turkey do? - Since we are comparing with the default culture, unless this is run on a turkish machine, this will pass
                if (CultureInfo.CurrentCulture.Name != "tr-TR")
                {
                    if (defaultComparer.Compare("file", "FILE") != 0)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, defaultComparer.Compare("file", "FILE"));
                    }
                }
                else
                {
                    if (defaultComparer.Compare("file", "FILE") == 0)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>", 0, defaultComparer.Compare("file", "FILE"));
                    }
                }

                //[]Other data types should work as is
                iCountTestcases++;
                if (defaultComparer.Compare(5, 5) != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_347tsfg! wrong value returned");
                }

                if (defaultComparer.Compare(5, 10) == 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_973425sdg! wrong value returned");
                }
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
    public static void ExecuteCaseInsensitiveComparer_Default()
    {
        bool bResult = false;
        var test = new CaseInsensitiveComparer_Default();

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
