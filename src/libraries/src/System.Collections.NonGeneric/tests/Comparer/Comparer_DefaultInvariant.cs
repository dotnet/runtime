// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Globalization;
using Xunit;

public class Comparer_DefaultInvariant
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Comparer comp;

        string[] str1 = { "Apple", "abc", };
        string[] str2 = { "Æble", "ABC" };

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////

                //[] Vanilla test case - The TextInfo property of the CultureInfo is used in the CaseInsensitiveHashCodeProvider
                //TextInfo has GetCaseInsensitiveHashCode() methods

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

                    iCountTestcases++;

                    CultureInfo.DefaultThreadCurrentCulture = culture;
                    CultureInfo.DefaultThreadCurrentUICulture = culture;
                    Assert.Equal(CultureInfo.CurrentCulture.Name, culture.Name);
                    Assert.Equal(CultureInfo.CurrentUICulture.Name, culture.Name);

                    comp = Comparer.DefaultInvariant;


                    //all cultures should sort the same way, irrespective of the thread's culture
                    if (comp.Compare(str1[0], str2[0]) != 1)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_39tsfg, Wrong value returned, {0}, culture: {1}", comp.Compare(str1[0], str2[0]), culture.Name);
                    }

                    if (comp.Compare(str1[1], str2[1]) != -1)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_39tsfg, Wrong value returned, {0}, culture: {1}", comp.Compare(str1[1], str2[1]), culture.Name);
                    }

                }
                /////////////////////////// END TESTS /////////////////////////////
            } while (false);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy!  exc_general==\n" + exc_general.ToString());
        }



        ////  Finish Diagnostics

        if (iCountErrors == 0)
        {
            return true;
        }
        else
        {
            Console.WriteLine("Fail! iCountErrors==" + iCountErrors);
            return false;
        }
    }



    [Fact]
    public static void ExecuteComparer_DefaultInvariant()
    {
        bool bResult = false;
        var cbA = new Comparer_DefaultInvariant();

        try
        {
            bResult = cbA.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine(" : FAiL! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.True(bResult);
    }
}

