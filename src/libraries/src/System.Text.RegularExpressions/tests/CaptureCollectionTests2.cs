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
    public static partial class CaptureCollectionTests
    {
        [Fact]
        public static void IListOfT_Item_Get()
        {
            IList<Capture> collection = CreateCollection();
            Assert.Equal("This ", collection[0].ToString());
            Assert.Equal("is ", collection[1].ToString());
            Assert.Equal("a ", collection[2].ToString());
            Assert.Equal("sentence", collection[3].ToString());
        }

        [Fact]
        public static void IReadOnlyListOfT_Item_Get()
        {
            IReadOnlyList<Capture> collection = CreateCollection();
            Assert.Equal("This ", collection[0].ToString());
            Assert.Equal("is ", collection[1].ToString());
            Assert.Equal("a ", collection[2].ToString());
            Assert.Equal("sentence", collection[3].ToString());
        }

        [Fact]
        public static void IList_Item_Get()
        {
            IList collection = CreateCollection();
            Assert.Equal("This ", collection[0].ToString());
            Assert.Equal("is ", collection[1].ToString());
            Assert.Equal("a ", collection[2].ToString());
            Assert.Equal("sentence", collection[3].ToString());
        }

        [Fact]
        public static void ICollectionOfT_Contains()
        {
            ICollection<Capture> collection = CreateCollection();
            foreach (Capture item in collection)
            {
                Assert.True(collection.Contains(item));
            }

            foreach (Capture item in CreateCollection())
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
            IList<Capture> collection = CreateCollection();

            int i = 0;
            foreach (Capture item in collection)
            {
                Assert.Equal(i, collection.IndexOf(item));
                i++;
            }

            foreach (Capture item in CreateCollection())
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
            string[] expected = new[] { "This ", "is ", "a ", "sentence" };
            ICollection<Capture> collection = CreateCollection();

            Capture[] array = new Capture[collection.Count];
            collection.CopyTo(array, 0);

            Assert.Equal(expected, array.Select(c => c.ToString()));
        }

        [Fact]
        public static void ICollectionOfT_CopyTo_Invalid()
        {
            ICollection<Capture> collection = CreateCollection();
            AssertExtensions.Throws<ArgumentNullException>("array", () => collection.CopyTo((Capture[])null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => collection.CopyTo(new Capture[1], -1));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new Capture[1], 0));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new Capture[1], 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("arrayIndex", () => collection.CopyTo(new Capture[1], 2));
        }

        [Fact]
        public static void IListOfT_IsReadOnly()
        {
            IList<Capture> list = CreateCollection();
            Assert.True(list.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => list.Add(default(Capture)));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(Capture)));
            Assert.Throws<NotSupportedException>(() => list.Remove(default(Capture)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default(Capture));
        }

        [Fact]
        public static void IList_IsReadOnly()
        {
            IList list = CreateCollection();
            Assert.True(list.IsReadOnly);
            Assert.True(list.IsFixedSize);
            Assert.Throws<NotSupportedException>(() => list.Add(default(Capture)));
            Assert.Throws<NotSupportedException>(() => list.Clear());
            Assert.Throws<NotSupportedException>(() => list.Insert(0, default(Capture)));
            Assert.Throws<NotSupportedException>(() => list.Remove(default(Capture)));
            Assert.Throws<NotSupportedException>(() => list.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => list[0] = default(Capture));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot)]
        public static void DebuggerAttributeTests()
        {
            CaptureCollection col = CreateCollection();
            DebuggerAttributes.ValidateDebuggerDisplayReferences(col);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(col);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Capture[] items = itemProperty.GetValue(info.Instance) as Capture[];
            Assert.Equal(col, items);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot)]
        public static void DebuggerAttributeTests_Null()
        {
            TargetInvocationException ex = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.ValidateDebuggerTypeProxyProperties(typeof(CaptureCollection), null));
            Assert.IsType<ArgumentNullException>(ex.InnerException);
        }
    }
}
