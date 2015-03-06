// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Test
{
    /// <summary>
    /// Tests the public properties and constructor in ObservableCollection<T>.
    /// </summary>
    public class ConstructorAndPropertyTests
    {
        /// <summary>
        /// Tests that the parameterless constructor works.
        /// </summary>
        [Fact]
        public static void ParameterlessConstructorTest()
        {
            ObservableCollection<String> col = new ObservableCollection<String>();
            Assert.NotNull(col);
            Assert.Equal(0, col.Count);
        }

        /// <summary>
        /// Tests that the Ienumerable constructor can take Ienumerable with items
        /// and empty Ienumerable.
        /// </summary>
        [Fact]
        public static void IEnumerableConstructorTest()
        {
            // Creating ObservableCollection with IEnumerable.
            string[] expectedCol = { "one", "two", "three" };
            ObservableCollection<String> actualCol = new ObservableCollection<String>((IEnumerable<String>)expectedCol);
            Assert.NotNull(actualCol);
            Assert.Equal(expectedCol.Length, actualCol.Count);

            for (int i = 0; i < actualCol.Count; i++)
            {
                string item = actualCol[i];
                bool contains = false;
                for (int j = 0; j < expectedCol.Length; j++)
                {
                    if (item.Equals(expectedCol[j]))
                    {
                        contains = true;
                        break;
                    }
                }
                Assert.True(contains);
            }

            for (int i = 0; i < expectedCol.Length; i++)
            {
                string item = expectedCol[i];
                bool contains = false;
                for (int j = 0; j < actualCol.Count; j++)
                {
                    if (item.Equals(actualCol[j]))
                    {
                        contains = true;
                        break;
                    }
                }
                Assert.True(contains);
            }

            // Creating ObservableCollection with empty IEnumerable.
            actualCol = new ObservableCollection<string>(new string[] { });
            Assert.Equal(0, actualCol.Count);
            foreach (var item in actualCol)
            {
                Assert.True(false, "Should not be able to iterate over an empty collection.");
            }
        }

        /// <summary>
        /// Tests that ArgumentNullException is thrown when given a null IEnumerable.
        /// </summary>
        [Fact]
        public static void IEnumerableConstructorTest_Negative()
        {
            Assert.Throws<ArgumentNullException>(() => new ObservableCollection<string>(null));
        }

        /// <summary>
        /// Tests that an item can be set using the index.
        /// </summary>
        [Fact]
        public static void ItemTestSet()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            for (int i = 0; i < col.Count; ++i)
            {
                Guid guid = Guid.NewGuid();
                col[i] = guid;
                Assert.Equal(guid, col[i]);
            }
        }

        /// <summary>
        /// Tests that:
        /// ArgumentOutOfRangeException is thrown when the Index is >= collection.Count
        /// or Index < 0.
        /// </summary>
        [Fact]
        public static void ItemTestSet_Negative()
        {
            // Negative index.
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            int[] iArrInvalidValues = new Int32[] { -1, -2, -100, -1000, -10000, -100000, -1000000, -10000000, -100000000, -1000000000, Int32.MinValue };
            Guid guid = Guid.Empty;
            foreach (var index in iArrInvalidValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => guid = col[index]);
            }

            // Index not in the array.
            anArray = new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            col = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            guid = Guid.Empty;
            int[] iArrLargeValues = new Int32[] { col.Count, Int32.MaxValue, Int32.MaxValue / 2, Int32.MaxValue / 10 };
            foreach (var index in iArrLargeValues)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => guid = col[index]);
            }

            // Index not in the array when collection is empty.
            col = new ObservableCollection<Guid>(new Guid[] { });
            guid = Guid.Empty;
            Assert.Throws<ArgumentOutOfRangeException>(() => guid = col[0]);
        }

        // ICollection<T>.IsReadOnly
        [Fact]
        public static void IsReadOnlyTest()
        {
            Guid[] anArray = { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            ObservableCollection<Guid> col = new ObservableCollection<Guid>((IEnumerable<Guid>)anArray);
            Assert.False(((ICollection<Guid>)col).IsReadOnly);
        }
    }
}
