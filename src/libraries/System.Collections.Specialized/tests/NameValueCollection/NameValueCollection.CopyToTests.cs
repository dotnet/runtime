// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class NameValueCollectionCopyToTests
    {
        [Theory]
        [InlineData(0, 0)]
        [InlineData(0, 1)]
        [InlineData(5, 0)]
        [InlineData(5, 1)]
        public void CopyTo(int count, int index)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            string[] dest = new string[count + index + 5];
            nameValueCollection.CopyTo(dest, index);
            for (int i = 0; i < index; i++)
            {
                Assert.Null(dest[i]);
            }
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(nameValueCollection.Get(i), dest[i + index]);
            }
            for (int i = index + count; i < dest.Length; i++)
            {
                Assert.Null(dest[i]);
            }

            nameValueCollection.CopyTo(dest, index);
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(nameValueCollection.Get(i), dest[i + index]);
            }
        }

        [Fact]
        public void CopyTo_MultipleValues_SameName()
        {
            NameValueCollection nameValueCollection = new NameValueCollection();
            string name = "name";
            nameValueCollection.Add(name, "value1");
            nameValueCollection.Add(name, "value2");
            nameValueCollection.Add(name, "value3");

            string[] dest = new string[1];
            nameValueCollection.CopyTo(dest, 0);
            Assert.Equal(nameValueCollection[0], dest[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void CopyTo_Invalid(int count)
        {
            NameValueCollection nameValueCollection = Helpers.CreateNameValueCollection(count);
            AssertExtensions.Throws<ArgumentNullException>("dest", () => nameValueCollection.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentException>("dest", null, () => nameValueCollection.CopyTo(new string[count, count], 0)); // in .NET Framework when passing multidimensional arrays Exception.ParamName is null.

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => nameValueCollection.CopyTo(new string[count], -1));

            AssertExtensions.Throws<ArgumentException>(null, () => nameValueCollection.CopyTo(new string[count], 1));
            AssertExtensions.Throws<ArgumentException>(null, () => nameValueCollection.CopyTo(new string[count], count + 1));
            if (count > 0)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => nameValueCollection.CopyTo(new string[count], count));
                Assert.Throws<InvalidCastException>(() => nameValueCollection.CopyTo(new DictionaryEntry[count], 0));
            }
            else
            {
                // InvalidCastException should not throw for an empty NameValueCollection
                nameValueCollection.CopyTo(new DictionaryEntry[count], 0);
            }
        }
    }
}
