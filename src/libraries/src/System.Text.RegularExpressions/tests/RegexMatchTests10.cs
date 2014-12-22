// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexMatchTests
{
    /*
    Tested Methods:
    
        public static Match Match(string input); First testing pattern "abcabc" : Actual    "(abc){2}"
            " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas &  100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?"

        public static Match Match(string input); Searching for numeric characters : Actual    "[0-9]"
            "12345asdfasdfasdfljkhsda67890"

        public static Match Match(string input); Different pattern specification. This time range of symbols is allowed. : Actual    "[a-z0-9]+"
            "[token1]? GARBAGEtoken2GARBAGE ;token3!"

        public static Match Match(string input); Trying empty string. : Actual    "[a-z0-9]+"
            ""

    */

    [Fact]
    public static void MatchTestCase10()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        String s;
        Int32 i;
        Int32[] iaExp1 =
        {
        0, 1, 2, 3, 4, 24, 25, 26, 27, 28
        }

        ;
        String[] saExp2 =
        {
        "token1", "token2", "token3"
        }

        ;
        Int32[] iaExp2 =
        {
        1, 17, 32
        }

        ;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Match Match(string input); First testing pattern "abcabc" : Actual    "(abc){2}"
            //" !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            r = new Regex("(abc){2}");
            s = " !abcabcasl  dkfjasiduf 12343214-//asdfjzpiouxoifzuoxpicvql23r\\` #$3245,2345278 :asdfas & 100% @daeeffga (ryyy27343) poiweurwabcabcasdfalksdhfaiuyoiruqwer{234}/[(132387 + x)]'aaa''?";
            for (m = r.Match(s); m.Success; m = m.NextMatch())
            {
                if (!m.Groups[0].ToString().Equals("abcabc") && ((m.Groups[0].Index != 2) || (m.Groups[0].Index != 125)))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234fsadg! doesnot match");
                }
            }

            // [] public static Match Match(string input); Searching for numeric characters : Actual    "[0-9]"
            //"12345asdfasdfasdfljkhsda67890"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            r = new Regex("[0-9]");
            s = "12345asdfasdfasdfljkhsda67890";
            i = 0;
            for (m = r.Match(s); m.Success; m = m.NextMatch(), i++)
            {
                if (!Char.IsDigit(m.Groups[0].Value[0]) || m.Groups[0].Index != iaExp1[i])
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234fsadg! doesnot match " + m.Groups[0].Index);
                }
            }

            // [] public static Match Match(string input); Different pattern specification. This time range of symbols is allowed. : Actual    "[a-z0-9]+"
            //"[token1]? GARBAGEtoken2GARBAGE ;token3!"
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            r = new Regex("[a-z0-9]+");
            s = "[token1]? GARBAGEtoken2GARBAGE ;token3!";
            i = 0;
            for (m = r.Match(s); m.Success; m = m.NextMatch(), i++)
            {
                if (!m.Groups[0].ToString().Equals(saExp2[i]) || m.Groups[0].Index != iaExp2[i])
                {
                    iCountErrors++;
                    Console.WriteLine("Err_452wfdf! doesnot match " + m.Groups[0].ToString() + " " + m.Groups[0].Index);
                }
            }

            // [] Scenario 4 
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex("[a-z0-9]+");
            m = r.Match("");
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