// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class IsMatchMatchSplitTests
{
    /*
    Tested Methods:


        public static Boolean IsMatch() variants

        public static Match Match(string input, String pattern, String options);     "abc","[aBc]","i"

        public static String[] Split(string input, String pattern, String options);     "[abc]", "i"
            "1A2B3C4"
    */

    [Fact]
    public static void IsMatchMatchSplit()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String s;
        String[] sa;
        String[] saExp1 =
        {
        "a", "b", "c"
        }

        ;
        String[] saExp2 =
        {
        "1", "2", "3", "4"
        }

        ;
        int i = 0;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static Boolean IsMatch() variants
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            if (!Regex.IsMatch("abc", "abc"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match");
            }

            if (!Regex.IsMatch("abc", "aBc", RegexOptions.IgnoreCase))
            {
                iCountErrors++;
                Console.WriteLine("Err_7432rwe! doesnot match");
            }

            if (!Regex.IsMatch("abc", "aBc", RegexOptions.IgnoreCase))
            {
                iCountErrors++;
                Console.WriteLine("Err_7432rwe! doesnot match");
            }

            strLoc = "Loc_0003";
            iCountTestcases++;
            // [] Scenario 3 
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            s = "1A2B3C4";
            sa = Regex.Split(s, "[abc]", RegexOptions.IgnoreCase);
            for (i = 0; i < sa.Length; i++)
            {
                if (!saExp2[i].Equals(sa[i]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_452wfdf! doesnot match");
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