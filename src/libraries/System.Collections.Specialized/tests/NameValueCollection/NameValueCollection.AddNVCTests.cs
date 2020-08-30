// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionAddNameValueCollectionTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 5)]
        [InlineData(5, 0)]
        [InlineData(5, 5)]
        public void Add(int count1, int count2)
        {
            NameValueCollection nameValueCollection1 = Helpers.CreateNameValueCollection(count1);
            NameValueCollection nameValueCollection2 = Helpers.CreateNameValueCollection(count2, count1);

            nameValueCollection2.Add(nameValueCollection1);
            Assert.Equal(count1 + count2, nameValueCollection2.Count);
            Assert.Equal(count1 + count2, nameValueCollection2.AllKeys.Length);
            Assert.Equal(count1 + count2, nameValueCollection2.Keys.Count);

            for (int i = 0; i < count1; i++)
            {
                string name = nameValueCollection1.GetKey(i);
                string value = nameValueCollection1.Get(i);
                Assert.Contains(name, nameValueCollection2.AllKeys);
                Assert.Contains(name, nameValueCollection2.Keys.Cast<string>());

                Assert.Equal(value, nameValueCollection2.Get(name));
                Assert.Equal(value, nameValueCollection2[name]);

                Assert.Equal(value, nameValueCollection2.Get(i + count2));
                Assert.Equal(value, nameValueCollection2[i + count2]);

                Assert.Equal(name, nameValueCollection2.GetKey(i + count2));

                Assert.Equal(new string[] { value }, nameValueCollection2.GetValues(name));
                Assert.Equal(new string[] { value }, nameValueCollection2.GetValues(i + count2));
            }
        }

        [Fact]
        public void Add_ExistingKeys()
        {
            NameValueCollection nameValueCollection1 = new NameValueCollection();
            NameValueCollection nameValueCollection2 = new NameValueCollection();

            string name = "name";
            string value1 = "value1";
            string value2 = "value2";
            nameValueCollection1.Add(name, value1);
            nameValueCollection2.Add(name, value2);

            nameValueCollection2.Add(nameValueCollection1);
            Assert.Equal(1, nameValueCollection2.Count);
            Assert.Equal(value2 + "," + value1, nameValueCollection2[name]);
            Assert.Equal(new string[] { value2, value1 }, nameValueCollection2.GetValues(name));
        }

        [Fact]
        public void Add_MultipleValues()
        {
            NameValueCollection nameValueCollection1 = new NameValueCollection();
            NameValueCollection nameValueCollection2 = new NameValueCollection();

            string name = "name";
            string value1 = "value1";
            string value2 = "value2";
            nameValueCollection1.Add(name, value1);
            nameValueCollection1.Add(name, value2);

            nameValueCollection2.Add(nameValueCollection1);
            Assert.Equal(1, nameValueCollection2.Count);
            Assert.Equal(value1 + "," + value2, nameValueCollection2[name]);
            Assert.Equal(new string[] { value1, value2 }, nameValueCollection2.GetValues(name));

        }

        [Fact]
        public void Add_NameValueCollection_WithNullKeys()
        {
            NameValueCollection nameValueCollection1 = new NameValueCollection();
            NameValueCollection nameValueCollection2 = new NameValueCollection();
            NameValueCollection nameValueCollection3 = new NameValueCollection();


            string nullKeyValue1 = "value";
            string nullKeyValue2 = "value";
            nameValueCollection1.Add(null, nullKeyValue1);
            nameValueCollection2.Add(null, nullKeyValue2);

            nameValueCollection3.Add(nameValueCollection1);

            Assert.Equal(1, nameValueCollection2.Count);
            Assert.Contains(null, nameValueCollection3.AllKeys);
            Assert.Equal(nullKeyValue1, nameValueCollection3[null]);

            nameValueCollection3.Add(nameValueCollection2);
            Assert.Contains(null, nameValueCollection3.AllKeys);
            Assert.Equal(nullKeyValue1 + "," + nullKeyValue2, nameValueCollection3[null]);
        }

        [Fact]
        public void Add_NameValueCollection_WithNullValues()
        {
            NameValueCollection nameValueCollection1 = new NameValueCollection();
            NameValueCollection nameValueCollection2 = new NameValueCollection();

            string nullValueName = "name";
            nameValueCollection1.Add(nullValueName, null);
            nameValueCollection2.Add(nameValueCollection1);

            Assert.Equal(1, nameValueCollection2.Count);
            Assert.Contains(nullValueName, nameValueCollection2.AllKeys);
            Assert.Null(nameValueCollection2[nullValueName]);
        }

        [Fact]
        public void Add_NullNameValueCollection_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("c", () => new NameValueCollection().Add(null));
        }
    }
}
