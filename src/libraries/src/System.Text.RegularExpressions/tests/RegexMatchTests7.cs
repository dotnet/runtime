// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input);     Alternation constructs : Actual - "(111|aaa)"
            "aaa"

        public static Match Match(string input);      : Actual - "abc(?(1)111|222)"
            "abc222"

        public static Match Match(string input);      : Actual - "(?< 1>\\d+)abc(?(1)222|111)"
            "111abc222"

    */

    [Fact]
    public static void RegexMatchTestCase7()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        Match match;
        String strMatch1 = "abc222";
        Int32[] iMatch1 =
        {
        0, 6
        }

        ;
        String[] strGroup1 =
        {
        "abc222", String.Empty
        }

        ;
        Int32[,] iGroup1 =
        {
        {
        0, 6
        }

        , {
        0, 0
        }
        }

        ;
        String[][] strGrpCap1 = new String[2][];
        strGrpCap1[0] = new String[]
        {
        "abc222"
        }

        ;
        strGrpCap1[1] = new String[]
        {
        String.Empty
        }

        ;
        Int32[][] iGrpCap1 = new Int32[2][];
        iGrpCap1[0] = new Int32[]
        {
        0, 6
        }

        ; //This is ignored
        iGrpCap1[1] = new Int32[]
        {
        0, 0
        }

        ; //The first half contains the Index and the latter half the Lengths
        String strMatch2 = "111abc222";
        Int32[] iMatch2 =
        {
        0, 9
        }

        ;
        String[] strGroup2 =
        {
        "111abc222", "111"
        }

        ;
        Int32[,] iGroup2 =
        {
        {
        0, 9
        }

        , {
        0, 3
        }
        }

        ;
        String[][] strGrpCap2 = new String[2][];
        strGrpCap2[0] = new String[]
        {
        "111abc222"
        }

        ;
        strGrpCap2[1] = new String[]
        {
        "111"
        }

        ;
        Int32[][] iGrpCap2 = new Int32[2][];
        iGrpCap2[0] = new Int32[]
        {
        0, 9
        }

        ; //This is ignored
        iGrpCap2[1] = new Int32[]
        {
        0, 3
        }

        ; //The first half contains the Index and the latter half the Lengths
        Int32[] iGrpCapCnt2 =
        {
        1, 1
        }

        ;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Alternation constructs : Actual - "(111|aaa)"
            //"aaa"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex("(111|aaa)");
            m = r.Match("aaa");
            if (!m.Success || !m.Groups[1].Value.Equals("aaa"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
            }

            // [] public static Match Match(string input);      : Actual - "abc(?(1)111|222)"
            //"abc222"    
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex("(abbc)(?(1)111|222)");
            match = r.Match("abc222");
            if (match.Success)
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

                if (match.Groups.Count != 2)
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
                    if (!match.Groups[i].Value.Equals(strGroup1[i]) || (match.Groups[i].Index != iGroup1[i, 0]) || (match.Groups[i].Length != iGroup1[i, 1]) || (match.Groups[i].Captures.Count != 0))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_1954eg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>, CaptureCount={6}", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup1[i], iGroup1[i, 0], iGroup1[i, 1], match.Groups[i].Captures.Count);
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

            // [] public static Match Match(string input);      : Actual - "(?<1>\\d+)abc(?(1)222|111)"
            //"111abc222"
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex(@"(?<MyDigits>\d+)abc(?(MyDigits)222|111)");
            match = r.Match("111abc222");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_452wfdf! doesnot match");
            }
            else
            {
                if (!match.Value.Equals(strMatch2) || (match.Index != iMatch2[0]) || (match.Length != iMatch2[1]) || (match.Captures.Count != 1))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_827345sdf! unexpected return result");
                }

                //Match.Captures always is Match
                if (!match.Captures[0].Value.Equals(strMatch2) || (match.Captures[0].Index != iMatch2[0]) || (match.Captures[0].Length != iMatch2[1]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_1074sf! unexpected return result");
                }

                if (match.Groups.Count != 2)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2175sgg! unexpected return result");
                }

                //Group 0 always is the Match
                if (!match.Groups[0].Value.Equals(strMatch2) || (match.Groups[0].Index != iMatch2[0]) || (match.Groups[0].Length != iMatch2[1]) || (match.Groups[0].Captures.Count != 1))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_68217fdgs! unexpected return result");
                }

                //Group 0's Capture is always the Match
                if (!match.Groups[0].Captures[0].Value.Equals(strMatch2) || (match.Groups[0].Captures[0].Index != iMatch2[0]) || (match.Groups[0].Captures[0].Length != iMatch2[1]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_139wn!! unexpected return result");
                }

                for (int i = 1; i < match.Groups.Count; i++)
                {
                    if (!match.Groups[i].Value.Equals(strGroup2[i]) || (match.Groups[i].Index != iGroup2[i, 0]) || (match.Groups[i].Length != iGroup2[i, 1]) || (match.Groups[i].Captures.Count != iGrpCapCnt2[i]))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_107vxg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>, CaptureCount = {6}", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup2[i], iGroup2[i, 0], iGroup2[i, 1], match.Groups[i].Captures.Count);
                    }

                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        if (!match.Groups[i].Captures[j].Value.Equals(strGrpCap2[i][j]) || (match.Groups[i].Captures[j].Index != iGrpCap2[i][j]) || (match.Groups[i].Captures[j].Length != iGrpCap2[i][match.Groups[i].Captures.Count + j]))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_745dsgg_" + i + "_" + j + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Captures[j].Value, match.Groups[i].Captures[j].Index, match.Groups[i].Captures[j].Length, strGrpCap2[i][j], iGrpCap2[i][j], iGrpCap2[i][match.Groups[i].Captures.Count + j]);
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