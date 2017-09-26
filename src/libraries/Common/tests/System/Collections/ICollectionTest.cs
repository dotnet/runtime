// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Tests.Collections
{
    public abstract class ICollectionTest<T> : IEnumerableTest<T>
    {
        private readonly bool _expectedIsSynchronized;

        protected ICollectionTest(bool isSynchronized)
        {
            _expectedIsSynchronized = isSynchronized;
            ValidArrayTypes = new[] {typeof (object)};
            InvalidArrayTypes = new[]
            {
                typeof (MyInvalidReferenceType),
                typeof (MyInvalidValueType)
            };
        }

        protected Type[] ValidArrayTypes { get; set; }
        protected Type[] InvalidArrayTypes { get; set; }
        protected abstract bool ItemsMustBeUnique { get; }
        protected abstract bool ItemsMustBeNonNull { get; }

        protected bool ExpectedIsSynchronized
        {
            get { return _expectedIsSynchronized; }
        }

        protected virtual bool CopyToOnlySupportsZeroLowerBounds
        {
            get { return true; }
        }

        protected ICollection GetCollection(object[] items)
        {
            IEnumerable obj = GetEnumerable(items);
            Assert.IsAssignableFrom<ICollection>(obj);
            return (ICollection) obj;
        }

        [Fact]
        public void Count()
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            Assert.Equal(items.Length, collection.Count);
        }

        [Fact]
        public void IsSynchronized()
        {
            ICollection collection = GetCollection(GenerateItems(16));
            Assert.Equal(
                ExpectedIsSynchronized,
                collection.IsSynchronized);
        }

        [Fact]
        public void SyncRootNonNull()
        {
            ICollection collection = GetCollection(GenerateItems(16));
            Assert.NotNull(collection.SyncRoot);
        }

        [Fact]
        public void SyncRootConsistent()
        {
            ICollection collection = GetCollection(GenerateItems(16));
            object syncRoot1 = collection.SyncRoot;
            object syncRoot2 = collection.SyncRoot;
            Assert.Equal(syncRoot1, syncRoot2);
        }

        [Fact]
        public void SyncRootCanBeLocked()
        {
            ICollection collection = GetCollection(GenerateItems(16));
            lock (collection.SyncRoot)
            {
            }
        }

        [Fact]
        public void SyncRootUnique()
        {
            ICollection collection1 = GetCollection(GenerateItems(16));
            ICollection collection2 = GetCollection(GenerateItems(16));
            Assert.NotEqual(collection1.SyncRoot, collection2.SyncRoot);
        }

        [Fact]
        public void CopyToNull()
        {
            ICollection collection = GetCollection(GenerateItems(16));
            Assert.Throws<ArgumentNullException>(
                () => collection.CopyTo(null, 0));
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        public void CopyToBadIndex(int index)
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            var items2 = (object[]) items.Clone();

            Assert.ThrowsAny<ArgumentException>(
                () => collection.CopyTo(items2, index));

            CollectionAssert.Equal(items, items2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNonZeroLowerBoundArraySupported))]
        public void CopyToArrayWithNonZeroBounds()
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            if (CopyToOnlySupportsZeroLowerBounds)
            {
                Array itemArray = Array.CreateInstance(
                    typeof (object),
                    new[] {collection.Count + 8},
                    new[] {-4});
                var tempItemArray = (Array) itemArray.Clone();
                Assert.Throws<ArgumentException>(
                    () => collection.CopyTo(itemArray, 0));
                CollectionAssert.Equal(tempItemArray, itemArray);
            }
            else
            {
                Array itemArray = Array.CreateInstance(
                    typeof (object),
                    new[] {collection.Count + 4},
                    new[] {-4});
                var tempItemArray = (Array) itemArray.Clone();
                Assert.Throws<ArgumentException>(
                    () => collection.CopyTo(itemArray, 1));
                CollectionAssert.Equal(tempItemArray, itemArray);

                itemArray = Array.CreateInstance(
                    typeof (object),
                    new[] {collection.Count + 4},
                    new[] {-6});
                tempItemArray = (Array) itemArray.Clone();
                Assert.Throws<ArgumentException>(
                    () => collection.CopyTo(itemArray, -1));
                CollectionAssert.Equal(tempItemArray, itemArray);
            }
        }

        [Fact]
        public void CopyToIndexArrayLength()
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            var tempArray = (object[]) items.Clone();
            Assert.Throws<ArgumentException>(
                () => collection.CopyTo(items, collection.Count));
            CollectionAssert.Equal(tempArray, items);
        }

        [Fact]
        public void CopyToCollectionLargerThanArray()
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            object[] itemArray = GenerateItems(collection.Count + 1);
            var tempItemArray = (object[]) itemArray.Clone();
            Assert.Throws<ArgumentException>(
                () => collection.CopyTo(itemArray, 2));
            CollectionAssert.Equal(tempItemArray, itemArray);
        }

        [Fact]
        public void CopyToMDArray()
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            Assert.Throws<ArgumentException>(
                () =>
                collection.CopyTo(new object[1, collection.Count], 0));
        }

        protected void AssertThrows(
            Type[] exceptionTypes,
            Action testCode)
        {
            Exception exception = Record.Exception(testCode);
            if (exception == null)
            {
                throw new AssertActualExpectedException(
                    exceptionTypes,
                    null,
                    "Expected an exception but got null.");
            }
            Type exceptionType = exception.GetType();
            if (!exceptionTypes.Contains(exceptionType))
            {
                throw new AssertActualExpectedException(
                    exceptionTypes,
                    exceptionType,
                    "Caught wrong exception.");
            }
        }

        [Fact]
        public void CopyToInvalidTypes()
        {
            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            Type[] expectedExceptionTypes = IsGenericCompatibility
                                                ? new[]
                                                {
                                                    typeof (
                                                      ArgumentException),
                                                    typeof (
                                                      InvalidCastException
                                                      )
                                                }
                                                : new[]
                                                {
                                                    typeof (
                                                      ArgumentException)
                                                };
            foreach (Type type in InvalidArrayTypes)
            {
                Array itemArray = Array.CreateInstance(
                    type,
                    collection.Count);
                var tempItemArray = (Array) itemArray.Clone();

                AssertThrows(
                    expectedExceptionTypes,
                    () => collection.CopyTo(itemArray, 0));
                CollectionAssert.Equal(tempItemArray, itemArray);
            }
        }

        [Theory]
        [InlineData(16, 20, -5, -1)]
        [InlineData(16, 21, -4, 1)]
        [InlineData(16, 20, 0, 0)]
        [InlineData(16, 20, 0, 4)]
        [InlineData(16, 24, 0, 4)]
        public void CopyTo(
            int size,
            int arraySize,
            int arrayLowerBound,
            int copyToIndex)
        {
            if (arrayLowerBound != 0 && !PlatformDetection.IsNonZeroLowerBoundArraySupported)
                return;

            object[] items = GenerateItems(16);
            ICollection collection = GetCollection(items);
            Array itemArray = Array.CreateInstance(
                typeof (object),
                new[] {arraySize},
                new[] {arrayLowerBound});
            var tempItemArray = (Array) itemArray.Clone();
            if (CopyToOnlySupportsZeroLowerBounds
                && arrayLowerBound != 0)
            {
                Assert.Throws<ArgumentException>(
                    () => collection.CopyTo(itemArray, copyToIndex));
            }
            else
            {
                collection.CopyTo(itemArray, copyToIndex);
                Array.Copy(
                    items,
                    0,
                    tempItemArray,
                    copyToIndex,
                    items.Length);
                CollectionAssert.Equal(tempItemArray, itemArray);
            }
        }

        [Fact]
        public void CopyToValidTypes()
        {
            foreach (Type type in ValidArrayTypes)
            {
                object[] items = GenerateItems(16);
                ICollection collection = GetCollection(items);
                Array itemArray = Array.CreateInstance(
                    type,
                    collection.Count);
                collection.CopyTo(itemArray, 0);
                CollectionAssert.Equal(items, itemArray);
            }
        }

        [Fact]
        public void CollectionShouldContainAllItems()
        {
            object[] items = GenerateItems(16);
            ICollection<T> collection = GetCollection(items) as ICollection<T>;
            if (collection == null)
                return;
            Assert.All(items, item => Assert.True(collection.Contains((T) item)));
        }

        internal class MyInvalidReferenceType
        {
        }

        internal struct MyInvalidValueType
        {
        }
    }
}
