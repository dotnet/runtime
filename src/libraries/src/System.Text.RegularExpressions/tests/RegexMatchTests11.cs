// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:

        public static Match Match(string input);     Numbering pattern slots: "(?< 1>\\d{3})(?< 2>\\d{3})(?< 3>\\d{4})"
            "8885551111"

        public static Match Match(string input);     Not naming pattern slots at all: "^(cat|chat)"
            "cats are bad"

        public static Match Match(string input);      "([0-9]+(\\.[0-9]+){3})"
            "209.25.0.111"

    */

    [Fact]
    public static void RegexMatchTestCase11()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        Match match;
        String s;
        String strMatch1 = "209.25.0.111";
        Int32[] iMatch1 =
        {
        0, 12
        }

        ;
        String[] strGroup1 =
        {
        "209.25.0.111", "209.25.0.111", ".111"
        }

        ;
        Int32[,] iGroup1 =
        {
        {
        0, 12
        }

        , {
        0, 12
        }

        , {
        8, 4
        }
        }

        ;
        String[][] strGrpCap1 = new String[3][];
        strGrpCap1[0] = new String[]
        {
        "209.25.0.111"
        }

        ;
        strGrpCap1[1] = new String[]
        {
        "209.25.0.111"
        }

        ;
        strGrpCap1[2] = new String[]
        {
        ".25", ".0", ".111"
        }

        ;
        Int32[][] iGrpCap1 = new Int32[3][];
        iGrpCap1[0] = new Int32[]
        {
        0, 12
        }

        ; //This is ignored
        iGrpCap1[1] = new Int32[]
        {
        0, 12
        }

        ; //The first half contains the Index and the latter half the Lengths
        iGrpCap1[2] = new Int32[]
        {
        3, 6, 8, 3, 2, 4
        }

        ; //The first half contains the Index and the latter half the Lengths
        Int32[] iGrpCapCnt1 =
        {
        1, 1, 3
        }

        ; //0 is ignored
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Numbering pattern slots: "(?<1>\\d{3})(?<2>\\d{3})(?<3>\\d{4})"
            //"8885551111"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            s = "8885551111";
            r = new Regex(@"(?<1>\d{3})(?<2>\d{3})(?<3>\d{4})");
            m = r.Match(s);
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
            }

            s = "Invalid string";
            m = r.Match(s);
            if (m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_764efdg! doesnot match");
            }

            // [] public static Match Match(string input);     Not naming pattern slots at all: "^(cat|chat)"
            //"cats are bad"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex("^(cat|chat)");
            m = r.Match("cats are bad");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
            }

            // [] public static Match Match(string input);      "([0-9]+(\\.[0-9]+){3})"
            //"209.25.0.111"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            s = "209.25.0.111";
            r = new Regex(@"([0-9]+(\.[0-9]+){3})");
            match = r.Match(s);
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
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

                if (match.Groups.Count != 3)
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