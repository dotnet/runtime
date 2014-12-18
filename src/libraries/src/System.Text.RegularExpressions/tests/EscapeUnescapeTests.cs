// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class EscapeUnescapeTests
{
    /*
    Tested Methods:
    
        public static String Escape(String str);     round tripping "#$^*+(){}<>\\|. "

        public static String Unescape(string str); 

    */

    [Fact]
    public static void EscapeUnescape()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        String s1;
        String s2;
        String s3;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] public static String Escape(String str);     round tripping "#$^*+(){}<>\\|. "
            //    public static String Unescape(string str); 
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            s1 = "#$^*+(){}<>\\|. ";
            s2 = Regex.Escape(s1);
            s3 = Regex.Unescape(s2);
            if (!s1.Equals(s3))
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