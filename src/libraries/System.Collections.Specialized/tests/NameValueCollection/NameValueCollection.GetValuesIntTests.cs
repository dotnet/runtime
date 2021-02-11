// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionGetValuesIntTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void GetValues_InvalidIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.GetValues(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.GetValues(count));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.GetValues(count + 1));
        }
    }
}
