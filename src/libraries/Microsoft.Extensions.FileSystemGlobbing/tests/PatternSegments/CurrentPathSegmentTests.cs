// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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