// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIobject license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Collections.Tests
{
    /// <summary>
    /// Contains tests that ensure the correctness of any class that implements the nongeneric
    /// ICollection interface
    /// </summary>
    public abstract class ICollection_NonGeneric_Tests : IEnumerable_NonGeneric_Tests
    {
        #region Helper methods

        /// <summary>
        /// Creates an instance of an ICollection that can be used for testing.
        /// </summary>
        /// <returns>An instance of an ICollection that can be used for testing.</returns>
        protected abstract ICollection NonGenericICollectionFactory();

        /// <summary>
        /// Creates an instance of an ICollection that can be used for testing.
        /// </summary>
        /// <param name="count">The number of unique items that the returned ICollection contains.</param>
        /// <returns>An instance of an ICollection that can be used for testing.</returns>
        protected virtual ICollection NonGenericICollectionFactory(int count)
        {
            ICollection collection = NonGenericICollectionFactory();
            AddToCollection(collection, count);
            return collection;
        }

        protected virtual bool DuplicateValuesAllowed { get { return true; } }
        protected virtual bool IsReadOnly { get { return false; } }
        protected virtual bool NullAllowed { get { return true; } }
        protected virtual bool ExpectedIsSynchronized { get { return false; } }
        protected virtual IEnumerable<object> InvalidValues { get { return new object[] { }; } }
        protected abstract void AddToCollection(ICollection collection, int numberOfItemsToAdd);

        /// <summary>
        /// Used for the ICollection_NonGeneric_CopyTo_ArrayOfEnumType test where we try to call CopyTo
        /// on an Array of Enum values. Some implementations special-case for this and throw an argumentexception,
        /// while others just throw an InvalidCastExcepton.
        /// </summary>
        protected virtual bool ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowsArgumentException { get { return false; } }

        #endregion

        #region IEnumerable Helper Methods

        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables { get { return new List<ModifyEnumerable>(); } }

        protected override IEnumerable NonGenericIEnumerableFactory(int count)
        {
            return NonGenericICollectionFactory(count);
        }

        #endregion

        #region Count

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_Count_Validity(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.Equal(count, collection.Count);
        }

        #endregion

        #region IsSynchronized

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_IsSynchronized(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.Equal(ExpectedIsSynchronized, collection.IsSynchronized);
        }

        #endregion

        #region SyncRoot

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_SyncRoot_NonNull(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.NotNull(collection.SyncRoot);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_SyncRootConsistent(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object syncRoot1 = collection.SyncRoot;
            object syncRoot2 = collection.SyncRoot;
            Assert.Same(syncRoot1, syncRoot2);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_SyncRootUnique(int count)
        {
            ICollection collection1 = NonGenericICollectionFactory(count);
            ICollection collection2 = NonGenericICollectionFactory(count);
            Assert.NotSame(collection1.SyncRoot, collection2.SyncRoot);
        }

        #endregion

        #region CopyTo

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_NullArray_ThrowsArgumentNullException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_TwoDimensionArray_ThrowsArgumentException(int count)
        {
            if (count > 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                Array arr = new object[count,count];
                Assert.Equal(2, arr.Rank);
                Assert.Throws<ArgumentException>(() => collection.CopyTo(arr, 0));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_NonZeroLowerBound(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            Array arr = Array.CreateInstance(typeof(object), new int[1] { 2 }, new int[1] { 2 });
            Assert.Equal(1, arr.Rank);
            Assert.Equal(2, arr.GetLowerBound(0));
            Assert.ThrowsAny<ArgumentException>(() => collection.CopyTo(arr, 0));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectValueType(int count)
        {
            if (count > 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                float[] array = new float[count * 3 / 2];
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 0));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType(int count)
        {
            if (count > 0)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                StringBuilder[] array = new StringBuilder[count * 3 / 2];
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 0));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public virtual void ICollection_NonGeneric_CopyTo_ArrayOfEnumType(int count)
        {
            Array enumArr = Enum.GetValues(typeof(EnumerableType));
            if (count > 0 && count < enumArr.Length)
            {
                ICollection collection = NonGenericICollectionFactory(count);
                if (ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowsArgumentException)
                    Assert.Throws<ArgumentException>(() => collection.CopyTo(enumArr, 0));
                else
                    Assert.Throws<InvalidCastException>(() => collection.CopyTo(enumArr, 0));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(array, int.MinValue));
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_IndexEqualToArrayCount_ThrowsArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            if (count > 0)
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, count));
            else
                collection.CopyTo(array, count); // does nothing since the array is empty
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_IndexLargerThanArrayCount_ThrowsAnyArgumentException(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            Assert.ThrowsAny<ArgumentException>(() => collection.CopyTo(array, count + 1)); // some implementations throw ArgumentOutOfRangeException for this scenario
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_NotEnoughSpaceInOffsettedArray_ThrowsArgumentException(int count)
        {
            if (count > 0) // Want the T array to have at least 1 element
            {
                ICollection collection = NonGenericICollectionFactory(count);
                object[] array = new object[count];
                Assert.Throws<ArgumentException>(() => collection.CopyTo(array, 1));
            }
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ExactlyEnoughSpaceInArray(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }

        [Theory]
        [MemberData("ValidCollectionSizes")]
        public void ICollection_NonGeneric_CopyTo_ArrayIsLargerThanCollection(int count)
        {
            ICollection collection = NonGenericICollectionFactory(count);
            object[] array = new object[count * 3 / 2];
            collection.CopyTo(array, 0);
            int i = 0;
            foreach (object obj in collection)
                Assert.Equal(array[i++], obj);
        }

        #endregion
    }
}
