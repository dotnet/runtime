// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the generic
    /// IList interface
    /// </summary>
    public abstract class IList_Generic_Tests<T> : ICollection_Generic_Tests<T>
    {
        #region IList<T> Helper Methods

        /// <summary>
        /// Creates an instance of an IList{T} that can be used for testing.
        /// </summary>
        /// <returns>An instance of an IList{T} that can be used for testing.</returns>
        protected abstract IList<T> GenericIListFactory();

        /// <summary>
        /// Creates an instance of an IList{T} that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned IList{T} contains.</param>
        /// <returns>An instance of an IList{T} that can be used for testing.</returns>
        protected virtual IList<T> GenericIListFactory(int count)
        {
            IList<T> collection = GenericIListFactory();
            AddToCollection(collection, count);
            return collection;
        }

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            foreach (ModifyEnumerable item in base.GetModifyEnumerables(operations))
                yield return item;

            if (!AddRemoveClear_ThrowsNotSupported && (operations & ModifyOperation.Insert) == ModifyOperation.Insert)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    IList<T> casted = ((IList<T>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.Insert(0, CreateT(12));
                        return true;
                    }
                    return false;
                };
            }
            if (!AddRemoveClear_ThrowsNotSupported && (operations & ModifyOperation.Overwrite) == ModifyOperation.Overwrite)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    IList<T> casted = ((IList<T>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted[0] = CreateT(12);
                        return true;
                    }
                    return false;
                };
            }
            if (!AddRemoveClear_ThrowsNotSupported && (operations & ModifyOperation.Remove) == ModifyOperation.Remove)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    IList<T> casted = ((IList<T>)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.RemoveAt(0);
                        return true;
                    }
                    return false;
                };
            }
        }

        #endregion

        #region ICollection<T> Helper Methods

        protected override bool DefaultValueWhenNotAllowed_Throws => false;

        protected override ICollection<T> GenericICollectionFactory() => GenericIListFactory();

        protected override ICollection<T> GenericICollectionFactory(int count) => GenericIListFactory(count);

        protected virtual Type IList_Generic_Item_InvalidIndex_ThrowType => typeof(ArgumentOutOfRangeException);

        #endregion

        #region Item Getter

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemGet_NegativeIndex_ThrowsException(int count)
        {
            IList<T> list = GenericIListFactory(count);
            CollectionAsserts.ThrowsElementAt(list, -1, IList_Generic_Item_InvalidIndex_ThrowType);
            CollectionAsserts.ThrowsElementAt(list, int.MinValue, IList_Generic_Item_InvalidIndex_ThrowType);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemGet_IndexGreaterThanListCount_ThrowsException(int count)
        {
            IList<T> list = GenericIListFactory(count);
            CollectionAsserts.ThrowsElementAt(list, count, IList_Generic_Item_InvalidIndex_ThrowType);
            CollectionAsserts.ThrowsElementAt(list, count + 1, IList_Generic_Item_InvalidIndex_ThrowType);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemGet_ValidGetWithinListBounds(int count)
        {
            IList<T> list = GenericIListFactory(count);
            Assert.All(Enumerable.Range(0, count), index => CollectionAsserts.ElementAtSucceeds(list, index));
        }

        #endregion

        #region Item Setter

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_NegativeIndex_ThrowsException(int count)
        {
            if (!IsReadOnly)
            {
                IList<T> list = GenericIListFactory(count);
                T validAdd = CreateT(0);
                Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[-1] = validAdd);
                Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[int.MinValue] = validAdd);
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_IndexGreaterThanListCount_ThrowsException(int count)
        {
            if (!IsReadOnly)
            {
                IList<T> list = GenericIListFactory(count);
                T validAdd = CreateT(0);
                Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[count] = validAdd);
                Assert.Throws(IList_Generic_Item_InvalidIndex_ThrowType, () => list[count + 1] = validAdd);
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_OnReadOnlyList(int count)
        {
            if (IsReadOnly && count > 0)
            {
                IList<T> list = GenericIListFactory(count);
                T before = list[count / 2];
                Assert.Throws<NotSupportedException>(() => list[count / 2] = CreateT(321432));
                CollectionAsserts.EqualAt(list, count / 2, before);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_FirstItemToNonDefaultValue(int count)
        {
            if (count > 0 && !IsReadOnly)
            {
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(123452);
                list[0] = value;
                CollectionAsserts.EqualAt(list, 0, value);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_FirstItemToDefaultValue(int count)
        {
            if (count > 0 && !IsReadOnly)
            {
                IList<T> list = GenericIListFactory(count);
                if (DefaultValueAllowed)
                {
                    list[0] = default(T);
                    CollectionAsserts.EqualAt(list, 0, default(T));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => list[0] = default(T));
                    CollectionAsserts.NotEqualAt(list, 0, default(T));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_LastItemToNonDefaultValue(int count)
        {
            if (count > 0 && !IsReadOnly)
            {
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(123452);
                int lastIndex = count > 0 ? count - 1 : 0;
                list[lastIndex] = value;
                CollectionAsserts.EqualAt(list, lastIndex, value);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_LastItemToDefaultValue(int count)
        {
            if (count > 0 && !IsReadOnly && DefaultValueAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                int lastIndex = count > 0 ? count - 1 : 0;
                if (DefaultValueAllowed)
                {
                    list[lastIndex] = default(T);
                    CollectionAsserts.EqualAt(list, lastIndex, default(T));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => list[lastIndex] = default(T));
                    CollectionAsserts.NotEqualAt(list, lastIndex, default(T));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_DuplicateValues(int count)
        {
            if (count >= 2 && !IsReadOnly && DuplicateValuesAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(123452);
                list[0] = value;
                list[1] = value;
                CollectionAsserts.EqualAt(list, 0, value);
                CollectionAsserts.EqualAt(list, 1, value);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_ItemSet_InvalidValue(int count)
        {
            if (count > 0&& !IsReadOnly)
            {
                Assert.All(InvalidValues, value =>
                {
                    IList<T> list = GenericIListFactory(count);
                    Assert.Throws<ArgumentException>(() => list[count / 2] = value);
                });
            }
        }

        #endregion

        #region IndexOf

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_DefaultValueNotContainedInList(int count)
        {
            if (DefaultValueAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                T value = default(T);
                if (list.Contains(value))
                {
                    if (IsReadOnly)
                        return;
                    list.Remove(value);
                }
                Assert.Equal(-1, list.IndexOf(value));
            }
            else
            {
                IList<T> list = GenericIListFactory(count);
                Assert.Throws<ArgumentNullException>(() => list.IndexOf(default(T)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_DefaultValueContainedInList(int count)
        {
            if (count > 0 && DefaultValueAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                T value = default(T);
                if (!list.Contains(value))
                {
                    if (IsReadOnly)
                        return;
                    list[0] = value;
                }
                Assert.Equal(0, list.IndexOf(value));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_ValidValueNotContainedInList(int count)
        {
            IList<T> list = GenericIListFactory(count);
            int seed = 54321;
            T value = CreateT(seed++);
            while (list.Contains(value))
                value = CreateT(seed++);
            Assert.Equal(-1, list.IndexOf(value));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_ValueInCollectionMultipleTimes(int count)
        {
            if (count > 0 && !IsReadOnly && DuplicateValuesAllowed)
            {
                // IndexOf should always return the lowest index for which a matching element is found
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(12345);
                list[0] = value;
                list[count / 2] = value;
                Assert.Equal(0, list.IndexOf(value));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_EachValueNoDuplicates(int count)
        {
            // Assumes no duplicate elements contained in the list returned by GenericIListFactory
            IList<T> list = GenericIListFactory(count);
            Assert.All(Enumerable.Range(0, count), index =>
            {
                Assert.Equal(index, list.IndexOf(list[index]));
            });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_InvalidValue(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, value =>
                {
                    IList<T> list = GenericIListFactory(count);
                    Assert.Throws<ArgumentException>(() => list.IndexOf(value));
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_IndexOf_ReturnsFirstMatchingValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                foreach (T duplicate in list.ToList()) // hard copies list to circumvent enumeration error
                    list.Add(duplicate);
                List<T> expectedList = list.ToList();

                Assert.All(Enumerable.Range(0, count), (index =>
                    Assert.Equal(index, list.IndexOf(expectedList[index]))
                ));
            }
        }

        #endregion

        #region Insert

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                T validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, validAdd));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(int.MinValue, validAdd));
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_IndexGreaterThanListCount_Appends(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                T validAdd = CreateT(12350);
                list.Insert(count, validAdd);
                CollectionAsserts.HasCount(list, count + 1);
                CollectionAsserts.EqualAt(list, count, validAdd);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_ToReadOnlyList(int count)
        {
            if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => list.Insert(count / 2, CreateT(321432)));
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_FirstItemToNonDefaultValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(123452);
                list.Insert(0, value);
                CollectionAsserts.EqualAt(list, 0, value);
                CollectionAsserts.HasCount(list, count + 1);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_FirstItemToDefaultValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                T value = default(T);
                list.Insert(0, value);
                CollectionAsserts.EqualAt(list, 0, value);
                CollectionAsserts.HasCount(list, count + 1);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_LastItemToNonDefaultValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(123452);
                int lastIndex = count > 0 ? count - 1 : 0;
                list.Insert(lastIndex, value);
                CollectionAsserts.EqualAt(list, lastIndex, value);
                CollectionAsserts.HasCount(list, count + 1);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_LastItemToDefaultValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DefaultValueAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                T value = default(T);
                int lastIndex = count > 0 ? count - 1 : 0;
                list.Insert(lastIndex, value);
                CollectionAsserts.EqualAt(list, lastIndex, value);
                CollectionAsserts.HasCount(list, count + 1);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_DuplicateValues(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported && DuplicateValuesAllowed)
            {
                IList<T> list = GenericIListFactory(count);
                T value = CreateT(123452);
                if (AddRemoveClear_ThrowsNotSupported)
                {
                    Assert.Throws<NotSupportedException>(() => list.Insert(0, value));
                }
                else
                {
                    list.Insert(0, value);
                    list.Insert(1, value);
                    CollectionAsserts.EqualAt(list, 0, value);
                    CollectionAsserts.EqualAt(list, 1, value);
                    CollectionAsserts.HasCount(list, count + 2);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_Insert_InvalidValue(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                Assert.All(InvalidValues, value =>
                {
                    IList<T> list = GenericIListFactory(count);
                    Assert.Throws<ArgumentException>(() => list.Insert(count / 2, value));
                });
            }
        }

        #endregion

        #region RemoveAt

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_RemoveAt_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                T validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(int.MinValue));
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_RemoveAt_IndexGreaterThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                T validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(count));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(count + 1));
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_RemoveAt_OnReadOnlyList(int count)
        {
            if (IsReadOnly || AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => list.RemoveAt(count / 2));
                CollectionAsserts.HasCount(list, count);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_RemoveAt_AllValidIndices(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                CollectionAsserts.HasCount(list, count);
                Assert.All(Enumerable.Range(0, count).Reverse(), index =>
                {
                    list.RemoveAt(index);
                    CollectionAsserts.HasCount(list, index);
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_RemoveAt_ZeroMultipleTimes(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> list = GenericIListFactory(count);
                Assert.All(Enumerable.Range(0, count), index =>
                {
                    list.RemoveAt(0);
                    CollectionAsserts.HasCount(list, count - index - 1);
                });
            }
        }

        #endregion

        #region Enumerator.Current

        // Test Enumerator.Current at end after new elements was added
        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IList_Generic_CurrentAtEnd_AfterAdd(int count)
        {
            if (!IsReadOnly && !AddRemoveClear_ThrowsNotSupported)
            {
                IList<T> collection = GenericIListFactory(count);

                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext()) ; // Go to end of enumerator

                    T current = default(T);
                    if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
                    {
                        Assert.Throws<InvalidOperationException>(() => enumerator.Current); // enumerator.Current should fail
                    }
                    else
                    {
                        current = enumerator.Current;
                        Assert.Equal(default(T), current);
                    }

                    // Test after add
                    int seed = 3538963;
                    for (int i = 0; i < 3; i++)
                    {
                        collection.Add(CreateT(seed++));

                        if (count == 0 ? Enumerator_Empty_Current_UndefinedOperation_Throws : Enumerator_Current_UndefinedOperation_Throws)
                        {
                            Assert.Throws<InvalidOperationException>(() => enumerator.Current); // enumerator.Current should fail
                        }
                        else
                        {
                            current = enumerator.Current;
                            Assert.Equal(default(T), current);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
