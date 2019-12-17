// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        #region IList<T> Helper Methods

        protected override IList<T> GenericIListFactory()
        {
            return GenericListFactory();
        }

        protected override IList<T> GenericIListFactory(int count)
        {
            return GenericListFactory(count);
        }

        #endregion

        #region List<T> Helper Methods

        protected virtual List<T> GenericListFactory()
        {
            return new List<T>();
        }

        protected virtual List<T> GenericListFactory(int count)
        {
            IEnumerable<T> toCreateFrom = CreateEnumerable(EnumerableType.List, null, count, 0, 0);
            return new List<T>(toCreateFrom);
        }

        protected void VerifyList(List<T> list, List<T> expectedItems)
        {
            Assert.Equal(expectedItems.Count, list.Count);

            //Only verify the indexer. List should be in a good enough state that we
            //do not have to verify consistency with any other method.
            for (int i = 0; i < list.Count; ++i)
            {
                Assert.True(list[i] == null ? expectedItems[i] == null : list[i].Equals(expectedItems[i]));
            }
        }

        #endregion

        #region CopyTo_Span

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_NegativeSourceIndex_ThrowsArgumentOutOfRangeException(int count)
        {
            List<T> list = GenericListFactory(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(-1, count, span); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(int.MinValue, count, span); });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_NegativeCount_ThrowsArgumentOutOfRangeException(int count)
        {
            List<T> list = GenericListFactory(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(0, -1, span); });
            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(0, int.MinValue, span); });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_SourceIndexLargerThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            List<T> list = GenericListFactory(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(count + 1, 0, span); });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_CountLargerThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            List<T> list = GenericListFactory(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(0, count + 1, span); });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_SourceIndexPlusCountLargerThanListCount_ThrowsArgumentOutOfRangeException(int count)
        {
            List<T> list = GenericListFactory(count);

            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count]; list.CopyTo(1, count, span); });
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_NotEnoughSpaceInSpan_ThrowsArgumentException(int count)
        {
            if (count > 0)
            {
                List<T> list = GenericListFactory(count);

                Assert.Throws<ArgumentException>("destination", () => { Span<T> span = new T[count - 1]; list.CopyTo(0, count, span); });
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_ExactlyEnoughSpaceInArray(int count)
        {
            List<T> list = GenericListFactory(count);
            Span<T> span = new T[count];
            list.CopyTo(0, count, span);
            Assert.True(Enumerable.SequenceEqual(list, span.ToArray()));

            span = new T[count];
            list.CopyTo(span);
            Assert.True(Enumerable.SequenceEqual(list, span.ToArray()));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_SpanIsLargerThanCollection(int count)
        {
            List<T> list = GenericListFactory(count);
            Span<T> span = new T[count * 3 / 2];
            list.CopyTo(span);

            Assert.True(Enumerable.SequenceEqual(list, span.Slice(0, count).ToArray()));
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_SourceIndexGreaterThanZero(int count)
        {
            if (count > 0)
            {
                List<T> list = GenericListFactory(count);
                Span<T> span = new T[count];
                list.CopyTo(1, count - 1, span);

                Assert.True(Enumerable.SequenceEqual(list.Skip(1), span.Slice(0, count - 1).ToArray()));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_CountSmallerListCount(int count)
        {
            if (count > 0)
            {
                List<T> list = GenericListFactory(count);
                Span<T> span = new T[count];
                list.CopyTo(0, count - 1, span);

                Assert.True(Enumerable.SequenceEqual(list.Take(count - 1), span.Slice(0, count - 1).ToArray()));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_SourceIndexGreaterThanZeroCountSmallerListCount(int count)
        {
            if (count > 1)
            {
                List<T> list = GenericListFactory(count);
                Span<T> span = new T[count];
                list.CopyTo(1, count - 2, span);

                Assert.True(Enumerable.SequenceEqual(list.Skip(1).Take(count - 2), span.Slice(0, count - 2).ToArray()));
            }
        }

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyToSpan_EnsureDoesntCopyMoreThanListCount(int count)
        {
            List<T> list = GenericListFactory(count);
            list.Capacity = count + 1;

            Assert.Throws<ArgumentOutOfRangeException>(() => { Span<T> span = new T[count + 1]; list.CopyTo(0, count + 1, span); });

            Span<T> span = new T[count + 1];
            T nonDefaultValue;
            do
            {
                nonDefaultValue = CreateT(1);
            }
            while (EqualityComparer<T>.Default.Equals(nonDefaultValue, default));
            
            span[count] = nonDefaultValue;

            list.CopyTo(span);
            Assert.Equal(span[count], nonDefaultValue);

        }
        #endregion

        [Theory]
        [MemberData(nameof(ValidCollectionSizes))]
        public void CopyTo_ArgumentValidity(int count)
        {
            List<T> list = GenericListFactory(count);
            AssertExtensions.Throws<ArgumentException>(null, () => list.CopyTo(0, new T[0], 0, count + 1));
            AssertExtensions.Throws<ArgumentException>(null, () => list.CopyTo(count, new T[0], 0, 1));
        }
    }
}
