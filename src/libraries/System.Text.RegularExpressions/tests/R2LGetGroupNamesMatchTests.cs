// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class R2LGetGroupNamesMatchTests
{
    /*
    Tested Methods:

        public static Boolean RightToLeft;

        public static String[] GetGroupNames();     "(?<first_name>\\S+)\\s(?<last_name>\\S+)"

        public static Match Match(string input);
            "David Bau"

        public static Boolean IsMatch(string input);     //D+
            "12321"

    */

    [Fact]
    public static void R2LGetGroupNamesMatch()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        Match m;
        String s;
        String[] names;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            s = "David Bau";
            r = new Regex("(?<first_name>\\S+)\\s(?<last_name>\\S+)");
            // [] public static String[] GetGroupNames();     "(?<first_name>\\S+)\\s(?<last_name>\\S+)"
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            names = r.GetGroupNames();
            if (!names[0].Equals("0"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! unexpected result");
            }

            if (!names[1].Equals("first_name"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! unexpected result");
            }

            if (!names[2].Equals("last_name"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! unexpected result");
            }

            // [] public static Match Match(string input);
            //"David Bau"
            //-----------------------------------------------------------------
            strLoc = "Loc_563sdfg";
            iCountTestcases++;
            m = r.Match(s);
            if (!m.Success)
            {
                iCountErrors++;
                Console.WriteLine("Err_87543! doesnot match");
            }

            // [] public static Match Match(string input);
            //"David Bau"
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            s = "12321";
            r = new Regex(@"\D+");
            if (r.IsMatch(s))
            {
                iCountErrors++;
                Console.WriteLine("Err_fsdf! doesnot match");
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
    }
}