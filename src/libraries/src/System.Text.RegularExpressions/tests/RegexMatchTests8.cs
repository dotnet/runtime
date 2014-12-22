// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input);     Using "n" Regex option. Only explicitly named groups should be captured : Actual - "([0-9]*)\\s(?<s>[a-z_A-Z]+)", "n"
            "200 dollars"

        public static Match Match(string input);     Single line mode "s". Includes new line character. : Actual - "([^/]+)","s"
            "abc\n"

        public static Match Match(string input);     "x" option. Removes unescaped white space from the pattern. : Actual - " ([^/]+) ","x"
            "abc"

        public static Match Match(string input);     "x" option. Removes unescaped white space from the pattern. : Actual - "\x20([^/]+)\x20","x"
            "abc"

    */

    [Fact]
    public static void RegexMatchTestCase8()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        Match match;
        String strMatch1 = "200 dollars";
        Int32[] iMatch1 =
        {
        0, 11
        }

        ;
        String[] strGroup1 =
        {
        "200 dollars", "dollars"
        }

        ;
        Int32[,] iGroup1 =
        {
        {
        0, 11
        }

        , {
        4, 7
        }
        }

        ;
        String[][] strGrpCap1 = new String[2][];
        strGrpCap1[0] = new String[]
        {
        "200 dollars"
        }

        ;
        strGrpCap1[1] = new String[]
        {
        "dollars"
        }

        ;
        Int32[][] iGrpCap1 = new Int32[2][];
        iGrpCap1[0] = new Int32[]
        {
        0, 11
        }

        ; //This is ignored
        iGrpCap1[1] = new Int32[]
        {
        4, 7
        }

        ; //The first half contains the Index and the latter half the Lengths
        Int32[] iGrpCapCnt1 =
        {
        1, 1
        }

        ; //0 is ignored
        String strMatch2 = "abc\nsfc";
        Int32[] iMatch2 =
        {
        0, 7
        }

        ;
        String[] strGroup2 =
        {
        "abc\nsfc", "abc\nsfc"
        }

        ;
        Int32[,] iGroup2 =
        {
        {
        0, 7
        }

        , {
        0, 7
        }
        }

        ;
        String[][] strGrpCap2 = new String[2][];
        strGrpCap2[0] = new String[]
        {
        "abc\nsfc"
        }

        ;
        strGrpCap2[1] = new String[]
        {
        "abc\nsfc"
        }

        ;
        Int32[][] iGrpCap2 = new Int32[2][];
        iGrpCap2[0] = new Int32[]
        {
        0, 11
        }

        ; //This is ignored
        iGrpCap2[1] = new Int32[]
        {
        0, 7
        }

        ; //The first half contains the Index and the latter half the Lengths
        Int32[] iGrpCapCnt2 =
        {
        1, 1
        }

        ; //0 is ignored
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Using "n" Regex option. Only explicitly named groups should be captured : Actual - "([0-9]*)\\s(?<s>[a-z_A-Z]+)", "n"
            //"200 dollars"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex(@"([0-9]*)\s(?<s>[a-z_A-Z]+)", RegexOptions.ExplicitCapture);
            match = r.Match("200 dollars");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
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

            // [] public static Match Match(string input);     Single line mode "s". Includes new line character. : Actual - "([^/]+)","s"
            //"abc\n" 
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex("(.*)", RegexOptions.Singleline);
            match = r.Match("abc\nsfc");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
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
                        Console.WriteLine("Err_107vxg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>, CaptureCount = <{6}:{7}>", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup2[i], iGroup2[i, 0], iGroup2[i, 1], match.Groups[i].Captures.Count, iGrpCapCnt2[i]);
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

            // [] public static Match Match(string input);     "x" option. Removes unescaped white space from the pattern. : Actual - " ([^/]+) ","x"
            //"abc"
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex("            ((.)+)      ", RegexOptions.IgnorePatternWhitespace);
            m = r.Match("abc");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_452wfdf! doesnot match");
            }

            // [] public static Match Match(string input);     "x" option. Removes unescaped white space from the pattern. : Actual - "\x20([^/]+)\x20","x"
            //"abc"
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex("\x20([^/]+)\x20\x20\x20\x20\x20\x20\x20", RegexOptions.IgnorePatternWhitespace);
            m = r.Match(" abc       ");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_865rfsg! doesnot match");
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