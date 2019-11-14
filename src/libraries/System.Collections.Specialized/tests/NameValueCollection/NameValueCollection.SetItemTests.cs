// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionSetItemTests
    {
        [Fact]
        public void Item_Set()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            for (int i = 0; i < 10; i++)
            {
                string newName = "Name_" + i;
                string newValue = "Value_" + i;
                nameValueCollection[newName] = newValue;

                Assert.Equal(i + 1, nameValueCollection.Count);
                Assert.Equal(newValue, nameValueCollection[newName]);
            }
        }

        [Fact]
        public void Item_Set_OvewriteExistingValue()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            string name = "name";
            string value = "value";
            nameValueCollection.Add(name, "old-value");

            nameValueCollection[name] = value;
            Assert.Equal(value, nameValueCollection[name]);
            Assert.Equal(new string[] { value }, nameValueCollection.GetValues(name));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Set_NullName(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);

            string nullNameValue = "value";
            nameValueCollection[null] = nullNameValue;
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Equal(nullNameValue, nameValueCollection[null]);

            string newNullNameValue = "newvalue";
            nameValueCollection[null] = newNullNameValue;
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Equal(newNullNameValue, nameValueCollection[null]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Set_NullValue(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);

            string nullValueName = "name";
            nameValueCollection[nullValueName] = null;
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Null(nameValueCollection[nullValueName]);

            nameValueCollection[nullValueName] = "abc";
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Equal("abc", nameValueCollection[nullValueName]);

            nameValueCollection[nullValueName] = null;
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Null(nameValueCollection[nullValueName]);
        }

        [Fact]
        public void Item_Set_IsCaseSensitive()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection["name"] = "value1";
            nameValueCollection["Name"] = "value2";
            nameValueCollection["NAME"] = "value3";
            Assert.Equal(1, nameValueCollection.Count);
            Assert.Equal("value3", nameValueCollection["name"]);
        }
    }
}
