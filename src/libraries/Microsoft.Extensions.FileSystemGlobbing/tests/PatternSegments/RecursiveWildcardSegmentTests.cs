// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileSystemGlobbing.Internal.PathSegments;
using Xunit;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.PatternSegments
{
    public class RecursiveWildcardSegmentTests
    {
        [Fact]
        public void Match()
        {
            var pathSegment = new RecursiveWildcardSegment();
            Assert.False(pathSegment.Match("Anything"));
        }
    }
}