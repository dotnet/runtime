// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class CaseInsensitiveComparer_CultureInfo
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
            ///////////////////////////////////////////////////////////////////

            //[]vanila - the ctor simply sets the ComapreInfo to the current culture - CultureInfo.CurrentCulture.CompareInfo
            //There is no easy way to test this other than make sure that string comparison is case insenstive

            iCountTestcases++;
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

                if (comparer.Compare("hello", "HELLO") != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93745sdg! wrong value returned. Expected - <{0}>, Returned - <{1}>, Culture: {2}", 0, comparer.Compare("hello", "HELLO"), culture.Name);
                }

                //same strings should work ok
                if (comparer.Compare("hello", "hello") != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_93745sdg! wrong value returned. Expected - <{0}>, Returned - <{1}>, Culture: {2}", 0, comparer.Compare("hello", "hello"), culture.Name);
                }

                //different strings should return false
                if (comparer.Compare("hello", "mello") == 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>, Culture: {2}", "non-zero", comparer.Compare("hello", "mello"), culture.Name);
                }

                // "tr-TR" = "Turkish (Turkey)"
                // Turkish has lower-case and upper-case version of the dotted "i", so the upper case of "i" (U+0069) isn't "I" (U+0049), but rather "İ" (U+0130).
                if (culture.Name != "tr-TR")
                {
                    if (comparer.Compare("file", "FILE") != 0)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>, Culture: {2}", 0, comparer.Compare("file", "FILE"), culture.Name);
                    }
                }
                else
                {
                    if (comparer.Compare("file", "FILE") == 0)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_3846tdfsg! wrong value returned. Expected - <{0}>, Returned - <{1}>, Culture: {2}", "non-zero", comparer.Compare("file", "FILE"), culture.Name);
                    }
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
            }

            //[]parm test
            iCountTestcases++;
            try
            {


                comparer = new CaseInsensitiveComparer(null);

                iCountErrors++;
                Console.WriteLine("Err_9745sdg! Exception not thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception ex)
            {
                iCountErrors++;
                Console.WriteLine("Err_9745sdg! Unexpected exception thrown, " + ex.GetType().Name);
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy!  exc_general==" + exc_general.ToString());
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
    public static void ExecuteCaseInsensitiveComparer_CultureInfo()
    {
        bool bResult = false;
        var test = new CaseInsensitiveComparer_CultureInfo();

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


