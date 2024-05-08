// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static partial class MatchCollectionTests
    {
        [Fact]
        public static void IListOfT_Item_Get()
        {
            IList<Match> collection = CreateCollection();
            Assert.Equal("t", collection[0].ToString());
            Assert.Equal("t", collection[1].ToString());
        }

        [Fact]
        public static void IReadOnlyListOfT_Item_Get()
        {
            IReadOnlyList<Match> collection = CreateCollection();
            Assert.Equal("t", collection[0].ToString());
            Assert.Equal("t", collection[1].ToString());
        }

        [Fact]
        public static void IList_Item_Get()
        {
            IList collection = CreateCollection();
            Assert.Equal("t", collection[0].ToString());
            Assert.Equal("t", collection[1].ToString());
        }

        [Fact]
        public static void ICollectionOfT_Contains()
        {
            ICollection<Match> collection = CreateCollection();
            foreach (Match item in collection)
            {
                Assert.True(collection.Contains(item));
            }

            foreach (Match item in CreateCollection())
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
            IList<Match> collection = CreateCollection();

            int i = 0;
            foreach (Match item in collection)
            {
                Assert.Equal(i, collection.IndexOf(item));
                i++;
            }

            foreach (Match item in CreateCollection())
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
            string[] expected = ["t", "t"];
            ICollection<Match> collection = CreateCollection();

            Match[] array = new Match[collection.Count];
            collection.CopyTo(array, 0);

            Assert.Equal(expected, array.Select(c => c.ToString()));
        }

        [Fact]
        public static void ICollectionOfT_CopyTo_Invalid()
        {
            ICollection<Match> collection = CreateCollection();
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(new Match[1], -1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => collection.CopyTo(new Match[1], 0));
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => collection.CopyTo(new Match[1], 1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => collection.CopyTo(new Match[1], 2));
        }

        [Fact]
        public static void IListOfT_IsReadOnly()
        {
            IList<Match> list = CreateCollection();
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(default(Match)));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(Match)));
            Assert.Throws<NotSupportedException>(() => list.Remove(default(Match)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default(Match));
        }

        [Fact]
        public static void IList_IsReadOnly()
        {
            IList list = CreateCollection();
            Assert.True(list.IsReadOnly);
            Assert.True(list.IsFixedSize);
            Assert.Throws<NotSupportedException>(() => list.Add(default(Match)));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(Match)));
            Assert.Throws<NotSupportedException>(() => list.Remove(default(Match)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default(Match));
        }

        [Fact]
        public static void DebuggerAttributeTests()
        {
            MatchCollection col = CreateCollection();
            DebuggerAttributes.ValidateDebuggerDisplayReferences(col);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(col);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Match[] items = itemProperty.GetValue(info.Instance) as Match[];
            Assert.Equal(col, items);
        }

        [Fact]
        public static void DebuggerAttributeTests_Null()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.CreateDebuggerTypeProxyWithNullArgument(typeof(MatchCollection)));
            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }
    }
}
