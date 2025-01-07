// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static partial class MatchCollectionTests
    {
        public static IEnumerable<object[]> NoneCompiledBacktracking()
        {
            yield return new object[] { RegexOptions.None };
            yield return new object[] { RegexOptions.Compiled };
            if (PlatformDetection.IsNetCore)
            {
                yield return new object[] { RegexHelpers.RegexOptionNonBacktracking };
            }
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public static void GetEnumerator(RegexOptions options)
        {
            Regex regex = new Regex("e", options);
            MatchCollection matches = regex.Matches("dotnet");
            IEnumerator enumerator = matches.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Same(matches[counter], enumerator.Current);
                    counter++;
                }
                Assert.False(enumerator.MoveNext());
                Assert.Equal(matches.Count, counter);
                enumerator.Reset();
            }
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public static void GetEnumerator_Invalid(RegexOptions options)
        {
            Regex regex = new Regex("e", options);
            MatchCollection matches = regex.Matches("dotnet");
            IEnumerator enumerator = matches.GetEnumerator();

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            while (enumerator.MoveNext()) ;
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            enumerator.Reset();
            Assert.True(enumerator.MoveNext());
            enumerator.Reset();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public static void Item_Get()
        {
            MatchCollection collection = CreateCollection();
            Assert.Equal("t", collection[0].ToString());
            Assert.Equal("t", collection[1].ToString());
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public static void Item_Get_InvalidIndex_ThrowsArgumentOutOfRangeException(RegexOptions options)
        {
            Regex regex = new Regex("e", options);
            MatchCollection matches = regex.Matches("dotnet");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("i", () => matches[-1]);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("i", () => matches[matches.Count]);
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public static void ICollection_Properties(RegexOptions options)
        {
            Regex regex = new Regex("e", options);
            MatchCollection matches = regex.Matches("dotnet");
            ICollection collection = matches;

            Assert.False(collection.IsSynchronized);
            Assert.NotNull(collection.SyncRoot);
            Assert.Same(matches, collection.SyncRoot);
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public static void ICollection_CopyTo(RegexOptions options)
        {
            foreach (int index in new[] { 0, 5 })
            {
                Regex regex = new Regex("e", options);
                MatchCollection matches = regex.Matches("dotnet");
                ICollection collection = matches;

                Match[] copy = new Match[collection.Count + index];
                collection.CopyTo(copy, index);

                for (int i = 0; i < index; i++)
                {
                    Assert.Null(copy[i]);
                }
                for (int i = index; i < copy.Length; i++)
                {
                    Assert.Same(matches[i - index], copy[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(NoneCompiledBacktracking))]
        public static void ICollection_CopyTo_Invalid(RegexOptions options)
        {
            Regex regex = new Regex("e", options);
            ICollection collection = regex.Matches("dotnet");

            // Array is null
            AssertExtensions.Throws<ArgumentNullException>("destinationArray", "dest", () => collection.CopyTo(null, 0));

            // Array is multidimensional
            AssertExtensions.Throws<ArgumentException>(null, () => collection.CopyTo(new object[10, 10], 0));

            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                // Array has a non-zero lower bound
                Array o = Array.CreateInstance(typeof(object), [10], [10]);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", "dstIndex", () => collection.CopyTo(o, 0));
            }

            // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", "dstIndex", () => collection.CopyTo(new object[collection.Count], -1));

            // Invalid index + length
            AssertExtensions.Throws<ArgumentException>("destinationArray", string.Empty, () => collection.CopyTo(new object[collection.Count], 1));
            AssertExtensions.Throws<ArgumentException>("destinationArray", string.Empty, () => collection.CopyTo(new object[collection.Count + 1], 2));
        }

        private static MatchCollection CreateCollection() => new Regex("t").Matches("dotnet");
    }
}
