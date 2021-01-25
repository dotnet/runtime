// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.Patterns
{
    public class PatternTests
    {
        [Theory]
        [InlineData("abc", 1)]
        [InlineData("/abc", 1)]
        [InlineData("abc/efg", 2)]
        [InlineData("abc/efg/h*j", 3)]
        [InlineData("abc/efg/h*j/*.*", 4)]
        [InlineData("abc/efg/hij", 3)]
        [InlineData("abc/efg/hij/klm", 4)]
        [InlineData("../abc/efg/hij/klm", 5)]
        [InlineData("../../abc/efg/hij/klm", 6)]
        public void BuildLinearPattern(string sample, int segmentCount)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(sample);

            Assert.True(pattern is ILinearPattern);
            Assert.Equal(segmentCount, (pattern as ILinearPattern).Segments.Count);
        }

        [Theory]
        [InlineData("abc/efg/**")]
        [InlineData("/abc/efg/**")]
        [InlineData("abc/efg/**/hij/klm")]
        [InlineData("abc/efg/**/hij/**/klm")]
        [InlineData("abc/efg/**/hij/**/klm/**")]
        [InlineData("abc/efg/**/hij/**/klm/**/")]
        [InlineData("**/hij/**/klm")]
        [InlineData("**/hij/**")]
        [InlineData("/**/hij/**")]
        [InlineData("**/**/hij/**")]
        [InlineData("ab/**/**/hij/**")]
        [InlineData("ab/**/**/hij/**/")]
        [InlineData("/ab/**/**/hij/**/")]
        [InlineData("/ab/**/**/hij/**")]
        public void BuildLinearPatternNegative(string sample)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(sample) as ILinearPattern;

            Assert.Null(pattern);
        }


        [Theory]
        [InlineData("/abc/", 2, 1, 0, 0)]
        [InlineData("abc/", 2, 1, 0, 0)]
        [InlineData("abc/efg/", 3, 2, 0, 0)]
        [InlineData("abc/efg/h*j/*.*/", 5, 4, 0, 0)]
        [InlineData("abc/efg/**", 3, 2, 0, 0)]
        [InlineData("/abc/efg/**", 3, 2, 0, 0)]
        [InlineData("abc/efg/**/hij/klm", 5, 2, 0, 2)]
        [InlineData("abc/efg/**/hij/**/klm", 6, 2, 1, 1)]
        [InlineData("abc/efg/**/hij/**/klm/**", 7, 2, 2, 0)]
        [InlineData("abc/efg/**/hij/**/klm/**/", 8, 2, 2, 0)]
        [InlineData("**/hij/**/klm", 4, 0, 1, 1)]
        [InlineData("**/hij/**", 3, 0, 1, 0)]
        [InlineData("/**/hij/**", 3, 0, 1, 0)]
        [InlineData("**/**/hij/**", 4, 0, 1, 0)]
        [InlineData("ab/**/**/hij/**", 5, 1, 1, 0)]
        [InlineData("ab/**/**/hij/**/", 6, 1, 1, 0)]
        [InlineData("/ab/**/**/hij/**/", 6, 1, 1, 0)]
        [InlineData("/ab/**/**/hij/**", 5, 1, 1, 0)]
        [InlineData("**/*.suffix", 2, 0, 0, 1)]
        [InlineData("**.suffix", 2, 0, 0, 1)]
        [InlineData("ab/**.suffix", 3, 1, 0, 1)]
        public void BuildRaggedPattern(string sample,
                             int segmentCount,
                             int startSegmentsCount,
                             int containSegmentCount,
                             int endSegmentCount)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(sample) as IRaggedPattern;

            Assert.NotNull(pattern);
            Assert.Equal(segmentCount, pattern.Segments.Count);
            Assert.Equal(startSegmentsCount, pattern.StartsWith.Count);
            Assert.Equal(endSegmentCount, pattern.EndsWith.Count);
            Assert.Equal(containSegmentCount, pattern.Contains.Count);
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("/abc")]
        [InlineData("abc/efg")]
        [InlineData("abc/efg/h*j")]
        [InlineData("abc/efg/h*j/*.*")]
        [InlineData("abc/efg/hij")]
        [InlineData("abc/efg/hij/klm")]
        public void BuildRaggedPatternNegative(string sample)
        {
            var builder = new PatternBuilder();
            var pattern = builder.Build(sample) as IRaggedPattern;

            Assert.Null(pattern);
        }

        [Theory]
        [InlineData("a/../")]
        [InlineData("a/..")]
        [InlineData("/a/../")]
        [InlineData("./a/../")]
        [InlineData("**/../")]
        [InlineData("*.cs/../")]
        public void ThrowExceptionForInvalidParentsPath(string sample)
        {
            // parent segment is only allowed at the beginning of the pattern
            Assert.Throws<ArgumentException>(() =>
            {
                var builder = new PatternBuilder();
                var pattern = builder.Build(sample);

                Assert.Null(pattern);
            });
        }

        [Fact]
        public void ThrowExceptionForNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var builder = new PatternBuilder();
                builder.Build(null);
            });
        }
    }
}
