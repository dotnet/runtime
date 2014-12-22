// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input);     Using beginning/end of string chars \A, \Z, ^ : Actual - "\\Aaaa\\w+zzz\\Z"
            "aaaasdfajsdlfjzzz"

        public static Match Match(string input);     Using beginning/end of string chars \A, \Z, ^ : Actual - "\\Aaaa\\w+zzz\\Z"
            "aaaasdfajsdlfjzzza"

        public static Match Match(string input, Int32 startat);     Using beginning/end of string chars \A, \Z, ^ : Actual - "\\Gbbb"
            "aaabbb", 3

        public static Match Match(string input, Int32 startat);     Using beginning/end of string chars \A, \Z, ^ : Actual - "^b"
            "ab", 1

    */

    [Fact]
    public static void RegexMatchTestCase5()
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
            // [] public static Match Match(string input);     Using beginning/end of string chars \A, \Z : Actual - "\\Aaaa\\w+zzz\\Z"
            //"aaaasdfajsdlfjzzz"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex(@"\Aaaa\w+zzz\Z");
            m = r.Match("aaaasdfajsdlfjzzz");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
            }

            // [] public static Match Match(string input);     Using beginning/end of string chars \A, \Z : Actual - "\\Aaaa\\w+zzz\\Z"
            //"aaaasdfajsdlfjzzza"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex(@"\Aaaa\w+zzz\Z");
            m = r.Match("aaaasdfajsdlfjzzza");
            if (m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
            }

            // [] public static Match Match(string input);     Using beginning/end of string chars \A, \Z : Actual - "\\Aaaa\\w+zzz\\Z"
            //"line2\nline3\n"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex(@"\A(line2\n)line3\Z", RegexOptions.Multiline);
            m = r.Match("line2\nline3\n");
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_872nj! doesnot match");
            }

            // [] public static Match Match(string input, Int32 startat);     Using beginning/end of string chars ^ : Actual - "^b"
            //"ab", 1
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex("^b");
            m = r.Match("ab");
            if (m.Success)
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