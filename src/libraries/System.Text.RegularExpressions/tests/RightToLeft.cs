// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RightToLeft
{
    // This test case was added to improve code coverage on methods that have a different code path with the RightToLeft option
    [Fact]
    public static void RightToLeftTests()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo"; // 000ooOooo spooky
        String strValue = String.Empty;
        int iCountErrors = 0;
        Regex r;
        String s;
        Match m;
        MatchCollection mCollection;
        string replace, actualResult, expectedResult;
        string[] splitResult, expectedSplitResult;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            //[]IsMatch(string) with RightToLeft
            strLoc = "Loc_sdfa9849";
            r = new Regex(@"\s+\d+", RegexOptions.RightToLeft);
            s = @"sdf 12sad";
            if (!r.IsMatch(s))
            {
                iCountErrors++;
                Console.WriteLine("Err_16891dfa Expected match");
            }

            s = @" asdf12 ";
            if (r.IsMatch(s))
            {
                iCountErrors++;
                Console.WriteLine("Err_16891dafes Did not expect match");
            }

            //[]Match(string, int, int) with RightToLeft
            strLoc = "Loc_6589aead";
            r = new Regex(@"foo\d+", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo         ";
            m = r.Match(s, 0, s.Length);
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_86847sdae Expected match");
            }

            if (m.Value != "foo4567890")
            {
                iCountErrors++;
                Console.WriteLine("Err_4988awea Expected Value={0} actual={1}", "foo4567890", m.Value);
            }

            m = r.Match(s, 10, s.Length - 10);
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_658asea Expected match");
            }

            if (m.Value != "foo4567890")
            {
                iCountErrors++;
                Console.WriteLine("Err_6488ead Expected Value={0} actual={1}", "foo4567890", m.Value);
            }

            m = r.Match(s, 10, 4);
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_2389eads Expected match");
            }

            if (m.Value != "foo4")
            {
                iCountErrors++;
                Console.WriteLine("Err_65489asdead Expected Value={0} actual={1}", "foo4", m.Value);
            }

            m = r.Match(s, 10, 3);
            if (m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_6489sdfwa Did not expect match");
            }

            m = r.Match(s, 11, s.Length - 11);
            if (m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_64542adesa Did not expect match");
            }

            //[]Matches(string) with RightToLeft
            strLoc = "Loc_49487depmz";
            r = new Regex(@"foo\d+", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo1foo  0987";
            mCollection = r.Matches(s);
            if (mCollection.Count != 2)
            {
                iCountErrors++;
                Console.WriteLine("Err_49889eadf Expected match");
            }
            else
            {
                if (mCollection[0].Value != "foo1")
                {
                    iCountErrors++;
                    Console.WriteLine("Err_8977eade Expected Value={0} actual={1}", "foo1", mCollection[0].Value);
                }

                if (mCollection[1].Value != "foo4567890")
                {
                    iCountErrors++;
                    Console.WriteLine("Err_97884dsae Expected Value={0} actual={1}", "foo4567890", mCollection[0].Value);
                }
            }

            //[]Replace(string, string) with RightToLeft
            strLoc = "Loc_3215dsfg";
            r = new Regex(@"foo\s+", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo         ";
            replace = "bar";
            actualResult = r.Replace(s, replace);
            expectedResult = "0123456789foo4567890bar";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_65489asdead Expected Result={0} actual={1}", expectedResult, actualResult);
            }

            //[]Replace(string, string, int count) with RightToLeft
            strLoc = "Loc_90822safwe";
            r = new Regex(@"\d", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo         ";
            replace = "#";
            actualResult = r.Replace(s, replace, 17);
            expectedResult = "##########foo#######foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_1658asead Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            actualResult = r.Replace(s, replace, 7);
            expectedResult = "0123456789foo#######foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_1238jhioa Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            actualResult = r.Replace(s, replace, 0);
            expectedResult = "0123456789foo4567890foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_56498dafe Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            actualResult = r.Replace(s, replace, -1);
            expectedResult = "##########foo#######foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_80972jnklase Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            //[]Replace(string, MatchEvaluator) with RightToLeft
            strLoc = "Loc_5645eade";
            r = new Regex(@"foo\s+", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo         ";
            replace = "bar";
            actualResult = r.Replace(s, new MatchEvaluator(MyBarMatchEvaluator));
            expectedResult = "0123456789foo4567890bar";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_489894sead Expected Result={0} actual={1}", expectedResult, actualResult);
            }

            //[]Replace(string, string, int count) with RightToLeft
            strLoc = "Loc_6548eada";
            r = new Regex(@"\d", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo         ";
            actualResult = r.Replace(s, new MatchEvaluator(MyPoundMatchEvaluator), 17);
            expectedResult = "##########foo#######foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_6589adsfe Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            actualResult = r.Replace(s, new MatchEvaluator(MyPoundMatchEvaluator), 7);
            expectedResult = "0123456789foo#######foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_65489eadea Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            actualResult = r.Replace(s, new MatchEvaluator(MyPoundMatchEvaluator), 0);
            expectedResult = "0123456789foo4567890foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_56489eafd Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            actualResult = r.Replace(s, new MatchEvaluator(MyPoundMatchEvaluator), -1);
            expectedResult = "##########foo#######foo         ";
            if (actualResult != expectedResult)
            {
                iCountErrors++;
                Console.WriteLine("Err_6487eafd Expected Result='{0}' actual='{1}'", expectedResult, actualResult);
            }

            //[]Split(string) with RightToLeft
            strLoc = "Loc_5645eade";
            r = new Regex(@"foo", RegexOptions.RightToLeft);
            s = @"0123456789foo4567890foo         ";
            splitResult = r.Split(s);
            expectedSplitResult = new string[]
            {
            "0123456789", "4567890", "         "
            }

            ;
            if (splitResult.Length != expectedSplitResult.Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_65489aeee Expected {0} string actual={1}", expectedSplitResult.Length, splitResult.Length);
            }
            else
            {
                for (int i = 0; i < expectedSplitResult.Length; i++)
                {
                    if (splitResult[i] != expectedSplitResult[i])
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_0232jkoas Expected Result={0} actual={1}", expectedSplitResult[i], splitResult[i]);
                    }
                }
            }

            //[]Split(string, int) with RightToLeft
            strLoc = "Loc_5645eade";
            r = new Regex(@"\d", RegexOptions.RightToLeft);
            s = @"1a2b3c4d5e6f7g8h9i0k";
            splitResult = r.Split(s, 11);
            expectedSplitResult = new string[]
            {
            "", "a", "b", "c", "d", "e", "f", "g", "h", "i", "k"
            }

            ;
            if (splitResult.Length != expectedSplitResult.Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_68ead Expected {0} string actual={1}", expectedSplitResult.Length, splitResult.Length);
            }
            else
            {
                for (int i = 0; i < expectedSplitResult.Length; i++)
                {
                    if (splitResult[i] != expectedSplitResult[i])
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_1235eas Expected Result={0} actual={1}", expectedSplitResult[i], splitResult[i]);
                    }
                }
            }

            splitResult = r.Split(s, 10);
            expectedSplitResult = new string[]
            {
            "1a", "b", "c", "d", "e", "f", "g", "h", "i", "k"
            }

            ;
            if (splitResult.Length != expectedSplitResult.Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_66898adejl Expected {0} string actual={1}", expectedSplitResult.Length, splitResult.Length);
            }
            else
            {
                for (int i = 0; i < expectedSplitResult.Length; i++)
                {
                    if (splitResult[i] != expectedSplitResult[i])
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_9648kojk Expected Result={0} actual={1}", expectedSplitResult[i], splitResult[i]);
                    }
                }
            }

            splitResult = r.Split(s, 2);
            expectedSplitResult = new string[]
            {
            "1a2b3c4d5e6f7g8h9i", "k"
            }

            ;
            if (splitResult.Length != expectedSplitResult.Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_66898adejl Expected {0} string actual={1}", expectedSplitResult.Length, splitResult.Length);
            }
            else
            {
                for (int i = 0; i < expectedSplitResult.Length; i++)
                {
                    if (splitResult[i] != expectedSplitResult[i])
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_9648kojk Expected Result={0} actual={1}", expectedSplitResult[i], splitResult[i]);
                    }
                }
            }

            splitResult = r.Split(s, 1);
            expectedSplitResult = new string[]
            {
            "1a2b3c4d5e6f7g8h9i0k"
            }

            ;
            if (splitResult.Length != expectedSplitResult.Length)
            {
                iCountErrors++;
                Console.WriteLine("Err_56987dellp Expected {0} string actual={1}", expectedSplitResult.Length, splitResult.Length);
            }
            else
            {
                for (int i = 0; i < expectedSplitResult.Length; i++)
                {
                    if (splitResult[i] != expectedSplitResult[i])
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_13678jhuy Expected Result={0} actual={1}", expectedSplitResult[i], splitResult[i]);
                    }
                }
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }

        Assert.Equal(0, iCountErrors);
    }

    private static string MyBarMatchEvaluator(Match m)
    {
        return "bar";
    }

    private static string MyPoundMatchEvaluator(Match m)
    {
        return "#";
    }
}