// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionGetKeyTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Get_InvalidIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.Get(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.Get(count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.Get(count + 1));
        }
    }
}
