// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// There is an area of API between IEnumerable{T} and ICollection{T} for which
    /// no common interface exists. This area is shared by the very similar Queue and Stack.
    /// For the purposes of testing the common behavior of this API,
    /// tests are consolidated into this class and delegates are used for methods that
    /// differ in name but are similar in behavior (e.g. enqueue / push both Add)
    /// </summary>
    public abstract class IGenericSharedAPI_Tests<T> : IEnumerable_Generic_Tests<T>
    {
        #region IGenericSharedAPI<T> Helper methods

        protected virtual bool DuplicateValuesAllowed => true;
        protected virtual bool DefaultValueWhenNotAllowed_Throws => true;
        protected virtual bool IsReadOnly => false;
        protected virtual bool DefaultValueAllowed => true;
        protected virtual IEnumerable<T> InvalidValues => Array.Empty<T>();

        /// <summary>
        /// Used for the IGenericSharedAPI_CopyTo_IndexLargerThanArrayCount_ThrowsArgumentException tests. Some
        /// implementations throw a different exception type (e.g. ArgumentOutOfRangeException).
        /// </summary>
        protected virtual Type IGenericSharedAPI_CopyTo_IndexLargerThanArrayCount_ThrowType => typeof(ArgumentException);

        protected virtual void AddToCollection(IEnumerable<T> collection, int numberOfItemsToAdd)
        {
            int seed = 9600;
            IEqualityComparer<T> comparer = GetIEqualityComparer();
            while (Count(collection) < numberOfItemsToAdd)
            {
                T toAdd = CreateT(seed++);
                while (collection.Contains(toAdd, comparer) || InvalidValues.Contains(toAdd, comparer))
                    toAdd = CreateT(seed++);
                Add(collection, toAdd);
            }
        }

        // There are a number of methods shared between Queue, and Stack for which there is no
        // common interface. To enable high code reuse, delegates are used to defer to those methods for
        // checking validity.
        protected abstract int Count(IEnumerable<T> enumerable);
        protected abstract void Add(IEnumerable<T> enumerable, T value);
        protected abstract void Clear(IEnumerable<T> enumerable);
        protected abstract bool Contains(IEnumerable<T> enumerable, T value);
        protected abstract void CopyTo(IEnumerable<T> enumerable, T[] array, int index);
        protected abstract bool Remove(IEnumerable<T> enumerable);

        #endregion

        #region IEnumerable<T> helper methods

        protected override IEnumerable<T> GenericIEnumerableFactory(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory();
            AddToCollection(collection, count);
            return collection;
        }

        protected abstract IEnumerable<T> GenericIEnumerableFactory();

        /// <summary>
        /// Returns a set of ModifyEnumerable delegates that modify the enumerable passed to them.
        /// </summary>
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations)
        {
            if ((operations & ModifyOperation.Add) == ModifyOperation.Add)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    Add(enumerable, CreateT(12));
                    return true;
                };
            }
            if ((operations & ModifyOperation.Remove) == ModifyOperation.Remove)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    if (Count(enumerable) > 0)
                    {
                        return Remove(enumerable);
                    }
                    return false;
                };
            }
            if ((operations & ModifyOperation.Clear) == ModifyOperation.Clear)
            {
                yield return (IEnumerable<T> enumerable) =>
                {
                    if (Count(enumerable) > 0)
                    {
                        Clear(enumerable);
                        return true;
                    }
                    return false;
                };
            }
        }
        #endregion

        #region Count

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Count_Validity(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            Assert.Equal(count, Count(collection));
        }

        #endregion

        #region Add

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_DefaultValue(int count)
        {
            if (DefaultValueAllowed && !IsReadOnly)
            {
                IEnumerable<T> collection = GenericIEnumerableFactory(count);
                Add(collection, default(T));
                Assert.Equal(count + 1, Count(collection));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_InvalidValueToMiddleOfCollection(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    IEnumerable<T> collection = GenericIEnumerableFactory(count);
                    Add(collection, invalidValue);
                    for (int i = 0; i < count; i++)
                        Add(collection, CreateT(i));
                    Assert.Equal(count * 2, Count(collection));
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_InvalidValueToBeginningOfCollection(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    IEnumerable<T> collection = GenericIEnumerableFactory(0);
                    Add(collection, invalidValue);
                    for (int i = 0; i < count; i++)
                        Add(collection, CreateT(i));
                    Assert.Equal(count, Count(collection));
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_InvalidValueToEndOfCollection(int count)
        {
            if (!IsReadOnly)
            {
                Assert.All(InvalidValues, invalidValue =>
                {
                    IEnumerable<T> collection = GenericIEnumerableFactory(count);
                    Add(collection, invalidValue);
                    Assert.Equal(count, Count(collection));
                });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_DuplicateValue(int count)
        {
            if (!IsReadOnly)
            {
                if (DuplicateValuesAllowed)
                {
                    IEnumerable<T> collection = GenericIEnumerableFactory(count);
                    T duplicateValue = CreateT(700);
                    Add(collection, duplicateValue);
                    Add(collection, duplicateValue);
                    Assert.Equal(count + 2, Count(collection));
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_AfterCallingClear(int count)
        {
            if (!IsReadOnly)
            {
                IEnumerable<T> collection = GenericIEnumerableFactory(count);
                Clear(collection);
                AddToCollection(collection, 5);
                Assert.Equal(5, Count(collection));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_AfterRemovingAnyValue(int count)
        {
            if (!IsReadOnly)
            {
                int seed = 840;
                IEnumerable<T> collection = GenericIEnumerableFactory(count);
                List<T> items = collection.ToList();
                T toAdd = CreateT(seed++);
                while (Contains(collection, toAdd))
                    toAdd = CreateT(seed++);
                Add(collection, toAdd);
                Remove(collection);

                toAdd = CreateT(seed++);
                while (Contains(collection, toAdd))
                    toAdd = CreateT(seed++);

                Add(collection, toAdd);
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Add_AfterRemovingAllItems(int count)
        {
            if (!IsReadOnly)
            {
                IEnumerable<T> collection = GenericIEnumerableFactory(count);
                List<T> itemsToRemove = collection.ToList();
                for (int i = 0; i < count; i++)
                    Remove(collection);
                Add(collection, CreateT(254));
                Assert.Equal(1, Count(collection));
            }
        }

        #endregion

        #region Clear

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Clear(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            if (IsReadOnly)
            {
                Assert.Throws<NotSupportedException>(() => Clear(collection));
                Assert.Equal(count, Count(collection));
            }
            else
            {
                Clear(collection);
                Assert.Equal(0, Count(collection));
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void IGenericSharedAPI_ClearOrRemove_ReferenceRemovedFromCollection(bool useRemove)
        {
            if (typeof(T).IsValueType || IsReadOnly)
            {
                return;
            }

            IEnumerable<T> collection = GenericIEnumerableFactory();

            WeakReference<object> wr = PopulateAndRemove(collection, useRemove);
            Assert.True(SpinWait.SpinUntil(() =>
            {
                GC.Collect();
                return !wr.TryGetTarget(out _);
            }, 30_000));
            GC.KeepAlive(collection);

            [MethodImpl(MethodImplOptions.NoInlining)]
            WeakReference<object> PopulateAndRemove(IEnumerable<T> collection, bool useRemove)
            {
                AddToCollection(collection, 1);
                T value = collection.First();

                if (useRemove)
                {
                    Assert.True(Remove(collection));
                }
                else
                {
                    Clear(collection);
                }
                Assert.Equal(0, Count(collection));

                return new WeakReference<object>(value);
            }
        }

        #endregion

        #region Contains

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Contains_BasicFunctionality(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            T[] array = collection.ToArray();

            // Collection should contain all items that result from enumeration
            Assert.All(array, item => Assert.True(Contains(collection, item)));

            Clear(collection);

            // Collection should not contain any items after being cleared
            Assert.All(array, item => Assert.False(Contains(collection, item)));

            foreach (T item in array)
                Add(collection, item);

            // Collection should contain whatever items are added back to it
            Assert.All(array, item => Assert.True(Contains(collection, item)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Contains_ValidValueOnCollectionNotContainingThatValue(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            int seed = 4315;
            T item = CreateT(seed++);
            while (Contains(collection, item))
                item = CreateT(seed++);
            Assert.False(Contains(collection, item));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_IGenericSharedAPI_Contains_ValidValueOnCollectionContainingThatValue(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            foreach (T item in collection)
                Assert.True(Contains(collection, item));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Contains_DefaultValueOnCollectionNotContainingDefaultValue(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            if (DefaultValueAllowed)
                Assert.False(Contains(collection, default(T)));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Contains_DefaultValueOnCollectionContainingDefaultValue(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            if (DefaultValueAllowed && !IsReadOnly)
            {
                Add(collection, default(T));
                Assert.True(Contains(collection, default(T)));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Contains_ValidValueThatExistsTwiceInTheCollection(int count)
        {
            if (DuplicateValuesAllowed && !IsReadOnly)
            {
                IEnumerable<T> collection = GenericIEnumerableFactory(count);
                T item = CreateT(12);
                Add(collection, item);
                Add(collection, item);
                Assert.Equal(count + 2, Count(collection));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_Contains_InvalidValue_ThrowsArgumentException(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            Assert.All(InvalidValues, invalidValue =>
                Assert.Throws<ArgumentException>(() => Contains(collection, invalidValue))
            );
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public virtual void IGenericSharedAPI_Contains_DefaultValueWhenNotAllowed(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            if (!DefaultValueAllowed && !IsReadOnly)
            {
                if (DefaultValueWhenNotAllowed_Throws)
                    Assert.Throws<ArgumentNullException>(() => Contains(collection, default(T)));
                else
                    Assert.False(Contains(collection, default(T)));
            }
        }

        #endregion

        #region CopyTo

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_NullArray_ThrowsArgumentNullException(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            Assert.Throws<ArgumentNullException>(() => CopyTo(collection, null, 0));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            T[] array = new T[count];
            Assert.Throws<ArgumentOutOfRangeException>(() => CopyTo(collection, array, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CopyTo(collection, array, int.MinValue));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            T[] array = new T[count];
            if (count > 0)
                Assert.Throws<ArgumentException>(() => CopyTo(collection, array, count));
            else
                CopyTo(collection, array, count); // does nothing since the array is empty
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            T[] array = new T[count];
            Assert.Throws(IGenericSharedAPI_CopyTo_IndexLargerThanArrayCount_ThrowType, () => CopyTo(collection, array, count + 1));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
        {
            if (count > 0) // Want the T array to have at least 1 element
            {
                IEnumerable<T> collection = GenericIEnumerableFactory(count);
                T[] array = new T[count];
                Assert.Throws<ArgumentException>(() => CopyTo(collection, array, 1));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_ExactlyEnoughSpaceInArray(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            T[] array = new T[count];
            CopyTo(collection, array, 0);
            Assert.Equal(collection, array);
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void IGenericSharedAPI_CopyTo_ArrayIsLargerThanCollection(int count)
        {
            IEnumerable<T> collection = GenericIEnumerableFactory(count);
            T[] array = new T[count * 3 / 2];
            CopyTo(collection, array, 0);
            Assert.Equal(collection, array.Take(count));
        }

        #endregion
    }
}
