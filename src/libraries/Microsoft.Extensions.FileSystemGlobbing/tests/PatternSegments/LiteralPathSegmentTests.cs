// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternSegments
{
    public class LiteralPathSegmentTests
    {
        [Fact]
        public void ThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                var pathSegment = new LiteralPathSegment(value: null, comparisonType: StringComparison.OrdinalIgnoreCase);
            });
        }

        [Fact]
        public void AllowEmptyInDefaultConstructor()
        {
            var pathSegment = new LiteralPathSegment(string.Empty, comparisonType: StringComparison.Ordinal);
            Assert.NotNull(pathSegment);
        }

        [Theory]
        [InlineData("something", "anything", StringComparison.Ordinal, false)]
        [InlineData("something", "Something", StringComparison.Ordinal, false)]
        [InlineData("something", "something", StringComparison.Ordinal, true)]
        [InlineData("something", "anything", StringComparison.OrdinalIgnoreCase, false)]
        [InlineData("something", "Something", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("something", "something", StringComparison.OrdinalIgnoreCase, true)]
        public void Match(string initialValue, string testSample, StringComparison comparisonType, bool expectation)
        {
            var pathSegment = new LiteralPathSegment(initialValue, comparisonType);
            Assert.Equal(initialValue, pathSegment.Value);
            Assert.Equal(expectation, pathSegment.Match(testSample));
        }
    }
}