// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionRemoveTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Remove(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            nameValueCollection.Remove("no-such-name");
            nameValueCollection.Remove(null);
            Assert.Equal(count, nameValueCollection.Count);

            for (int i = 0; i < count; i++)
            {
                string name = "Name_" + i;
                // Remove should be case insensitive
                if (i == 0)
                {
                    nameValueCollection.Remove(name.ToUpperInvariant());
                }
                else if (i == 1)
                {
                    nameValueCollection.Remove(name.ToLowerInvariant());
                }
                else
                {
                    nameValueCollection.Remove(name);
                }
                Assert.Equal(count - i - 1, nameValueCollection.Count);
                Assert.Equal(count - i - 1, nameValueCollection.AllKeys.Length);
                Assert.Equal(count - i - 1, nameValueCollection.Keys.Count);

                Assert.Null(nameValueCollection[name]);
                Assert.Null(nameValueCollection.Get(name));

                Assert.DoesNotContain(name, nameValueCollection.AllKeys);
                Assert.DoesNotContain(name, nameValueCollection.Keys.Cast<string>());
            }
        }

        [Fact]
        public void Remove_MultipleValues_SameName()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            string name = "name";
            nameValueCollection.Add(name, "value1");
            nameValueCollection.Add(name, "value2");
            nameValueCollection.Add(name, "value3");

            nameValueCollection.Remove(name);
            Assert.Null(nameValueCollection[name]);
        }

        [Fact]
        public void Remove_NullName()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection.Add(null, "value");
            nameValueCollection.Remove(null);
            Assert.Equal(0, nameValueCollection.Count);
            Assert.Null(nameValueCollection[null]);
        }
    }
}
