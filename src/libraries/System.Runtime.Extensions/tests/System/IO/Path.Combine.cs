// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public static partial class PathTests
    {
        private static readonly char s_separator = Path.DirectorySeparatorChar;

        public static IEnumerable<object[]> Combine_Basic_TestData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { "abc" } };
            yield return new object[] { new string[] { "abc", "def" } };
            yield return new object[] { new string[] { "abc", "def", "ghi", "jkl", "mno" } };
            yield return new object[] { new string[] { "abc" + s_separator + "def", "def", "ghi", "jkl", "mno" } };

            // All paths are empty
            yield return new object[] { new string[] { "" } };
            yield return new object[] { new string[] { "", "" } };
            yield return new object[] { new string[] { "", "", "" } };
            yield return new object[] { new string[] { "", "", "", "" } };
            yield return new object[] { new string[] { "", "", "", "", "" } };

            // Elements are all separated
            yield return new object[] { new string[] { "abc" + s_separator, "def" + s_separator } };
            yield return new object[] { new string[] { "abc" + s_separator, "def" + s_separator, "ghi" + s_separator } };
            yield return new object[] { new string[] { "abc" + s_separator, "def" + s_separator, "ghi" + s_separator, "jkl" + s_separator } };
            yield return new object[] { new string[] { "abc" + s_separator, "def" + s_separator, "ghi" + s_separator, "jkl" + s_separator, "mno" + s_separator } };
        }

        public static IEnumerable<string> Combine_CommonCases_Input_TestData()
        {
            // Any path is rooted (starts with \, \\, A:)
            yield return s_separator + "abc";
            yield return s_separator + s_separator + "abc";

            // Any path is empty (skipped)
            yield return "";

            // Any path is single element
            yield return "abc";
            yield return "abc" + s_separator;

            // Any path is multiple element
            yield return Path.Combine("abc", Path.Combine("def", "ghi"));

            // Wildcard characters
            yield return "*";
            yield return "?";

            // Obscure wildcard characters
            yield return "\"";
            yield return "<";
            yield return ">";
        }

        public static IEnumerable<object[]> Combine_CommonCases_TestData()
        {
            foreach (string testPath in Combine_CommonCases_Input_TestData())
            {
                yield return new object[] { new string[] { testPath } };

                yield return new object[] { new string[] { "abc", testPath } };
                yield return new object[] { new string[] { testPath, "abc" } };

                yield return new object[] { new string[] { "abc", "def", testPath } };
                yield return new object[] { new string[] { "abc", testPath, "def" } };
                yield return new object[] { new string[] { testPath, "abc", "def" } };

                yield return new object[] { new string[] { "abc", "def", "ghi", testPath } };
                yield return new object[] { new string[] { "abc", "def", testPath, "ghi" } };
                yield return new object[] { new string[] { "abc", testPath, "def", "ghi" } };
                yield return new object[] { new string[] { testPath, "abc", "def", "ghi" } };

                yield return new object[] { new string[] { "abc", "def", "ghi", "jkl", testPath } };
                yield return new object[] { new string[] { "abc", "def", "ghi", testPath, "jkl" } };
                yield return new object[] { new string[] { "abc", "def", testPath, "ghi", "jkl" } };
                yield return new object[] { new string[] { "abc", testPath, "def", "ghi", "jkl" } };
                yield return new object[] { new string[] { testPath, "abc", "def", "ghi", "jkl" } };
            }
        }

        [Theory]
        [MemberData(nameof(Combine_Basic_TestData))]
        [MemberData(nameof(Combine_CommonCases_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "New Wildcards support added on Core hasn't ported to NETFX. https://github.com/dotnet/corefx/pull/8669")]
        public static void Combine(string[] paths)
        {
            string expected = string.Empty;
            if (paths.Length > 0) expected = paths[0];
            for (int i = 1; i < paths.Length; i++)
            {
                expected = Path.Combine(expected, paths[i]);
            }

            // Combine(string[])
            Assert.Equal(expected, Path.Combine(paths));

            // Verify special cases
            switch (paths.Length)
            {
                case 2:
                    // Combine(string, string)
                    Assert.Equal(expected, Path.Combine(paths[0], paths[1]));
                    break;

                case 3:
                    // Combine(string, string, string)
                    Assert.Equal(expected, Path.Combine(paths[0], paths[1], paths[2]));
                    break;

                case 4:
                    // Combine(string, string, string, string)
                    Assert.Equal(expected, Path.Combine(paths[0], paths[1], paths[2], paths[3]));
                    break;
            }
        }

        [Fact]
        public static void PathIsNull()
        {
            VerifyException<ArgumentNullException>(null);
        }

        [Fact]
        public static void PathIsNullWihtoutRooted()
        {
            //any path is null without rooted after (ANE)
            CommonCasesException<ArgumentNullException>(null);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithoutRooted()
        {
            CommonCasesException<ArgumentException>("ab\0cd");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithoutRooted_Core()
        {
            Assert.Equal("ab\0cd", Path.Combine("ab\0cd"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithoutRooted_Windows()
        {
            //any path contains invalid character without rooted after (AE)
            CommonCasesException<ArgumentException>("ab|cd");
            CommonCasesException<ArgumentException>("ab\bcd");
            CommonCasesException<ArgumentException>("ab\0cd");
            CommonCasesException<ArgumentException>("ab\tcd");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithoutRooted_Windows_Core()
        {
            Assert.Equal("ab|cd", Path.Combine("ab|cd"));
            Assert.Equal("ab\bcd", Path.Combine("ab\bcd"));
            Assert.Equal("ab\0cd", Path.Combine("ab\0cd"));
            Assert.Equal("ab\tcd", Path.Combine("ab\tcd"));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithRooted()
        {
            //any path contains invalid character with rooted after (AE)
            CommonCasesException<ArgumentException>("ab\0cd", s_separator + "abc");
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithRooted_Core()
        {
            Assert.Equal(s_separator + "abc", Path.Combine("ab\0cd", s_separator + "abc"));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithRooted_Windows()
        {
            //any path contains invalid character with rooted after (AE)
            CommonCasesException<ArgumentException>("ab|cd", s_separator + "abc");
            CommonCasesException<ArgumentException>("ab\bcd", s_separator + "abc");
            CommonCasesException<ArgumentException>("ab\tcd", s_separator + "abc");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Tests Windows-specific invalid paths
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void ContainsInvalidCharWithRooted_Windows_core()
        {
            Assert.Equal(s_separator + "abc", Path.Combine("ab|cd", s_separator + "abc"));
            Assert.Equal(s_separator + "abc", Path.Combine("ab\bcd", s_separator + "abc"));
            Assert.Equal(s_separator + "abc", Path.Combine("ab\tcd", s_separator + "abc"));
        }

        private static void VerifyException<T>(string[] paths) where T : Exception
        {
            Assert.Throws<T>(() => Path.Combine(paths));

            //verify passed as elements case
            if (paths != null)
            {
                Assert.InRange(paths.Length, 1, 5);

                Assert.Throws<T>(() =>
                {
                    switch (paths.Length)
                    {
                        case 0:
                            Path.Combine();
                            break;
                        case 1:
                            Path.Combine(paths[0]);
                            break;
                        case 2:
                            Path.Combine(paths[0], paths[1]);
                            break;
                        case 3:
                            Path.Combine(paths[0], paths[1], paths[2]);
                            break;
                        case 4:
                            Path.Combine(paths[0], paths[1], paths[2], paths[3]);
                            break;
                        case 5:
                            Path.Combine(paths[0], paths[1], paths[2], paths[3], paths[4]);
                            break;
                    }
                });
            }
        }

        private static void CommonCasesException<T>(string testing) where T : Exception
        {
            VerifyException<T>(new string[] { testing });

            VerifyException<T>(new string[] { "abc", testing });
            VerifyException<T>(new string[] { testing, "abc" });

            VerifyException<T>(new string[] { "abc", "def", testing });
            VerifyException<T>(new string[] { "abc", testing, "def" });
            VerifyException<T>(new string[] { testing, "abc", "def" });

            VerifyException<T>(new string[] { "abc", "def", "ghi", testing });
            VerifyException<T>(new string[] { "abc", "def", testing, "ghi" });
            VerifyException<T>(new string[] { "abc", testing, "def", "ghi" });
            VerifyException<T>(new string[] { testing, "abc", "def", "ghi" });

            VerifyException<T>(new string[] { "abc", "def", "ghi", "jkl", testing });
            VerifyException<T>(new string[] { "abc", "def", "ghi", testing, "jkl" });
            VerifyException<T>(new string[] { "abc", "def", testing, "ghi", "jkl" });
            VerifyException<T>(new string[] { "abc", testing, "def", "ghi", "jkl" });
            VerifyException<T>(new string[] { testing, "abc", "def", "ghi", "jkl" });
        }

        private static void CommonCasesException<T>(string testing, string testing2) where T : Exception
        {
            VerifyException<T>(new string[] { testing, testing2 });

            VerifyException<T>(new string[] { "abc", testing, testing2 });
            VerifyException<T>(new string[] { testing, "abc", testing2 });
            VerifyException<T>(new string[] { testing, testing2, "def" });

            VerifyException<T>(new string[] { "abc", "def", testing, testing2 });
            VerifyException<T>(new string[] { "abc", testing, "def", testing2 });
            VerifyException<T>(new string[] { "abc", testing, testing2, "ghi" });
            VerifyException<T>(new string[] { testing, "abc", "def", testing2 });
            VerifyException<T>(new string[] { testing, "abc", testing2, "ghi" });
            VerifyException<T>(new string[] { testing, testing2, "def", "ghi" });

            VerifyException<T>(new string[] { "abc", "def", "ghi", testing, testing2 });
            VerifyException<T>(new string[] { "abc", "def", testing, "ghi", testing2 });
            VerifyException<T>(new string[] { "abc", "def", testing, testing2, "jkl" });
            VerifyException<T>(new string[] { "abc", testing, "def", "ghi", testing2 });
            VerifyException<T>(new string[] { "abc", testing, "def", testing2, "jkl" });
            VerifyException<T>(new string[] { "abc", testing, testing2, "ghi", "jkl" });
            VerifyException<T>(new string[] { testing, "abc", "def", "ghi", testing2 });
            VerifyException<T>(new string[] { testing, "abc", "def", testing2, "jkl" });
            VerifyException<T>(new string[] { testing, "abc", testing2, "ghi", "jkl" });
            VerifyException<T>(new string[] { testing, testing2, "def", "ghi", "jkl" });
        }
    }
}
