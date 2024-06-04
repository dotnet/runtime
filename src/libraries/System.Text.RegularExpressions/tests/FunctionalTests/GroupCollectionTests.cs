// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static partial class GroupCollectionTests
    {
        [Fact]
        public static void GetEnumerator()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");

            GroupCollection groups = match.Groups;
            IEnumerator enumerator = groups.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(groups[counter], enumerator.Current);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(groups.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Interface not implemented on .NET Framework")]
        public static void GetEnumerator_Generic()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");

            GroupCollection groups = match.Groups;
            IEnumerator<KeyValuePair<string, Group>> enumerator = ((IEnumerable<KeyValuePair<string, Group>>)groups).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(groups[counter], enumerator.Current.Value);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(groups.Count, counter);
                enumerator.Reset();
            }
        }

        [Fact]
        public static void GetEnumerator_Invalid()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");

            IEnumerator enumerator = match.Groups.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Interface not implemented on .NET Framework")]
        public static void GetEnumerator_Generic_Invalid()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            Match match = regex.Match("aaabbccccccccccaaaabc");

            IEnumerator<KeyValuePair<string, Group>> enumerator = ((IEnumerable<KeyValuePair<string, Group>>)match.Groups).GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void Item_Get()
        {
            GroupCollection collection = CreateCollection();
            Assert.Equal("212-555-6666", collection[0].ToString());
            Assert.Equal("212", collection[1].ToString());
            Assert.Equal("555-6666", collection[2].ToString());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Interface not implemented on .NET Framework")]
        public static void ContainsKey()
        {
            IReadOnlyDictionary<string, Group> collection = (IReadOnlyDictionary<string, Group>)CreateCollection();
            Assert.True(collection.ContainsKey("0"));
            Assert.True(collection.ContainsKey("1"));
            Assert.True(collection.ContainsKey("2"));
            Assert.False(collection.ContainsKey("3"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public static void Item_Invalid(int groupNumber)
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            GroupCollection groups = regex.Match("aaabbccccccccccaaaabc").Groups;

            Group group = groups[groupNumber];
            Assert.Same(string.Empty, group.Value);
            Assert.Equal(0, group.Index);
            Assert.Equal(0, group.Length);
            Assert.Equal(0, group.Captures.Count);
        }

        [Fact]
        public static void ICollection_Properties()
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            GroupCollection groups = regex.Match("aaabbccccccccccaaaabc").Groups;
            ICollection collection = groups;

            Assert.False(collection.IsSynchronized);
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(collection.SyncRoot, collection.SyncRoot);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public static void ICollection_CopyTo(int index)
        {
            Regex regex = new Regex(@"(?<A1>a*)(?<A2>b*)(?<A3>c*)");
            GroupCollection groups = regex.Match("aaabbccccccccccaaaabc").Groups;
            ICollection collection = groups;

            Group[] copy = new Group[collection.Count + index];
            collection.CopyTo(copy, index);
            for (int i = 0; i < index; i++)
            {
                Assert.Null(copy[i]);
            }
            for (int i = index; i < copy.Length; i++)
            {
                Assert.Same(groups[i - index], copy[i]);
            }
        }

        [Fact]
        public static void ICollection_CopyTo_Invalid()
        {
            Regex regex = new Regex("e");
            ICollection collection = regex.Match("aaabbccccccccccaaaabc").Groups;

            // Array is null
            AssertExtensions.Throws<ArgumentNullException>("array", () => collection.CopyTo(null, 0));

            // Array is multidimensional
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new object[10, 10], 0));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has a non-zero lower bound
                Array o = Array.CreateInstance(typeof(object), [10], [10]);
                Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(o, 0));
            }

            // Index < 0
            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(new object[collection.Count], -1));

            // Invalid index + length
            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(new object[collection.Count], 1));
            Assert.Throws<IndexOutOfRangeException>(() => collection.CopyTo(new object[collection.Count + 1], 2));
        }

        private static GroupCollection CreateCollection()
        {
            Regex regex = new Regex(@"(\d{3})-(\d{3}-\d{4})");
            Match match = regex.Match("212-555-6666");
            return match.Groups;
        }
    }
}
