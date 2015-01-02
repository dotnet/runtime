// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:

        public static Match Match(string input);     Using greedy quantifiers : Actual - "(a+)(b*)(c?)"
            "aaabbbccc"

        public static Match Match(string input);     Using lazy quantifiers: Actual - "(d+?)(e*?)(f??)"
            "dddeeefff"

    */

    [Fact]
    public static void RegexMatchTestCase3()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match match;
        String strMatch1 = "aaabbbc";
        Int32[] iMatch1 =
        {
        0, 7
        }

        ;
        String[] strGroup1 =
        {
        "aaabbbc", "aaa", "bbb", "c"
        }

        ;
        Int32[,] iGroup1 =
        {
        {
        0, 7
        }

        , {
        0, 3
        }

        , {
        3, 3
        }

        , {
        6, 1
        }
        }

        ;
        String[][] strGrpCap1 = new String[4][];
        strGrpCap1[0] = new String[]
        {
        "aaabbbc"
        }

        ;
        strGrpCap1[1] = new String[]
        {
        "aaa"
        }

        ;
        strGrpCap1[2] = new String[]
        {
        "bbb"
        }

        ;
        strGrpCap1[3] = new String[]
        {
        "c"
        }

        ;
        Int32[][] iGrpCap1 = new Int32[4][];
        iGrpCap1[0] = new Int32[]
        {
        5, 9, 3, 3
        }

        ; //This is ignored
        iGrpCap1[1] = new Int32[]
        {
        0, 3
        }

        ; //The first half contains the Index and the latter half the Lengths
        iGrpCap1[2] = new Int32[]
        {
        3, 3
        }

        ; //The first half contains the Index and the latter half the Lengths
        iGrpCap1[3] = new Int32[]
        {
        6, 1
        }

        ; //The first half contains the Index and the latter half the Lengths
        String strMatch2 = "d";
        Int32[] iMatch2 =
        {
        0, 1
        }

        ;
        String[] strGroup2 =
        {
        "d", "d", String.Empty, String.Empty
        }

        ;
        Int32[,] iGroup2 =
        {
        {
        0, 1
        }

        , {
        0, 1
        }

        , {
        1, 0
        }

        , {
        1, 0
        }
        }

        ;
        String[][] strGrpCap2 = new String[4][];
        strGrpCap2[0] = new String[]
        {
        "d"
        }

        ;
        strGrpCap2[1] = new String[]
        {
        "d"
        }

        ;
        strGrpCap2[2] = new String[]
        {
        String.Empty
        }

        ;
        strGrpCap2[3] = new String[]
        {
        String.Empty
        }

        ;
        Int32[][] iGrpCap2 = new Int32[4][];
        iGrpCap2[0] = new Int32[]
        {
        0, 1
        }

        ; //This is ignored
        iGrpCap2[1] = new Int32[]
        {
        0, 1
        }

        ; //The first half contains the Index and the latter half the Lengths
        iGrpCap2[2] = new Int32[]
        {
        1, 0
        }

        ; //The first half contains the Index and the latter half the Lengths
        iGrpCap2[3] = new Int32[]
        {
        1, 0
        }

        ; //The first half contains the Index and the latter half the Lengths
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Using greedy quantifiers : Actual - "(a+)(b*)(c?)"
            //"aaabbbccc"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex("(a+)(b*)(c?)");
            match = r.Match("aaabbbccc");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_5743rfsfg! doesnot match");
            }
            else
            {
                if (!match.Value.Equals(strMatch1) || (match.Index != iMatch1[0]) || (match.Length != iMatch1[1]) || (match.Captures.Count != 1))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_98275dsg: unexpected return result");
                }

                //Match.Captures always is Match
                if (!match.Captures[0].Value.Equals(strMatch1) || (match.Captures[0].Index != iMatch1[0]) || (match.Captures[0].Length != iMatch1[1]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2046gsg! unexpected return result");
                }

                if (match.Groups.Count != 4)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_75324sg! unexpected return result");
                }

                //Group 0 always is the Match
                if (!match.Groups[0].Value.Equals(strMatch1) || (match.Groups[0].Index != iMatch1[0]) || (match.Groups[0].Length != iMatch1[1]) || (match.Groups[0].Captures.Count != 1))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2046gsg! unexpected return result");
                }

                //Group 0's Capture is always the Match
                if (!match.Groups[0].Captures[0].Value.Equals(strMatch1) || (match.Groups[0].Captures[0].Index != iMatch1[0]) || (match.Groups[0].Captures[0].Length != iMatch1[1]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2975edg!! unexpected return result");
                }

                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (!match.Groups[i].Value.Equals(strGroup1[i]) || (match.Groups[i].Index != iGroup1[i, 0]) || (match.Groups[i].Length != iGroup1[i, 1]) || (match.Groups[i].Captures.Count != 1))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_1954eg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup1[i], iGroup1[i, 0], iGroup1[i, 1]);
                    }

                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        if (!match.Groups[i].Captures[j].Value.Equals(strGrpCap1[i][j]) || (match.Groups[i].Captures[j].Index != iGrpCap1[i][j]) || (match.Groups[i].Captures[j].Length != iGrpCap1[i][match.Groups[i].Captures.Count + j]))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_5072dn_" + i + "_" + j + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Captures[j].Value, match.Groups[i].Captures[j].Index, match.Groups[i].Captures[j].Length, strGrpCap1[i][j], iGrpCap1[i][j], iGrpCap1[i][match.Groups[i].Captures.Count + j]);
                        }
                    }
                }
            }

            // [] public static Match Match(string input);     Using lazy quantifiers: Actual - "(d+?)(e*?)(f??)"
            //"dddeeefff"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            //Interesting match from this pattern and input. If needed to go to the end of the string change the ? to + in the last lazy quantifier
            r = new Regex("(d+?)(e*?)(f??)");
            match = r.Match("dddeeefff");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_78653refgs! doesnot match");
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

                if (match.Groups.Count != 4)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_873gfsg! unexpected return result");
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
                    if (!match.Groups[i].Value.Equals(strGroup2[i]) || (match.Groups[i].Index != iGroup2[i, 0]) || (match.Groups[i].Length != iGroup2[i, 1]) || (match.Groups[i].Captures.Count != 1))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_845sgds_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup2[i], iGroup2[i, 0], iGroup2[i, 1]);
                    }

                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        if (!match.Groups[i].Captures[j].Value.Equals(strGrpCap2[i][j]) || (match.Groups[i].Captures[j].Index != iGrpCap2[i][j]) || (match.Groups[i].Captures[j].Length != iGrpCap2[i][match.Groups[i].Captures.Count + j]))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_2075egg_" + i + "_" + j + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Captures[j].Value, match.Groups[i].Captures[j].Index, match.Groups[i].Captures[j].Length, strGrpCap2[i][j], iGrpCap2[i][j], iGrpCap2[i][match.Groups[i].Captures.Count + j]);
                        }
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

        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }
}