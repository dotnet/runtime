// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionGetStringTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Get_NoSuchName_ReturnsNull(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            Assert.Null(nameValueCollection.Get("no-such-name"));
            Assert.Null(nameValueCollection.Get(null));
        }
    }
}
