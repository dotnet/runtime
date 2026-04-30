// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of the List class.
    /// </summary>
    public abstract partial class List_Generic_Tests<T> : IList_Generic_Tests<T>
    {
        #region Helpers

        public delegate int IndexOfDelegate(List<T> list, T value);

        private IndexOfDelegate IndexOfDelegateFromType(CollectionTestData.IndexOfMethod methodType)
        {
            switch (methodType)
            {
                case (CollectionTestData.IndexOfMethod.IndexOf_T):
                    return ((List<T> list, T value) => { return list.IndexOf(value); });
                case (CollectionTestData.IndexOfMethod.IndexOf_T_int):
                    return ((List<T> list, T value) => { return list.IndexOf(value, 0); });
                case (CollectionTestData.IndexOfMethod.IndexOf_T_int_int):
                    return ((List<T> list, T value) => { return list.IndexOf(value, 0, list.Count); });
                case (CollectionTestData.IndexOfMethod.LastIndexOf_T):
                    return ((List<T> list, T value) => { return list.LastIndexOf(value); });
                case (CollectionTestData.IndexOfMethod.LastIndexOf_T_int):
                    return ((List<T> list, T value) => { return list.LastIndexOf(value, list.Count - 1); });
                case (CollectionTestData.IndexOfMethod.LastIndexOf_T_int_int):
                    return ((List<T> list, T value) => { return list.LastIndexOf(value, list.Count - 1, list.Count); });
                default:
                    throw new Exception("Invalid IndexOfMethod");
            }
        }

        #endregion

        #region IndexOf

        [Theory]
        [MemberData(nameof(CollectionTestData.IndexOfTestData), MemberType = typeof(CollectionTestData))]
        public void IndexOf_NoDuplicates(CollectionTestData.IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            _ = frontToBackOrder;
            List<T> list = GenericListFactory(count);
            List<T> expectedList = list.ToList();
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                Assert.Equal(i, IndexOf(list, expectedList[i]));
            });
        }

        [Theory]
        [MemberData(nameof(CollectionTestData.IndexOfTestData), MemberType = typeof(CollectionTestData))]
        public void IndexOf_NonExistingValues(CollectionTestData.IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            _ = frontToBackOrder;
            List<T> list = GenericListFactory(count);
            IEnumerable<T> nonexistentValues = CreateEnumerable(EnumerableType.List, list, count: count, numberOfMatchingElements: 0, numberOfDuplicateElements: 0);
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);

            Assert.All(nonexistentValues, nonexistentValue =>
            {
                Assert.Equal(-1, IndexOf(list, nonexistentValue));
            });
        }

        [Theory]
        [MemberData(nameof(CollectionTestData.IndexOfTestData), MemberType = typeof(CollectionTestData))]
        public void IndexOf_DefaultValue(CollectionTestData.IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            _ = frontToBackOrder;
            T defaultValue = default;
            List<T> list = GenericListFactory(count);
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);
            while (list.Remove(defaultValue))
                count--;
            list.Add(defaultValue);
            Assert.Equal(count, IndexOf(list, defaultValue));
        }

        [Theory]
        [MemberData(nameof(CollectionTestData.IndexOfTestData), MemberType = typeof(CollectionTestData))]
        public void IndexOf_OrderIsCorrect(CollectionTestData.IndexOfMethod indexOfMethod, int count, bool frontToBackOrder)
        {
            List<T> list = GenericListFactory(count);
            List<T> withoutDuplicates = list.ToList();
            list.AddRange(list);
            IndexOfDelegate IndexOf = IndexOfDelegateFromType(indexOfMethod);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                if (frontToBackOrder)
                    Assert.Equal(i, IndexOf(list, withoutDuplicates[i]));
                else
                    Assert.Equal(count + i, IndexOf(list, withoutDuplicates[i]));
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes), MemberType = typeof(TestBase))]
        public void IndexOf_int_OrderIsCorrectWithManyDuplicates(int count)
        {
            List<T> list = GenericListFactory(count);
            List<T> withoutDuplicates = list.ToList();
            list.AddRange(list);
            list.AddRange(list);
            list.AddRange(list);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                Assert.All(Enumerable.Range(0, 4), j =>
                {
                    int expectedIndex = (j * count) + i;
                    Assert.Equal(expectedIndex, list.IndexOf(withoutDuplicates[i], (count * j)));
                    Assert.Equal(expectedIndex, list.IndexOf(withoutDuplicates[i], (count * j), count));
                });
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes), MemberType = typeof(TestBase))]
        public void LastIndexOf_int_OrderIsCorrectWithManyDuplicates(int count)
        {
            List<T> list = GenericListFactory(count);
            List<T> withoutDuplicates = list.ToList();
            list.AddRange(list);
            list.AddRange(list);
            list.AddRange(list);

            Assert.All(Enumerable.Range(0, count), i =>
            {
                Assert.All(Enumerable.Range(0, 4), j =>
                {
                    int expectedIndex = (j * count) + i;
                    Assert.Equal(expectedIndex, list.LastIndexOf(withoutDuplicates[i], (count * (j + 1)) - 1));
                    Assert.Equal(expectedIndex, list.LastIndexOf(withoutDuplicates[i], (count * (j + 1)) - 1, count));
                });
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes), MemberType = typeof(TestBase))]
        public void IndexOf_int_OutOfRangeExceptions(int count)
        {
            List<T> list = GenericListFactory(count);
            T element = CreateT(234);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count + 1)); //"Expect ArgumentOutOfRangeException for index greater than length of list.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count + 10)); //"Expect ArgumentOutOfRangeException for index greater than length of list.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, -1)); //"Expect ArgumentOutOfRangeException for negative index."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, int.MinValue)); //"Expect ArgumentOutOfRangeException for negative index."
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes), MemberType = typeof(TestBase))]
        public void IndexOf_int_int_OutOfRangeExceptions(int count)
        {
            List<T> list = GenericListFactory(count);
            T element = CreateT(234);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count, 1)); //"ArgumentOutOfRangeException expected on index larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count + 1, 1)); //"ArgumentOutOfRangeException expected  on index larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, 0, count + 1)); //"ArgumentOutOfRangeException expected  on count larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, count / 2, count / 2 + 2)); //"ArgumentOutOfRangeException expected.."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, 0, count + 1)); //"ArgumentOutOfRangeException expected  on count larger than array."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, 0, -1)); //"ArgumentOutOfRangeException expected on negative count."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(element, -1, 1)); //"ArgumentOutOfRangeException expected on negative index."
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes), MemberType = typeof(TestBase))]
        public void LastIndexOf_int_OutOfRangeExceptions(int count)
        {
            List<T> list = GenericListFactory(count);
            T element = CreateT(234);
            Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count)); //"ArgumentOutOfRangeException expected."
            if (count == 0)  // IndexOf with a 0 count List is special cased to return -1.
                Assert.Equal(-1, list.LastIndexOf(element, -1));
            else
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, -1));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes), MemberType = typeof(TestBase))]
        public void LastIndexOf_int_int_OutOfRangeExceptions(int count)
        {
            List<T> list = GenericListFactory(count);
            T element = CreateT(234);

            if (count > 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, 0, count + 1)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count / 2, count / 2 + 2)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, 0, count + 1)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, 0, -1)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, -1, count)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, -1, 1)); //"Expected ArgumentOutOfRangeException."                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count, 0)); //"Expected ArgumentOutOfRangeException."
                Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(element, count, 1)); //"Expected ArgumentOutOfRangeException."
            }
            else // IndexOf with a 0 count List is special cased to return -1.
            {
                Assert.Equal(-1, list.LastIndexOf(element, 0, count + 1));
                Assert.Equal(-1, list.LastIndexOf(element, count / 2, count / 2 + 2));
                Assert.Equal(-1, list.LastIndexOf(element, 0, count + 1));
                Assert.Equal(-1, list.LastIndexOf(element, 0, -1));
                Assert.Equal(-1, list.LastIndexOf(element, -1, count));
                Assert.Equal(-1, list.LastIndexOf(element, -1, 1));
                Assert.Equal(-1, list.LastIndexOf(element, count, 0));
                Assert.Equal(-1, list.LastIndexOf(element, count, 1));
            }
        }

        #endregion
    }
}
