// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class RegexConstructorTests
{
    [Fact]
    public static void RegexConstructor()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            //[]RegEx with null expression
            strLoc = "Loc_sdfa9849";
            iCountTestcases++;
            try
            {
                r = new Regex(null, RegexOptions.None);
                iCountErrors++;
                Console.WriteLine("Err_16891 Expected Regex to throw ArgumentNullException and nothing was thrown");
            }
            catch (ArgumentNullException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_9877sawa Expected Regex to throw ArgumentNullException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with negative RegexOptions
            strLoc = "Loc_3198sdf";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", (RegexOptions)(-1));
                iCountErrors++;
                Console.WriteLine("Err_2389asfd Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_898asdf Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with to high RegexOptions
            strLoc = "Loc_23198awd";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", (RegexOptions)0x400);
                iCountErrors++;
                Console.WriteLine("Err_1238sadw Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_6579asdf Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with ECMA RegexOptions with all other valid options
            strLoc = "Loc_3198sdf";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_97878dsaw Expected Regex not to throw and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with ECMA RegexOptions with all other valid options plus RightToLeft
            strLoc = "Loc_9875asd";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);
                iCountErrors++;
                Console.WriteLine("Err_9789swd Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_9489sdjk Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with ECMA RegexOptions with all other valid options plus ExplicitCapture
            strLoc = "Loc_54864jhlt";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
                iCountErrors++;
                Console.WriteLine("Err_6556jhkj Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_2189jhss Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with ECMA RegexOptions with all other valid options plus Singleline
            strLoc = "Loc_9891asfes";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Singleline);
                iCountErrors++;
                Console.WriteLine("Err_3156add Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_456hjhj Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx with ECMA RegexOptions with all other valid options plus IgnorePatternWhitespace
            strLoc = "Loc_23889asddf";
            iCountTestcases++;
            try
            {
                r = new Regex("foo", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace);
                iCountErrors++;
                Console.WriteLine("Err_3568sdae Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_4657dsacd Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
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