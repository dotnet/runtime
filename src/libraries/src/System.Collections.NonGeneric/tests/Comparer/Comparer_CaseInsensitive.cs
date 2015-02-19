// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using System.Globalization;
using Xunit;

public class Comparer_CaseInsensitive
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
                ///////////////////////////////////////////////////////////////////

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

                    comp = new Comparer(culture);

                    //The following cultures do this the other way round
                    //da-DK, is-IS, nb-NO, nn-NO
                    if (culture.Name != "da-DK" && culture.Name != "is-IS" && culture.Name != "nb-NO" && culture.Name != "nn-NO")
                    {
                        if (comp.Compare(str1[0], str2[0]) != 1)
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_3245sdg, Wrong value returned, {0}, culture: {1}", comp.Compare(str1[0], str2[0]), culture);
                        }
                    }
                    else
                    {
                        if (comp.Compare(str1[0], str2[0]) != -1)
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_297dg, Wrong value returned, {0}, culture: {1}", comp.Compare(str1[0], str2[0]), culture.Name);
                        }
                    }

                    if (comp.Compare(str1[1], str2[1]) != -1)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_3467tsg, Wrong value returned, {0}, culture: {1}", comp.Compare(str1[1], str2[1]), culture.Name);
                    }
                }

                //[] Call ctor with null CultureInfo
                try
                {
                    comp = new Comparer((CultureInfo)null);
                    iCountErrors++;
                    Console.WriteLine("Err_89743asjppn Expected ctor to throw ArgumentNullException");
                }
                catch (ArgumentNullException) { }
                catch (Exception e)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_4447abcn Unexpected exceptin thrown: {0}", e);
                }
                /////////////////////////// END TESTS /////////////////////////////
            } while (false);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==\n" + exc_general.ToString());
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
    public static void ExecuteComparer_CaseInsensitive()
    {
        bool bResult = false;
        var cbA = new Comparer_CaseInsensitive();

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

