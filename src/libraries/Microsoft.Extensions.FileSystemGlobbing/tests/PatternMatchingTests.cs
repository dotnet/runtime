// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests
{
    public class PatternMatchingTests
    {
        [Fact]
        public void EmptyCollectionWhenNoFilesPresent()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("alpha.txt")
                .Execute();

            scenario.AssertExact();
        }

        [Fact]
        public void MatchingFileIsFound()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("alpha.txt")
                .Files("alpha.txt")
                .Execute();

            scenario.AssertExact("alpha.txt");
        }

        [Fact]
        public void MismatchedFileIsIgnored()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("alpha.txt")
                .Files("omega.txt")
                .Execute();

            scenario.AssertExact();
        }

        [Fact]
        public void FolderNamesAreTraversed()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("beta/alpha.txt")
                .Files("beta/alpha.txt")
                .Execute();

            scenario.AssertExact("beta/alpha.txt");
        }

        [Theory]
        [InlineData(@"beta/alpha.txt", @"beta/alpha.txt")]
        [InlineData(@"beta\alpha.txt", @"beta/alpha.txt")]
        [InlineData(@"beta/alpha.txt", @"beta\alpha.txt")]
        [InlineData(@"beta\alpha.txt", @"beta\alpha.txt")]
        [InlineData(@"\beta\alpha.txt", @"beta\alpha.txt")]
        public void SlashPolarityIsIgnored(string includePattern, string filePath)
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include(includePattern)
                .Files("one/two.txt", filePath, "three/four.txt")
                .Execute();

            scenario.AssertExact("beta/alpha.txt");
        }

        [Theory]
        [InlineData(@"*.txt", new[] { "alpha.txt", "beta.txt" })]
        [InlineData(@"alpha.*", new[] { "alpha.txt" })]
        [InlineData(@"*.*", new[] { "alpha.txt", "beta.txt", "gamma.dat" })]
        [InlineData(@"*", new[] { "alpha.txt", "beta.txt", "gamma.dat" })]
        [InlineData(@"*et*", new[] { "beta.txt" })]
        [InlineData(@"b*et*t", new[] { "beta.txt" })]
        [InlineData(@"b*et*x", new string[0])]
        public void PatternMatchingWorks(string includePattern, string[] matchesExpected)
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include(includePattern)
                .Files("alpha.txt", "beta.txt", "gamma.dat")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }

        [Theory]
        [InlineData(@"1234*5678", new[] { "12345678" })]
        [InlineData(@"12345*5678", new string[0])]
        [InlineData(@"12*3456*78", new[] { "12345678" })]
        [InlineData(@"12*23*", new string[0])]
        [InlineData(@"*67*78", new string[0])]
        [InlineData(@"*45*56", new string[0])]
        public void PatternBeginAndEndCantOverlap(string includePattern, string[] matchesExpected)
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include(includePattern)
                .Files("12345678")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }


        [Theory]
        [InlineData(@"*mm*/*", new[] { "gamma/hello.txt" })]
        [InlineData(@"/*mm*/*", new[] { "gamma/hello.txt" })]
        [InlineData(@"*alpha*/*", new[] { "alpha/hello.txt" })]
        [InlineData(@"/*alpha*/*", new[] { "alpha/hello.txt" })]
        [InlineData(@"*/*", new[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"/*/*", new[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"*.*/*", new[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"/*.*/*", new[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        public void PatternMatchingWorksInFolders(string includePattern, string[] matchesExpected)
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include(includePattern)
                .Files("alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }

        [Theory]
        [InlineData(@"", new string[] { })]
        [InlineData(@"./", new string[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"./alpha/hello.txt", new string[] { "alpha/hello.txt" })]
        [InlineData(@"./**/hello.txt", new string[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"././**/hello.txt", new string[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"././**/./hello.txt", new string[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"././**/./**/hello.txt", new string[] { "alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt" })]
        [InlineData(@"./*mm*/hello.txt", new string[] { "gamma/hello.txt" })]
        [InlineData(@"./*mm*/*", new string[] { "gamma/hello.txt" })]
        public void PatternMatchingCurrent(string includePattern, string[] matchesExpected)
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include(includePattern)
                .Files("alpha/hello.txt", "beta/hello.txt", "gamma/hello.txt")
                .Execute();

            scenario.AssertExact(matchesExpected);
        }

        [Fact]
        public void StarDotStarIsSameAsStar()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*.*")
                .Files("alpha.txt", "alpha.", ".txt", ".", "alpha", "txt")
                .Execute();

            scenario.AssertExact("alpha.txt", "alpha.", ".txt", ".", "alpha", "txt");
        }

        [Fact]
        public void IncompletePatternsDoNotInclude()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*/*.txt")
                .Files("one/x.txt", "two/x.txt", "x.txt")
                .Execute();

            scenario.AssertExact("one/x.txt", "two/x.txt");
        }

        [Fact]
        public void IncompletePatternsDoNotExclude()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*/*.txt")
                .Exclude("one/hello.txt")
                .Files("one/x.txt", "two/x.txt")
                .Execute();

            scenario.AssertExact("one/x.txt", "two/x.txt");
        }

        [Fact]
        public void TrailingRecursiveWildcardMatchesAllFiles()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("one/**")
                .Files("one/x.txt", "two/x.txt", "one/x/y.txt")
                .Execute();

            scenario.AssertExact("one/x.txt", "one/x/y.txt");
        }

        [Fact]
        public void LeadingRecursiveWildcardMatchesAllLeadingPaths()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("**/*.cs")
                .Files("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs")
                .Files("one/x.txt", "two/x.txt", "one/two/x.txt", "x.txt")
                .Execute();

            scenario.AssertExact("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs");
        }

        [Fact]
        public void InnerRecursiveWildcardMuseStartWithAndEndWith()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("one/**/*.cs")
                .Files("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs")
                .Files("one/x.txt", "two/x.txt", "one/two/x.txt", "x.txt")
                .Execute();

            scenario.AssertExact("one/x.cs", "one/two/x.cs");
        }


        [Fact]
        public void ExcludeMayEndInDirectoryName()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*.cs", "*/*.cs", "*/*/*.cs")
                .Exclude("bin", "one/two")
                .Files("one/x.cs", "two/x.cs", "one/two/x.cs", "x.cs", "bin/x.cs", "bin/two/x.cs")
                .Execute();

            scenario.AssertExact("one/x.cs", "two/x.cs", "x.cs");
        }


        [Fact]
        public void RecursiveWildcardSurroundingContainsWith()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("**/x/**")
                .Files("x/1", "1/x/2", "1/x", "x", "1", "1/2")
                .Execute();

            scenario.AssertExact("x/1", "1/x/2");
        }


        [Fact]
        public void SequentialFoldersMayBeRequired()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("a/b/**/1/2/**/2/3/**")
                .Files("1/2/2/3/x", "1/2/3/y", "a/1/2/4/2/3/b", "a/2/3/1/2/b")
                .Files("a/b/1/2/2/3/x", "a/b/1/2/3/y", "a/b/a/1/2/4/2/3/b", "a/b/a/2/3/1/2/b")
                .Execute();

            scenario.AssertExact("a/b/1/2/2/3/x", "a/b/a/1/2/4/2/3/b");
        }

        [Fact]
        public void RecursiveAloneIncludesEverything()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("**")
                .Files("1/2/2/3/x", "1/2/3/y")
                .Execute();

            scenario.AssertExact("1/2/2/3/x", "1/2/3/y");
        }

        [Fact]
        public void ExcludeCanHaveSurroundingRecursiveWildcards()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("**")
                .Exclude("**/x/**")
                .Files("x/1", "1/x/2", "1/x", "x", "1", "1/2")
                .Execute();

            scenario.AssertExact("1/x", "x", "1", "1/2");
        }

        [Fact]
        public void LeadingDotDotCanComeThroughPattern()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*.cs")
                .Include("../2/*.cs")
                .Files("1/x.cs", "1/x.txt", "2/x.cs", "2/x.txt")
                .SubDirectory("1")
                .Execute();

            scenario.AssertExact("x.cs", "../2/x.cs");
        }

        [Fact]
        public void LeadingDotDotWithRecursiveCanComeThroughPattern()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*.cs")
                .Include("../2/**/*.cs")
                .Files("1/x.cs", "1/x.txt", "2/x.cs", "2/x.txt", "2/3/x.cs", "2/3/4/z.cs", "2/3/x.txt")
                .SubDirectory("1")
                .Execute();

            scenario.AssertExact("x.cs", "../2/x.cs", "../2/3/x.cs", "../2/3/4/z.cs");
        }

        [Fact]
        public void ExcludeFolderRecursively()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*.*")
                .Include("../sibling/**/*.*")
                .Exclude("../sibling/exc/**/*.*")
                .Exclude("../sibling/inc/2.txt")
                .Files("main/1.txt", "main/2.txt", "sibling/1.txt", "sibling/inc/1.txt", "sibling/inc/2.txt", "sibling/exc/1.txt", "sibling/exc/2.txt")
                .SubDirectory("main")
                .Execute();

            scenario.AssertExact("1.txt", "2.txt", "../sibling/1.txt", "../sibling/inc/1.txt");
        }

        [Fact]
        public void ExcludeFolderByName()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("*.*")
                .Include("../sibling/**/*.*")
                .Exclude("../sibling/exc/")
                .Exclude("../sibling/inc/2.txt")
                .Files("main/1.txt", "main/2.txt", "sibling/1.txt", "sibling/inc/1.txt", "sibling/inc/2.txt", "sibling/exc/1.txt", "sibling/exc/2.txt")
                .SubDirectory("main")
                .Execute();

            scenario.AssertExact("1.txt", "2.txt", "../sibling/1.txt", "../sibling/inc/1.txt");
        }

        [Fact]
        public void MultipleRecursiveWildcardStemMatch()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("sub/**/bar/**/*.txt")
                .Files("root.txt", "sub/one.txt", "sub/two.txt", "sub/sub2/bar/baz/three.txt", "sub/sub3/sub4/bar/three.txt")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "sub/sub2/bar/baz/three.txt", stem: "sub2/bar/baz/three.txt"),
                new FilePatternMatch(path: "sub/sub3/sub4/bar/three.txt", stem: "sub3/sub4/bar/three.txt")
            }, scenario.Result.Files.ToArray());
        }

        [Fact]
        public void RecursiveWildcardStemMatch()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("sub/**/*.txt")
                .Files("root.txt", "sub/one.txt", "sub/two.txt", "sub/sub2/three.txt")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "sub/one.txt", stem: "one.txt"),
                new FilePatternMatch(path: "sub/two.txt", stem: "two.txt"),
                new FilePatternMatch(path: "sub/sub2/three.txt", stem: "sub2/three.txt")
            }, scenario.Result.Files.ToArray());
        }

        [Fact]
        public void WildcardMidSegmentMatch()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("sub/w*.txt")
                .Files("root.txt", "sub/woah.txt", "sub/wow.txt", "sub/blah.txt")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "sub/woah.txt", stem: "woah.txt"),
                new FilePatternMatch(path: "sub/wow.txt", stem: "wow.txt")
            }, scenario.Result.Files.ToArray());
        }

        [Fact]
        public void StemMatchOnExactFile()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("sub/sub/three.txt")
                .Files("root.txt", "sub/one.txt", "sub/two.txt", "sub/sub/three.txt")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "sub/sub/three.txt", stem: "three.txt"),
            }, scenario.Result.Files.ToArray());
        }

        [Fact]
        public void SimpleStemMatching()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("sub/*")
                .Files("root.txt", "sub/one.txt", "sub/two.txt", "sub/sub/three.txt")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "sub/one.txt", stem: "one.txt"),
                new FilePatternMatch(path: "sub/two.txt", stem: "two.txt")
            }, scenario.Result.Files.ToArray());
        }

        [Fact]
        public void StemMatchingWithFileExtension()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("sub/*.txt")
                .Files("root.txt", "sub/one.txt", "sub/two.txt", "sub/three.dat")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "sub/one.txt", stem: "one.txt"),
                new FilePatternMatch(path: "sub/two.txt", stem: "two.txt")
            }, scenario.Result.Files.ToArray());
        }

        [Fact]
        public void StemMatchingWithParentDir()
        {
            var matcher = new Matcher();
            var scenario = new FileSystemGlobbingTestContext(@"c:\files\", matcher)
                .Include("../files/sub/*.txt")
                .Files("root.txt", "sub/one.txt", "sub/two.txt", "sub/three.dat")
                .Execute();

            // Check the stem of the matched items
            Assert.Equal(new[] {
                new FilePatternMatch(path: "../files/sub/one.txt", stem: "one.txt"),
                new FilePatternMatch(path: "../files/sub/two.txt", stem: "two.txt")
            }, scenario.Result.Files.ToArray());
        }

        // exclude: **/.*/**
        // exclude: node_modules/*
        // exclude: **/.cs
    }
}
