// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class GroupNamesAndNumber
{
    /*
    Tested Methods:

        public string[] GetGroupNames();

        public int[] GetGroupNumbers();

        public string GroupNameFromNumber(int i);

        public int GroupNumberFromName(string name);

    */

    [Fact]
    public static void GroupNamesAndNumberTestCase()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        String s;
        String[] expectedNames;
        String[] expectedGroups;
        int[] expectedNumbers;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            //[]Vanilla
            s = "Ryan Byington";
            r = new Regex("(?<first_name>\\S+)\\s(?<last_name>\\S+)");
            strLoc = "Loc_498yg";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "first_name", "last_name"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2
            }

            ;
            expectedGroups = new String[]
            {
            "Ryan Byington", "Ryan", "Byington"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_79793asdwk! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_12087ahas! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_08712saopz! Unexpected Groups");
            }

            //[]RegEx from SDK
            s = "abc208923xyzanqnakl";
            r = new Regex(@"((?<One>abc)\d+)?(?<Two>xyz)(.*)");
            strLoc = "Loc_0822aws";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "1", "2", "One", "Two"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2, 3, 4
            }

            ;
            expectedGroups = new String[]
            {
            "abc208923xyzanqnakl", "abc208923", "anqnakl", "abc", "xyz"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_79793asdwk! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_12087ahas! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_0822klas! Unexpected Groups");
            }

            //[]RegEx with numeric names
            s = "0272saasdabc8978xyz][]12_+-";
            r = new Regex(@"((?<256>abc)\d+)?(?<16>xyz)(.*)");
            strLoc = "Loc_0982asd";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "1", "2", "16", "256"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2, 16, 256
            }

            ;
            expectedGroups = new String[]
            {
            "abc8978xyz][]12_+-", "abc8978", "][]12_+-", "xyz", "abc"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_79793asdwk! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_12087ahas! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_7072ankla! Unexpected Groups");
            }

            //[]RegEx with numeric names and string names
            s = "0272saasdabc8978xyz][]12_+-";
            r = new Regex(@"((?<4>abc)(?<digits>\d+))?(?<2>xyz)(?<everything_else>.*)");
            strLoc = "Loc_98968asdf";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "1", "2", "digits", "4", "everything_else"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2, 3, 4, 5
            }

            ;
            expectedGroups = new String[]
            {
            "abc8978xyz][]12_+-", "abc8978", "xyz", "8978", "abc", "][]12_+-"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_9496sad! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_6984awsd! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_7072ankla! Unexpected Groups");
            }

            //[]RegEx with 0 numeric names
            try
            {
                r = new Regex(@"foo(?<0>bar)");
                iCountErrors++;
                Console.WriteLine("Err_16891 Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_9877sawa Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx without closing >
            try
            {
                r = new Regex(@"foo(?<1bar)");
                iCountErrors++;
                Console.WriteLine("Err_2389uop Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_3298asoia Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[] Duplicate string names
            s = "Ryan Byington";
            r = new Regex("(?<first_name>\\S+)\\s(?<first_name>\\S+)");
            strLoc = "Loc_sdfa9849";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "first_name"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1
            }

            ;
            expectedGroups = new String[]
            {
            "Ryan Byington", "Byington"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_32189asdd! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_7978assd! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_98732soiya! Unexpected Groups");
            }

            //[] Duplicate numeric names
            s = "Ryan Byington";
            r = new Regex("(?<15>\\S+)\\s(?<15>\\S+)");
            strLoc = "Loc_89198asda";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "15"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 15
            }

            ;
            expectedGroups = new String[]
            {
            "Ryan Byington", "Byington"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_97654awwa! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_6498asde! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_316jkkl! Unexpected Groups");
            }

            /******************************************************************
            Repeat the same steps from above but using (?'foo') instead
            ******************************************************************/
            //[]Vanilla
            s = "Ryan Byington";
            r = new Regex("(?'first_name'\\S+)\\s(?'last_name'\\S+)");
            strLoc = "Loc_0982aklpas";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "first_name", "last_name"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2
            }

            ;
            expectedGroups = new String[]
            {
            "Ryan Byington", "Ryan", "Byington"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_464658safsd! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_15689asda! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_31568kjkj! Unexpected Groups");
            }

            //[]RegEx from SDK
            s = "abc208923xyzanqnakl";
            r = new Regex(@"((?'One'abc)\d+)?(?'Two'xyz)(.*)");
            strLoc = "Loc_98977uouy";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "1", "2", "One", "Two"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2, 3, 4
            }

            ;
            expectedGroups = new String[]
            {
            "abc208923xyzanqnakl", "abc208923", "anqnakl", "abc", "xyz"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_65498yuiy! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_5698yuiyh! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_2168hkjh! Unexpected Groups");
            }

            //[]RegEx with numeric names
            s = "0272saasdabc8978xyz][]12_+-";
            r = new Regex(@"((?'256'abc)\d+)?(?'16'xyz)(.*)");
            strLoc = "Loc_9879hjly";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "1", "2", "16", "256"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2, 16, 256
            }

            ;
            expectedGroups = new String[]
            {
            "abc8978xyz][]12_+-", "abc8978", "][]12_+-", "xyz", "abc"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_21689hjkh! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_2689juj! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_2358adea! Unexpected Groups");
            }

            //[]RegEx with numeric names and string names
            s = "0272saasdabc8978xyz][]12_+-";
            r = new Regex(@"((?'4'abc)(?'digits'\d+))?(?'2'xyz)(?'everything_else'.*)");
            strLoc = "Loc_23189uioyp";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "1", "2", "digits", "4", "everything_else"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1, 2, 3, 4, 5
            }

            ;
            expectedGroups = new String[]
            {
            "abc8978xyz][]12_+-", "abc8978", "xyz", "8978", "abc", "][]12_+-"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_3219hjkj! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_23189aseq! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_2318adew! Unexpected Groups");
            }

            //[]RegEx with 0 numeric names
            try
            {
                r = new Regex(@"foo(?'0'bar)");
                iCountErrors++;
                Console.WriteLine("Err_16891 Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_9877sawa Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[]RegEx without closing >
            try
            {
                r = new Regex(@"foo(?'1bar)");
                iCountErrors++;
                Console.WriteLine("Err_979asja Expected Regex to throw ArgumentException and nothing was thrown");
            }
            catch (ArgumentException)
            {
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_16889asdfw Expected Regex to throw ArgumentException and the following exception was thrown:\n {0}", e);
            }

            //[] Duplicate string names
            s = "Ryan Byington";
            r = new Regex("(?'first_name'\\S+)\\s(?'first_name'\\S+)");
            strLoc = "Loc_2318opa";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "first_name"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 1
            }

            ;
            expectedGroups = new String[]
            {
            "Ryan Byington", "Byington"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_28978adfe! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_3258adsw! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_2198asd! Unexpected Groups");
            }

            //[] Duplicate numeric names
            s = "Ryan Byington";
            r = new Regex("(?'15'\\S+)\\s(?'15'\\S+)");
            strLoc = "Loc_3289hjaa";
            iCountTestcases++;
            expectedNames = new String[]
            {
            "0", "15"
            }

            ;
            expectedNumbers = new int[]
            {
            0, 15
            }

            ;
            expectedGroups = new String[]
            {
            "Ryan Byington", "Byington"
            }

            ;
            if (!VerifyGroupNames(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_13289asd! Unexpected GroupNames");
            }

            if (!VerifyGroupNumbers(r, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_23198asdf! Unexpected GroupNumbers");
            }

            if (!VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers))
            {
                iCountErrors++;
                Console.WriteLine("Err_15689teraku! Unexpected Groups");
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

    public static bool VerifyGroupNames(Regex r, String[] expectedNames, int[] expectedNumbers)
    {
        string[] names = r.GetGroupNames();
        if (names.Length != expectedNames.Length)
        {
            Console.WriteLine("Err_08722aswa! Expect {0} names actual={1}", expectedNames.Length, names.Length);
            return false;
        }

        for (int i = 0; i < expectedNames.Length; i++)
        {
            if (!names[i].Equals(expectedNames[i]))
            {
                Console.WriteLine("Err_09878asfas! Expected GroupNames[{0}]={1} actual={2}", i, expectedNames[i], names[i]);
                return false;
            }

            if (expectedNames[i] != r.GroupNameFromNumber(expectedNumbers[i]))
            {
                Console.WriteLine("Err_6589sdafn!GroupNameFromNumber({0})={1} actual={2}", expectedNumbers[i], expectedNames[i], r.GroupNameFromNumber(expectedNumbers[i]));
                return false;
            }
        }

        return true;
    }

    public static bool VerifyGroupNumbers(Regex r, String[] expectedNames, int[] expectedNumbers)
    {
        int[] numbers = r.GetGroupNumbers();
        if (numbers.Length != expectedNumbers.Length)
        {
            Console.WriteLine("Err_7978awoyp! Expect {0} numbers actual={1}", expectedNumbers.Length, numbers.Length);
            return false;
        }

        for (int i = 0; i < expectedNumbers.Length; i++)
        {
            if (numbers[i] != expectedNumbers[i])
            {
                Console.WriteLine("Err_4342asnmc! Expected GroupNumbers[{0}]={1} actual={2}", i, expectedNumbers[i], numbers[i]);
                return false;
            }

            if (expectedNumbers[i] != r.GroupNumberFromName(expectedNames[i]))
            {
                Console.WriteLine("Err_98795ajkas!GroupNumberFromName({0})={1} actual={2}", expectedNames[i], expectedNumbers[i], r.GroupNumberFromName(expectedNames[i]));
                return false;
            }
        }

        return true;
    }

    public static bool VerifyGroups(Regex r, String s, String[] expectedGroups, String[] expectedNames, int[] expectedNumbers)
    {
        Match m = r.Match(s);
        Group g;
        if (!m.Success)
        {
            Console.WriteLine("Err_08220kha Match not a success");
            return false;
        }

        if (m.Groups.Count != expectedGroups.Length)
        {
            Console.WriteLine("Err_9722asqa! Expect {0} groups actual={1}", expectedGroups.Length, m.Groups.Count);
            return false;
        }

        for (int i = 0; i < expectedNumbers.Length; i++)
        {
            if (null == (g = m.Groups[expectedNames[i]]) || expectedGroups[i] != g.Value)
            {
                Console.WriteLine("Err_3327nkoo! Expected Groups[{0}]={1} actual={2}", expectedNames[i], expectedGroups[i], g == null ? "<null>" : g.Value);
                return false;
            }

            if (null == (g = m.Groups[expectedNumbers[i]]) || expectedGroups[i] != g.Value)
            {
                Console.WriteLine("Err_9465sdjh! Expected Groups[{0}]={1} actual={2}", expectedNumbers[i], expectedGroups[i], g == null ? "<null>" : g.Value);
                return false;
            }
        }

        return true;
    }
}