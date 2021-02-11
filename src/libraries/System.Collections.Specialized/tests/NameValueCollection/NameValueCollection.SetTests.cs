// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionSetTests
    {
        [Fact]
        public void Set()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            int newCount = 10;
            for (int i = 0; i < newCount; i++)
            {
                string newName = "Name_" + i;
                string newValue = "Value_" + i;
                nameValueCollection.Set(newName, newValue);

                Assert.Equal(i + 1, nameValueCollection.Count);
                Assert.Equal(newValue, nameValueCollection.Get(newName));
            }
        }

        [Fact]
        public void Set_OvewriteExistingValue()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            string name = "name";
            string value = "value";
            nameValueCollection.Add(name, "old-value");

            nameValueCollection.Set(name, value);
            Assert.Equal(value, nameValueCollection.Get(name));
            Assert.Equal(new string[] { value }, nameValueCollection.GetValues(name));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Set_NullName(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);

            string nullNameValue = "value";
            nameValueCollection.Set(null, nullNameValue);
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Equal(nullNameValue, nameValueCollection.Get(null));

            string newNullNameValue = "newvalue";
            nameValueCollection.Set(null, newNullNameValue);
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Equal(newNullNameValue, nameValueCollection.Get(null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Set_NullValue(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);

            string nullValueName = "name";
            nameValueCollection.Set(nullValueName, null);
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Null(nameValueCollection.Get(nullValueName));

            nameValueCollection.Set(nullValueName, "abc");
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Equal("abc", nameValueCollection.Get(nullValueName));

            nameValueCollection.Set(nullValueName, null);
            Assert.Equal(count + 1, nameValueCollection.Count);
            Assert.Null(nameValueCollection.Get(nullValueName));
        }

        [Fact]
        public void Set_IsCaseSensitive()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            nameValueCollection.Set("name", "value1");
            nameValueCollection.Set("Name", "value2");
            nameValueCollection.Set("NAME", "value3");
            Assert.Equal(1, nameValueCollection.Count);
            Assert.Equal("value3", nameValueCollection.Get("name"));
        }
    }
}
