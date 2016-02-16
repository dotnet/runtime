// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Specialized;

using Xunit;

namespace System.Collections.Tests
{
    public static class CollectionsUtilTests
    {
        [Fact]
        public static void TestCreateCaseInsensitiveHashtable()
        {
            Hashtable hashtable = CollectionsUtil.CreateCaseInsensitiveHashtable();
            Assert.Equal(0, hashtable.Count);

            hashtable.Add("key1", "value1");
            Assert.Equal("value1", hashtable["key1"]);

            Assert.Throws<ArgumentException>(() => hashtable.Add("key1", "value1"));
        }

        [Fact]
        public static void TestCreateCaseInsensitiveHashtable_Capacity()
        {
            Hashtable hashtable = CollectionsUtil.CreateCaseInsensitiveHashtable(15);
            Assert.Equal(0, hashtable.Count);

            hashtable.Add("key1", "value1");
            Assert.Equal("value1", hashtable["key1"]);
            Assert.Equal(1, hashtable.Count);

            Assert.Throws<ArgumentException>(() => hashtable.Add("key1", "value1"));
        }

        [Fact]
        public static void TestCreateCaseInsensitiveHashtable_IDictionary()
        {
            Hashtable hashtable1 = CollectionsUtil.CreateCaseInsensitiveHashtable();
            hashtable1.Add("key1", "value1");

            Hashtable hashtable2 = CollectionsUtil.CreateCaseInsensitiveHashtable(hashtable1);
            Assert.Equal(1, hashtable2.Count);

            hashtable2.Add("key2", "value2");
            Assert.Equal("value1", hashtable2["key1"]);
            Assert.Equal(2, hashtable2.Count);

            Assert.Throws<ArgumentException>(() => hashtable2.Add("key1", "value1"));
        }

        [Fact]
        public static void TestCreateCaseInsensitiveSortedList()
        {
            SortedList sortedList = CollectionsUtil.CreateCaseInsensitiveSortedList();
            Assert.Equal(0, sortedList.Count);

            sortedList.Add("key1", "value1");
            Assert.Equal("value1", sortedList["key1"]);
            Assert.Equal(1, sortedList.Count);

            Assert.Throws<ArgumentException>(() => sortedList.Add("key1", "value1"));
        }
    }
}
