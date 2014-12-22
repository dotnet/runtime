// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input);     Turning on case insensitive option in mid-pattern : Actual - "aaa(?i:match this)bbb"
            "aaaMaTcH ThIsbbb"

        public static Match Match(string input);     Turning off case insensitive option in mid-pattern : Actual - "aaa(?-i:match this)bbb", "i"
            "AaAmatch thisBBb"

        public static Match Match(string input);     Turning on/off all the options at once : Actual - "aaa(?imnsx-imnsx:match this)bbb", "i"
            "AaAmatch thisBBb"

        public static Match Match(string input);     Comments : Actual - "aaa(?#ignore this completely)bbb"
            "aaabbb"

    */

    [Fact]
    public static void RegexMatchTestCase9()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input);     Turning on case insensitive option in mid-pattern : Actual - "aaa(?i:match this)bbb"
            //"aaaMaTcH ThIsbbb" 
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            if ("i".ToUpper() == "I")
            {
                r = new Regex("aaa(?i:match this)bbb");
                m = r.Match("aaaMaTcH ThIsbbb");
                if (!m.Success)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234fsadg! doesnot match");
                }
            }

            // [] public static Match Match(string input);     Turning off case insensitive option in mid-pattern : Actual - "aaa(?-i:match this)bbb", "i"
            //"AaAmatch thisBBb"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex("aaa(?-i:match this)bbb", RegexOptions.IgnoreCase);
            m = r.Match("AaAmatch thisBBb");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
            }

            // [] public static Match Match(string input);     Turning on/off all the options at once : Actual - "aaa(?imnsx-imnsx:match this)bbb", "i"
            //"AaAmatch thisBBb"
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex("aaa(?-i:match this)bbb", RegexOptions.IgnoreCase);
            m = r.Match("AaAmatcH thisBBb");
            if (m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_452wfdf! doesnot match");
            }

            // [] public static Match Match(string input);     Comments : Actual - "aaa(?#ignore this completely)bbb"
            //"aaabbb"    
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex("aaa(?#ignore this completely)bbb");
            m = r.Match("aaabbb");
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