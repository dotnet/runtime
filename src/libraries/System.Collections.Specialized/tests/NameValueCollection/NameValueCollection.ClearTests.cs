// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionClearTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        public void Clear(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            nameValueCollection.Clear();
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);
            Assert.Equal(0, nameValueCollection.Keys.Count);

            nameValueCollection.Clear();
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Equal(0, nameValueCollection.AllKeys.Length);
            Assert.Equal(0, nameValueCollection.Keys.Count);
        }
    }
}
