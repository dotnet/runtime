// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Collections.Generic;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the SortedSet class.
    /// </summary>
    public abstract class SortedSet_Generic_Tests<T> : ISet_Generic_Tests<T>
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
            if (comparer == null)
                Assert.Equal(Comparer<T>.Default, set.Comparer);
            else
                Assert.Equal(comparer, set.Comparer);
        }

        [Fact]
        public void SortedSet_Generic_Constructor_IComparer_Null()
        {
            IComparer<T> comparer = GetIComparer();
            SortedSet<T> set = new SortedSet<T>((IComparer<T>)null);
            Assert.Equal(Comparer<T>.Default, set.Comparer);
        }

        [Theory]
        [MemberData("EnumerableTestData")]
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
        [MemberData("EnumerableTestData")]
        public void SortedSet_Generic_Constructor_IEnumerable_IComparer(EnumerableType enumerableType, int setLength, int enumerableLength, int numberOfMatchingElements, int numberOfDuplicateElements)
        {
            IEnumerable<T> enumerable = CreateEnumerable(enumerableType, null, enumerableLength, 0, 0);
            SortedSet<T> set = new SortedSet<T>(enumerable, GetIComparer());
            Assert.True(set.SetEquals(enumerable));
        }

        #endregion

        #region Max and Min

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_MaxAndMin(int setLength)
        {
            if (setLength > 0)
            {
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                List<T> expected = set.ToList();
                expected.Sort(GetIComparer());
                Assert.Equal(expected[0], set.Min);
                Assert.Equal(expected[setLength - 1], set.Max);
            }
        }

        #endregion

        #region GetViewBetween

        [Theory]
        [MemberData("ValidCollectionSizes")]
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
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_GetViewBetween_MiddleOfSet(int setLength)
        {
            if (setLength >= 3)
            {
                IComparer<T> comparer = GetIComparer();
                if (comparer == null)
                    comparer = Comparer<T>.Default;
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
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_GetViewBetween_LowerValueGreaterThanUpperValue_ThrowsArgumentException(int setLength)
        {
            if (setLength >= 2)
            {
                IComparer<T> comparer = GetIComparer();
                if (comparer == null)
                    comparer = Comparer<T>.Default;
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                T firstElement = set.ElementAt(0);
                T lastElement = set.ElementAt(setLength - 1);
                if (comparer.Compare(firstElement, lastElement) < 0)
                    Assert.Throws<ArgumentException>(() => set.GetViewBetween(lastElement, firstElement));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_GetViewBetween_SubsequentOutOfRangeCall_ThrowsArgumentOutOfRangeException(int setLength)
        {
            if (setLength >= 3)
            { 
                SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
                IComparer<T> comparer = GetIComparer();
                if (comparer == null)
                    comparer = Comparer<T>.Default;
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

        #endregion

        #region RemoveWhere

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_RemoveWhere_AllElements(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            int removedCount = set.RemoveWhere((value) => { return true; });
            Assert.Equal(setLength, removedCount);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_RemoveWhere_NoElements(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            int removedCount = set.RemoveWhere((value) => { return false; });
            Assert.Equal(0, removedCount);
            Assert.Equal(setLength, set.Count);
        }

        #endregion

        #region Enumeration and Ordering

        [Theory]
        [MemberData("ValidCollectionSizes")]
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
        [MemberData("ValidCollectionSizes")]
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
        [MemberData("ValidCollectionSizes")]
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
        [MemberData("ValidCollectionSizes")]
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
        [MemberData("ValidCollectionSizes")]
        public void SortedSet_Generic_CopyTo_NegativeCount_ThrowsArgumentOutOfRangeException(int setLength)
        {
            SortedSet<T> set = (SortedSet<T>)GenericISetFactory(setLength);
            T[] actual = new T[setLength];
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(actual, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(actual, 0, int.MinValue));
        }

        #endregion
    }
}
