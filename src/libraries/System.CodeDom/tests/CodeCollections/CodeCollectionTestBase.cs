// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Tests
{
    public abstract class CodeCollectionTestBase<TCollection, TItem> where TCollection: class where TItem : class, new()
    {
        public abstract TCollection Ctor();
        public abstract TCollection CtorArray(TItem[] array);
        public abstract TCollection CtorCollection(TCollection collection);

        public abstract int Count(TCollection collection);

        public abstract TItem GetItem(TCollection collection, int index);
        public abstract void SetItem(TCollection collection, int index, TItem value);

        public abstract void AddRange(TCollection collection, TItem[] array);
        public abstract void AddRange(TCollection collection, TCollection value);

        public abstract object Add(TCollection collection, TItem seed);

        public abstract void Insert(TCollection collection, int index, TItem value);
        public abstract void Remove(TCollection collection, TItem value);

        public abstract int IndexOf(TCollection collection, TItem value);
        public abstract bool Contains(TCollection collection, TItem value);

        public abstract void CopyTo(TCollection collection, TItem[] array, int index);

        [Fact]
        public void Ctor_Empty()
        {
            var collection = Ctor();
            Assert.Equal(0, Count(collection));
        }

        public static IEnumerable<object[]> AddRange_TestData()
        {
            yield return new object[] { new TItem[0] };
            yield return new object[] { new TItem[] { new TItem() } };
            yield return new object[] { new TItem[] { new TItem(), new TItem() } };
        }

        [Theory]
        [MemberData(nameof(AddRange_TestData))]
        public void Ctor_Array_Works(TItem[] value)
        {
            var collection = CtorArray(value);
            VerifyCollection(collection, value);
        }

        [Theory]
        [MemberData(nameof(AddRange_TestData))]
        public void Ctor_CodeStatementCollection_Works(TItem[] value)
        {
            var collection = CtorCollection(CtorArray(value));
            VerifyCollection(collection, value);
        }

        [Theory]
        [MemberData(nameof(AddRange_TestData))]
        public void AddRange_CodeStatementArray_Works(TItem[] value)
        {
            var collection = Ctor();
            AddRange(collection, value);
            VerifyCollection(collection, value);
        }

        [Theory]
        [MemberData(nameof(AddRange_TestData))]
        public void AddRange_CodeStatementCollection_Works(TItem[] value)
        {
            var collection = Ctor();
            AddRange(collection, CtorCollection(CtorArray(value)));
            VerifyCollection(collection, value);
        }

        [Fact]
        public void AddRange_Null_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => CtorArray(null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => CtorCollection(null));

            var collection = Ctor();
            AssertExtensions.Throws<ArgumentNullException>("value", () => AddRange(collection, (TItem[])null));
            AssertExtensions.Throws<ArgumentNullException>("value", () => AddRange(collection, (TCollection)null));
        }

        [Fact]
        public void AddRange_NullObjectInValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => CtorArray(new TItem[] { null }));

            var collection = Ctor();
            AssertExtensions.Throws<ArgumentNullException>("value", () => AddRange(collection, new TItem[] { null }));
        }

        [Fact]
        public void Add_CodeStatement_Insert_Remove()
        {
            var collection = Ctor();

            var value1 = new TItem();
            Assert.Equal(0, Add(collection, value1));
            Assert.Equal(1, Count(collection));
            Assert.Equal(value1, GetItem(collection, 0));

            var value2 = new TItem();
            Insert(collection, 0, value2);
            Assert.Equal(2, Count(collection));
            Assert.Same(value2, GetItem(collection, 0));

            Remove(collection, value1);
            Assert.Equal(1, Count(collection));

            Remove(collection, value2);
            Assert.Equal(0, Count(collection));
        }

        [Fact]
        public void Add_Null_ThrowsArgumentNullException()
        {
            var collection = Ctor();
            AssertExtensions.Throws<ArgumentNullException>("value", () => Add(collection, null));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var collection = Ctor();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => Insert(collection, index, new TItem()));
        }

        [Fact]
        public void Insert_Null_ThrowsArgumentNullException()
        {
            var collection = Ctor();
            AssertExtensions.Throws<ArgumentNullException>("value", () => Insert(collection, 0, null));
        }

        [Fact]
        public void Remove_Null_ThrowsArgumentNullException()
        {
            var collection = Ctor();
            AssertExtensions.Throws<ArgumentNullException>("value", () => Remove(collection, null));
        }

        [Fact]
        public void Remove_NoSuchObject_ThrowsArgumentException()
        {
            var collection = Ctor();
            AssertExtensions.Throws<ArgumentException>(null, () => Remove(collection, new TItem()));
        }

        [Fact]
        public void Contains_NoSuchObject_ReturnsMinusOne()
        {
            var collection = Ctor();
            Assert.False(Contains(collection, null));
            Assert.False(Contains(collection, new TItem()));
        }

        [Fact]
        public void IndexOf_NoSuchObject_ReturnsMinusOne()
        {
            var collection = Ctor();
            Assert.Equal(-1, IndexOf(collection, null));
            Assert.Equal(-1, IndexOf(collection, new TItem()));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public void Item_InvalidIndex_ThrowsArgumentOutOfRangeException(int index)
        {
            var collection = Ctor();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => GetItem(collection, index));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => SetItem(collection, index, new TItem()));
        }

        [Fact]
        public void ItemSet_Get_ReturnsExpected()
        {
            var value1 = new TItem();
            var value2 = new TItem();
            var collection = Ctor();
            Add(collection, value1);

            SetItem(collection, 0, value2);
            Assert.Equal(1, Count(collection));
            Assert.Same(value2, GetItem(collection, 0));
        }

        private void VerifyCollection(TCollection collection, TItem[] contents)
        {
            Assert.Equal(contents.Length, Count(collection));
            for (int i = 0; i < contents.Length; i++)
            {
                TItem content = GetItem(collection, i);
                Assert.Equal(i, IndexOf(collection, content));
                Assert.True(Contains(collection, content));
                Assert.Same(content, GetItem(collection, i));
            }

            const int Index = 1;
            var copy = new TItem[contents.Length + Index];
            CopyTo(collection, copy, Index);
            Assert.Null(copy[0]);
            for (int i = Index; i < copy.Length; i++)
            {
                Assert.Same(contents[i - Index], copy[i]);
            }
        }
    }
}
