// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternSegments
{
    public class ParentPathSegmentTests
    {
        [Theory]
        [InlineData(".", false)]
        [InlineData("..", true)]
        [InlineData("...", false)]
        public void Match(string testSample, bool expectation)
        {
            var pathSegment = new ParentPathSegment();
            Assert.Equal(expectation, pathSegment.Match(testSample));
        }
    }
}
