// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionReadOnlyTests
    {
        [Fact]
        public void ReadOnlyCollection_AttemptingToModifyCollection_ThrowsNotSupportedException()
        {
            var nameValueCollection = new ReadOnlyNameValueCollection();

            Assert.Throws<NotSupportedException>(() => nameValueCollection.Add("name", "value"));
            Assert.Throws<NotSupportedException>(() => nameValueCollection.Set("name", "value"));

            Assert.Throws<NotSupportedException>(() => nameValueCollection.Remove("name"));
            Assert.Throws<NotSupportedException>(() => nameValueCollection.Clear());
        }

        private class ReadOnlyNameValueCollection : NameValueCollection
        {
            public ReadOnlyNameValueCollection()
            {
                IsReadOnly = true;
            }
        }
    }
}
