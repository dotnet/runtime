// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static partial class GroupCollectionTests
    {
        [Fact]
        public static void IListOfT_Item_Get()
        {
            IList<Group> collection = CreateCollection();
            Assert.Equal("212-555-6666", collection[0].ToString());
            Assert.Equal("212", collection[1].ToString());
            Assert.Equal("555-6666", collection[2].ToString());
        }

        [Fact]
        public static void IReadOnlyListOfT_Item_Get()
        {
            IReadOnlyList<Group> collection = CreateCollection();
            Assert.Equal("212-555-6666", collection[0].ToString());
            Assert.Equal("212", collection[1].ToString());
            Assert.Equal("555-6666", collection[2].ToString());
        }

        [Fact]
        public static void IList_Item_Get()
        {
            IList collection = CreateCollection();
            Assert.Equal("212-555-6666", collection[0].ToString());
            Assert.Equal("212", collection[1].ToString());
            Assert.Equal("555-6666", collection[2].ToString());
        }

        [Fact]
        public static void ICollectionOfT_Contains()
        {
            ICollection<Group> collection = CreateCollection();
            foreach (Group item in collection)
            {
                Assert.True(collection.Contains(item));
            }

            foreach (Group item in CreateCollection())
            {
                Assert.False(collection.Contains(item));
            }
        }

        [Fact]
        public static void IList_Contains()
        {
            IList collection = CreateCollection();
            foreach (object item in collection)
            {
                Assert.True(collection.Contains(item));
            }

            foreach (object item in CreateCollection())
            {
                Assert.False(collection.Contains(item));
            }

            Assert.False(collection.Contains(new object()));
        }

        [Fact]
        public static void IListOfT_IndexOf()
        {
            IList<Group> collection = CreateCollection();

            int i = 0;
            foreach (Group item in collection)
            {
                Assert.Equal(i, collection.IndexOf(item));
                i++;
            }

            foreach (Group item in CreateCollection())
            {
                Assert.Equal(-1, collection.IndexOf(item));
            }
        }

        [Fact]
        public static void IList_IndexOf()
        {
            IList collection = CreateCollection();

            int i = 0;
            foreach (object item in collection)
            {
                Assert.Equal(i, collection.IndexOf(item));
                i++;
            }

            foreach (object item in CreateCollection())
            {
                Assert.Equal(-1, collection.IndexOf(item));
            }

            Assert.Equal(-1, collection.IndexOf(new object()));
        }

        [Fact]
        public static void ICollectionOfT_CopyTo()
        {
            string[] expected = new[] { "212-555-6666", "212", "555-6666" };
            ICollection<Group> collection = CreateCollection();

            Group[] array = new Group[collection.Count];
            collection.CopyTo(array, 0);

            Assert.Equal(expected, array.Select(c => c.ToString()));
        }

        [Fact]
        public static void ICollectionOfT_CopyTo_Invalid()
        {
            ICollection<Group> collection = CreateCollection();
            AssertExtensions.Throws<ArgumentNullException>("array", () => collection.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => collection.CopyTo(new Group[1], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new Group[1], 0));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new Group[1], 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => collection.CopyTo(new Group[1], 2));
        }

        [Fact]
        public static void IListOfT_IsReadOnly()
        {
            IList<Group> list = CreateCollection();
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(default(Group)));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(Group)));
            Assert.Throws<NotSupportedException>(() => list.Remove(default(Group)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default(Group));
        }

        [Fact]
        public static void IList_IsReadOnly()
        {
            IList list = CreateCollection();
            Assert.True(list.IsReadOnly);
            Assert.True(list.IsFixedSize);
            Assert.Throws<NotSupportedException>(() => list.Add(default(Group)));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(Group)));
            Assert.Throws<NotSupportedException>(() => list.Remove(default(Group)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default(Group));
        }

        [Fact]
        public static void DebuggerAttributeTests()
        {
            GroupCollection col = CreateCollection();
            DebuggerAttributes.ValidateDebuggerDisplayReferences(col);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(col);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Group[] items = itemProperty.GetValue(info.Instance) as Group[];
            Assert.Equal(col, items);
        }

        [Fact]
        public static void DebuggerAttributeTests_Null()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(GroupCollection), null));
            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }
    }
}
