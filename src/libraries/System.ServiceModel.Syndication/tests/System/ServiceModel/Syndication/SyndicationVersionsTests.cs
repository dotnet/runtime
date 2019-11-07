// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ServiceModel.Syndication.Tests
{
    public class SyndicationVersionsTests
    {
        [Fact]
        public void Atom10_Get_ReturnsExpected()
        {
            Assert.Equal("Atom10", SyndicationVersions.Atom10);
        }
        [Fact]
        public void Rss20_Get_ReturnsExpected()
        {
            Assert.Equal("Rss20", SyndicationVersions.Rss20);
        }
    }
}
