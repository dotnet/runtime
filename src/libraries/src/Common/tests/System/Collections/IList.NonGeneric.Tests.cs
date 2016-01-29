// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the nongeneric
    /// IList interface
    /// </summary>
    public abstract class IList_NonGeneric_Tests : ICollection_NonGeneric_Tests
    {
        #region IList Helper methods

        /// <summary>
        /// Creates an instance of an IList that can be used for testing.
        /// </summary>
        /// <returns>An instance of an IList that can be used for testing.</returns>
        protected abstract IList NonGenericIListFactory();

        /// <summary>
        /// Creates an instance of an IList that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned IList contains.</param>
        /// <returns>An instance of an IList that can be used for testing.</returns>
        protected virtual IList NonGenericIListFactory(int count)
        {
            IList collection = NonGenericIListFactory();
            AddToCollection(collection, count);
            return collection;
        }

        protected virtual void AddToCollection(IList collection, int numberOfItemsToAdd)
        {
            int seed = 9600;
            while (collection.Count < numberOfItemsToAdd)
            {
                object toAdd = CreateT(seed++);
                while (collection.Contains(toAdd) || InvalidValues.Contains(toAdd))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
            }
        }

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected virtual object CreateT(int seed)
        {
            if (seed % 2 == 0)
            {
                int stringLength = seed % 10 + 5;
                Random rand = new Random(seed);
                byte[] bytes = new byte[stringLength];
                rand.NextBytes(bytes);
                return Convert.ToBase64String(bytes);
            }
            else
            {
                Random rand = new Random(seed);
                return rand.Next();
            }
        }

        protected virtual bool ExpectedFixedSize { get { return false; } }

        #endregion

        #region ICollection Helper Methods

        protected override ICollection NonGenericICollectionFactory()
        {
            return NonGenericIListFactory();
        }

        protected override ICollection NonGenericICollectionFactory(int count)
        {
            return NonGenericIListFactory(count);
        }

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables
        {
            get
            {
                yield return (IEnumerable enumerable) => {
                    IList casted = ((IList)enumerable);
                    casted.Add(CreateT(2344));
                    return true;
                };
                yield return (IEnumerable enumerable) => {
                    IList casted = ((IList)enumerable);
                    casted.Insert(0, CreateT(12));
                    return true;
                };
                yield return (IEnumerable enumerable) => {
                    IList casted = ((IList)enumerable);
                    casted[0] = CreateT(12);
                    return true;
                };

                yield return (IEnumerable enumerable) => {
                    IList casted = ((IList)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.Remove(casted[0]);
                    }
                    return false;
                };
                yield return (IEnumerable enumerable) => {
                    IList casted = ((IList)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.RemoveAt(0);
                        return true;
                    }
                    return false;
                };
                yield return (IEnumerable enumerable) => {
                    IList casted = ((IList)enumerable);
                    if (casted.Count > 0)
                    {
                        casted.Clear();
                        return true;
                    }
                    return false;
                };
            }
        }

        #endregion

        #region IsFixedSize

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_IsFixedSize_Validity(int count)
        {
            IList collection = NonGenericIListFactory(count);
            Assert.Equal(ExpectedFixedSize, collection.IsFixedSize);
        }

        #endregion

        #region IsReadOnly

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_IsReadOnly_Validity(int count)
        {
            IList collection = NonGenericIListFactory(count);
            Assert.Equal(IsReadOnly, collection.IsReadOnly);
        }

        #endregion

        #region Item Getter

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemGet_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            IList list = NonGenericIListFactory(count);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[int.MinValue]);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemGet_IndexGreaterThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            IList list = NonGenericIListFactory(count);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[count]);
            Assert.Throws<ArgumentOutOfRangeException>(() => list[count + 1]);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemGet_ValidGetWithinListBounds(int count)
        {
            IList list = NonGenericIListFactory(count);
            object result;
            Assert.All(Enumerable.Range(0, count), index => result = list[index]);
        }

        #endregion

        #region Item Setter

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[-1] = validAdd);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[int.MinValue] = validAdd);
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_IndexGreaterThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[count] = validAdd);
                Assert.Throws<ArgumentOutOfRangeException>(() => list[count + 1] = validAdd);
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_OnReadOnlyList(int count)
        {
            if (IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => list[count / 2] = CreateT(321432));
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_FirstItemToNonNull(int count)
        {
            if (count > 0 && !IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object value = CreateT(123452);
                list[0] = value;
                Assert.Equal(value, list[0]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_FirstItemToNull(int count)
        {
            if (count > 0 && !IsReadOnly && NullAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = null;
                list[0] = value;
                Assert.Equal(value, list[0]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_LastItemToNonNull(int count)
        {
            if (count > 0 && !IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object value = CreateT(123452);
                int lastIndex = count > 0 ? count - 1 : 0;
                list[lastIndex] = value;
                Assert.Equal(value, list[lastIndex]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_LastItemToNull(int count)
        {
            if (count > 0 && !IsReadOnly && NullAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = null;
                int lastIndex = count > 0 ? count - 1 : 0;
                list[lastIndex] = value;
                Assert.Equal(value, list[lastIndex]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_DuplicateValues(int count)
        {
            if (count >= 2 && !IsReadOnly && DuplicateValuesAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = CreateT(123452);
                list[0] = value;
                list[1] = value;
                Assert.Equal(value, list[0]);
                Assert.Equal(value, list[1]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_ItemSet_InvalidValue(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, value =>
                {
                    IList list = NonGenericIListFactory(count);
                    Assert.Throws<ArgumentException>(() => list[count / 2] = value);
                });
            }
        }

        #endregion

        #region Add

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_Null(int count)
        {
            if (NullAllowed && !IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                collection.Add(null);
                Assert.Equal(count + 1, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_InvalidValueToMiddleOfCollection(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    IList collection = NonGenericIListFactory(count);
                    collection.Add(invalidValue);
                    for (int i = 0; i < count; i++)
                        collection.Add(CreateT(i));
                    Assert.Equal(count * 2, collection.Count);
                });
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_InvalidValueToBeginningOfCollection(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    IList collection = NonGenericIListFactory(0);
                    collection.Add(invalidValue);
                    for (int i = 0; i < count; i++)
                        collection.Add(CreateT(i));
                    Assert.Equal(count, collection.Count);
                });
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_InvalidValueToEndOfCollection(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    IList collection = NonGenericIListFactory(count);
                    collection.Add(invalidValue);
                    Assert.Equal(count, collection.Count);
                });
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_DuplicateValue(int count)
        {
            if (!IsReadOnly)
            {
                if (DuplicateValuesAllowed)
                {
                    IList collection = NonGenericIListFactory(count);
                    object duplicateValue = CreateT(700);
                    collection.Add(duplicateValue);
                    collection.Add(duplicateValue);
                    Assert.Equal(count + 2, collection.Count);
                }
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_AfterCallingClear(int count)
        {
            if (!IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                collection.Clear();
                AddToCollection(collection, 5);
                Assert.Equal(5, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_AfterRemovingAnyValue(int count)
        {
            if (!IsReadOnly)
            {
                int seed = 840;
                IList collection = NonGenericIListFactory(count);
                object[] items = new object[count];
                collection.CopyTo(items, 0);
                object toAdd = CreateT(seed++);
                while (collection.Contains(toAdd))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
                collection.RemoveAt(0);

                toAdd = CreateT(seed++);
                while (collection.Contains(toAdd))
                    toAdd = CreateT(seed++);

                collection.Add(toAdd);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_AfterRemovingAllItems(int count)
        {
            if (!IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                object[] arr = new object[count];
                collection.CopyTo(arr, 0);
                for (int i = 0; i < count; i++)
                    collection.Remove(arr[i]);
                collection.Add(CreateT(254));
                Assert.Equal(1, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_ToReadOnlyCollection(int count)
        {
            if (IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => collection.Add(CreateT(0)));
                Assert.Equal(count, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Add_AfterRemoving(int count)
        {
            if (!IsReadOnly)
            {
                int seed = 840;
                IList collection = NonGenericIListFactory(count);
                object toAdd = CreateT(seed++);
                while (collection.Contains(toAdd))
                    toAdd = CreateT(seed++);
                collection.Add(toAdd);
                collection.Remove(toAdd);
                collection.Add(toAdd);
            }
        }

        #endregion

        #region Clear

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Clear(int count)
        {
            IList collection = NonGenericIListFactory(count);
            if (IsReadOnly)
            {
                Assert.Throws<NotSupportedException>(() => collection.Clear());
                Assert.Equal(count, collection.Count);
            }
            else
            {
                collection.Clear();
                Assert.Equal(0, collection.Count);
            }
        }

        #endregion

        #region Contains

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Contains_ValidValueOnCollectionNotContainingThatValue(int count)
        {
            IList collection = NonGenericIListFactory(count);
            int seed = 4315;
            object item = CreateT(seed++);
            while (collection.Contains(item))
                item = CreateT(seed++);
            Assert.False(collection.Contains(item));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_ICollection_NonGeneric_Contains_ValidValueOnCollectionContainingThatValue(int count)
        {
            IList collection = NonGenericIListFactory(count);
            foreach (object item in collection)
                Assert.True(collection.Contains(item));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Contains_NullOnCollectionNotContainingNull(int count)
        {
            IList collection = NonGenericIListFactory(count);
            if (NullAllowed)
                Assert.False(collection.Contains(null));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Contains_NullOnCollectionContainingNull(int count)
        {
            IList collection = NonGenericIListFactory(count);
            if (NullAllowed && !IsReadOnly)
            {
                collection.Add(null);
                Assert.True(collection.Contains(null));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Contains_ValidValueThatExistsTwiceInTheCollection(int count)
        {
            if (DuplicateValuesAllowed && !IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                object item = CreateT(12);
                collection.Add(item);
                collection.Add(item);
                Assert.Equal(count + 2, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Contains_InvalidValue_ThrowsArgumentException(int count)
        {
            IList collection = NonGenericIListFactory(count);
            Assert.All(InvalidValues, invalidValue =>
                Assert.Throws<ArgumentException>(() => collection.Contains(invalidValue))
            );
        }

        #endregion

        #region IndexOf

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_IndexOf_NullNotContainedInList(int count)
        {
            if (NullAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = null;
                if (list.Contains(value))
                {
                    if (IsReadOnly)
                        return;
                    list.Remove(value);
                }
                Assert.Equal(-1, list.IndexOf(value));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_IndexOf_NullContainedInList(int count)
        {
            if (count > 0 && NullAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = null;
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
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_IndexOf_ValueInCollectionMultipleTimes(int count)
        {
            if (count > 0 && !IsReadOnly && DuplicateValuesAllowed)
            {
                // IndexOf should always return the lowest index for which a matching element is found
                IList list = NonGenericIListFactory(count);
                object value = CreateT(12345);
                list[0] = value;
                list[count / 2] = value;
                Assert.Equal(0, list.IndexOf(value));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_IndexOf_EachValueNoDuplicates(int count)
        {
            // Assumes no duplicate elements contained in the list returned by NonGenericIListFactory
            IList list = NonGenericIListFactory(count);
            Assert.All(Enumerable.Range(0, count), index =>
            {
                Assert.Equal(index, list.IndexOf(list[index]));
            });
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_IndexOf_InvalidValue(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, value =>
                {
                    IList list = NonGenericIListFactory(count);
                    Assert.Throws<ArgumentException>(() => list.IndexOf(value));
                });
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_IndexOf_ReturnsFirstMatchingValue(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);

                object[] arr = new object[count];
                list.CopyTo(arr, 0);

                foreach (object duplicate in arr) // hard copies list to circumvent enumeration error
                    list.Add(duplicate);
                object[] expected = new object[count * 2];
                list.CopyTo(expected, 0);

                Assert.All(Enumerable.Range(0, count), (index =>
                    Assert.Equal(index, list.IndexOf(expected[index]))
                ));
            }
        }

        #endregion

        #region Insert

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, validAdd));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(int.MinValue, validAdd));
                Assert.Equal(count, list.Count);
            }
        }


        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_IndexGreaterThanListCount_Appends(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object validAdd = CreateT(12350);
                list.Insert(count, validAdd);
                Assert.Equal(count + 1, list.Count);
                Assert.Equal(validAdd, list[count]);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_ToReadOnlyList(int count)
        {
            if (IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => list.Insert(count / 2, CreateT(321432)));
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_FirstItemToNonNull(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object value = CreateT(123452);
                list.Insert(0, value);
                Assert.Equal(value, list[0]);
                Assert.Equal(count + 1, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_FirstItemToNull(int count)
        {
            if (!IsReadOnly && NullAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = null;
                list.Insert(0, value);
                Assert.Equal(value, list[0]);
                Assert.Equal(count + 1, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_LastItemToNonNull(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object value = CreateT(123452);
                int lastIndex = count > 0 ? count - 1 : 0;
                list.Insert(lastIndex, value);
                Assert.Equal(value, list[lastIndex]);
                Assert.Equal(count + 1, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_LastItemToNull(int count)
        {
            if (!IsReadOnly && NullAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = null;
                int lastIndex = count > 0 ? count - 1 : 0;
                list.Insert(lastIndex, value);
                Assert.Equal(value, list[lastIndex]);
                Assert.Equal(count + 1, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_DuplicateValues(int count)
        {
            if (!IsReadOnly && DuplicateValuesAllowed)
            {
                IList list = NonGenericIListFactory(count);
                object value = CreateT(123452);
                list.Insert(0, value);
                list.Insert(1, value);
                Assert.Equal(value, list[0]);
                Assert.Equal(value, list[1]);
                Assert.Equal(count + 2, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_Insert_InvalidValue(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, value =>
                {
                    IList list = NonGenericIListFactory(count);
                    Assert.Throws<ArgumentException>(() => list.Insert(count / 2, value));
                });
            }
        }

        #endregion

        #region Remove

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_OnReadOnlyCollection_ThrowsNotSupportedException(int count)
        {
            if (IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => collection.Remove(CreateT(34543)));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_NullNotContainedInCollection(int count)
        {
            if (!IsReadOnly && NullAllowed && !Enumerable.Contains(InvalidValues, null))
            {
                int seed = count * 21;
                IList collection = NonGenericIListFactory(count);
                object value = null;
                while (collection.Contains(value))
                {
                    collection.Remove(value);
                    count--;
                }
                collection.Remove(value);
                Assert.Equal(count, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_NonNullNotContainedInCollection(int count)
        {
            if (!IsReadOnly)
            {
                int seed = count * 251;
                IList collection = NonGenericIListFactory(count);
                object value = CreateT(seed++);
                while (collection.Contains(value) || Enumerable.Contains(InvalidValues, value))
                    value = CreateT(seed++);
                collection.Remove(value);
                Assert.Equal(count, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_NullContainedInCollection(int count)
        {
            if (!IsReadOnly && NullAllowed && !Enumerable.Contains(InvalidValues, null))
            {
                int seed = count * 21;
                IList collection = NonGenericIListFactory(count);
                object value = null;
                if (!collection.Contains(value))
                {
                    collection.Add(value);
                    count++;
                }
                collection.Remove(value);
                Assert.Equal(count - 1, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_NonNullContainedInCollection(int count)
        {
            if (!IsReadOnly)
            {
                int seed = count * 251;
                IList collection = NonGenericIListFactory(count);
                object value = CreateT(seed++);
                if (!collection.Contains(value))
                {
                    collection.Add(value);
                    count++;
                }
                collection.Remove(value);
                Assert.Equal(count - 1, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_ValueThatExistsTwiceInCollection(int count)
        {
            if (!IsReadOnly && DuplicateValuesAllowed)
            {
                int seed = count * 90;
                IList collection = NonGenericIListFactory(count);
                object value = CreateT(seed++);
                collection.Add(value);
                collection.Add(value);
                count += 2;
                collection.Remove(value);
                Assert.True(collection.Contains(value));
                Assert.Equal(count - 1, collection.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_EveryValue(int count)
        {
            if (!IsReadOnly)
            {
                IList collection = NonGenericIListFactory(count);
                object[] arr = new object[count];
                collection.CopyTo(arr, 0);
                Assert.All(arr, value =>
                {
                    collection.Remove(value);
                });
                Assert.Empty(collection);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Remove_InvalidValue_ThrowsArgumentException(int count)
        {
            IList collection = NonGenericIListFactory(count);
            Assert.All(InvalidValues, value =>
            {
                Assert.ThrowsAny<ArgumentException>(() => collection.Remove(value));
            });
            Assert.Equal(count, collection.Count);
        }

        #endregion

        #region RemoveAt

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_RemoveAt_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(int.MinValue));
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_RemoveAt_IndexGreaterThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                object validAdd = CreateT(0);
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(count));
                Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(count + 1));
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_RemoveAt_OnReadOnlyList(int count)
        {
            if (IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                Assert.Throws<NotSupportedException>(() => list.RemoveAt(count / 2));
                Assert.Equal(count, list.Count);
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_RemoveAt_AllValidIndices(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                Assert.Equal(count, list.Count);
                Assert.All(Enumerable.Range(0, count).Reverse(), index =>
                {
                    list.RemoveAt(index);
                    Assert.Equal(index, list.Count);
                });
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void IList_NonGeneric_RemoveAt_ZeroMultipleTimes(int count)
        {
            if (!IsReadOnly)
            {
                IList list = NonGenericIListFactory(count);
                Assert.All(Enumerable.Range(0, count), index =>
                {
                    list.RemoveAt(0);
                    Assert.Equal(count - index - 1, list.Count);
                });
            }
        }

        #endregion
    }
}
