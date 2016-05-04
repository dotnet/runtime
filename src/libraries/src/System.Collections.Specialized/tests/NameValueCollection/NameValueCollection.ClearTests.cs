// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionClearTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Clear(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(10);
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
