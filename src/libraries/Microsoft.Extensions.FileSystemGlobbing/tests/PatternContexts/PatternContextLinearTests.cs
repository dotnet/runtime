// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternContexts
{
    public class PatternContextLinearIncludeTests
    {
        [Fact]
        public void PredictBeforeEnterDirectoryShouldThrow()
        {
            var pattern = MockLinearPatternBuilder.New().Add("a").Build();
            var context = new PatternContextLinearInclude(pattern);

            Assert.Throws<InvalidOperationException>(() =>
            {
                context.Declare((segment, last) =>
                {
                    Assert.Fail("No segment should be declared.");
                });
            });
        }

        [Theory]
        [InlineData(new string[] { "a", "b" }, new string[] { "root" }, "a", false)]
        [InlineData(new string[] { "a", "b" }, new string[] { "root", "a" }, "b", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root" }, "a", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        public void PredictReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string expectSegment, bool expectLast)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Declare((segment, last) =>
            {
                var literal = segment as MockNonRecursivePathSegment;

                Assert.NotNull(segment);
                Assert.Equal(expectSegment, literal.Value);
                Assert.Equal(expectLast, last);
            });
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "b" })]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "c" })]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "d" })]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "c" })]
        public void PredictNotCallBackWhenEnterUnmatchDirectory(string[] testSegments, string[] pushDirectory)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Declare((segment, last) =>
            {
                Assert.Fail("No segment should be declared.");
            });
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "d", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "c" }, "d", false)]
        public void TestFileForIncludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string filename, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new FakeFileInfo(filename));

            Assert.Equal(expectResult, result.IsSuccessful);
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "d", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b", "c" }, "d", false)]
        public void TestFileForExcludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string filename, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearExclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new FakeFileInfo(filename));

            Assert.Equal(expectResult, result.IsSuccessful);
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root" }, "a", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "b", true)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "c", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", false)]
        public void TestDirectoryForIncludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string directoryName, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new FakeDirectoryInfo(directoryName));

            Assert.Equal(expectResult, result);
        }

        [Theory]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root" }, "a", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "b", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a" }, "c", false)]
        [InlineData(new string[] { "a", "b", "c" }, new string[] { "root", "a", "b" }, "c", true)]
        public void TestDirectoryForExcludeReturnsCorrectResult(string[] testSegments, string[] pushDirectory, string directoryName, bool expectResult)
        {
            var pattern = MockLinearPatternBuilder.New().Add(testSegments).Build();
            var context = new PatternContextLinearExclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            var result = context.Test(new FakeDirectoryInfo(directoryName));

            Assert.Equal(expectResult, result);
        }

        private class FakeDirectoryInfo : DirectoryInfoBase
        {
            public FakeDirectoryInfo(string name)
            {
                Name = name;
            }

            public override string FullName { get { throw new NotImplementedException(); } }

            public override string Name { get; }

            public override DirectoryInfoBase ParentDirectory { get { throw new NotImplementedException(); } }

            public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos() { throw new NotImplementedException(); }

            public override DirectoryInfoBase GetDirectory(string path) { throw new NotImplementedException(); }

            public override FileInfoBase GetFile(string path) { throw new NotImplementedException(); }
        }

        private class FakeFileInfo : FileInfoBase
        {
            public FakeFileInfo(string name)
            {
                Name = name;
            }

            public override string FullName { get { throw new NotImplementedException(); } }

            public override string Name { get; }

            public override DirectoryInfoBase ParentDirectory { get { throw new NotImplementedException(); } }
        }
    }
}
