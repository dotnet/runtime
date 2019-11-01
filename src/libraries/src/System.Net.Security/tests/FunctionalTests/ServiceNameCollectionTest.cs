// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace System.Security.Authentication.ExtendedProtection.Tests
{
    public class ServiceNameCollectionTest
    {
        [Fact]
        public void Constructor_NullParam_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ServiceNameCollection(null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Constructor_CollectionContainsNullOrEmpty_Throws(string item)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new ServiceNameCollection(new[] { item }));
            AssertExtensions.Throws<ArgumentException>(null, () => new ServiceNameCollection(new[] { "first", item }));
            AssertExtensions.Throws<ArgumentException>(null, () => new ServiceNameCollection(new[] { item, "second" }));
        }

        [Fact]
        public void Constructor_NonStringCollection_Throws()
        {
            Assert.Throws<InvalidCastException>(() => new ServiceNameCollection(new[] { 1 }));
            Assert.Throws<InvalidCastException>(() => new ServiceNameCollection(new[] { new object() }));
        }

        [Fact]
        public void Constructor_NonStringEmptyCollection_Success()
        {
            new ServiceNameCollection(Array.Empty<int>());
            new ServiceNameCollection(new List<int>());
        }

        public static object[][] ConstructorCollectionsTestData =
        {
            new object[] { Array.Empty<string>(), Array.Empty<string>() },
            new object[] { Array.Empty<string>(), new List<string>() },
            new object[] { Array.Empty<string>(), new ServiceNameCollection(Array.Empty<string>()) },

            new object[] { new[] { "first", "second" }, new[] { "first", "second" } },
            new object[] { new[] { "first", "second" }, new List<string> { "first", "second" } },
            new object[] { new[] { "first", "second" }, new ServiceNameCollection(new[] { "first", "second" }) },

            new object[] { new[] { "first", "second" }, new[] { "first", "second", "first", "SECOND" } },
            new object[] { new[] { "first", "second" }, new List<string> { "first", "second", "first", "SECOND" } },
        };

        [Theory]
        [MemberData(nameof(ConstructorCollectionsTestData))]
        public void Constructor_VariousCollections_Success(string[] expected, ICollection items)
        {
            var collection = new ServiceNameCollection(items);
            Assert.Equal(expected, collection.Cast<string>());
            Assert.Equal(expected.Length, collection.Count);
        }

        [Fact]
        public void IsSynchronized_ReturnsFalse()
        {
            ICollection collection = new ServiceNameCollection(new[] { "first", "second" });
            Assert.False(collection.IsSynchronized);
        }

        [Fact]
        public void SyncRoot_NonNullSameObject()
        {
            ICollection collection = new ServiceNameCollection(new[] { "first", "second" });
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Fact]
        public void Contains_Found()
        {
            var collection = new ServiceNameCollection(new[] { "first", "second", "localhost:3000/test", "www.test.com" });
            Assert.True(collection.Contains("first"));
            Assert.True(collection.Contains("second"));
            Assert.True(collection.Contains("localhost:3000/test"));
            Assert.True(collection.Contains("www.test.com"));
        }

        [Fact]
        public void Contains_NotFound()
        {
            var collection = new ServiceNameCollection(new[] { "first", "second" });
            Assert.False(collection.Contains(null));
            Assert.False(collection.Contains(string.Empty));
            Assert.False(collection.Contains("third"));
            Assert.False(collection.Contains("localhost:3000//test"));
        }

        [Fact]
        public void CopyTo_ValidDestination_Success()
        {
            string[] expected = new[] { "first", "second" };
            ICollection collection = new ServiceNameCollection(expected);
            string[] destination = new string[collection.Count];

            collection.CopyTo(destination, 0);

            Assert.Equal(expected, destination);
        }

        [Fact]
        public void CopyTo_NullDestination_Throws()
        {
            ICollection collection = new ServiceNameCollection(new[] { "first", "second" });
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
        }

        [Fact]
        public void CopyTo_DestinationTooSmall_Throws()
        {
            ICollection collection = new ServiceNameCollection(new[] { "first", "second" });
            int[] destination = new int[collection.Count - 1];
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(destination, 0));
        }

        [Fact]
        public void CopyTo_InvalidIndex_Throws()
        {
            ICollection collection = new ServiceNameCollection(new[] { "first", "second" });
            int[] destination = new int[collection.Count];
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(destination, -1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(destination, destination.Length));
        }

        [Fact]
        public void CopyTo_InvalidDestinationType_Throws()
        {
            ICollection collection = new ServiceNameCollection(new[] { "first", "second" });
            int[] destination = new int[collection.Count];

            Assert.Throws<InvalidCastException>(() => collection.CopyTo(destination, 0));
        }

        [Fact]
        public void Enumerator_BehavesAsExpected()
        {
            string item1 = "first";
            string item2 = "second";
            string item3 = "third";

            var collection = new ServiceNameCollection(new[] { item1, item2, item3 });

            IEnumerator e = collection.GetEnumerator();

            for (int i = 0; i < 2; i++)
            {
                // Not started
                Assert.Throws<InvalidOperationException>(() => e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item1, e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item2, e.Current);

                Assert.True(e.MoveNext());
                Assert.Same(item3, e.Current);

                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());
                Assert.False(e.MoveNext());

                // Ended
                Assert.Throws<InvalidOperationException>(() => e.Current);

                e.Reset();
            }
        }

        [Fact]
        public void Merge_NullOrEmptyString_Throws()
        {
            var collection = new ServiceNameCollection(new[] { "first", "second" });
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Merge((string)null));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Merge(string.Empty));
        }

        [Fact]
        public void Merge_NonDuplicateString_Succeeds()
        {
            string[] expected = new[] { "first", "second", "third" };
            var collection = new ServiceNameCollection(new[] { "first", "second" });

            ServiceNameCollection merged = collection.Merge("third");
            Assert.Equal(expected, merged.Cast<string>());
        }

        [Theory]
        [InlineData("first")]
        [InlineData("SECOND")]
        public void Merge_DuplicateString_DuplicateFiltered(string duplicate)
        {
            string[] expected = new[] { "first", "second" };
            var collection = new ServiceNameCollection(new[] { "first", "second" });

            ServiceNameCollection merged = collection.Merge(duplicate);
            Assert.Equal(expected, merged.Cast<string>());
        }

        [Fact]
        public void Merge_NullEnumerable_Throws()
        {
            var collection = new ServiceNameCollection(new[] { "first", "second" });

            // This really should be ArgumentNullException, but the full framework does not
            // guard against null, so NullReferenceException ends up being thrown instead.
            Assert.Throws<NullReferenceException>(() => collection.Merge((IEnumerable)null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Merge_EnumerableContainingNullOrEmpty_Throws(string item)
        {
            var collection = new ServiceNameCollection(new[] { "first", "second" });
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Merge(new[] { item }));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Merge(new[] { "third", item }));
        }

        [Fact]
        public void Merge_NonStringEnumerable_Throws()
        {
            var collection = new ServiceNameCollection(new[] { "first", "second" });
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Merge(new[] { 3 }));
            AssertExtensions.Throws<ArgumentException>(null, () => collection.Merge(new[] { new object() }));
        }

        public static object[][] MergeCollectionsTestData =
        {
            new object[] { new[] { "first", "second", "third", "forth" }, new[] { "third", "forth" } },
            new object[] { new[] { "first", "second", "third", "forth" }, new List<string> { "third", "forth" } },
            new object[] { new[] { "first", "second", "third", "forth" }, new ServiceNameCollection(new[] { "third", "forth" }) },

            new object[] { new[] { "first", "second", "third", "forth" }, new[] { "third", "forth", "THIRD", "forth" } },
            new object[] { new[] { "first", "second", "third", "forth" }, new List<string> { "third", "forth", "THIRD", "forth" } },
        };

        [Theory]
        [MemberData(nameof(MergeCollectionsTestData))]
        public void Merge_VariousEnumerables_Success(string[] expected, IEnumerable serviceNames)
        {
            var collection = new ServiceNameCollection(new[] { "first", "second" });
            ServiceNameCollection merged = collection.Merge(serviceNames);
            Assert.Equal(expected, merged.Cast<string>());
            Assert.Equal(expected.Length, merged.Count);
        }
    }
}
