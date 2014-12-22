// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input);     Using [a-z], \s, \w : Actual - "([a-zA-Z]+)\\s(\\w+)"
            "David Bau"

        public static Match Match(string input);     \\S, \\d, \\D, \\W: Actual - "(\\S+):\\W(\\d+)\\s(\\D+)"
            "Price: 5 dollars"

        public static Match Match(string input);     \\S, \\d, \\D, \\W: Actual - "[^0-9]+(\\d+)"
            "Price: 30 dollars"

    */

    [Fact]
    public static void RegexMatchTestCase2()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        Match match;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Using [a-z], \s, \w : Actual - "([a-zA-Z]+)\\s(\\w+)"
            //"David Bau"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex(@"([a-zA-Z]+)\s(\w+)");
            match = r.Match("David Bau");
            if (!match.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_7563rfsgf! doesnot match");
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

            // [] public static Match Match(string input);     \\S, \\d, \\D, \\W: Actual - "(\\S+):\\W(\\d+)\\s(\\D+)"
            //"Price: 5 dollars"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex(@"(\S+):\W(\d+)\s(\D+)");
            m = r.Match("Price: 5 dollars");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_75erfdg! doesnot match");
            }

            // [] public static Match Match(string input);     \\S, \\d, \\D, \\W: Actual - "[^0-9]+(\\d+)"
            //"Price: 30 dollars"
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex(@"[^0-9]+(\d+)");
            m = r.Match("Price: 30 dollars");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_5743rfsfg! doesnot match");
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