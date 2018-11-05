// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternSegments
{
    public class CurrentPathSegmentTests
    {
        [Theory]
        [InlineData("anything")]
        [InlineData("")]
        [InlineData(null)]
        public void Match(string testSample)
        {
            var pathSegment = new CurrentPathSegment();
            Assert.False(pathSegment.Match(testSample));
        }
    }
}