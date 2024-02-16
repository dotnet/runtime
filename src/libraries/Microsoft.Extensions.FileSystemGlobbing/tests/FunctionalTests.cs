// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests
{
    public class FunctionalTests : IDisposable
    {
        private readonly DisposableFileSystem _context;

        public FunctionalTests()
        {
            _context = CreateContext();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Theory]
        [InlineData("sub/source2.cs", "sub/source2.cs")]
        [InlineData("sub\\source2.cs", "sub\\source2.cs")]
        [InlineData("sub/source2.cs", "sub\\source2.cs")]
        public void DuplicatePatterns(string pattern1, string pattern2)
        {
            var matcher = new Matcher();
            matcher.AddInclude(pattern1);
            matcher.AddInclude(pattern2);

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/sub/source2.cs");
        }

        [Theory]
        [InlineData("src/project", "source1.cs", new string[] { "source1.cs" })]
        [InlineData("src/project", "Source1.cs", new string[] { })]
        [InlineData("src/project", "compiler/preprocess/**/*.cs", new string[] { "compiler/preprocess/preprocess-source1.cs",
                                                                                 "compiler/preprocess/sub/preprocess-source2.cs",
                                                                                 "compiler/preprocess/sub/sub/preprocess-source3.cs" })]
        [InlineData("src/project", "compiler/Preprocess/**.cs", new string[] { })]
        public void IncludeCaseSensitive(string root, string includePattern, string[] expectedFiles)
        {
            var matcher = new Matcher(StringComparison.Ordinal);
            matcher.AddInclude(includePattern);

            ExecuteAndVerify(matcher, root, expectedFiles.Select(f => root + "/" + f).ToArray());
        }

        [Theory]
        [InlineData("src/project", "source1.cs", new string[] { "source1.cs" })]
        [InlineData("src/project", "Source1.cs", new string[] { "Source1.cs" })]
        [InlineData("src/project", "compiler/preprocess/**/*.cs", new string[] { "compiler/preprocess/preprocess-source1.cs",
                                                                                 "compiler/preprocess/sub/preprocess-source2.cs",
                                                                                 "compiler/preprocess/sub/sub/preprocess-source3.cs" })]
        [InlineData("src/project", "compiler/Preprocess/**.cs", new string[] { "compiler/Preprocess/preprocess-source1.cs",
                                                                                 "compiler/Preprocess/sub/preprocess-source2.cs",
                                                                                 "compiler/Preprocess/sub/sub/preprocess-source3.cs" })]
        public void IncludeCaseInsensitive(string root, string includePattern, string[] expectedFiles)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude(includePattern);

            ExecuteAndVerify(matcher, root, expectedFiles.Select(f => root + "/" + f).ToArray());
        }

        [Theory]
        [InlineData("src/project/compiler/preprocess/", "source.cs", new string[] { "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "preprocess-source1.cs", new string[] {
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "preprocesS-source1.cs", new string[] {
                                                                                    "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "**/Preprocess*", new string[] { "preprocess-source1.cs",
                                                                                     "sub/preprocess-source2.cs",
                                                                                     "sub/sub/preprocess-source3.cs",
                                                                                     "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "**/preprocess*", new string[] { })]
        [InlineData("src/project/compiler/preprocess/", "**/*source*.cs", new string[] { "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "**/*Source*.cs", new string[] {
                                                                                    "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "sub/sub/*", new string[] { "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs" })]
        [InlineData("src/project/compiler/preprocess/", "sub/Sub/*", new string[] { "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        public void ExcludeCaseSensitive(string root, string excludePattern, string[] expectedFiles)
        {
            var matcher = new Matcher(StringComparison.Ordinal);
            matcher.AddInclude("**/*.*");
            matcher.AddExclude(excludePattern);

            ExecuteAndVerify(matcher, root, expectedFiles.Select(f => root + "/" + f).ToArray());
        }

        [Theory]
        [InlineData("src/project/compiler/preprocess/", "source.cs", new string[] { "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "preprocess-source1.cs", new string[] {
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "preprocesS-source1.cs", new string[] {
                                                                                    "sub/preprocess-source2.cs",
                                                                                    "sub/sub/preprocess-source3.cs",
                                                                                    "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "**/Preprocess*", new string[] { })]
        [InlineData("src/project/compiler/preprocess/", "**/preprocess*", new string[] { })]
        [InlineData("src/project/compiler/preprocess/", "**/*source*.cs", new string[] { "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "**/*Source*.cs", new string[] { "sub/sub/preprocess-source3.txt" })]
        [InlineData("src/project/compiler/preprocess/", "sub/sub/*", new string[] { "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs" })]
        [InlineData("src/project/compiler/preprocess/", "sub/Sub/*", new string[] { "preprocess-source1.cs",
                                                                                    "sub/preprocess-source2.cs" })]
        public void ExcludeCaseInsensitive(string root, string excludePattern, string[] expectedFiles)
        {
            var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
            matcher.AddInclude("**/*.*");
            matcher.AddExclude(excludePattern);

            ExecuteAndVerify(matcher, root, expectedFiles.Select(f => root + "/" + f).ToArray());
        }

        [Fact]
        public void RecursiveAndDoubleParentsWithRecursiveSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude("**/*.cs")
                   .AddInclude(@"../../lib/**/*.cs");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/source1.cs",
                "src/project/sub/source2.cs",
                "src/project/sub/source3.cs",
                "src/project/sub2/source4.cs",
                "src/project/sub2/source5.cs",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "lib/source6.cs",
                "lib/sub3/source7.cs",
                "lib/sub4/source8.cs");
        }

        [Fact]
        public void RecursiveAndDoubleParentsSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude("**/*.cs")
                   .AddInclude(@"../../lib/*.cs");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/source1.cs",
                "src/project/sub/source2.cs",
                "src/project/sub/source3.cs",
                "src/project/sub2/source4.cs",
                "src/project/sub2/source5.cs",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "lib/source6.cs");
        }

        [Fact]
        public void WildcardAndDoubleParentWithRecursiveSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"..\..\lib\**\*.cs");
            matcher.AddInclude(@"*.cs");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/source1.cs",
                "lib/source6.cs",
                "lib/sub3/source7.cs",
                "lib/sub4/source8.cs");
        }

        [Fact]
        public void WildcardAndDoubleParentsSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"..\..\lib\*.cs");
            matcher.AddInclude(@"*.cs");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/source1.cs",
                "lib/source6.cs");
        }

        [Fact]
        public void DoubleParentsWithRecursiveSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"..\..\lib\**\*.cs");

            ExecuteAndVerify(matcher, @"src/project",
                "lib/source6.cs",
                "lib/sub3/source7.cs",
                "lib/sub4/source8.cs");
        }

        [Fact]
        public void OneLevelParentAndRecursiveSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"../project2/**/*.cs");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project2/source1.cs",
                "src/project2/sub/source2.cs",
                "src/project2/sub/source3.cs",
                "src/project2/sub2/source4.cs",
                "src/project2/sub2/source5.cs",
                "src/project2/compiler/preprocess/preprocess-source1.cs",
                "src/project2/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project2/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project2/compiler/shared/shared1.cs",
                "src/project2/compiler/shared/sub/shared2.cs",
                "src/project2/compiler/shared/sub/sub/sharedsub.cs");
        }

        [Fact]
        public void RecursiveSuffixSearch()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"**.txt");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.txt",
                "src/project/compiler/shared/shared1.txt",
                "src/project/compiler/shared/sub/shared2.txt",
                "src/project/content1.txt");
        }

        [Fact]
        public void FolderExclude()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"**/*.*");
            matcher.AddExclude(@"obj");
            matcher.AddExclude(@"bin");
            matcher.AddExclude(@".*");

            ExecuteAndVerify(matcher, @"src/project",
                "src/project/source1.cs",
                "src/project/sub/source2.cs",
                "src/project/sub/source3.cs",
                "src/project/sub2/source4.cs",
                "src/project/sub2/source5.cs",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.txt",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/shared1.txt",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/shared2.txt",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "src/project/compiler/resources/resource.res",
                "src/project/compiler/resources/sub/resource2.res",
                "src/project/compiler/resources/sub/sub/resource3.res",
                "src/project/content1.txt");
        }

        [Fact]
        public void FolderInclude()
        {
            var matcher = new Matcher();
            matcher.AddInclude(@"compiler/");
            ExecuteAndVerify(matcher, @"src/project",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.txt",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/shared1.txt",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/shared2.txt",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "src/project/compiler/resources/resource.res",
                "src/project/compiler/resources/sub/resource2.res",
                "src/project/compiler/resources/sub/sub/resource3.res");
        }

        [Theory]
        [InlineData("source1.cs", "src/project/source1.cs")]
        [InlineData("../project2/source1.cs", "src/project2/source1.cs")]
        public void SingleFile(string pattern, string expect)
        {
            var matcher = new Matcher();
            matcher.AddInclude(pattern);
            ExecuteAndVerify(matcher, "src/project", expect);
        }

        [Fact]
        public void SingleFileAndRecursive()
        {
            var matcher = new Matcher();
            matcher.AddInclude("**/*.cs");
            matcher.AddInclude("../project2/source1.cs");
            ExecuteAndVerify(matcher, "src/project",
                "src/project/source1.cs",
                "src/project/sub/source2.cs",
                "src/project/sub/source3.cs",
                "src/project/sub2/source4.cs",
                "src/project/sub2/source5.cs",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "src/project2/source1.cs");
        }

        [Fact]
        public void StemCorrectWithDifferentWildCards()
        {
            var matcher = new Matcher();
            matcher.AddInclude("sub/*.cs");
            matcher.AddInclude("**/*.cs");

            var directoryPath = Path.Combine(_context.RootPath, "src/project");
            var results = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directoryPath)));

            var actual = results.Files.Select(match => match.Stem);
            var expected = new string[]
            {
                "source1.cs",
                "source2.cs",
                "source3.cs",
                "sub2/source4.cs",
                "sub2/source5.cs",
                "compiler/preprocess/preprocess-source1.cs",
                "compiler/preprocess/sub/preprocess-source2.cs",
                "compiler/preprocess/sub/sub/preprocess-source3.cs",
                "compiler/shared/shared1.cs",
                "compiler/shared/sub/shared2.cs",
                "compiler/shared/sub/sub/sharedsub.cs"
            };

            AssertExtensions.CollectionEqual(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void StemCorrectWithDifferentWildCards_WithInMemory()
        {
            var matcher = new Matcher();
            matcher.AddInclude("src/project/sub/*.cs");
            matcher.AddInclude("src/project/**/*.cs");

            var files = GetFileList();
            var results = matcher.Match("./", files);

            var actual = results.Files.Select(match => match.Stem);
            var expected = new string[]
            {
                "source1.cs",
                "source2.cs",
                "source3.cs",
                "sub2/source4.cs",
                "sub2/source5.cs",
                "compiler/preprocess/preprocess-source1.cs",
                "compiler/preprocess/sub/preprocess-source2.cs",
                "compiler/preprocess/sub/sub/preprocess-source3.cs",
                "compiler/shared/shared1.cs",
                "compiler/shared/sub/shared2.cs",
                "compiler/shared/sub/sub/sharedsub.cs"
            };

            AssertExtensions.CollectionEqual(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void MultipleSubDirsAfterFirstWildcardMatch_HasCorrectStem()
        {
            var matcher = new Matcher();
            matcher.AddInclude("compiler/**/*.cs");

            var directoryPath = Path.Combine(_context.RootPath, "src/project");
            var results = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directoryPath)));

            var actual = results.Files.Select(match => match.Stem);
            var expected = new string[]
            {
                "preprocess/preprocess-source1.cs",
                "preprocess/sub/preprocess-source2.cs",
                "preprocess/sub/sub/preprocess-source3.cs",
                "shared/shared1.cs",
                "shared/sub/shared2.cs",
                "shared/sub/sub/sharedsub.cs"
            };

            AssertExtensions.CollectionEqual(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void MultipleSubDirsAfterFirstWildcardMatch_HasCorrectStem_WithInMemory()
        {
            var matcher = new Matcher();
            matcher.AddInclude("src/project/compiler/**/*.cs");

            var files = GetFileList();
            var results = matcher.Match("./", files);

            var actual = results.Files.Select(match => match.Stem);
            var expected = new string[]
            {
                "preprocess/preprocess-source1.cs",
                "preprocess/sub/preprocess-source2.cs",
                "preprocess/sub/sub/preprocess-source3.cs",
                "shared/shared1.cs",
                "shared/sub/shared2.cs",
                "shared/sub/sub/sharedsub.cs"
            };

            AssertExtensions.CollectionEqual(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        [Theory] // rootDir, includePattern, expectedPath
        [InlineData(@"root", @"*.0",         @"test.0")]
        [InlineData(@"root", @"**/*.0",      @"test.0")]
        public void PathIncludesAllSegmentsFromPattern_RootDirectory(string root, string includePattern, string expectedPath)
        {
            var matcher = new Matcher();
            matcher.AddInclude(includePattern);

            var results = matcher.Match(root, new[] { expectedPath });
            var actualPath = results.Files.Select(file => file.Path).SingleOrDefault();

            Assert.Equal(expectedPath, actualPath);

            // Also test all scenarios with the `./` current directory prefix
            matcher = new Matcher();
            matcher.AddInclude("./" + includePattern);

            results = matcher.Match(root, new[] { expectedPath });
            actualPath = results.Files.Select(file => file.Path).SingleOrDefault();

            Assert.Equal(expectedPath, actualPath);
        }

        [Theory] // rootDir,      includePattern,    expectedPath
        [InlineData(@"root/dir1", @"*.1",            @"test.1")]
        [InlineData(@"root/dir1", @"**/*.1",         @"test.1")]
        [InlineData(@"root",      @"dir1/*.1",       @"dir1/test.1")]
        [InlineData(@"root",      @"dir1/**/*.1",    @"dir1/test.1")]
        [InlineData(@"root",      @"**/dir1/*.1",    @"dir1/test.1")]
        [InlineData(@"root",      @"**/dir1/**/*.1", @"dir1/test.1")]
        [InlineData(@"root",      @"**/*.1",         @"dir1/test.1")]
        public void PathIncludesAllSegmentsFromPattern_OneDirectoryDeep(string root, string includePattern, string expectedPath)
        {
            var matcher = new Matcher();
            matcher.AddInclude(includePattern);

            var results = matcher.Match(root, new[] { expectedPath });
            var actualPath = results.Files.Select(file => file.Path).SingleOrDefault();

            Assert.Equal(expectedPath, actualPath);

            // Also test all scenarios with the `./` current directory prefix
            matcher = new Matcher();
            matcher.AddInclude("./" + includePattern);

            results = matcher.Match(root, new[] { expectedPath });
            actualPath = results.Files.Select(file => file.Path).SingleOrDefault();

            Assert.Equal(expectedPath, actualPath);
        }

        [Theory] // rootDir,           includePattern,            expectedPath
        [InlineData(@"root/dir1/dir2", @"*.2",                    @"test.2")]
        [InlineData(@"root/dir1/dir2", @"**/*.2",                 @"test.2")]
        [InlineData(@"root/dir1",      @"dir2/*.2",               @"dir2/test.2")]
        [InlineData(@"root/dir1",      @"dir2/**/*.2",            @"dir2/test.2")]
        [InlineData(@"root/dir1",      @"**/dir2/*.2",            @"dir2/test.2")]
        [InlineData(@"root/dir1",      @"**/dir2/**/*.2",         @"dir2/test.2")]
        [InlineData(@"root/dir1",      @"**/*.2",                 @"dir2/test.2")]
        [InlineData(@"root",           @"dir1/dir2/*.2",          @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"dir1/dir2/**/*.2",       @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/dir2/**/*.2",    @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/**/dir2/*.2",    @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/**/dir2/**/*.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"dir1/**/*.2",            @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/**/*.2",         @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir2/*.2",            @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir2/**/*.2",         @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/*.2",                 @"dir1/dir2/test.2")]
        public void PathIncludesAllSegmentsFromPattern_TwoDirectoriesDeep(string root, string includePattern, string expectedPath)
        {
            var matcher = new Matcher();
            matcher.AddInclude(includePattern);

            var results = matcher.Match(root, new[] { expectedPath });
            var actualPath = results.Files.Select(file => file.Path).SingleOrDefault();

            Assert.Equal(expectedPath, actualPath);

            // Also test all scenarios with the `./` current directory prefix
            matcher = new Matcher();
            matcher.AddInclude("./" + includePattern);

            results = matcher.Match(root, new[] { expectedPath });
            actualPath = results.Files.Select(file => file.Path).SingleOrDefault();

            Assert.Equal(expectedPath, actualPath);
        }

        [Theory] // rootDir, includePattern, expectedStem
        [InlineData(@"root", @"*.0",         @"test.0")]
        [InlineData(@"root", @"**/*.0",      @"test.0")]
        public void StemIncludesAllSegmentsFromPatternStartingAtWildcard_RootDirectory(string root, string includePattern, string expectedStem)
        {
            string fileToFind = "test.0";

            var matcher = new Matcher();
            matcher.AddInclude(includePattern);

            var results = matcher.Match(root, new[] { fileToFind });
            var actualStem = results.Files.Select(file => file.Stem).SingleOrDefault();

            Assert.Equal(expectedStem, actualStem);

            // Also test all scenarios with the `./` current directory prefix
            matcher = new Matcher();
            matcher.AddInclude("./" + includePattern);

            results = matcher.Match(root, new[] { fileToFind });
            actualStem = results.Files.Select(file => file.Stem).SingleOrDefault();

            Assert.Equal(expectedStem, actualStem);
        }

        [Theory] // rootDir,      includePattern,    fileToFind      expectedStem
        [InlineData(@"root/dir1", @"*.1",            @"test.1",      @"test.1")]
        [InlineData(@"root/dir1", @"**/*.1",         @"test.1",      @"test.1")]
        [InlineData(@"root",      @"dir1/*.1",       @"dir1/test.1", @"test.1")]
        [InlineData(@"root",      @"dir1/**/*.1",    @"dir1/test.1", @"test.1")]
        [InlineData(@"root",      @"**/dir1/*.1",    @"dir1/test.1", @"dir1/test.1")]
        [InlineData(@"root",      @"**/dir1/**/*.1", @"dir1/test.1", @"dir1/test.1")]
        [InlineData(@"root",      @"**/*.1",         @"dir1/test.1", @"dir1/test.1")]
        public void StemIncludesAllSegmentsFromPatternStartingAtWildcard_OneDirectoryDeep(string root, string includePattern, string fileToFind, string expectedStem)
        {
            var matcher = new Matcher();
            matcher.AddInclude(includePattern);

            var results = matcher.Match(root, new[] { fileToFind });
            var actualStem = results.Files.Select(file => file.Stem).SingleOrDefault();

            Assert.Equal(expectedStem, actualStem);

            // Also test all scenarios with the `./` current directory prefix
            matcher = new Matcher();
            matcher.AddInclude("./" + includePattern);

            results = matcher.Match(root, new[] { fileToFind });
            actualStem = results.Files.Select(file => file.Stem).SingleOrDefault();

            Assert.Equal(expectedStem, actualStem);
        }

        [Theory] // rootDir,           includePattern,            fileToFind           expectedStem
        [InlineData(@"root/dir1/dir2", @"*.2",                    @"test.2",           @"test.2")]
        [InlineData(@"root/dir1/dir2", @"**/*.2",                 @"test.2",           @"test.2")]
        [InlineData(@"root/dir1",      @"dir2/*.2",               @"dir2/test.2",      @"test.2")]
        [InlineData(@"root/dir1",      @"dir2/**/*.2",            @"dir2/test.2",      @"test.2")]
        [InlineData(@"root/dir1",      @"**/dir2/*.2",            @"dir2/test.2",      @"dir2/test.2")]
        [InlineData(@"root/dir1",      @"**/dir2/**/*.2",         @"dir2/test.2",      @"dir2/test.2")]
        [InlineData(@"root/dir1",      @"**/*.2",                 @"dir2/test.2",      @"dir2/test.2")]
        [InlineData(@"root",           @"dir1/dir2/*.2",          @"dir1/dir2/test.2", @"test.2")]
        [InlineData(@"root",           @"dir1/dir2/**/*.2",       @"dir1/dir2/test.2", @"test.2")]
        [InlineData(@"root",           @"**/dir1/dir2/**/*.2",    @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/**/dir2/*.2",    @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/**/dir2/**/*.2", @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"dir1/**/*.2",            @"dir1/dir2/test.2", @"dir2/test.2")]
        [InlineData(@"root",           @"**/dir1/**/*.2",         @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir2/*.2",            @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/dir2/**/*.2",         @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        [InlineData(@"root",           @"**/*.2",                 @"dir1/dir2/test.2", @"dir1/dir2/test.2")]
        public void StemIncludesAllSegmentsFromPatternStartingAtWildcard_TwoDirectoriesDeep(string root, string includePattern, string fileToFind, string expectedStem)
        {
            var matcher = new Matcher();
            matcher.AddInclude(includePattern);

            var results = matcher.Match(root, new[] { fileToFind });
            var actualStem = results.Files.Select(file => file.Stem).SingleOrDefault();

            Assert.Equal(expectedStem, actualStem);

            // Also test all scenarios with the `./` current directory prefix
            matcher = new Matcher();
            matcher.AddInclude("./" + includePattern);

            results = matcher.Match(root, new[] { fileToFind });
            actualStem = results.Files.Select(file => file.Stem).SingleOrDefault();

            Assert.Equal(expectedStem, actualStem);
        }

        [Theory]
        [InlineData("/", '/')]
        public void RootDir_IsPathRoot_WithInMemory_AllOS(string rootDir, char separator)
        {
            RootDir_IsPathRoot_WithInMemory(rootDir, separator);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("C:\\", '\\')]
        [InlineData("C:/", '/')]
        public void RootDir_IsPathRoot_WithInMemory_WindowsOnly(string rootDir, char separator)
        {
            RootDir_IsPathRoot_WithInMemory(rootDir, separator);
        }

        private static void RootDir_IsPathRoot_WithInMemory(string rootDir, char separator)
        {
            var matcher = new Matcher();
            matcher.AddInclude($"**{separator}*.cs");

            IEnumerable<string> files = GetFileList(rootDir, separator);
            PatternMatchingResult results = matcher.Match(rootDir, files);

            IEnumerable<string> actual = results.Files.Select(match => match.Path);
            IEnumerable<string> expected = new string[]
            {
                "src/project/source1.cs",
                "src/project/sub/source2.cs",
                "src/project/sub/source3.cs",
                "src/project/sub2/source4.cs",
                "src/project/sub2/source5.cs",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "src/project2/source1.cs",
                "src/project2/sub/source2.cs",
                "src/project2/sub/source3.cs",
                "src/project2/sub2/source4.cs",
                "src/project2/sub2/source5.cs",
                "src/project2/compiler/preprocess/preprocess-source1.cs",
                "src/project2/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project2/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project2/compiler/shared/shared1.cs",
                "src/project2/compiler/shared/sub/shared2.cs",
                "src/project2/compiler/shared/sub/sub/sharedsub.cs",
                "lib/source6.cs",
                "lib/sub3/source7.cs",
                "lib/sub4/source8.cs",
            };

            Assert.Equal(
                expected.OrderBy(e => e),
                actual.OrderBy(e => e),
                StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("/src/project", '/')]
        [InlineData("/src/project/", '/')]
        public void RootDir_IsAbsolutePath_WithInMemory_AllOS(string rootDir, char separator)
        {
            RootDir_IsAbsolutePath_WithInMemory(rootDir, separator);
        }
        
        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData("C:\\src\\project", '\\')]
        [InlineData("C:\\src\\project\\", '\\')]
        [InlineData("C:/src/project", '/')]
        [InlineData("C:/src/project/", '/')]
        public void RootDir_IsAbsolutePath_WithInMemory_WindowsOnly(string rootDir, char separator)
        {
            RootDir_IsAbsolutePath_WithInMemory(rootDir, separator);
        }

        private static void RootDir_IsAbsolutePath_WithInMemory(string rootDir, char separator)
        {
            var matcher = new Matcher();
            matcher.AddInclude($"**{separator}*.cs");

            IEnumerable<string> files = GetFileList(Path.GetPathRoot(rootDir), separator);
            PatternMatchingResult results = matcher.Match(rootDir, files);

            IEnumerable<string> actual = results.Files.Select(match => match.Path);
            IEnumerable<string> expected = new string[]
            {
                "source1.cs",
                "sub/source2.cs",
                "sub/source3.cs",
                "sub2/source4.cs",
                "sub2/source5.cs",
                "compiler/preprocess/preprocess-source1.cs",
                "compiler/preprocess/sub/preprocess-source2.cs",
                "compiler/preprocess/sub/sub/preprocess-source3.cs",
                "compiler/shared/shared1.cs",
                "compiler/shared/sub/shared2.cs",
                "compiler/shared/sub/sub/sharedsub.cs"
            };

            Assert.Equal(
                expected.OrderBy(e => e),
                actual.OrderBy(e => e),
                StringComparer.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> GetFileList(string rootDir = "", char directorySeparator = '/')
        {
            var files = new List<string>
            {
                "root/test.0",
                "root/dir1/test.1",
                "root/dir1/dir2/test.2",
                "src/project/source1.cs",
                "src/project/sub/source2.cs",
                "src/project/sub/source3.cs",
                "src/project/sub2/source4.cs",
                "src/project/sub2/source5.cs",
                "src/project/compiler/preprocess/preprocess-source1.cs",
                "src/project/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project/compiler/preprocess/sub/sub/preprocess-source3.txt",
                "src/project/compiler/shared/shared1.cs",
                "src/project/compiler/shared/shared1.txt",
                "src/project/compiler/shared/sub/shared2.cs",
                "src/project/compiler/shared/sub/shared2.txt",
                "src/project/compiler/shared/sub/sub/sharedsub.cs",
                "src/project/compiler/resources/resource.res",
                "src/project/compiler/resources/sub/resource2.res",
                "src/project/compiler/resources/sub/sub/resource3.res",
                "src/project/content1.txt",
                "src/project/obj/object.o",
                "src/project/bin/object",
                "src/project/.hidden/file1.hid",
                "src/project/.hidden/sub/file2.hid",
                "src/project2/source1.cs",
                "src/project2/sub/source2.cs",
                "src/project2/sub/source3.cs",
                "src/project2/sub2/source4.cs",
                "src/project2/sub2/source5.cs",
                "src/project2/compiler/preprocess/preprocess-source1.cs",
                "src/project2/compiler/preprocess/sub/preprocess-source2.cs",
                "src/project2/compiler/preprocess/sub/sub/preprocess-source3.cs",
                "src/project2/compiler/preprocess/sub/sub/preprocess-source3.txt",
                "src/project2/compiler/shared/shared1.cs",
                "src/project2/compiler/shared/shared1.txt",
                "src/project2/compiler/shared/sub/shared2.cs",
                "src/project2/compiler/shared/sub/shared2.txt",
                "src/project2/compiler/shared/sub/sub/sharedsub.cs",
                "src/project2/compiler/resources/resource.res",
                "src/project2/compiler/resources/sub/resource2.res",
                "src/project2/compiler/resources/sub/sub/resource3.res",
                "src/project2/content1.txt",
                "src/project2/obj/object.o",
                "src/project2/bin/object",
                "lib/source6.cs",
                "lib/sub3/source7.cs",
                "lib/sub4/source8.cs",
                "res/resource1.text",
                "res/resource2.text",
                "res/resource3.text",
                ".hidden/file1.hid",
                ".hidden/sub/file2.hid"
            };

            return files.Select(x => (rootDir + x).Replace('/', directorySeparator));
        }

        private DisposableFileSystem CreateContext()
        {
            var context = new DisposableFileSystem();
            context.CreateFiles(GetFileList().ToArray());

            return context;
        }

        private void ExecuteAndVerify(Matcher matcher, string directoryPath, params string[] expectFiles)
        {
            directoryPath = Path.Combine(_context.RootPath, directoryPath);
            var results = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(directoryPath)));

            var actual = results.Files.Select(match => Path.GetFullPath(Path.Combine(_context.RootPath, directoryPath, match.Path)));
            var expected = expectFiles.Select(relativePath => Path.GetFullPath(Path.Combine(_context.RootPath, relativePath)));

            AssertExtensions.CollectionEqual(expected, actual, StringComparer.OrdinalIgnoreCase);
        }

        [Fact] // https://github.com/dotnet/runtime/issues/44767
        public void VerifyAbsolutePaths_HasMatches()
        {
            var fileMatcher = new Matcher();
            fileMatcher.AddInclude("**/*");

            if (PlatformDetection.IsWindows)
            {
                // Windows-like absolute paths are not supported on Unix.
                string fakeWindowsPath = "C:\\This\\is\\a\\nested\\windows-like\\path\\somefile.cs";
                Assert.True(fileMatcher.Match(Path.GetPathRoot(fakeWindowsPath), fakeWindowsPath).HasMatches);
            }
            
            // Unix-like absolute paths are treated as relative paths on Windows.
            string fakeUnixPath = "/This/is/a/nested/unix-like/path/somefile.cs";
            Assert.True(fileMatcher.Match(Path.GetPathRoot(fakeUnixPath), fakeUnixPath).HasMatches);
        }

        [Fact] // https://github.com/dotnet/runtime/issues/36415
        public void VerifyInMemoryDirectoryInfo_IsNotEmpty()
        {
            IEnumerable<string> files = new[] { @"pagefile.sys" };
            InMemoryDirectoryInfo directoryInfo;
            IEnumerable<FileSystemInfoBase> fileSystemInfos;

            if (PlatformDetection.IsWindows)
            {
                directoryInfo = new InMemoryDirectoryInfo(@"C:\", files);
                fileSystemInfos = directoryInfo.EnumerateFileSystemInfos();

                Assert.Equal(1, fileSystemInfos.Count());
            }

            directoryInfo = new InMemoryDirectoryInfo("/", files);
            fileSystemInfos = directoryInfo.EnumerateFileSystemInfos();

            Assert.Equal(1, fileSystemInfos.Count());
        }
    }
}
