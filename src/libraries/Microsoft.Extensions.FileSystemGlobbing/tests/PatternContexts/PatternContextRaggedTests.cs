// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PatternContexts;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternContexts
{
    public class PatternContextRaggedIncludeTests
    {
        [Fact]
        public void PredictBeforeEnterDirectoryShouldThrow()
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build("**") as IRaggedPattern;
            var context = new PatternContextRaggedInclude(pattern);

            Assert.Throws<InvalidOperationException>(() =>
            {
                context.Declare((segment, last) =>
                {
                    Assert.Fail("No segment should be declared.");
                });
            });
        }

        [Theory]
        [InlineData("/a/b/**/c/d", new string[] { "root" }, "a")]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a" }, "b")]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b" }, null)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b", "whatever" }, null)]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "b", "whatever", "anything" }, null)]
        public void PredictReturnsCorrectResult(string patternString, string[] pushDirectory, string expectSegment)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(patternString) as IRaggedPattern;
            Assert.NotNull(pattern);

            var context = new PatternContextRaggedInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Declare((segment, last) =>
            {
                if (expectSegment != null)
                {
                    var mockSegment = segment as LiteralPathSegment;

                    Assert.NotNull(mockSegment);
                    Assert.False(last);
                    Assert.Equal(expectSegment, mockSegment.Value);
                }
                else
                {
                    Assert.Equal(WildcardPathSegment.MatchAll, segment);
                }
            });
        }

        [Theory]
        [InlineData("/a/b/**/c/d", new string[] { "root", "b" })]
        [InlineData("/a/b/**/c/d", new string[] { "root", "a", "c" })]
        public void PredictNotCallBackWhenEnterUnmatchDirectory(string patternString, string[] pushDirectory)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(patternString) as IRaggedPattern;
            var context = new PatternContextRaggedInclude(pattern);
            PatternContextHelper.PushDirectory(context, pushDirectory);

            context.Declare((segment, last) =>
            {
                Assert.Fail("No segment should be declared.");
            });
        }
    }
}
