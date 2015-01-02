// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexSplitTests
{
    /*
    Tested Methods:

        public static String[] Split(string input);     ":"
            "kkk:lll:mmm:nnn:ooo"

        public static String[] Split(string input, Int32 count);     ":"
            "kkk:lll:mmm:nnn:ooo", 2

        public static String[] Split(string input, Int32 count, Int32 startat);     ":"
            "kkk:lll:mmm:nnn:ooo", 3, 6

    */

    [Fact]
    public static void RegexSplit()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        String s;
        String[] sa;
        String[] saExp = new String[]
        {
        "kkk", "lll", "mmm", "nnn", "ooo"
        }

        ;
        String[] saExp1 = new String[]
        {
        "kkk", "lll:mmm:nnn:ooo"
        }

        ;
        String[] saExp2 = new String[]
        {
        "kkk:lll", "mmm", "nnn:ooo"
        }

        ;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            s = "kkk:lll:mmm:nnn:ooo";
            r = new Regex(":");
            // [] Scenario 1 
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            sa = r.Split(s);
            if (sa.Length != 5)
            {
                iCountErrors++;
                Console.WriteLine("Err_8452esgf! doesnot match");
            }

            for (int i = 0; i < sa.Length; i++)
            {
                if (!sa[i].Equals(saExp[i]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234fsadg! doesnot match {0} Expected - {1} Returned - {2}", i, saExp[i], sa[i]);
                }
            }

            //a count of 0 means split all
            sa = r.Split(s, 0);
            if (sa.Length != 5)
            {
                iCountErrors++;
                Console.WriteLine("Err_765wgdd! doesnot match");
            }

            for (int i = 0; i < sa.Length; i++)
            {
                if (!sa[i].Equals(saExp[i]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0174wfg! doesnot match {0} Expected - {1} Returned - {2}", i, saExp[i], sa[i]);
                }
            }

            // [] public static String[] Split(string input, Int32 count);     ":"
            //"kkk:lll:mmm:nnn:ooo", 2
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            sa = r.Split(s, 2);
            if (sa.Length != 2)
            {
                iCountErrors++;
                Console.WriteLine("Err_2435fsd! doesnot match");
            }

            for (int i = 0; i < sa.Length; i++)
            {
                if (!sa[i].Equals(saExp1[i]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_s2345fs! doesnot match {0} Expected - {1} Returned - {2}", i, saExp1[i], sa[i]);
                }
            }

            // [] public static String[] Split(string input, Int32 count, Int32 startat);     ":"
            //"kkk:lll:mmm:nnn:ooo", 3, 6
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            iCountTestcases++;
            sa = r.Split(s, 3, 6);
            if (sa.Length != 3)
            {
                iCountErrors++;
                Console.WriteLine("Err_4532gdg! does not match");
            }

            for (int i = 0; i < sa.Length; i++)
            {
                if (!sa[i].Equals(saExp2[i]))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_845fd! doesnot match {0} Expected - {1} Returned - {2}", i, saExp2[i], sa[i]);
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