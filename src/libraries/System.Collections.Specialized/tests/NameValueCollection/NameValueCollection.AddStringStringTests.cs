// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionAddStringStringTests
    {
        [Fact]
        public void Add()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            Assert.False(nameValueCollection.HasKeys());
            for (int i = 0; i < 10; i++)
            {
                string name = "Name_" + i;
                string value = "Value_" + i;
                nameValueCollection.Add(name, value);
                Assert.Equal(i + 1, nameValueCollection.Count);
                Assert.Equal(i + 1, nameValueCollection.AllKeys.Length);
                Assert.Equal(i + 1, nameValueCollection.Keys.Count);

                // We should be able to access values by the name
                Assert.Equal(value, nameValueCollection[name]);
                Assert.Equal(value, nameValueCollection.Get(name));

                Assert.Contains(name, nameValueCollection.AllKeys);
                Assert.Contains(name, nameValueCollection.Keys.Cast<string>());

                Assert.Equal(new string[] { value }, nameValueCollection.GetValues(name));

                // Get(string), GetValues(string) and this[string] should be case insensitive
                Assert.Equal(value, nameValueCollection[name.ToUpperInvariant()]);
                Assert.Equal(value, nameValueCollection[name.ToLowerInvariant()]);

                Assert.Equal(value, nameValueCollection.Get(name.ToUpperInvariant()));
                Assert.Equal(value, nameValueCollection.Get(name.ToLowerInvariant()));

                Assert.Equal(new string[] { value }, nameValueCollection.GetValues(name.ToUpperInvariant()));
                Assert.Equal(new string[] { value }, nameValueCollection.GetValues(name.ToLowerInvariant()));

                Assert.DoesNotContain(name.ToUpperInvariant(), nameValueCollection.AllKeys);
                Assert.DoesNotContain(name.ToLowerInvariant(), nameValueCollection.AllKeys);

                Assert.DoesNotContain(name.ToUpperInvariant(), nameValueCollection.Keys.Cast<string>());
                Assert.DoesNotContain(name.ToLowerInvariant(), nameValueCollection.Keys.Cast<string>());

                // We should be able to access values and keys in the order they were added
                Assert.Equal(value, nameValueCollection[i]);
                Assert.Equal(value, nameValueCollection.Get(i));
                Assert.Equal(name, nameValueCollection.GetKey(i));
                Assert.Equal(new string[] { value }, nameValueCollection.GetValues(i));
            }
            Assert.True(nameValueCollection.HasKeys());
        }

        [Fact]
        public void Add_NullName()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            string value = "value";
            nameValueCollection.Add(null, value);
            Assert.Equal(1, nameValueCollection.Count);
            Assert.Equal(1, nameValueCollection.AllKeys.Length);
            Assert.Equal(1, nameValueCollection.Keys.Count);

            Assert.Contains(null, nameValueCollection.AllKeys);
            Assert.Contains(null, nameValueCollection.Keys.Cast<string>());

            Assert.Equal(value, nameValueCollection[null]);
            Assert.Equal(value, nameValueCollection.Get(null));

            Assert.Equal(value, nameValueCollection[0]);
            Assert.Equal(value, nameValueCollection.Get(0));

            Assert.Equal(new string[] { value }, nameValueCollection.GetValues(null));
            Assert.Equal(new string[] { value }, nameValueCollection.GetValues(0));

            Assert.Null(nameValueCollection.GetKey(0));
            Assert.False(nameValueCollection.HasKeys());
        }

        [Fact]
        public void Add_NullValue()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            string name = "name";
            nameValueCollection.Add(name, null);
            Assert.Equal(1, nameValueCollection.Count);
            Assert.Equal(1, nameValueCollection.AllKeys.Length);
            Assert.Equal(1, nameValueCollection.Keys.Count);

            Assert.Contains(name, nameValueCollection.AllKeys);
            Assert.Contains(name, nameValueCollection.Keys.Cast<string>());

            Assert.Null(nameValueCollection[name]);
            Assert.Null(nameValueCollection.Get(name));

            Assert.Null(nameValueCollection[0]);
            Assert.Null(nameValueCollection.Get(0));

            Assert.Null(nameValueCollection.GetValues(name));
            Assert.Null(nameValueCollection.GetValues(0));

            Assert.Equal(name, nameValueCollection.GetKey(0));
            Assert.True(nameValueCollection.HasKeys());
        }

        [Fact]
        public void Add_AddingValueToExistingName_AppendsValueToOriginalValue()
        {
            var nameValueCollection = new NameValueCollection();
            string name = "name";
            nameValueCollection.Add(name, "value1");
            nameValueCollection.Add(name, "value2");
            nameValueCollection.Add(name, null);

            Assert.Equal(1, nameValueCollection.Count);
            Assert.Equal(1, nameValueCollection.AllKeys.Length);
            Assert.Equal(1, nameValueCollection.Keys.Count);

            Assert.Contains(name, nameValueCollection.AllKeys);
            Assert.Contains(name, nameValueCollection.Keys.Cast<string>());

            string[] expected = new string[] { "value1", "value2" };
            string expectedString = string.Join(",", expected);

            Assert.Equal(expectedString, nameValueCollection[name]);
            Assert.Equal(expectedString, nameValueCollection.Get(name));

            Assert.Equal(expectedString, nameValueCollection[0]);
            Assert.Equal(expectedString, nameValueCollection.Get(0));

            Assert.Equal(expected, nameValueCollection.GetValues(name));
            Assert.Equal(expected, nameValueCollection.GetValues(0));

            Assert.Equal(name, nameValueCollection.GetKey(0));
            Assert.True(nameValueCollection.HasKeys());
        }
    }
}
