// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionGetItemTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Get_InvalidIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection[count]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection[count + 1]);
        }
    }
}
