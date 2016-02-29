// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Xunit;

public class GroupNamesAndNumber
{
    /*
        public string[] GetGroupNames();
        public int[] GetGroupNumbers();
        public string GroupNameFromNumber(int i);
        public int GroupNumberFromName(string name);
    */
    [Fact]
    public static void GroupNamesAndNumberTestCase()
    {
        string s = "Ryan Byington";
        Regex r = new Regex("(?<first_name>\\S+)\\s(?<last_name>\\S+)");
        string[] expectedNames = new string[] { "0", "first_name", "last_name" };
        int[] expectedNumbers = new int[] { 0, 1, 2 };
        string[] expectedGroups = new string[] { "Ryan Byington", "Ryan", "Byington" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex from SDK
        s = "abc208923xyzanqnakl";
        r = new Regex(@"((?<One>abc)\d+)?(?<Two>xyz)(.*)");
        expectedNames = new string[] { "0", "1", "2", "One", "Two" };
        expectedNumbers = new int[] { 0, 1, 2, 3, 4 };
        expectedGroups = new string[] { "abc208923xyzanqnakl", "abc208923", "anqnakl", "abc", "xyz" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex with numeric names
        s = "0272saasdabc8978xyz][]12_+-";
        r = new Regex(@"((?<256>abc)\d+)?(?<16>xyz)(.*)");
        expectedNames = new string[] { "0", "1", "2", "16", "256" };
        expectedNumbers = new int[] { 0, 1, 2, 16, 256 };
        expectedGroups = new string[] { "abc8978xyz][]12_+-", "abc8978", "][]12_+-", "xyz", "abc" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex with numeric names and string names
        s = "0272saasdabc8978xyz][]12_+-";
        r = new Regex(@"((?<4>abc)(?<digits>\d+))?(?<2>xyz)(?<everything_else>.*)");
        expectedNames = new string[] { "0", "1", "2", "digits", "4", "everything_else" };
        expectedNumbers = new int[] { 0, 1, 2, 3, 4, 5 };
        expectedGroups = new string[] { "abc8978xyz][]12_+-", "abc8978", "xyz", "8978", "abc", "][]12_+-" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex with 0 numeric names
        Assert.Throws<ArgumentException>(() => new Regex(@"foo(?<0>bar)"));

        // Regex without closing >
        Assert.Throws<ArgumentException>(() => new Regex(@"foo(?<1bar)"));

        // Duplicate string names
        s = "Ryan Byington";
        r = new Regex("(?<first_name>\\S+)\\s(?<first_name>\\S+)");
        expectedNames = new string[] { "0", "first_name" };
        expectedNumbers = new int[] { 0, 1 };
        expectedGroups = new string[] { "Ryan Byington", "Byington" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Duplicate numeric names
        s = "Ryan Byington";
        r = new Regex("(?<15>\\S+)\\s(?<15>\\S+)");
        expectedNames = new string[] { "0", "15" };
        expectedNumbers = new int[] { 0, 15 };
        expectedGroups = new string[] { "Ryan Byington", "Byington" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        /******************************************************************
        Repeat the same steps from above but using (?'foo') instead
        ******************************************************************/
        // Vanilla
        s = "Ryan Byington";
        r = new Regex("(?'first_name'\\S+)\\s(?'last_name'\\S+)");
        expectedNames = new string[] { "0", "first_name", "last_name" };
        expectedNumbers = new int[] { 0, 1, 2 };
        expectedGroups = new string[] { "Ryan Byington", "Ryan", "Byington" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex from SDK
        s = "abc208923xyzanqnakl";
        r = new Regex(@"((?'One'abc)\d+)?(?'Two'xyz)(.*)");
        expectedNames = new string[] { "0", "1", "2", "One", "Two" };
        expectedNumbers = new int[] { 0, 1, 2, 3, 4 };
        expectedGroups = new string[] { "abc208923xyzanqnakl", "abc208923", "anqnakl", "abc", "xyz" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex with numeric names
        s = "0272saasdabc8978xyz][]12_+-";
        r = new Regex(@"((?'256'abc)\d+)?(?'16'xyz)(.*)");
        expectedNames = new string[] { "0", "1", "2", "16", "256" };
        expectedNumbers = new int[] { 0, 1, 2, 16, 256 };
        expectedGroups = new string[] { "abc8978xyz][]12_+-", "abc8978", "][]12_+-", "xyz", "abc" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex with numeric names and string names
        s = "0272saasdabc8978xyz][]12_+-";
        r = new Regex(@"((?'4'abc)(?'digits'\d+))?(?'2'xyz)(?'everything_else'.*)");
        expectedNames = new string[] { "0", "1", "2", "digits", "4", "everything_else" };
        expectedNumbers = new int[] { 0, 1, 2, 3, 4, 5 };
        expectedGroups = new string[] { "abc8978xyz][]12_+-", "abc8978", "xyz", "8978", "abc", "][]12_+-" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Regex with 0 numeric names
        Assert.Throws<ArgumentException>(() => new Regex(@"foo(?'0'bar)"));

        // Regex without closing >
        Assert.Throws<ArgumentException>(() => new Regex(@"foo(?'1bar)"));

        // Duplicate string names
        s = "Ryan Byington";
        r = new Regex("(?'first_name'\\S+)\\s(?'first_name'\\S+)");
        expectedNames = new string[] { "0", "first_name" };
        expectedNumbers = new int[] { 0, 1 };
        expectedGroups = new string[] { "Ryan Byington", "Byington" };
        
        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);

        // Duplicate numeric names
        s = "Ryan Byington";
        r = new Regex("(?'15'\\S+)\\s(?'15'\\S+)");
        expectedNames = new string[] { "0", "15" } ;
        expectedNumbers = new int[] { 0, 15 };
        expectedGroups = new string[] { "Ryan Byington", "Byington" };

        VerifyGroups(r, s, expectedGroups, expectedNames, expectedNumbers);
    }

    public static void VerifyGroups(Regex regex, string input, string[] expectedGroups, string[] expectedNames, int[] expectedNumbers)
    {
        Match match = regex.Match(input);
        Assert.True(match.Success);

        int[] numbers = regex.GetGroupNumbers();
        Assert.Equal(expectedNumbers.Length, numbers.Length);

        string[] names = regex.GetGroupNames();
        Assert.Equal(expectedNames.Length, names.Length);

        Assert.Equal(expectedGroups.Length, match.Groups.Count);
        for (int i = 0; i < expectedNumbers.Length; i++)
        {
            Assert.Equal(expectedGroups[i], match.Groups[expectedNames[i]].Value);
            Assert.Equal(expectedGroups[i], match.Groups[expectedNumbers[i]].Value);

            Assert.Equal(expectedNumbers[i], numbers[i]);
            Assert.Equal(expectedNumbers[i], regex.GroupNumberFromName(expectedNames[i]));

            Assert.Equal(expectedNames[i], names[i]);
            Assert.Equal(expectedNames[i], regex.GroupNameFromNumber(expectedNumbers[i]));
        }
    }
}
