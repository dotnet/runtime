// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class GetGroupNamesTests
    {
        public static IEnumerable<object[]> GetGroupNames_MemberData()
        {
            yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", RegexOptions.None, new string[] { "0", "first_name", "last_name" } };
            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", RegexHelpers.RegexOptionNonBacktracking, new string[] { "0" } };
            }
        }

        [Theory]
        [MemberData(nameof(GetGroupNames_MemberData))]
        public void GetGroupNames(string pattern, RegexOptions options, string[] expectedGroupNames)
        {
            Regex regex = new Regex(pattern, options);
            Assert.Equal(expectedGroupNames, regex.GetGroupNames());
        }

        public static IEnumerable<object[]> GroupNamesAndNumbers_TestData()
        {
            yield return new object[]
            {
                "(?<first_name>\\S+)\\s(?<last_name>\\S+)", "Ryan Byington",
                new string[] { "0", "first_name", "last_name"},
                new int[] { 0, 1, 2 },
                new string[] { "Ryan Byington", "Ryan", "Byington" }
            };

            yield return new object[]
            {
                @"((?<One>abc)\d+)?(?<Two>xyz)(.*)", "abc208923xyzanqnakl",
                new string[] { "0", "1", "2", "One", "Two" },
                new int[] { 0, 1, 2, 3, 4 },
                new string[] { "abc208923xyzanqnakl", "abc208923", "anqnakl", "abc", "xyz" }
            };

            yield return new object[]
            {
                @"((?<256>abc)\d+)?(?<16>xyz)(.*)", "0272saasdabc8978xyz][]12_+-",
                new string[] { "0", "1", "2", "16", "256" },
                new int[] { 0, 1, 2, 16, 256 },
                new string[] { "abc8978xyz][]12_+-", "abc8978", "][]12_+-", "xyz", "abc" }
            };

            yield return new object[]
            {
                @"((?<4>abc)(?<digits>\d+))?(?<2>xyz)(?<everything_else>.*)", "0272saasdabc8978xyz][]12_+-",
                new string[] { "0", "1", "2", "digits", "4", "everything_else" },
                new int[] { 0, 1, 2, 3, 4, 5 },
                new string[] { "abc8978xyz][]12_+-", "abc8978", "xyz", "8978", "abc", "][]12_+-" }
            };

            yield return new object[]
            {
                "(?<first_name>\\S+)\\s(?<first_name>\\S+)", "Ryan Byington",
                new string[] { "0", "first_name" },
                new int[] { 0, 1 },
                new string[] { "Ryan Byington", "Byington" }
            };

            yield return new object[]
            {
                "(?<15>\\S+)\\s(?<15>\\S+)", "Ryan Byington",
                new string[] { "0", "15" },
                new int[] { 0, 15 },
                new string[] { "Ryan Byington", "Byington" }
            };

            yield return new object[]
            {
                "(?'first_name'\\S+)\\s(?'last_name'\\S+)", "Ryan Byington",
                new string[] { "0", "first_name", "last_name" },
                new int[] { 0, 1, 2 },
                new string[] { "Ryan Byington", "Ryan", "Byington" }
            };

            yield return new object[]
            {
                @"((?'One'abc)\d+)?(?'Two'xyz)(.*)", "abc208923xyzanqnakl",
                new string[] { "0", "1", "2", "One", "Two" },
                new int[] { 0, 1, 2, 3, 4 },
                new string[] { "abc208923xyzanqnakl", "abc208923", "anqnakl", "abc", "xyz" }
            };

            yield return new object[]
            {
                @"((?'256'abc)\d+)?(?'16'xyz)(.*)", "0272saasdabc8978xyz][]12_+-",
                new string[] { "0", "1", "2", "16", "256" },
                new int[] { 0, 1, 2, 16, 256 },
                new string[] { "abc8978xyz][]12_+-", "abc8978", "][]12_+-", "xyz", "abc" }
            };

            yield return new object[]
            {
                @"((?'4'abc)(?'digits'\d+))?(?'2'xyz)(?'everything_else'.*)", "0272saasdabc8978xyz][]12_+-",
                new string[] { "0", "1", "2", "digits", "4", "everything_else" },
                new int[] { 0, 1, 2, 3, 4, 5 },
                new string[] { "abc8978xyz][]12_+-", "abc8978", "xyz", "8978", "abc", "][]12_+-" }
            };

            yield return new object[]
            {
                "(?'first_name'\\S+)\\s(?'first_name'\\S+)", "Ryan Byington",
                new string[] { "0", "first_name" },
                new int[] { 0, 1 },
                new string[] { "Ryan Byington", "Byington" }
            };

            yield return new object[]
            {
                "(?'15'\\S+)\\s(?'15'\\S+)", "Ryan Byington",
                new string[] { "0", "15" },
                new int[] { 0, 15 },
                new string[] { "Ryan Byington", "Byington" }
            };

            if (PlatformDetection.IsNetCore)
            {
                yield return new object[]
                {
                    "(?'15'\\S+)\\s(?'15'\\S+)", "Ryan Byington",
                    new string[] { "0" },
                    new int[] { 0 },
                    new string[] { "Ryan Byington" },
                    RegexHelpers.RegexOptionNonBacktracking
                };

                yield return new object[]
                {
                    "(?<first_name>\\S+)\\s(?<last_name>\\S+)", "Ryan Byington",
                    new string[] { "0" },
                    new int[] { 0 },
                    new string[] { "Ryan Byington" },
                    RegexHelpers.RegexOptionNonBacktracking
                };
            }
        }

        [Theory]
        [MemberData(nameof(GroupNamesAndNumbers_TestData))]
        public void GroupNamesAndNumbers(string pattern, string input, string[] expectedNames, int[] expectedNumbers, string[] expectedGroups, RegexOptions options = RegexOptions.None)
        {
            Regex regex = new Regex(pattern, options);
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

        public static IEnumerable<object[]> GroupNameFromNumber_InvalidIndex_ReturnsEmptyString_MemberData()
        {
            yield return new object[] { "foo", 1 };
            yield return new object[] { "foo", -1 };
            yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", -1 };
            yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", 3 };
            yield return new object[] { @"((?<256>abc)\d+)?(?<16>xyz)(.*)", -1 };

            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { "(f)(oo)", 1, RegexHelpers.RegexOptionNonBacktracking };
                yield return new object[] { "(f)(oo)", -1, RegexHelpers.RegexOptionNonBacktracking };
                yield return new object[] { "(f)(oo)", 2, RegexHelpers.RegexOptionNonBacktracking };
            }
        }

        [Theory]
        [MemberData(nameof(GroupNameFromNumber_InvalidIndex_ReturnsEmptyString_MemberData))]
        public void GroupNameFromNumber_InvalidIndex_ReturnsEmptyString(string pattern, int index, RegexOptions options = RegexOptions.None)
        {
            Assert.Same(string.Empty, new Regex(pattern, options).GroupNameFromNumber(index));
        }

        public static IEnumerable<object[]> GroupNumberFromName_InvalidName_ReturnsMinusOne_MemberData()
        {
            yield return new object[] { "foo", "no-such-name" };
            yield return new object[] { "foo", "1" };
            yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", "no-such-name" };
            yield return new object[] { "(?<first_name>\\S+)\\s(?<last_name>\\S+)", "FIRST_NAME" };

            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { "(f)(oo)", "no-such-name", RegexHelpers.RegexOptionNonBacktracking };
                yield return new object[] { "(f)(oo)", "1", RegexHelpers.RegexOptionNonBacktracking };
                yield return new object[] { "(f)(oo)", "2", RegexHelpers.RegexOptionNonBacktracking };
            }
        }

        [Theory]
        [MemberData(nameof(GroupNumberFromName_InvalidName_ReturnsMinusOne_MemberData))]
        public void GroupNumberFromName_InvalidName_ReturnsMinusOne(string pattern, string name, RegexOptions options = RegexOptions.None)
        {
            Assert.Equal(-1, new Regex(pattern, options).GroupNumberFromName(name));
        }

        [Fact]
        public void GroupNumberFromName_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => new Regex("foo").GroupNumberFromName(null));
        }
    }
}
