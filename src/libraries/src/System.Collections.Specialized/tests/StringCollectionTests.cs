// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public static class StringCollectionTests
    {
        private const string ElementNotPresent = "element-not-present";

        /// <summary>
        /// Data used for testing with Insert.
        /// </summary>
        /// Format is:
        ///  1. initial Collection
        ///  2. internal data
        ///  3. data to insert (ElementNotPresent or null)
        ///  4. location to insert (0, count / 2, count)
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> Insert_Data()
        {
            foreach (object[] data in StringCollection_Data().Concat(StringCollection_Duplicates_Data()))
            {
                string[] d = (string[])(data[1]);
                foreach (string element in new[] { ElementNotPresent, null })
                {
                    foreach (int location in new[] { 0, d.Length / 2, d.Length }.Distinct())
                    {
                        StringCollection initial = new StringCollection();
                        initial.AddRange(d);
                        yield return new object[] { initial, d, element, location };
                    }
                }
            }
        }/// <summary>

        /// Data used for testing with RemoveAt.
        /// </summary>
        /// Format is:
        ///  1. initial Collection
        ///  2. internal data
        ///  3. location to remove (0, count / 2, count)
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> RemoveAt_Data()
        {
            foreach (object[] data in StringCollection_Data().Concat(StringCollection_Duplicates_Data()))
            {
                string[] d = (string[])(data[1]);
                if (d.Length > 0)
                {
                    foreach (int location in new[] { 0, d.Length / 2, d.Length - 1 }.Distinct())
                    {
                        StringCollection initial = new StringCollection();
                        initial.AddRange(d);
                        yield return new object[] { initial, d, location };
                    }
                }
            }
        }

        /// <summary>
        /// Data used for testing with a set of collections.
        /// </summary>
        /// Format is:
        ///  1. Collection
        ///  2. internal data
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> StringCollection_Data()
        {
            yield return ConstructRow(new string[] { /* empty */ });
            yield return ConstructRow(new string[] { null });
            yield return ConstructRow(new string[] { "single" });
            yield return ConstructRow(Enumerable.Range(0, 100).Select(x => x.ToString()).ToArray());
            for (int index = 0; index < 100; index += 25)
            {
                yield return ConstructRow(Enumerable.Range(0, 100).Select(x => x == index ? null : x.ToString()).ToArray());
            }
        }

        /// <summary>
        /// Data used for testing with a set of collections, where the data has duplicates.
        /// </summary>
        /// Format is:
        ///  1. Collection
        ///  2. internal data
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> StringCollection_Duplicates_Data()
        {
            yield return ConstructRow(Enumerable.Range(0, 100).Select(x => (string)null).ToArray());
            yield return ConstructRow(Enumerable.Range(0, 100).Select(x => x % 10 == 0 ? null : (x % 10).ToString()).ToArray());
            yield return ConstructRow(Enumerable.Range(0, 100).Select(x => (x % 10).ToString()).ToArray());
        }

        private static object[] ConstructRow(string[] data)
        {
            if (data.Contains(ElementNotPresent)) throw new ArgumentException("Do not include \"" + ElementNotPresent + "\" in data.");

            StringCollection col = new StringCollection();
            col.AddRange(data);
            return new object[] { col, data };
        }

        [Fact]
        public static void Constructor_DefaultTest()
        {
            StringCollection sc = new StringCollection();
            Assert.Equal(0, sc.Count);
            Assert.False(sc.Contains(null));
            Assert.False(sc.Contains(""));
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void AddTest(StringCollection collection, string[] data)
        {
            StringCollection added = new StringCollection();
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(i, added.Count);
                Assert.Throws<ArgumentOutOfRangeException>(() => added[i]);
                added.Add(data[i]);
                Assert.Equal(data[i], added[i]);
                Assert.Equal(i + 1, added.Count);
            }
            Assert.Equal(collection, added);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void Add_ExplicitInterface_Test(StringCollection collection, string[] data)
        {
            IList added = new StringCollection();
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(i, added.Count);
                Assert.Throws<ArgumentOutOfRangeException>(() => added[i]);
                added.Add(data[i]);
                Assert.Equal(data[i], added[i]);
                Assert.Equal(i + 1, added.Count);
            }
            Assert.Equal(collection, added);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void AddRangeTest(StringCollection collection, string[] data)
        {
            StringCollection added = new StringCollection();
            added.AddRange(data);
            Assert.Equal(collection, added);
            added.AddRange(new string[] { /*empty*/});
            Assert.Equal(collection, added);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void AddRange_NullTest(StringCollection collection, string[] data)
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => collection.AddRange(null));
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void ClearTest(StringCollection collection, string[] data)
        {
            Assert.Equal(data.Length, collection.Count);
            collection.Clear();
            Assert.Equal(0, collection.Count);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void CopyToTest(StringCollection collection, string[] data)
        {
            string[] full = new string[data.Length];
            collection.CopyTo(full, 0);
            Assert.Equal(data, full);

            string[] large = new string[data.Length * 2];
            collection.CopyTo(large, data.Length / 4);
            for (int i = 0; i < large.Length; i++)
            {
                if (i < data.Length / 4 || i >= data.Length + data.Length / 4)
                {
                    Assert.Null(large[i]);
                }
                else
                {
                    Assert.Equal(data[i - data.Length / 4], large[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void CopyTo_ExplicitInterface_Test(ICollection collection, string[] data)
        {
            string[] full = new string[data.Length];
            collection.CopyTo(full, 0);
            Assert.Equal(data, full);

            string[] large = new string[data.Length * 2];
            collection.CopyTo(large, data.Length / 4);
            for (int i = 0; i < large.Length; i++)
            {
                if (i < data.Length / 4 || i >= data.Length + data.Length / 4)
                {
                    Assert.Null(large[i]);
                }
                else
                {
                    Assert.Equal(data[i - data.Length / 4], large[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void CopyTo_ArgumentInvalidTest(StringCollection collection, string[] data)
        {
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(data, -1));
            if (data.Length > 0)
            {
                AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(new string[0], data.Length - 1));
                AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => collection.CopyTo(new string[data.Length - 1], 0));
            }

            // As explicit interface implementation
            Assert.Throws<ArgumentNullException>(() => ((ICollection)collection).CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((ICollection)collection).CopyTo(data, -1));
            if (data.Length > 0)
            {
                AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => ((ICollection)collection).CopyTo(new string[0], data.Length - 1));
                AssertExtensions.Throws<ArgumentException>("destinationArray", "", () => ((ICollection)collection).CopyTo(new string[data.Length - 1], 0));
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void CountTest(StringCollection collection, string[] data)
        {
            Assert.Equal(data.Length, collection.Count);
            collection.Clear();
            Assert.Equal(0, collection.Count);
            collection.Add("one");
            Assert.Equal(1, collection.Count);
            collection.AddRange(data);
            Assert.Equal(1 + data.Length, collection.Count);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void ContainsTest(StringCollection collection, string[] data)
        {
            Assert.All(data, element => Assert.True(collection.Contains(element)));
            Assert.All(data, element => Assert.True(((IList)collection).Contains(element)));
            Assert.False(collection.Contains(ElementNotPresent));
            Assert.False(((IList)collection).Contains(ElementNotPresent));
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void GetEnumeratorTest(StringCollection collection, string[] data)
        {
            bool repeat = true;
            StringEnumerator enumerator = collection.GetEnumerator();
            Assert.NotNull(enumerator);
            while (repeat)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                foreach (string element in data)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(element, enumerator.Current);
                    Assert.Equal(element, enumerator.Current);
                }
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.False(enumerator.MoveNext());

                enumerator.Reset();
                enumerator.Reset();
                repeat = false;
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void GetEnumerator_ModifiedCollectionTest(StringCollection collection, string[] data)
        {
            StringEnumerator enumerator = collection.GetEnumerator();
            Assert.NotNull(enumerator);
            if (data.Length > 0)
            {
                Assert.True(enumerator.MoveNext());
                string current = enumerator.Current;
                Assert.Equal(data[0], current);
                collection.RemoveAt(0);
                if (data.Length > 1 && data[0] != data[1])
                {
                    Assert.NotEqual(current, collection[0]);
                }
                Assert.Equal(current, enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            }
            else
            {
                collection.Add("newValue");
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void GetSetTest(StringCollection collection, string[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[i], collection[i]);
            }
            for (int i = 0; i < data.Length / 2; i++)
            {
                string temp = collection[i];
                collection[i] = collection[data.Length - i - 1];
                collection[data.Length - i - 1] = temp;
            }
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[data.Length - i - 1], collection[i]);
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void GetSet_ExplicitInterface_Test(IList collection, string[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[i], collection[i]);
            }
            for (int i = 0; i < data.Length / 2; i++)
            {
                object temp = collection[i];
                if (temp != null)
                {
                    Assert.IsType<string>(temp);
                }
                collection[i] = collection[data.Length - i - 1];
                collection[data.Length - i - 1] = temp;
            }
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[data.Length - i - 1], collection[i]);
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void GetSet_ArgumentInvalidTest(StringCollection collection, string[] data)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1] = ElementNotPresent);
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1] = null);
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[data.Length] = ElementNotPresent);
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[data.Length] = null);

            Assert.Throws<ArgumentOutOfRangeException>(() => collection[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => collection[data.Length]);

            // As explicitly implementing the interface
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection)[-1] = ElementNotPresent);
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection)[-1] = null);
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection)[data.Length] = ElementNotPresent);
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection)[data.Length] = null);

            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection)[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection)[data.Length]);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void IndexOfTest(StringCollection collection, string[] data)
        {
            Assert.All(data, element => Assert.Equal(Array.IndexOf(data, element), collection.IndexOf(element)));
            Assert.All(data, element => Assert.Equal(Array.IndexOf(data, element), ((IList)collection).IndexOf(element)));
            Assert.Equal(-1, collection.IndexOf(ElementNotPresent));
            Assert.Equal(-1, ((IList)collection).IndexOf(ElementNotPresent));
        }

        [Theory]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void IndexOf_DuplicateTest(StringCollection collection, string[] data)
        {
            // Only the index of the first element will be returned.
            data = data.Distinct().ToArray();
            Assert.All(data, element => Assert.Equal(Array.IndexOf(data, element), collection.IndexOf(element)));
            Assert.All(data, element => Assert.Equal(Array.IndexOf(data, element), ((IList)collection).IndexOf(element)));
            Assert.Equal(-1, collection.IndexOf(ElementNotPresent));
            Assert.Equal(-1, ((IList)collection).IndexOf(ElementNotPresent));
        }

        [Theory]
        [MemberData(nameof(Insert_Data))]
        public static void InsertTest(StringCollection collection, string[] data, string element, int location)
        {
            collection.Insert(location, element);
            Assert.Equal(data.Length + 1, collection.Count);
            if (element == ElementNotPresent)
            {
                Assert.Equal(location, collection.IndexOf(ElementNotPresent));
            }
            for (int i = 0; i < data.Length + 1; i++)
            {
                if (i < location)
                {
                    Assert.Equal(data[i], collection[i]);
                }
                else if (i == location)
                {
                    Assert.Equal(element, collection[i]);
                }
                else
                {
                    Assert.Equal(data[i - 1], collection[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Insert_Data))]
        public static void Insert_ExplicitInterface_Test(IList collection, string[] data, string element, int location)
        {
            collection.Insert(location, element);
            Assert.Equal(data.Length + 1, collection.Count);
            if (element == ElementNotPresent)
            {
                Assert.Equal(location, collection.IndexOf(ElementNotPresent));
            }
            for (int i = 0; i < data.Length + 1; i++)
            {
                if (i < location)
                {
                    Assert.Equal(data[i], collection[i]);
                }
                else if (i == location)
                {
                    Assert.Equal(element, collection[i]);
                }
                else
                {
                    Assert.Equal(data[i - 1], collection[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void Insert_ArgumentInvalidTest(StringCollection collection, string[] data)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(-1, ElementNotPresent));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.Insert(data.Length + 1, ElementNotPresent));

            // And as explicit interface implementation
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection).Insert(-1, ElementNotPresent));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IList)collection).Insert(data.Length + 1, ElementNotPresent));
        }

        [Fact]
        public static void IsFixedSizeTest()
        {
            Assert.False(((IList)new StringCollection()).IsFixedSize);
        }

        [Fact]
        public static void IsReadOnlyTest()
        {
            Assert.False(new StringCollection().IsReadOnly);
            Assert.False(((IList)new StringCollection()).IsReadOnly);
        }

        [Fact]
        public static void IsSynchronizedTest()
        {
            Assert.False(new StringCollection().IsSynchronized);
            Assert.False(((IList)new StringCollection()).IsSynchronized);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        public static void RemoveTest(StringCollection collection, string[] data)
        {
            Assert.All(data, element =>
            {
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
                collection.Remove(element);
                Assert.False(collection.Contains(element));
                Assert.False(((IList)collection).Contains(element));
            });
            Assert.Equal(0, collection.Count);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        public static void Remove_IListTest(StringCollection collection, string[] data)
        {
            Assert.All(data, element =>
            {
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
                ((IList)collection).Remove(element);
                Assert.False(collection.Contains(element));
                Assert.False(((IList)collection).Contains(element));
            });
            Assert.Equal(0, collection.Count);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void Remove_NotPresentTest(StringCollection collection, string[] data)
        {
            collection.Remove(ElementNotPresent);
            Assert.Equal(data.Length, collection.Count);
            ((IList)collection).Remove(ElementNotPresent);
            Assert.Equal(data.Length, collection.Count);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void Remove_DuplicateTest(StringCollection collection, string[] data)
        {
            // Only the first element will be removed.
            string[] first = data.Distinct().ToArray();
            Assert.All(first, element =>
            {
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
                collection.Remove(element);
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
            });
            Assert.Equal(data.Length - first.Length, collection.Count);
            for (int i = first.Length; i < data.Length; i++)
            {
                string element = data[i];
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
                collection.Remove(element);
                bool stillPresent = i < data.Length - first.Length;
                Assert.Equal(stillPresent, collection.Contains(element));
                Assert.Equal(stillPresent, ((IList)collection).Contains(element));
            }
            Assert.Equal(0, collection.Count);
        }

        [Theory]
        [MemberData(nameof(RemoveAt_Data))]
        public static void RemoveAtTest(StringCollection collection, string[] data, int location)
        {
            collection.RemoveAt(location);
            Assert.Equal(data.Length - 1, collection.Count);
            for (int i = 0; i < data.Length - 1; i++)
            {
                if (i < location)
                {
                    Assert.Equal(data[i], collection[i]);
                }
                else if (i >= location)
                {
                    Assert.Equal(data[i + 1], collection[i]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void RemoveAt_ArgumentInvalidTest(StringCollection collection, string[] data)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.RemoveAt(data.Length));
        }

        [Theory]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void Remove_Duplicate_IListTest(StringCollection collection, string[] data)
        {
            // Only the first element will be removed.
            string[] first = data.Distinct().ToArray();
            Assert.All(first, element =>
            {
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
                ((IList)collection).Remove(element);
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
            });
            Assert.Equal(data.Length - first.Length, collection.Count);
            for (int i = first.Length; i < data.Length; i++)
            {
                string element = data[i];
                Assert.True(collection.Contains(element));
                Assert.True(((IList)collection).Contains(element));
                ((IList)collection).Remove(element);
                bool stillPresent = i < data.Length - first.Length;
                Assert.Equal(stillPresent, collection.Contains(element));
                Assert.Equal(stillPresent, ((IList)collection).Contains(element));
            }
            Assert.Equal(0, collection.Count);
        }

        [Theory]
        [MemberData(nameof(StringCollection_Data))]
        [MemberData(nameof(StringCollection_Duplicates_Data))]
        public static void SyncRootTest(StringCollection collection, string[] data)
        {
            object syncRoot = collection.SyncRoot;
            Assert.NotNull(syncRoot);
            Assert.IsType<object>(syncRoot);

            Assert.Same(syncRoot, collection.SyncRoot);
            Assert.NotSame(syncRoot, new StringCollection().SyncRoot);
            StringCollection other = new StringCollection();
            other.AddRange(data);
            Assert.NotSame(syncRoot, other.SyncRoot);
        }
    }
}
