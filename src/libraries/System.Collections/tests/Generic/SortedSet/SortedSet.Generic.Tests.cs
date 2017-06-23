// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the SortedSet class.
    /// </summary>
    public abstract partial class SortedSet_Generic_Tests<T> : ISet_Generic_Tests<T>
    {
        #region ISet<T> Helper Methods

        protected override ISet<T> GenericISetFactory()
        {
            return new SortedSet<T>();
        }

        #endregion

        #region Constructors

        [Fact]
        public void SortedSet_Generic_Constructor()
        {
            SortedSet<T> set = new SortedSet<T>();
            Assert.Empty(set);
        }

        [Fact]
        public void SortedSet_Generic_Constructor_IComparer()
        {
            IComparer<T> comparer = GetIComparer();
            SortedSet<T> set = new SortedSet<T>(comparer);
            Assert.Equal(comparer ?? Comparer<T>.Default, set.Comparer);
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        public void SortedSet_Generic_Constructor_IEnumerable(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, numberOfDuplicateElements);
            SortedSet<T> set = new SortedSet<T>(enumerable);
            Assert.True(set.SetEquals(enumerable));
        }

        [Fact]
        public void SortedSet_Generic_Constructor_IEnumerable_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new SortedSet<T>((IEnumerable<T>)null));
            Assert.Throws<ArgumentNullException>(() => new SortedSet<T>((IEnumerable<T>)null, Comparer<T>.Default));
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        [ActiveIssue("dotnet/corefx #16790", TargetFrameworkMonikers.NetFramework)]
        public void SortedSet_Generic_Constructor_IEnumerable_IComparer_Netcoreapp(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, 0);
            SortedSet<T> set = new SortedSet<T>(enumerable, GetIComparer());
            Assert.True(set.SetEquals(enumerable));
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        [ActiveIssue("dotnet/corefx #16790", ~TargetFrameworkMonikers.NetFramework)]
        public void SortedSet_Generic_Constructor_IEnumerable_IComparer_Netfx(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, 0);
            SortedSet<T> set = new SortedSet<T>(enumerable, GetIComparer() ?? Comparer<T>.Default);
            Assert.True(set.SetEquals(enumerable));
        }

        [Theory]
        [MemberData(nameof(EnumerableTestData))]
        [ActiveIssue("dotnet/corefx #16790", TargetFrameworkMonikers.NetFramework)]
        public void SortedSet_Generic_Constructor_IEnumerable_IComparer_NullComparer_Netcoreapp(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, 0);
            SortedSet<T> set = new SortedSet<T>(enumerable, comparer: null);
            Assert.True(set.SetEquals(enumerable));
        }

#endregion

#region Max and Min

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_MaxAndMin(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            if (setLength > 0)
            {
                List<T> expected = set.ToList();
                expected.Sort(GetIComparer());
                Assert.Equal(expected[0], set.Min);
                Assert.Equal(expected[setLength - 1], set.Max);
            }
            else
            {
                Assert.Equal(default(T), set.Min);
                Assert.Equal(default(T), set.Max);
            }
        }

#endregion

