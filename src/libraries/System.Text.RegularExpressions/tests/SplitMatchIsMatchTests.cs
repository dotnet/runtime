// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class SplitMatchIsMatchTests
{
    /*
        Class: Regex
        Tested Methods:

            public static string[] Split(string input, string pattern); 
                very simple
    
            public static bool IsMatch(string input, string pattern, string options); 
                "m" option with 5 patterns

            public static bool IsMatch(string input, string pattern); 
                "abc", "^b"

            public static Match Match(string input, string pattern);     ???
                "XSP_TEST_FAILURE SUCCESS", ".*\\b(\\w+)\\b"
    */

    [Fact]
    public static void SplitMatchIsMatch()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Match match;
        String[] sa;
        String[] strMatch1 =
        {
        "line2\nline3", "line2\nline3", "line3\n\nline4", "line3\n\nline4", "line2\nline3"
        }

        ;
        Int32[,] iMatch1 =
        {
        {
        6, 11
        }

        , {
        6, 11
        }

        , {
        12, 12
        }

        , {
        12, 12
        }

        , {
        6, 11
        }
        }

        ;
        String[] strGroup1 =
        {
        }

        ;
        String[] strGrpCap1 =
        {
        }

        ;
        String strMatch2 = "XSP_TEST_FAILURE SUCCESS";
        Int32[] iMatch2 =
        {
        0, 24
        }

        ;
        String[] strGroup2 =
        {
        "XSP_TEST_FAILURE SUCCESS", "SUCCESS"
        }

        ;
        Int32[] iGroup2 =
        {
        17, 7
        }

        ;
        String[] strGrpCap2 =
        {
        "SUCCESS"
        }

        ;
        Int32[] iGrpCap2 =
        {
        17, 7
        }

        ;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            try
            {
                // []     public static string[] Split(string input, string pattern); 
                //    very simple
                //-----------------------------------------------------------------
                strLoc = "Loc_498yg";
                iCountTestcases++;
                sa = Regex.Split("word0    word1    word2    word3", "    ");
                for (int i = 0; i < sa.Length; i++)
                {
                    String s = "word" + i;
                    if (String.Compare(sa[i], s) != 0)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_7654fdgd! Fail : [" + s + "] not equal [" + sa[i] + "]");
                    }
                }
                //-----------------------------------------------------------------
            }
            catch
            {
            }

            try
            {
                // [] public static bool IsMatch(string input, string pattern, string options); 
                //"m" option with 5 patterns
                //-----------------------------------------------------------------
                strLoc = "Loc_298vy";
                iCountTestcases++;
                sa = new String[5];
                sa[0] = "(line2$\n)line3";
                sa[1] = "(line2\n^)line3";
                sa[2] = "(line3\n$\n)line4";
                sa[3] = "(line3\n^\n)line4";
                sa[4] = "(line2$\n^)line3";
                for (int ii = 0; ii < sa.Length; ii++)
                {
                    strLoc = "Loc_0002." + ii.ToString();
                    if (!Regex.IsMatch("line1\nline2\nline3\n\nline4", sa[ii], RegexOptions.Multiline))
                    {
                        iCountErrors++;
                        Console.WriteLine("Fail : " + strLoc + " : Pattern not match");
                    }
                    else
                    {
                        match = Regex.Match("line1\nline2\nline3\n\nline4", sa[ii], RegexOptions.Multiline);
                        if (!match.Value.Equals(strMatch1[ii]) || (match.Index != iMatch1[ii, 0]) || (match.Length != iMatch1[ii, 1]) || (match.Captures.Count != 1))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_75234_" + ii + " : unexpected return result");
                        }

                        for (int i = 0; i < match.Captures.Count; i++)
                        {
                            if (!match.Captures[i].Value.Equals(strMatch1[ii]) || (match.Captures[i].Index != iMatch1[ii, 0]) || (match.Captures[i].Length != iMatch1[ii, 1]))
                            {
                                iCountErrors++;
                                Console.WriteLine("Err_8743fsgd_" + ii + "_" + i + " : unexpected return result");
                            }
                        }

                        if (match.Groups.Count != 2)
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_3976dffd! unexpected return result");
                        }

                        for (int i = 0; i < match.Groups.Count; i++)
                        {
                            Console.WriteLine("\r\n    match.Groups[" + i + "].Value =  " + match.Groups[i].Value + ", Index = " + match.Groups[i].Index + ", Length = " + match.Groups[i].Length);
                            Console.WriteLine("    match.Groups[" + i + "].Captures.Count =  " + match.Groups[i].Captures.Count);
                            for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                            {
                                Console.WriteLine("        match.Groups[" + i + "].Captures[" + j + "].Value =  " + match.Groups[i].Captures[j].Value + ", Index = " + match.Groups[i].Captures[j].Index + ", Length = " + match.Groups[i].Captures[j].Length);
                            }
                        }
                    }
                }
                //-----------------------------------------------------------------
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                // [] public static bool IsMatch(string input, string pattern); 
                //"abc", "^b"
                //-----------------------------------------------------------------
                strLoc = "Loc_75rfds";
                iCountTestcases++;
                if (Regex.IsMatch("abc", "^b"))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_7356wgd! Fail : " + strLoc + " : unexpected match");
                }
                //-----------------------------------------------------------------
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                // [] public static Match Match(string input, string pattern);     ???
                //"XSP_TEST_FAILURE SUCCESS", ".*\\b(\\w+)\\b"
                //-----------------------------------------------------------------
                strLoc = "Loc_87423fs";
                iCountTestcases++;
                match = Regex.Match("XSP_TEST_FAILURE SUCCESS", @".*\b(\w+)\b");
                if (!match.Success)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_753rwef! Unexpected results returned");
                }
                else
                {
                    if (!match.Value.Equals(strMatch2) || (match.Index != iMatch2[0]) || (match.Length != iMatch2[1]) || (match.Captures.Count != 1))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_98275dsg: unexpected return result");
                    }

                    //Match.Captures always is Match
                    if (!match.Captures[0].Value.Equals(strMatch2) || (match.Captures[0].Index != iMatch2[0]) || (match.Captures[0].Length != iMatch2[1]))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2046gsg! unexpected return result");
                    }

                    if (match.Groups.Count != 2)
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_75324sg! unexpected return result");
                    }

                    //Group 0 always is the Match
                    if (!match.Groups[0].Value.Equals(strMatch2) || (match.Groups[0].Index != iMatch2[0]) || (match.Groups[0].Length != iMatch2[1]) || (match.Groups[0].Captures.Count != 1))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2046gsg! unexpected return result");
                    }

                    //Group 0's Capture is always the Match
                    if (!match.Groups[0].Captures[0].Value.Equals(strMatch2) || (match.Groups[0].Captures[0].Index != iMatch2[0]) || (match.Groups[0].Captures[0].Length != iMatch2[1]))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2975edg!! unexpected return result");
                    }

                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        if (!match.Groups[i].Value.Equals(strGroup2[i]) || (match.Groups[i].Index != iGroup2[0]) || (match.Groups[i].Length != iGroup2[1]) || (match.Groups[i].Captures.Count != 1))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_1954eg_" + i + "! unexpected return result");
                        }

                        for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                        {
                            if (!match.Groups[i].Captures[j].Value.Equals(strGrpCap2[j]) || (match.Groups[i].Captures[j].Index != iGrpCap2[0]) || (match.Groups[i].Captures[j].Length != iGrpCap2[1]))
                            {
                                iCountErrors++;
                                Console.WriteLine("Err_5072dn_" + i + "_" + j + "!! unexpected return result");
                            }
                        }
                    }
                }
                //-----------------------------------------------------------------
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }
}