// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RightToLeftMatchStartAtTests
{
    /*
    Tested Methods:
    
        public static Boolean RightToLeft;     

        public static Match Match(string input, Int32 startat);     "aaa"
            "aaabbb", 3

        public static Match Match(string input, Int32 startat);     "aaa", "r"
            "aaabbb", 3

        public static Match Match(string input, Int32 startat);     "AAA", "i"
            "aaabbb", 3
    */

    [Fact]
    public static void RightToLeftMatchStartAt()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        String s;
        Match m;
        Match match;
        String strMatch1 = "aaa";
        Int32[] iMatch1 =
        {
        0, 3
        }

        ;
        String[] strGroup1 =
        {
        "aaa"
        }

        ;
        Int32[,] iGroup1 =
        {
        {
        0, 3
        }
        }

        ;
        String[][] strGrpCap1 = new String[1][];
        strGrpCap1[0] = new String[]
        {
        "aaa"
        }

        ;
        Int32[][] iGrpCap1 = new Int32[1][];
        iGrpCap1[0] = new Int32[]
        {
        5, 9, 3, 3
        }

        ; //This is ignored
        Int32[] iGrpCapCnt1 =
        {
        1, 1
        }

        ; //0 is ignored
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Boolean RightToLeft;     
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex("aaa");
            if (r.RightToLeft)
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
            }

            // [] public static Boolean RightToLeft
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex("aaa", RegexOptions.RightToLeft);
            if (!r.RightToLeft)
            {
                iCountErrors++;
                Console.WriteLine("Err_452wfdf! doesnot match");
            }

            // [] public static Match Match(string input, Int32 startat);     "aaa"
            //"aaabbb", 3
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            s = "aaabbb";
            r = new Regex("aaa");
            m = r.Match(s, 3);
            if (m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
            }

            // [] public static Match Match(string input, Int32 startat);     "aaa", "r"
            //"aaabbb", 3
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            s = "aaabbb";
            r = new Regex("aaa", RegexOptions.RightToLeft);
            match = r.Match(s, 3);
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_865rfsg! doesnot match");
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

                if (match.Groups.Count != 1)
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
                    if (!match.Groups[i].Value.Equals(strGroup1[i]) || (match.Groups[i].Index != iGroup1[i, 0]) || (match.Groups[i].Length != iGroup1[i, 1]) || (match.Groups[i].Captures.Count != iGrpCapCnt1[i]))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_1954eg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>, CaptureCount = <{6}:{7}>", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup1[i], iGroup1[i, 0], iGroup1[i, 1], match.Groups[i].Captures.Count, iGrpCapCnt1[i]);
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

            // []     public static Match Match(string input);     "AAA", "i"
            //"aaabbb", 3
            //-----------------------------------------------------------------
            strLoc = "Loc_3452sdg";
            iCountTestcases++;
            s = "aaabbb";
            r = new Regex("AAA", RegexOptions.IgnoreCase);
            m = r.Match(s);
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_fsdfxcvz! doesnot match");
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