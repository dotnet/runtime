// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class PropertyReadOnlyNameValueCollectionTests
    {
        [Fact]
        public void ReadOnlyCollection_AttemptingToModifyCollection_Throws()
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
