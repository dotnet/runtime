// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:

        public static Match Match(string input);     Noncapturing group : Actual - "(a+)(?:b*)(ccc)"
            "aaabbbccc"

        public static Match Match(string input);     Zero-width positive lookahead assertion: Actual - "abc(?=XXX)\\w+"
            "abcXXXdef"

        public static Match Match(string input);     Zero-width negative lookahead assertion: Actual - "abc(?!XXX)\\w+"
            "abcXXXdef" - Negative

        public static Match Match(string input);     Zero-width positive lookbehind assertion: Actual - "(\\w){6}(?<  =  XXX ) def "
            " abcXXXdef "
        public  static  Match  Match ( string  input ) ; Zero-width  negative  lookbehind  assertion :  Actual - " ( \ \ w ) { 6 } ( ? < ! XXX ) def "
            " XXXabcdef "
        public  static  Match  Match ( string  input ) ; Nonbacktracking  subexpression :  Actual - " [ ^ 0 - 9 ] + ( ?>[0-9]+)3"
            "abc123"

    */

    [Fact]
    public static void RegexMatchTestCase4()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match match;
        String strMatch1 = "aaabbbccc";
        Int32[] iMatch1 =
        {
        0, 9
        }

        ;
        String[] strGroup1 =
        {
        "aaabbbccc", "aaa", "ccc"
        }

        ;
        Int32[,] iGroup1 =
        {
        {
        0, 9
        }

        , {
        0, 3
        }

        , {
        6, 3
        }
        }

        ;
        String[][] strGrpCap1 = new String[3][];
        strGrpCap1[0] = new String[]
        {
        "aaabbbccc"
        }

        ;
        strGrpCap1[1] = new String[]
        {
        "aaa"
        }

        ;
        strGrpCap1[2] = new String[]
        {
        "ccc"
        }

        ;
        Int32[][] iGrpCap1 = new Int32[3][];
        iGrpCap1[0] = new Int32[]
        {
        5, 9
        }

        ; //This is ignored
        iGrpCap1[1] = new Int32[]
        {
        0, 3
        }

        ; //The first half contains the Index and the latter half the Lengths
        iGrpCap1[2] = new Int32[]
        {
        6, 3
        }

        ; //The first half contains the Index and the latter half the Lengths
        String strMatch2 = "abcXXXdef";
        Int32[] iMatch2 =
        {
        0, 9
        }

        ;
        String[] strGroup2 =
        {
        "abcXXXdef"
        }

        ;
        Int32[,] iGroup2 =
        {
        {
        0, 9
        }
        }

        ;
        String[][] strGrpCap2 = new String[1][];
        strGrpCap2[0] = new String[]
        {
        "abcXXXdef"
        }

        ;
        Int32[][] iGrpCap2 = new Int32[1][];
        iGrpCap2[0] = new Int32[]
        {
        0, 9
        }

        ; //This is ignored
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Noncapturing group : Actual - "(a+)(?:b*)(ccc)"
            //"aaabbbccc"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex("(a+)(?:b*)(ccc)");
            match = r.Match("aaabbbccc");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_78653refgs! doesnot match");
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

            // [] public static Match Match(string input);     Zero-width positive lookahead assertion: Actual - "abc(?=XXX)\\w+"
            //"abcXXXdef"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex(@"abc(?=XXX)\w+");
            match = r.Match("abcXXXdef");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_7453efg! doesnot match");
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

                if (match.Groups.Count != 1)
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
                    if (!match.Groups[i].Value.Equals(strGroup2[i]) || (match.Groups[i].Index != iGroup2[i, 0]) || (match.Groups[i].Length != iGroup2[i, 1]) || (match.Groups[i].Captures.Count != 1))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_107vxg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup2[i], iGroup2[i, 0], iGroup2[i, 1]);
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

            // [] public static Match Match(string input);     Zero-width negative lookahead assertion: Actual - "abc(?!XXX)\\w+"
            //"abcXXXdef" - Negative
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex(@"abc(?!XXX)\w+");
            match = r.Match("abcXXXdef");
            if (match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_756tdfg! doesnot match");
            }

            // [] public static Match Match(string input);     Zero-width positive lookbehind assertion: Actual - "(\\w){6}(?<=XXX)def"
            //"abcXXXdef"
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex(@"(\w){6}(?<=XXX)def");
            match = r.Match("abcXXXdef");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_1072gdg! doesnot match");
            }
            else
            {
                Console.WriteLine("\r\nmatch.Value =  " + match.Value + ", Index = " + match.Index + ", Length = " + match.Length);
                Console.WriteLine("\r\nmatch.Captures.Count =  " + match.Captures.Count);
                for (int i = 0; i < match.Captures.Count; i++)
                {
                    Console.WriteLine("\tmatch.Captures[" + i + "].Value =  " + match.Captures[i].Value + ", Index = " + match.Captures[i].Index + ", Length = " + match.Captures[i].Length);
                }

                Console.WriteLine("\r\nmatch.Groups.Count =  " + match.Groups.Count);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    Console.WriteLine("\r\n\tmatch.Groups[" + i + "].Value =  " + match.Groups[i].Value + ", Index = " + match.Groups[i].Index + ", Length = " + match.Groups[i].Length);
                    Console.WriteLine("\tmatch.Groups[" + i + "].Captures.Count =  " + match.Groups[i].Captures.Count);
                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        Console.WriteLine("\t\tmatch.Groups[" + i + "].Captures[" + j + "].Value =  " + match.Groups[i].Captures[j].Value + ", Index = " + match.Groups[i].Captures[j].Index + ", Length = " + match.Groups[i].Captures[j].Length);
                    }
                }
            }

            // [] public static Match Match(string input);     Zero-width negative lookbehind assertion: Actual - "(\\w){6}(?<!XXX)def"
            //"XXXabcdef"
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex(@"(\w){6}(?<!XXX)def");
            match = r.Match("XXXabcdef");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_532resgf! doesnot match");
            }
            else
            {
                Console.WriteLine("\r\nmatch.Value =  " + match.Value + ", Index = " + match.Index + ", Length = " + match.Length);
                Console.WriteLine("\r\nmatch.Captures.Count =  " + match.Captures.Count);
                for (int i = 0; i < match.Captures.Count; i++)
                {
                    Console.WriteLine("\tmatch.Captures[" + i + "].Value =  " + match.Captures[i].Value + ", Index = " + match.Captures[i].Index + ", Length = " + match.Captures[i].Length);
                }

                Console.WriteLine("\r\nmatch.Groups.Count =  " + match.Groups.Count);
                for (int i = 0; i < match.Groups.Count; i++)
                {
                    Console.WriteLine("\r\n\tmatch.Groups[" + i + "].Value =  " + match.Groups[i].Value + ", Index = " + match.Groups[i].Index + ", Length = " + match.Groups[i].Length);
                    Console.WriteLine("\tmatch.Groups[" + i + "].Captures.Count =  " + match.Groups[i].Captures.Count);
                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        Console.WriteLine("\t\tmatch.Groups[" + i + "].Captures[" + j + "].Value =  " + match.Groups[i].Captures[j].Value + ", Index = " + match.Groups[i].Captures[j].Index + ", Length = " + match.Groups[i].Captures[j].Length);
                    }
                }
            }

            // [] public static Match Match(string input);     Nonbacktracking subexpression: Actual - "[^0-9]+(?>[0-9]+)3"
            //"abc123"
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex("[^0-9]+(?>[0-9]+)3");
            //The last 3 causes the match to fail, since the non backtracking subexpression does not give up the last digit it matched
            //for it to be a success. For a correct match, remove the last character, '3' from the pattern
            match = r.Match("abc123");
            if (match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
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