#region GetViewBetween

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_GetViewBetween_EntireSet(int setLength)
        {
            if (setLength > 0)
            {
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                T firstElement = set.ElementAt(0);
                T lastElement = set.ElementAt(setLength - 1);
                SortedSet<T> view = set.GetViewBetween(firstElement, lastElement);
                Assert.Equal(setLength, view.Count);
                Assert.True(set.SetEquals(view));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_GetViewBetween_MiddleOfSet(int setLength)
        {
            if (setLength >= 3)
            {
                IComparer<T> comparer = GetIComparer() ?? Comparer<T>.Default;
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                T firstElement = set.ElementAt(1);
                T lastElement = set.ElementAt(setLength - 2);

                List<T> expected = new List<T>(setLength - 2);
                foreach (T value in set)
                    if (comparer.Compare(value, firstElement) >= 0 && comparer.Compare(value, lastElement) <= 0)
                        expected.Add(value);

                SortedSet<T> view = set.GetViewBetween(firstElement, lastElement);
                Assert.Equal(expected.Count, view.Count);
                Assert.True(view.SetEquals(expected));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_GetViewBetween_LowerValueGreaterThanUpperValue_ThrowsArgumentException(int setLength)
        {
            if (setLength >= 2)
            {
                IComparer<T> comparer = GetIComparer() ?? Comparer<T>.Default;
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                T firstElement = set.ElementAt(0);
                T lastElement = set.ElementAt(setLength - 1);
                if (comparer.Compare(firstElement, lastElement) < 0)
                    AssertExtensions.Throws<ArgumentException>("lowerValue", null, () => set.GetViewBetween(lastElement, firstElement));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_GetViewBetween_SubsequentOutOfRangeCall_ThrowsArgumentOutOfRangeException(int setLength)
        {
            if (setLength >= 3)
            {
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                IComparer<T> comparer = GetIComparer() ?? Comparer<T>.Default;
                T firstElement = set.ElementAt(0);
                T middleElement = set.ElementAt(setLength / 2);
                T lastElement = set.ElementAt(setLength - 1);
                if ((comparer.Compare(firstElement, middleElement) < 0) && (comparer.Compare(middleElement, lastElement) < 0))
                {
                    SortedSet<T> view = set.GetViewBetween(firstElement, middleElement);
                    Assert.Throws<ArgumentOutOfRangeException>(() => view.GetViewBetween(middleElement, lastElement));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_GetViewBetween_Empty_MinMax(int setLength)
        {
            if (setLength < 4) return;

            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            Assert.Equal(setLength, set.Count);

            T firstElement = set.ElementAt(0);
            T secondElement = set.ElementAt(1);
            T nextToLastElement = set.ElementAt(setLength - 2);
            T lastElement = set.ElementAt(setLength - 1);

            T[] items = set.ToArray();
            for (int i = 1; i < setLength - 1; i++)
            {
                set.Remove(items[i]);
            }
            Assert.Equal(2, set.Count);

            SortedSet<T> view = set.GetViewBetween(secondElement, nextToLastElement);
            Assert.Equal(0, view.Count);

            Assert.Equal(default(T), view.Min);
            Assert.Equal(default(T), view.Max);
        }

        #endregion

#region RemoveWhere

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_RemoveWhere_AllElements(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            int removedCount = set.RemoveWhere((value) => { return true; });
            Assert.Equal(setLength, removedCount);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_RemoveWhere_NoElements(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            int removedCount = set.RemoveWhere((value) => { return false; });
            Assert.Equal(0, removedCount);
            Assert.Equal(setLength, set.Count);
        }

        [Fact]
        public void SortedSet_Generic_RemoveWhere_NullPredicate_ThrowsArgumentNullException()
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory();
            AssertExtensions.Throws<ArgumentNullException>("match", () => set.RemoveWhere(null));
        }

#endregion

#region Enumeration and Ordering

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_SetIsProperlySortedAccordingToComparer(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            List<T> expected = set.ToList();
            expected.Sort(GetIComparer());
            int expectedIndex = 0;
            foreach (T value in set)
                Assert.Equal(expected[expectedIndex++], value);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_ReverseSetIsProperlySortedAccordingToComparer(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            List<T> expected = set.ToList();
            expected.Sort(GetIComparer());
            expected.Reverse();
            int expectedIndex = 0;
            foreach (T value in set.Reverse())
                Assert.Equal(expected[expectedIndex++], value);
        }

        [Fact]
        public void SortedSet_Generic_TestSubSetEnumerator()
        {
            SortedSet<int> sortedSet = new SortedSet<int>();
            for (int i = 0; i < 10000; i++)
            {
                if (!sortedSet.Contains(i))
                    sortedSet.Add(i);
            }
            SortedSet<int> mySubSet = sortedSet.GetViewBetween(45, 90);

            Assert.Equal(46, mySubSet.Count); //"not all elements were encountered"

            IEnumerable<int> en = mySubSet.Reverse();
            Assert.True(mySubSet.SetEquals(en)); //"Expected to be the same set."
        }

#endregion

#region CopyTo

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_CopyTo_WithoutIndex(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            List<T> expected = set.ToList();
            expected.Sort(GetIComparer());
            T[] actual = new T[setLength];
            set.CopyTo(actual);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_CopyTo_WithValidFullCount(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            List<T> expected = set.ToList();
            expected.Sort(GetIComparer());
            T[] actual = new T[setLength];
            set.CopyTo(actual, 0, setLength);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void SortedSet_Generic_CopyTo_NegativeCount_ThrowsArgumentOutOfRangeException(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            T[] actual = new T[setLength];
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(actual, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(actual, 0, int.MinValue));
        }

#endregion

#region CreateSetComparer

        [Fact]
        public void SetComparer_SetEqualsTests()
        {
            List<T> objects = new List<T>() { CreateT(1), CreateT(2), CreateT(3), CreateT(4), CreateT(5), CreateT(6) };

            var set = new HashSet<SortedSet<T>>()
            {
                new SortedSet<T> { objects[0], objects[1], objects[2] },
                new SortedSet<T> { objects[3], objects[4], objects[5] }
            };

            var noComparerSet = new HashSet<SortedSet<T>>()
            {
                new SortedSet<T> { objects[0], objects[1], objects[2] },
                new SortedSet<T> { objects[3], objects[4], objects[5] }
            };

            var comparerSet1 = new HashSet<SortedSet<T>>(SortedSet<T>.CreateSetComparer())
            {
                new SortedSet<T> { objects[0], objects[1], objects[2] },
                new SortedSet<T> { objects[3], objects[4], objects[5] }
            };

            var comparerSet2 = new HashSet<SortedSet<T>>(SortedSet<T>.CreateSetComparer())
            {
                new SortedSet<T> { objects[3], objects[4], objects[5] },
                new SortedSet<T> { objects[0], objects[1], objects[2] }
            };

            Assert.False(noComparerSet.SetEquals(set));
            Assert.True(comparerSet1.SetEquals(set));
            Assert.True(comparerSet2.SetEquals(set));
        }
#endregion
    }
}
