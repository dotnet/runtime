// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.ObjectModel.Tests
{
    /// <summary>
    /// Since <see cref="Collection{T}"/> is just a wrapper base class around an <see cref="IList{T}"/>,
    /// we just verify that the underlying list is what we expect, validate that the calls which
    /// we expect are forwarded to the underlying list, and verify that the exceptions we expect
    /// are thrown.
    /// </summary>
    public partial class CollectionTests : CollectionTestBase
    {
        [Fact]
        public void Collection_AddRange_ToEmpty_Test()
        {
            int[] expected = new[] { 1, 2, 3, 4 };
            Collection<int> collection = new Collection<int>();

            collection.AddRange(expected);

            Assert.NotNull(collection);
            Assert.Equal(expected.Length, collection.Count);
            Assert.Equal(expected, collection.ToArray());
        }

        [Fact]
        public void Collection_AddRange_ToExisting_Test()
        {
            int[] initial = new int[] { 1, 2, 3, 4 };
            int[] dataToInsert = new int[] { 5, 6, 7, 8 };
            Collection<int> collection = new Collection<int>();
            for (int i = 0; i < initial.Length; i++)
                collection.Add(initial[i]);

            collection.AddRange(dataToInsert);

            Assert.NotNull(collection);
            Assert.Equal(initial.Length + dataToInsert.Length, collection.Count);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(initial, collectionAssertion.AsSpan(0, 4).ToArray());
            Assert.Equal(dataToInsert, collectionAssertion.AsSpan(4, 4).ToArray());
        }

        [Fact]
        public void Collection_AddRange_Empty_Test()
        {
            int[] expected = new int[0];
            Collection<int> collection = new Collection<int>();

            collection.AddRange(expected);

            Assert.NotNull(collection);
            Assert.Equal(expected.Length, collection.Count);
        }

        [Fact]
        public void Collection_AddRange_Null_Test()
        {
            Collection<int> collection = new Collection<int>();
            Exception ex = Assert.Throws<ArgumentNullException>(() => collection.AddRange(null));
        }

        [Fact]
        public void Collection_AddRange_ReadOnly_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4 };
            int[] baseCollection = new int[] { 5, 6, 7, 8 };
            Collection<int> collection = new Collection<int>(baseCollection);

            Exception ex = Assert.Throws<NotSupportedException>(() => collection.AddRange(expected));
        }

        [Fact]
        public void Collection_InsertRange_Beginning_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4, 5 };
            int[] originalCollection = new int[] { 10, 11, 12, 13 };
            List<int> baseCollection = new List<int>(originalCollection);

            int expectedLength = expected.Length + originalCollection.Length;
            Collection<int> collection = new Collection<int>(baseCollection);

            collection.InsertRange(0, expected);

            Assert.NotNull(collection);
            Assert.Equal(expectedLength, collection.Count);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(expected, collectionAssertion.AsSpan(0, 5).ToArray());
            Assert.Equal(originalCollection, collectionAssertion.AsSpan(5, 4).ToArray());
        }

        [Fact]
        public void Collection_InsertRange_End_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4, 5 };
            int[] originalCollection = new int[] { 10, 11, 12, 13 };
            List<int> baseCollection = new List<int>(originalCollection);

            int expectedLength = expected.Length + originalCollection.Length;
            Collection<int> collection = new Collection<int>(baseCollection);

            collection.InsertRange(expected.Length - 1, expected);

            Assert.NotNull(collection);
            Assert.Equal(expectedLength, collection.Count);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(originalCollection, collectionAssertion.AsSpan(0, 4).ToArray());
            Assert.Equal(expected, collectionAssertion.AsSpan(4, 5).ToArray());
        }

        [Fact]
        public void Collection_InsertRange_Middle_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4, 5 };
            int[] originalCollection = new int[] { 10, 11, 12, 13 };
            List<int> baseCollection = new List<int>(originalCollection);

            int expectedLength = expected.Length + originalCollection.Length;
            Collection<int> collection = new Collection<int>(baseCollection);

            collection.InsertRange(2, expected);

            Assert.NotNull(collection);
            Assert.Equal(expectedLength, collection.Count);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(originalCollection.AsSpan(0, 2).ToArray(), collectionAssertion.AsSpan(0, 2).ToArray());
            Assert.Equal(expected, collectionAssertion.AsSpan(2, 5).ToArray());
            Assert.Equal(originalCollection.AsSpan(2, 2).ToArray(), collectionAssertion.AsSpan(7, 2).ToArray());
        }

        [Fact]
        public void Collection_InsertRange_Empty_Test()
        {
            List<int> baseCollection = new List<int>(new[] { 10, 11, 12, 13 });
            Collection<int> collection = new Collection<int>(baseCollection);

            collection.InsertRange(0, new int[0]);

            Assert.NotNull(collection);
            Assert.Equal(baseCollection.Count, collection.Count);
            Assert.Equal(baseCollection, collection.ToArray());
        }

        [Fact]
        public void Collection_InsertRange_Null_Test()
        {
            Collection<int> collection = new Collection<int>();
            Exception ex = Assert.Throws<ArgumentNullException>(() => collection.InsertRange(0, null));
        }

        [Fact]
        public void Collection_InsertRange_ReadOnly_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4 };
            int[] baseCollection = new int[] { 5, 6, 7, 8 };
            Collection<int> collection = new Collection<int>(baseCollection);

            Exception ex = Assert.Throws<NotSupportedException>(() => collection.InsertRange(0, expected));
        }

        [Fact]
        public void Collection_InsertRange_IndexLessThan0_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4 };
            Collection<int> collection = new Collection<int>();
            Exception ex = Assert.Throws<ArgumentOutOfRangeException>(() => collection.InsertRange(-1, expected));
        }

        [Fact]
        public void Collection_InsertRange_IndexGreaterThanCount_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4 };
            Collection<int> collection = new Collection<int>();
            Exception ex = Assert.Throws<ArgumentOutOfRangeException>(() => collection.InsertRange(10, expected));
        }

        [Fact]
        public void Collection_RemoveRange_Overflow_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            Assert.Throws<ArgumentException>(() => collection.RemoveRange(0, 4));
        }

        [Fact]
        public void Collection_RemoveRange_IntMaxValueOverflow_Test()
        {
            var count = 500;
            Collection<int> collection = new Collection<int>();
            for (int i = 0; i < count; i++)
            {
                collection.Add(i);
            }

            Assert.Throws<ArgumentException>(() => collection.RemoveRange(collection.Count - 2, int.MaxValue));
        }

        [Fact]
        public void Collection_RemoveRange_CountIsZero_Test()
        {
            int[] expected = new int[] { 1, 2, 3, 4, 5 };
            Collection<int> collection = new Collection<int>();
            for (int i = 0; i < expected.Length; i++)
            {
                collection.Add(expected[i]);
            }

            collection.RemoveRange(0, 0);

            Assert.NotNull(collection);
            Assert.Equal(expected.Length, collection.Count);
            Assert.Equal(expected, collection.ToArray());
        }

        [Fact]
        public void Collection_RemoveRange_CountIsLessThanZero_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);

            Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveRange(0, -1));
        }

        [Fact]
        public void Collection_RemoveRange_All_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            collection.RemoveRange(0, 3);

            Assert.NotNull(collection);
            Assert.Equal(0, collection.Count);
        }

        [Fact]
        public void Collection_RemoveRange_FirstTwoItems_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            collection.RemoveRange(0, 2);

            Assert.NotNull(collection);
            Assert.Equal(1, collection.Count);
            Assert.Equal(3, collection[0]);
        }

        [Fact]
        public void Collection_RemoveRange_LastTwoItems_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            collection.RemoveRange(1, 2);

            Assert.NotNull(collection);
            Assert.Equal(1, collection.Count);
            Assert.Equal(1, collection[0]);
        }

        [Fact]
        public void Collection_RemoveRange_ZeroItems_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            collection.RemoveRange(0, 0);

            Assert.NotNull(collection);
            Assert.Equal(3, collection.Count);
            Assert.Equal(1, collection[0]);
            Assert.Equal(2, collection[1]);
            Assert.Equal(3, collection[2]);
        }

        [Fact]
        public void Collection_RemoveRange_IndexLessThanZero_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveRange(-1, 3));
        }

        [Fact]
        public void Collection_RemoveRange_IndexGreaterThanCollection_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);
            collection.Add(3);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.RemoveRange(4, 3));
        }

        [Fact]
        public void Collection_RemoveRange_ReadOnly_Test()
        {
            Collection<int> collection = new Collection<int>(new int[] { 1, 2, 3 });

            Assert.Throws<NotSupportedException>(() => collection.RemoveRange(0, 2));
        }

        [Fact]
        public void Collection_ReplaceRange_FirstTwo_Test()
        {
            int[] initial = new int[] { 1, 2, 3, 4 };
            int[] replace = new int[] { 5, 6, 7, 8 };
            Collection<int> collection = new Collection<int>();
            foreach (var item in initial)
                collection.Add(item);

            collection.ReplaceRange(0, 2, replace);

            Assert.NotNull(collection);
            Assert.Equal(initial.Length + 2, collection.Count);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(replace, collectionAssertion.AsSpan(0, 4).ToArray());
            Assert.Equal(initial.AsSpan(2, 2).ToArray(), collectionAssertion.AsSpan(4, 2).ToArray());
        }

        [Fact]
        public void Collection_ReplaceRange_LastTwo_Test()
        {
            int[] initial = new int[] { 1, 2, 3, 4 };
            int[] replace = new int[] { 5, 6, 7, 8 };
            Collection<int> collection = new Collection<int>();
            foreach (var item in initial)
                collection.Add(item);

            collection.ReplaceRange(2, 2, replace);

            Assert.NotNull(collection);
            Assert.Equal(initial.Length + 2, collection.Count);

            int[] collectionAssertion = collection.ToArray();
            Assert.Equal(initial.AsSpan(0, 2).ToArray(), collectionAssertion.AsSpan(0, 2).ToArray());
            Assert.Equal(replace.AsSpan(0, 4).ToArray(), collectionAssertion.AsSpan(2, 4).ToArray());
        }

        [Fact]
        public void Collection_ReplaceRange_MiddleTwo_Test()
        {
            int[] initial = new int[] { 1, 2, 3, 4 };
            int[] replace = new int[] { 5, 6, 7, 8 };
            Collection<int> collection = new Collection<int>();
            foreach (var item in initial)
                collection.Add(item);

            collection.ReplaceRange(1, 2, replace);

            Assert.NotNull(collection);
            Assert.Equal(initial.Length + 2, collection.Count);

            Assert.Equal(initial[0], collection[0]);
            Assert.Equal(replace, collection.ToArray().AsSpan(1, 4).ToArray());
            Assert.Equal(initial[3], collection[5]);
        }

        [Fact]
        public void Collection_ReplaceRange_NullCollection_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);

            Assert.Throws<ArgumentNullException>(() => collection.ReplaceRange(0, 2, null));
        }

        [Fact]
        public void Collection_ReplaceRange_ReadOnly_Test()
        {
            Collection<int> collection = new Collection<int>(new int[] { 1, 2, 3 });
            Assert.Throws<NotSupportedException>(() => collection.ReplaceRange(0, 2, new int[] { 4, 5 }));
        }

        [Fact]
        public void Collection_ReplaceRange_IndexLessThanZero_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.ReplaceRange(-2, 2, new int[] { 1, 2 }));
        }

        [Fact]
        public void Collection_ReplaceRange_IndexGreaterThanCount_Test()
        {
            Collection<int> collection = new Collection<int>();
            collection.Add(1);
            collection.Add(2);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => collection.ReplaceRange(4, 2, new int[] { 1, 2 }));
        }
    }
}
