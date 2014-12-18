// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input, string pattern);     Testing \B special character escape
            "adfadsfSUCCESSadsfadsf", ".*\\B(SUCCESS)\\B.*"

        public static Match Match(string input, string pattern);     Testing octal sequence matches
            "011", "\\060(\\061)?\\061" 

        public static Match Match(string input, string pattern);     Testing hexadecimal sequence matches
            "012", "(\\x30\\x31\\x32)"

        public static Match Match(string input, string pattern);     Testing control character escapes???
            "2", "(\u0032)"

    */

    [Fact]
    public static void RegexMatchTestCase0()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Match match;
        String s;
        String strMatch1 = "adfadsfSUCCESSadsfadsf";
        Int32[] iMatch1 =
        {
        0, 22
        }

        ;
        String[] strGroup1 =
        {
        "adfadsfSUCCESSadsfadsf", "SUCCESS"
        }

        ;
        Int32[] iGroup1 =
        {
        7, 7
        }

        ;
        String[] strGrpCap1 =
        {
        "SUCCESS"
        }

        ;
        Int32[,] iGrpCap1 =
        {
        {
        7, 7
        }
        }

        ;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input, string pattern);     Testing \B special character escape
            //"adfadsfSUCCESSadsfadsf", ".*\\B(SUCCESS)\\B.*"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            match = Regex.Match("adfadsfSUCCESSadsfadsf", @".*\B(SUCCESS)\B.*");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_7356wgd! Fail Do not found a match");
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
                    if (!match.Groups[i].Value.Equals(strGroup1[i]) || (match.Groups[i].Index != iGroup1[0]) || (match.Groups[i].Length != iGroup1[1]) || (match.Groups[i].Captures.Count != 1))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_1954eg_" + i + "! unexpected return result, Value = <{0}:{3}>, Index = <{1}:{4}>, Length = <{2}:{5}>", match.Groups[i].Value, match.Groups[i].Index, match.Groups[i].Length, strGroup1[i], iGroup1[0], iGroup1[1]);
                    }

                    for (int j = 0; j < match.Groups[i].Captures.Count; j++)
                    {
                        if (!match.Groups[i].Captures[j].Value.Equals(strGrpCap1[j]) || (match.Groups[i].Captures[j].Index != iGrpCap1[j, 0]) || (match.Groups[i].Captures[j].Length != iGrpCap1[j, 1]))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_5072dn_" + i + "_" + j + "!! unexpected return result, Value = {0}, Index = {1}, Length = {2}", match.Groups[i].Captures[j].Value, match.Groups[i].Captures[j].Index, match.Groups[i].Captures[j].Length);
                        }
                    }
                }
            }

            // [] public static Match Match(string input, string pattern);     Testing octal sequence matches
            //"011", "\\060(\\061)?\\061"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            //Octal \061 is ASCII 49
            match = Regex.Match("011", @"\060(\061)?\061");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_576trffg! Do not found octal sequence match");
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

            // [] public static Match Match(string input, string pattern);     Testing hexadecimal sequence matches
            //"012", "(\\x30\\x31\\x32)"
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            //Hex \x31 is ASCII 49
            match = Regex.Match("012", @"(\x30\x31\x32)");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_674tgdg! Do not found hexadecimal sequence match");
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

            // [] public static Match Match(string input, string pattern);     Testing control character escapes???
            //"2", "(\u0032)"
            //-----------------------------------------------------------------
            strLoc = "Loc_743gf";
            iCountTestcases++;
            s = "4"; // or "\u0034"
            match = Regex.Match(s, "(\u0034)");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_4532gvfs! Do not found unicode character match");
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
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error: Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }
}