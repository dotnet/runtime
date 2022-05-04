// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;
using System.Linq;

namespace System.IO.Tests
{
    public class Directory_GetFileSystemEntries_str_str : Directory_GetFileSystemEntries_str
    {
        #region Utilities

        public override string[] GetEntries(string dirName)
        {
            return Directory.GetFileSystemEntries(dirName, "*");
        }

        public virtual string[] GetEntries(string dirName, string searchPattern)
        {
            return Directory.GetFileSystemEntries(dirName, searchPattern);
        }

        #endregion

        #region UniversalTests

        [Fact]
        public void SearchPatternNull()
        {
            Assert.Throws<ArgumentNullException>(() => GetEntries(TestDirectory, null));
        }

        [Fact]
        public void SearchPatternEmpty()
        {
            // To avoid OS differences we have decided not to throw an argument exception when empty
            // string passed. But we should return 0 items.
            Assert.Empty(GetEntries(TestDirectory, string.Empty));
        }

        [Fact]
        public void SearchPatternValid()
        {
            Assert.Empty(GetEntries(TestDirectory, "a..b abc..d")); //Should not throw
        }

        [Fact]
        public void SearchPatternDotIsStar()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.CreateSubdirectory("TestDir1");
            testDir.CreateSubdirectory("TestDir2");
            using (File.Create(Path.Combine(testDir.FullName, "TestFile1")))
            using (File.Create(Path.Combine(testDir.FullName, "TestFile2")))
            {
                string[] strArr = GetEntries(testDir.FullName, ".");
                if (TestFiles)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "TestFile1"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestFile2"), strArr);
                }
                if (TestDirectories)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir1"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir2"), strArr);
                }
            }
        }

        [Fact]
        public void SearchPatternWithTrailingStar()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.CreateSubdirectory("TestDir1");
            testDir.CreateSubdirectory("TestDir2");
            testDir.CreateSubdirectory("TestDir3");
            using (File.Create(Path.Combine(testDir.FullName, "TestFile1")))
            using (File.Create(Path.Combine(testDir.FullName, "TestFile2")))
            using (File.Create(Path.Combine(testDir.FullName, "Test1File2")))
            using (File.Create(Path.Combine(testDir.FullName, "Test1Dir2")))
            {
                string[] strArr = GetEntries(testDir.FullName, "Test1*");
                if (TestFiles)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1File2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1Dir2"), strArr);
                }

                strArr = GetEntries(testDir.FullName, "*");
                if (TestFiles)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "TestFile1"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestFile2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1File2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1Dir2"), strArr);
                }
                if (TestDirectories)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir1"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir3"), strArr);
                }
            }
        }

        [Fact]
        public void SearchPatternWithLeadingStar()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            testDir.CreateSubdirectory("TestDir1");
            testDir.CreateSubdirectory("TestDir2");
            testDir.CreateSubdirectory("TestDir3");
            using (File.Create(Path.Combine(testDir.FullName, "TestFile1")))
            using (File.Create(Path.Combine(testDir.FullName, "TestFile2")))
            using (File.Create(Path.Combine(testDir.FullName, "Test1File2")))
            using (File.Create(Path.Combine(testDir.FullName, "Test1Dir2")))
            {
                string[] strArr = GetEntries(testDir.FullName, "*2");
                if (TestFiles)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1Dir2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1File2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestFile2"), strArr);
                }
                if (TestDirectories)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir2"), strArr);
                }

                strArr = GetEntries(testDir.FullName, "*Dir*");
                if (TestFiles)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1Dir2"), strArr);
                }
                if (TestDirectories)
                {
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir1"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir2"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestDir3"), strArr);
                }
            }
        }

        [Fact]
        public void SearchPatternByExtension()
        {
            if (TestFiles)
            {
                DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
                using (File.Create(Path.Combine(testDir.FullName, "TestFile1.txt")))
                using (File.Create(Path.Combine(testDir.FullName, "TestFile2.xxt")))
                using (File.Create(Path.Combine(testDir.FullName, "Test1File2.txt")))
                using (File.Create(Path.Combine(testDir.FullName, "Test1Dir2.txx")))
                {
                    string[] strArr = GetEntries(testDir.FullName, "*.txt");
                    Assert.Equal(2, strArr.Length);
                    Assert.Contains(Path.Combine(testDir.FullName, "TestFile1.txt"), strArr);
                    Assert.Contains(Path.Combine(testDir.FullName, "Test1File2.txt"), strArr);
                }
            }
        }

        [Fact]
        public void SearchPatternExactMatch()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Directory.CreateDirectory(Path.Combine(testDir.FullName, "AAA"));
            Directory.CreateDirectory(Path.Combine(testDir.FullName, "AAAB"));
            Directory.CreateDirectory(Path.Combine(testDir.FullName, "CAAA"));
            using (File.Create(Path.Combine(testDir.FullName, "AAABB")))
            using (File.Create(Path.Combine(testDir.FullName, "AAABBC")))
            using (File.Create(Path.Combine(testDir.FullName, "CAAABB")))
            {
                if (TestFiles)
                {
                    string[] results = GetEntries(testDir.FullName, "AAABB");
                    Assert.Equal(1, results.Length);
                    Assert.Contains(Path.Combine(testDir.FullName, "AAABB"), results);
                }
                if (TestDirectories)
                {
                    string[] results = GetEntries(testDir.FullName, "AAA");
                    Assert.Equal(1, results.Length);
                    Assert.Contains(Path.Combine(testDir.FullName, "AAA"), results);
                }
            }
        }

        [Fact]
        public void SearchPatternIgnoreSubDirectories()
        {
            // Shouldn't get files on full path by default
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            Directory.CreateDirectory(Path.Combine(testDir.FullName, GetTestFileName()));
            using (File.Create(Path.Combine(testDir.FullName, GetTestFileName())))
            using (File.Create(Path.Combine(TestDirectory, GetTestFileName())))
            {
                string[] results = GetEntries(TestDirectory, Path.Combine(testDir.Name, "*"));
                if (TestDirectories && TestFiles)
                    Assert.Equal(2, results.Length);
                else
                    Assert.Equal(1, results.Length);
            }
        }

        [Theory,
            // '[' should not matter, but is special to Unix matching APIs
            InlineData(
                "[foo]",
                new string[] { @"f", @"o", @"o", @"foo", @"[foo]" },
                new string[] { @"[foo]" }),
            ]
        public void PatternTests_UnixPatterns(string pattern, string[] sourceFiles, string[] expected)
        {
            string testDir = PrepareDirectory(sourceFiles);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        [Theory,
            // Question marks collapse (right) to periods
            InlineData(
                "f???.txt",
                new string[] { @"f.txt", @"foo.txt", @"foob.txt", @"fooba.txt", @"foobar.txt" },
                new string[] { @"f.txt", @"foo.txt", @"foob.txt" }),
            // Question marks don't collapse to !periods
            InlineData(
                "???b??.txt",
                new string[] { @"f.txt", @"foo.txt", @"foob.txt", @"fooba.txt", @"foobar.txt" },
                new string[] { @"foob.txt", @"fooba.txt", @"foobar.txt" }),
            // Question marks collapse (right) to end
            InlineData(
                "foo.t??",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo.t", @"foo.tx", @"foo.txt" }),
            ]
        public void PatternTests_DosQM(string pattern, string[] sourceFiles, string[] expected)
        {
            // Question marks always collapse right to periods or the end of the string if they are contiguous
            string testDir = PrepareDirectory(sourceFiles);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        [Theory,
            // Periods are optional if left of ? and end of match
            InlineData(
                "foo.???",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt" }),
            // Periods are optional if left of ? and end of match
            InlineData(
                "foo.???.?.?.?",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt" }),
            // Periods are optional if left of ? and end of match
            InlineData(
                "foo.?.??.???.?",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t" }),
            // Periods are optional if left of ? and end of match
            InlineData(
                "foo.??.???.?",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx" }),
            // Periods are optional if left of * and end of match, question marks collapse (right) to end
            InlineData(
                "foo.*??",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"foo.txxt" }),
            // Periods are optional if left of * and end of match, question marks collapse (right) to end
            InlineData(
                "foo.*??*",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"foo.txxt" })
                ]
        public void PatternTests_DosDotQm(string pattern, string[] sourceFiles, string[] expected)
        {
            // Tests for collapsing question marks and DOS_DOT, ", which is what periods get changed to when they are followed by a '?' or '*'.
            string testDir = PrepareDirectory(sourceFiles);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        [Theory,
            // Periods are optional if left of * and end of match
            InlineData(
                "foo.*",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"foo.txxt" }),
            // Periods are optional if left of * and end of match
            InlineData(
                "foo.*.*.*.*",
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.t", @"foo.tx", @"foo.txt", @"foo.txxt" })
            ]
        public void PatternTests_DosDot(string pattern, string[] sourceFiles, string[] expected)
        {
            // Tests for DOS_DOT, ", which is what periods get changed to when they are followed by a '?' or '*'.
            string testDir = PrepareDirectory(sourceFiles);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        // Can't do these without extended path support on Windows, UsingNewNormalization filters appropriately
        [ConditionalTheory(nameof(UsingNewNormalization)),
            // Periods are optional if left of * or ? and end of match
            InlineData(
                "foo.*",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"foo.txxt" }),
            InlineData(
                "foo.*.*",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"foo.txxt" }),
            InlineData(
                "foo.?",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t" }),
            InlineData(
                "foo.??",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx" }),
            InlineData(
                "foo.?.?",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t" }),
            InlineData(
                "foo.??.??",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx" }),
            InlineData(
                "foo.?.*",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t" }),
            InlineData(
                "foo.??.*",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo.txxt" },
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx" }),
            ]
        public void PatternTests_DosDotTrailingDot(string pattern, string[] sourceFiles, string[] expected)
        {
            // Tests for DOS_DOT, ", which is what periods get changed to when they are followed by a '?' or '*'.
            // We don't want to eat trailing space/periods in this test
            string testDir = PrepareDirectory(sourceFiles, useExtendedPaths: true);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        [Theory,
            InlineData(
                "foo*.",
                new string[] { @"foo", @"foobar", @"foo.bar" },
                new string[] { @"foo", @"foobar" })
                ]
        public void PatternTests_DosStar(string pattern, string[] sourceFiles, string[] expected)
        {
            // Tests for DOS_STAR, which only occurs when the source pattern ends in *.
            string testDir = PrepareDirectory(sourceFiles);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        // Can't do these without extended path support on Windows, UsingNewNormalization filters appropriately
        [ConditionalTheory(nameof(UsingNewNormalization)),
            InlineData(
                "foo*.",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo..", @"foo...", @"foo .", @"foo. . .", @"foo. t" },
                new string[] { @"foo", @"foo .", @"foo.", @"foo..", @"foo...", @"foo. . ." }),
            InlineData(
                "foodies*.",
                new string[] { @"foodies.", @"foodies. ", @"foodies.  " },
                new string[] { @"foodies." }),
            InlineData(
                "foodies*.",
                new string[] { @"foodies. ", @"foodies.  ", @"foodies.   " },
                new string[] { }),
            InlineData(
                "foooooo*.",
                new string[] { @"foooooo.", @"foooooo. ", @"foooooo.  " },
                new string[] { @"foooooo." }),
            InlineData(
                "foooooo*.",
                new string[] { @"foooooo. ", @"foooooo.  ", @"foooooo.   " },
                new string[] { }),
            InlineData(
                "foodies*.",
                new string[] { @"foodies.", @"foodies. ", @"foodies.  ", @"foodies.   ", @"foodies.    ", @"foodies.     " },
                new string[] { @"foodies." }),
            InlineData(
                "foodies*.",
                new string[] { @"foodies. ", @"foodies.  ", @"foodies.   ", @"foodies.    ", @"foodies.     " },
                new string[] { }),
            InlineData(
                "foo*.",
                new string[] { @"foo..", @"foo...", @"foo....", @"foo.....", @"foo......" },
                new string[] { @"foo..", @"foo...", @"foo....", @"foo.....", @"foo......" }),
            ]
        public void PatternTests_DosStarSpace(string pattern, string[] sourceFiles, string[] expected)
        {
            // Tests for DOS_STAR, which only occurs when the source pattern ends in *. These are the subset of tests
            // with trailing spaces that work as documented.

            // We don't want to eat trailing space/periods in this test
            string testDir = PrepareDirectory(sourceFiles, useExtendedPaths: true);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        [OuterLoop("These are pretty corner, don't need to run all the time.")]
        [Theory,
            // "foo*." actually becomes "foo<" when passed to NT. It matches all characters up to, and including, the final period.
            //
            // There is a "bug" somewhere in the Windows stack where *some* files with trailing spaces after the final period will be returned when
            // using "*." at the end of a string (which becomes "<"). According to the rules (and the actual pattern matcher used FsRtlIsNameInExpression)
            // *nothing* should match after the final period.
            //
            // We've made Core effectively call RtlIsNameInExpression directly, so this test validates the normally buggy cases. See the test above
            // for what Windows really does. These are super obscure and the bug pattern isn't obvious so we're just going with "correct".
            InlineData(
                "foo*.",
                new string[] { @"foo", @"foo.", @"foo.t", @"foo.tx", @"foo.txt", @"bar.txt", @"foo..", @"foo...", @"foo. ", @"foo.  ", @"foo .", @"foo. . .", @"foo. t" },
                new string[] { @"foo", @"foo.", @"foo..", @"foo...", @"foo .", @"foo. . ." }),
            InlineData(
                "*.",
                new string[] { @"foo. ", @"foo.  ", @"foo..", @"foo. t" },
                new string[] { @"foo.." }),
            InlineData(
                "f*.",
                new string[] { @"foo. ", @"foo.  ", @"foo..", @"foo. t" },
                new string[] { @"foo.." }),
            InlineData(
                "fo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo..", @"foo. t" },
                new string[] { @"foo.." }),
            InlineData(
                "foo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo.   ", @"foo.    " },
                new string[] { }),
            InlineData(
                "foo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo." },
                new string[] { @"foo." }),
            InlineData(
                "foo*.",
                new string[] { @"foo.", @"foo. ", @"foo.  ", @"foo.   ", @"foo.    " },
                new string[] { @"foo." }),
            InlineData(
                "foo*.",
                new string[] { @"foo.", @"foo", @"foo. ", @"foo.  ", @"foo.   ", @"foo.    " },
                new string[] { @"foo.", @"foo" }),
            InlineData(
                "foo*.",
                new string[] { @"foo.", @"foo. ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo" },
                new string[] { @"foo.", @"foo" }),
            InlineData(
                "foo*.",
                new string[] { @"foo.    ", @"foo", @"foo.", @"foo. ", @"foo.  ", @"foo.   " },
                new string[] { @"foo.", @"foo" }),
            InlineData(
                "foo*.",
                new string[] { @"foo.    ", @"foo", @"food", @"foo.", @"foo. ", @"foo.  ", @"foo.   " },
                new string[] { @"foo.", @"foo", @"food" }),
            InlineData(
                "fo*.",
                new string[] { @"foo.", @"foo. ", @"foo.  ", @"foo.   ", @"foo.    " },
                new string[] { @"foo." }),
            InlineData(
                "foo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo.     " },
                new string[] { }),
            InlineData(
                "foo*.",
                new string[] { @"foo. ", @"foo. .", @"foo. . ", @"foo. . .", @"foo. . . " },
                new string[] { @"foo. .", @"foo. . ." }),
            InlineData(
                "foo*.",
                new string[] { @"foo. ", @"foo. .", @"foo.. .", @"foo.... .", @"foo..... ." },
                new string[] { @"foo. .", @"foo.. .", @"foo.... .", @"foo..... ." }),
            InlineData(
                "fo*.",
                new string[] { @"foo. ", @"foo. .", @"foo. . ", @"foo. . .", @"foo. . . " },
                new string[] { @"foo. .", @"foo. . ."}),
            InlineData(
                "foo*.",
                new string[] { @"foo.", @"foo. ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo.     " },
                new string[] { @"foo." }),
            InlineData(
                "food*.",
                new string[] { @"food.", @"food. ", @"food.  ", @"food.   ", @"food.    ", @"food.     " },
                new string[] { @"food." }),
            InlineData(
                "food*.",
                new string[] { @"food.", @"food. ", @"food.  ", @"food.   ", @"food.    ", @"food.     ", @"foodi." },
                new string[] { @"food.", @"foodi." }),
            InlineData(
                "foodi*.",
                new string[] { @"foodi.", @"foodi. ", @"foodi.  ", @"foodi.   ", @"foodi.    ", @"foodi.     " },
                new string[] { @"foodi." }),
            InlineData(
                "foodie*.",
                new string[] { @"foodie.", @"foodie. ", @"foodie.  ", @"foodie.   ", @"foodie.    ", @"foodie.     " },
                new string[] { @"foodie." }),
            InlineData(
                "fooooo*.",
                new string[] { @"foooooo.", @"foooooo. ", @"foooooo.  " },
                new string[] { @"foooooo." }),
            InlineData(
                "fooooo*.",
                new string[] { @"foooooo. ", @"foooooo.  ", @"foooooo.   " },
                new string[] { }),
            InlineData(
                "fo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo.     " },
                new string[] { }),
            InlineData(
                "fo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo.     ", @"foo.      ", @"foo.       " },
                new string[] { }),
            InlineData(
                "fo*.",
                new string[] { @"fo. ", @"fo.  ", @"fo.   ", @"fo.    ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo.     ", @"foo.      ", @"foo.       " },
                new string[] { }),
            InlineData(
                "fo*.",
                new string[] { @"fo. ", @"fo.  ", @"fo.   ", @"fo.    ", @"fo.     ", @"fo.      ", @"foo.  ", @"foo.   ", @"foo.    ", @"foo.     ", @"foo.      ", @"foo.       " },
                new string[] { }),
            InlineData(
                "foo*.",
                new string[] { @"foo. ", @"foo.  ", @"foo..", @"foo. t", @"foo.   ", @"foo.    " },
                new string[] { @"foo.." }),
            ]
        public void PatternTests_DosStarOddSpace_Core(string pattern, string[] sourceFiles, string[] expected)
        {
            // Tests for DOS_STAR, which only occurs when the source pattern ends in *.
            // These cases don't match documented behavior on Windows- matching *should* end at the final period.

            // We don't want to eat trailing space/periods in this test
            string testDir = PrepareDirectory(sourceFiles, useExtendedPaths: true);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        private string PrepareDirectory(string[] sourceFiles, bool useExtendedPaths = false)
        {
            string testDir = Directory.CreateDirectory(GetTestFilePath()).FullName;

            foreach (string file in sourceFiles)
                CreateItem(useExtendedPaths && PlatformDetection.IsWindows
                    ? @"\\?\" + Path.Combine(testDir, file)
                    : Path.Combine(testDir, file));

            return testDir;
        }

        private void ValidatePatternMatch(string[] expected, string[] result)
        {
            Assert.Equal(expected.OrderBy(s => s), result.Select(Path.GetFileName).OrderBy(s => s));
        }

        #endregion

        #region PlatformSpecific

        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [Theory,
            InlineData(
                @"foo\bar",
                new string[] { @"foo", @"bar", @"foo\bar" },
                new string[] { @"foo\bar" }),
            ]
        public void PatternTests_UnixEscape(string pattern, string[] sourceFiles, string[] expected)
        {
            // We shouldn't be allowing escaping in Unix filename searches
            string testDir = PrepareDirectory(sourceFiles);
            ValidatePatternMatch(expected, GetEntries(testDir, pattern));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsSearchPatternLongSegment()
        {
            // Create a path segment longer than the normal max of 255
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string longName = new string('k', 257);

            GetEntries(testDir.FullName, longName);
        }

        [ConditionalFact(nameof(AreAllLongPathsAvailable))]
        public void SearchPatternLongPath()
        {
            // Create a destination path longer than the traditional Windows limit of 256 characters
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

            string longName = new string('k', 254);
            string longFullname = Path.Combine(testDir.FullName, longName);

            if (TestFiles)
            {
                using (File.Create(longFullname)) { }
            }
            else
            {
                Directory.CreateDirectory(longFullname);
            }

            string[] results = GetEntries(testDir.FullName, longName);
            Assert.Contains(longFullname, results);
        }

        [Fact]
        public void SearchPatternWithDoubleDots_Core()
        {
            // Search pattern with double dots no longer throws ArgumentException
            string directory = Directory.CreateDirectory(GetTestFilePath()).FullName;
            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(directory, Path.Combine("..ab ab.. .. abc..d", "abc..")));
            GetEntries(directory, "..");
            GetEntries(directory, @".." + Path.DirectorySeparatorChar);

            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(directory, Path.Combine("..ab ab.. .. abc..d", "abc", "..")));
            GetEntries(directory, Path.Combine("..ab ab.. .. abc..d", "..", "abc"));
            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(directory, Path.Combine("..", "..ab ab.. .. abc..d", "abc")));
            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(directory, Path.Combine("..", "..ab ab.. .. abc..d", "abc") + Path.DirectorySeparatorChar));
        }

        private static char[] OldWildcards = new char[] { '*', '?' };
        private static char[] NewWildcards = new char[] { '<', '>', '\"' };

        [Fact]
        public void SearchPatternInvalid_Core()
        {
            GetEntries(TestDirectory, "|");

            Assert.All(Path.GetInvalidFileNameChars().Except(OldWildcards).Except(NewWildcards), invalidChar =>
            {
                switch (invalidChar)
                {
                    case '\\':
                        if (PlatformDetection.IsWindows)
                        {
                            Assert.Throws<DirectoryNotFoundException>(() => GetEntries(TestDirectory, string.Format("te{0}st", invalidChar.ToString())));
                        }
                        else
                        {
                            GetEntries(TestDirectory, string.Format("te{0}st", invalidChar.ToString()));
                        }
                        break;

                    case '/':
                        Assert.Throws<DirectoryNotFoundException>(() => GetEntries(TestDirectory, string.Format("te{0}st", invalidChar.ToString())));
                        break;

                    case '\0':
                        Assert.Throws<ArgumentException>(() => GetEntries(TestDirectory, "\0"));
                        break;

                    default:
                        GetEntries(TestDirectory, string.Format("te{0}st", invalidChar.ToString()));
                        break;
                }
            });
        }

        [Fact] // .NET Core doesn't throw on wildcards
        public void WindowsSearchPatternInvalid_Wildcards_netcoreapp()
        {
            Assert.All(OldWildcards, invalidChar =>
            {
                GetEntries(TestDirectory, string.Format("te{0}st", invalidChar.ToString()));
            });
            Assert.All(NewWildcards, invalidChar =>
            {
                GetEntries(TestDirectory, string.Format("te{0}st", invalidChar.ToString()));
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // ? in search pattern returns results
        public virtual void WindowsSearchPatternQuestionMarks()
        {
            string testDir1Str = GetTestFileName();
            DirectoryInfo testDir = new DirectoryInfo(TestDirectory);
            testDir.CreateSubdirectory(testDir1Str);

            using (File.Create(Path.Combine(TestDirectory, testDir1Str, GetTestFileName())))
            using (File.Create(Path.Combine(TestDirectory, GetTestFileName())))
            {
                string[] results = GetEntries(TestDirectory, string.Format("{0}.???", new string('?', GetTestFileName().Length)));
                if (TestFiles && TestDirectories)
                    Assert.Equal(2, results.Length);
                else
                    Assert.Equal(1, results.Length);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Whitespace in search pattern returns nothing
        public void WindowsSearchPatternWhitespace()
        {
            Assert.Empty(GetEntries(TestDirectory, "           "));
            Assert.Empty(GetEntries(TestDirectory, "\n"));
            Assert.Empty(GetEntries(TestDirectory, " "));
            Assert.Empty(GetEntries(TestDirectory, "\t"));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCaseSensitiveOS))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67853", TestPlatforms.tvOS)]
        public void SearchPatternCaseSensitive()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testBase = GetTestFileName();
            testDir.CreateSubdirectory(testBase + "aBBBBBBBBBBb");
            testDir.CreateSubdirectory(testBase + "aBBBBBBBBBBB");

            File.Create(Path.Combine(testDir.FullName, testBase + "AAAAAAAAAA")).Dispose();
            File.Create(Path.Combine(testDir.FullName, testBase + "aAAAAAAAAa")).Dispose();

            if (TestDirectories)
            {
                Assert.Equal(2, GetEntries(testDir.FullName, "*BBBBBBBB*").Length);
            }
            if (TestFiles)
            {
                Assert.Equal(2, GetEntries(testDir.FullName, "*AAAAAAAA*").Length);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsCaseInsensitiveOS))]
        public void SearchPatternCaseInsensitive()
        {
            DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
            string testBase = GetTestFileName();
            testDir.CreateSubdirectory(testBase + "yZZZZZZZZZZz");
            testDir.CreateSubdirectory(testBase + "yZZZZZZZZZZZ");

            File.Create(Path.Combine(testDir.FullName, testBase + "YYYYYYYYYYYY")).Dispose();
            File.Create(Path.Combine(testDir.FullName, testBase + "yYYYYYYYYYYy")).Dispose();

            if (TestDirectories)
            {
                Assert.Equal(1, GetEntries(testDir.FullName, "*ZZZZZZZZZZ*").Length);
            }
            if (TestFiles)
            {
                Assert.Equal(1, GetEntries(testDir.FullName, "*YYYYYYYYYY*").Length);
            }
        }

        [Theory,
            InlineData("         "),
            InlineData(" "),
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix-valid chars in file search patterns
        public void UnixSearchPatternFileValidChar(string valid)
        {
            if (TestFiles)
            {
                DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());
                File.Create(Path.Combine(testDir.FullName, valid)).Dispose();

                Assert.Contains(Path.Combine(testDir.FullName, valid), GetEntries(testDir.FullName, valid));
            }
        }

        [Theory,
            InlineData("         "),
            InlineData(" "),
            InlineData("\n"),
            InlineData(">"),
            InlineData("<"),
            InlineData("\t")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix-valid chars in directory search patterns
        public void UnixSearchPatternDirectoryValidChar(string valid)
        {
            if (TestDirectories)
            {
                DirectoryInfo testDir = Directory.CreateDirectory(GetTestFilePath());

                testDir.CreateSubdirectory(valid);

                Assert.Contains(Path.Combine(testDir.FullName, valid), GetEntries(testDir.FullName, valid));
            }
        }

        #endregion
    }
}
