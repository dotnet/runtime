// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Immutable.Test
{
    public class ImmutableArrayBuilderTest : SimpleElementImmutablesTestBase
    {
        [Fact]
        public void CreateBuilderDefaultCapacity()
        {
            var builder = ImmutableArray.CreateBuilder<int>();
            Assert.NotNull(builder);
            Assert.NotSame(builder, ImmutableArray.CreateBuilder<int>());
        }

        [Fact]
        public void CreateBuilderInvalidCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateBuilder<int>(-1));
        }

        [Fact]
        public void NormalConstructionValueType()
        {
            var builder = ImmutableArray.CreateBuilder<int>(3);
            Assert.Equal(0, builder.Count);
            Assert.False(((ICollection<int>)builder).IsReadOnly);
            for (int i = 0; i < builder.Count; i++)
            {
                Assert.Equal(0, builder[i]);
            }

            builder.Add(5);
            builder.Add(6);
            builder.Add(7);

            Assert.Equal(5, builder[0]);
            Assert.Equal(6, builder[1]);
            Assert.Equal(7, builder[2]);
        }

        [Fact]
        public void NormalConstructionRefType()
        {
            var builder = new ImmutableArray<GenericParameterHelper>.Builder(3);
            Assert.Equal(0, builder.Count);
            Assert.False(((ICollection<GenericParameterHelper>)builder).IsReadOnly);
            for (int i = 0; i < builder.Count; i++)
            {
                Assert.Null(builder[i]);
            }

            builder.Add(new GenericParameterHelper(5));
            builder.Add(new GenericParameterHelper(6));
            builder.Add(new GenericParameterHelper(7));

            Assert.Equal(5, builder[0].Data);
            Assert.Equal(6, builder[1].Data);
            Assert.Equal(7, builder[2].Data);
        }

        [Fact]
        public void AddRangeIEnumerable()
        {
            var builder = new ImmutableArray<int>.Builder(2);
            builder.AddRange((IEnumerable<int>)new[] { 1 });
            Assert.Equal(1, builder.Count);

            builder.AddRange((IEnumerable<int>)new[] { 2 });
            Assert.Equal(2, builder.Count);

            // Exceed capacity
            builder.AddRange(Enumerable.Range(3, 2)); // use an enumerable without a breakable Count
            Assert.Equal(4, builder.Count);

            Assert.Equal(Enumerable.Range(1, 4), builder);
        }

        [Fact]
        public void Add()
        {
            var builder = ImmutableArray.CreateBuilder<int>(0);
            builder.Add(1);
            builder.Add(2);
            Assert.Equal(new int[] { 1, 2 }, builder);
            Assert.Equal(2, builder.Count);

            builder = ImmutableArray.CreateBuilder<int>(1);
            builder.Add(1);
            builder.Add(2);
            Assert.Equal(new int[] { 1, 2 }, builder);
            Assert.Equal(2, builder.Count);

            builder = ImmutableArray.CreateBuilder<int>(2);
            builder.Add(1);
            builder.Add(2);
            Assert.Equal(new int[] { 1, 2 }, builder);
            Assert.Equal(2, builder.Count);
        }

        [Fact]
        public void AddRangeBuilder()
        {
            var builder1 = new ImmutableArray<int>.Builder(2);
            var builder2 = new ImmutableArray<int>.Builder(2);

            builder1.AddRange(builder2);
            Assert.Equal(0, builder1.Count);
            Assert.Equal(0, builder2.Count);

            builder2.Add(1);
            builder2.Add(2);
            builder1.AddRange(builder2);
            Assert.Equal(2, builder1.Count);
            Assert.Equal(2, builder2.Count);
            Assert.Equal(new[] { 1, 2 }, builder1);
        }

        [Fact]
        public void AddRangeImmutableArray()
        {
            var builder1 = new ImmutableArray<int>.Builder(2);
            var array = ImmutableArray.Create(1, 2, 3);

            builder1.AddRange(array);
            Assert.Equal(new[] { 1, 2, 3 }, builder1);
        }

        [Fact]
        public void AddRangeDerivedArray()
        {
            var builder = new ImmutableArray<object>.Builder();
            builder.AddRange(new[] { "a", "b" });
            Assert.Equal(new[] { "a", "b" }, builder);
        }

        [Fact]
        public void AddRangeDerivedImmutableArray()
        {
            var builder = new ImmutableArray<object>.Builder();
            builder.AddRange(new[] { "a", "b" }.ToImmutableArray());
            Assert.Equal(new[] { "a", "b" }, builder);
        }

        [Fact]
        public void AddRangeDerivedBuilder()
        {
            var builder = new ImmutableArray<string>.Builder();
            builder.AddRange(new[] { "a", "b" });

            var builderBase = new ImmutableArray<object>.Builder();
            builderBase.AddRange(builder);
            Assert.Equal(new[] { "a", "b" }, builderBase);
        }

        [Fact]
        public void Contains()
        {
            var builder = new ImmutableArray<int>.Builder();
            Assert.False(builder.Contains(1));
            builder.Add(1);
            Assert.True(builder.Contains(1));
        }

        [Fact]
        public void IndexOf()
        {
            IndexOfTests.IndexOfTest(
                seq => (ImmutableArray<int>.Builder)this.GetEnumerableOf(seq),
                (b, v) => b.IndexOf(v),
                (b, v, i) => b.IndexOf(v, i),
                (b, v, i, c) => b.IndexOf(v, i, c),
                (b, v, i, c, eq) => b.IndexOf(v, i, c, eq));
        }

        [Fact]
        public void LastIndexOf()
        {
            IndexOfTests.LastIndexOfTest(
                seq => (ImmutableArray<int>.Builder)this.GetEnumerableOf(seq),
                (b, v) => b.LastIndexOf(v),
                (b, v, eq) => b.LastIndexOf(v, b.Count > 0 ? b.Count - 1 : 0, b.Count, eq),
                (b, v, i) => b.LastIndexOf(v, i),
                (b, v, i, c) => b.LastIndexOf(v, i, c),
                (b, v, i, c, eq) => b.LastIndexOf(v, i, c, eq));
        }

        [Fact]
        public void Insert()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3);
            builder.Insert(1, 4);
            builder.Insert(4, 5);
            Assert.Equal(new[] { 1, 4, 2, 3, 5 }, builder);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Insert(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Insert(builder.Count + 1, 0));
        }

        [Fact]
        public void Remove()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3, 4);
            Assert.True(builder.Remove(1));
            Assert.False(builder.Remove(6));
            Assert.Equal(new[] { 2, 3, 4 }, builder);
            Assert.True(builder.Remove(3));
            Assert.Equal(new[] { 2, 4 }, builder);
            Assert.True(builder.Remove(4));
            Assert.Equal(new[] { 2 }, builder);
            Assert.True(builder.Remove(2));
            Assert.Equal(0, builder.Count);
        }

        [Fact]
        public void RemoveAt()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3, 4);
            builder.RemoveAt(0);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.RemoveAt(3));
            Assert.Equal(new[] { 2, 3, 4 }, builder);
            builder.RemoveAt(1);
            Assert.Equal(new[] { 2, 4 }, builder);
            builder.RemoveAt(1);
            Assert.Equal(new[] { 2 }, builder);
            builder.RemoveAt(0);
            Assert.Equal(0, builder.Count);
        }

        [Fact]
        public void ReverseContents()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3, 4);
            builder.ReverseContents();
            Assert.Equal(new[] { 4, 3, 2, 1 }, builder);

            builder.RemoveAt(0);
            builder.ReverseContents();
            Assert.Equal(new[] { 1, 2, 3 }, builder);

            builder.RemoveAt(0);
            builder.ReverseContents();
            Assert.Equal(new[] { 3, 2 }, builder);

            builder.RemoveAt(0);
            builder.ReverseContents();
            Assert.Equal(new[] { 2 }, builder);

            builder.RemoveAt(0);
            builder.ReverseContents();
            Assert.Equal(new int[0], builder);
        }

        [Fact]
        public void Sort()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(2, 4, 1, 3);
            builder.Sort();
            Assert.Equal(new[] { 1, 2, 3, 4 }, builder);
        }

        [Fact]
        public void SortRange()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(2, 4, 1, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Sort(-1, 2, Comparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Sort(1, 4, Comparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Sort(0, -1, Comparer<int>.Default));

            builder.Sort(builder.Count, 0, Comparer<int>.Default);
            Assert.Equal(new int[] { 2, 4, 1, 3 }, builder);

            Assert.Throws<ArgumentNullException>(() => builder.Sort(1, 2, null));
            builder.Sort(1, 2, Comparer<int>.Default);
            Assert.Equal(new[] { 2, 1, 4, 3 }, builder);
        }

        [Fact]
        public void SortComparer()
        {
            var builder1 = new ImmutableArray<string>.Builder();
            var builder2 = new ImmutableArray<string>.Builder();
            builder1.AddRange("c", "B", "a");
            builder2.AddRange("c", "B", "a");
            builder1.Sort(StringComparer.OrdinalIgnoreCase);
            builder2.Sort(StringComparer.Ordinal);
            Assert.Equal(new[] { "a", "B", "c" }, builder1);
            Assert.Equal(new[] { "B", "a", "c" }, builder2);
        }

        [Fact]
        public void Count()
        {
            var builder = new ImmutableArray<int>.Builder(3);

            // Initial count is at zero, which is less than capacity.
            Assert.Equal(0, builder.Count);

            // Expand the accessible region of the array by increasing the count, but still below capacity.
            builder.Count = 2;
            Assert.Equal(2, builder.Count);
            Assert.Equal(2, builder.ToList().Count);
            Assert.Equal(0, builder[0]);
            Assert.Equal(0, builder[1]);
            Assert.Throws<IndexOutOfRangeException>(() => builder[2]);

            // Expand the accessible region of the array beyond the current capacity.
            builder.Count = 4;
            Assert.Equal(4, builder.Count);
            Assert.Equal(4, builder.ToList().Count);
            Assert.Equal(0, builder[0]);
            Assert.Equal(0, builder[1]);
            Assert.Equal(0, builder[2]);
            Assert.Equal(0, builder[3]);
            Assert.Throws<IndexOutOfRangeException>(() => builder[4]);
        }

        [Fact]
        public void CountContract()
        {
            var builder = new ImmutableArray<int>.Builder(100);
            builder.AddRange(Enumerable.Range(1, 100));
            builder.Count = 10;
            Assert.Equal(Enumerable.Range(1, 10), builder);
            builder.Count = 100;
            Assert.Equal(Enumerable.Range(1, 10).Concat(new int[90]), builder);
        }

        [Fact]
        public void IndexSetter()
        {
            var builder = new ImmutableArray<int>.Builder();
            Assert.Throws<IndexOutOfRangeException>(() => builder[0] = 1);
            Assert.Throws<IndexOutOfRangeException>(() => builder[-1] = 1);

            builder.Count = 1;
            builder[0] = 2;
            Assert.Equal(2, builder[0]);

            builder.Count = 10;
            builder[9] = 3;
            Assert.Equal(3, builder[9]);

            builder.Count = 2;
            Assert.Equal(2, builder[0]);
            Assert.Throws<IndexOutOfRangeException>(() => builder[2]);
        }

        [Fact]
        public void ToImmutable()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3);

            ImmutableArray<int> array = builder.ToImmutable();
            Assert.Equal(1, array[0]);
            Assert.Equal(2, array[1]);
            Assert.Equal(3, array[2]);

            // Make sure that subsequent mutation doesn't impact the immutable array.
            builder[1] = 5;
            Assert.Equal(5, builder[1]);
            Assert.Equal(2, array[1]);

            builder.Clear();
            Assert.True(builder.ToImmutable().IsEmpty);
        }

        [Fact]
        public void Clear()
        {
            var builder = new ImmutableArray<int>.Builder(2);
            builder.Add(1);
            builder.Add(1);
            builder.Clear();
            Assert.Equal(0, builder.Count);
            Assert.Throws<IndexOutOfRangeException>(() => builder[0]);
        }

        [Fact]
        public void MutationsSucceedAfterToImmutable()
        {
            var builder = new ImmutableArray<int>.Builder(1);
            builder.Add(1);
            var immutable = builder.ToImmutable();
            builder[0] = 0;
            Assert.Equal(0, builder[0]);
            Assert.Equal(1, immutable[0]);
        }

        [Fact]
        public void Enumerator()
        {
            var empty = new ImmutableArray<int>.Builder(0);
            var enumerator = empty.GetEnumerator();
            Assert.False(enumerator.MoveNext());

            var manyElements = new ImmutableArray<int>.Builder(3);
            manyElements.AddRange(1, 2, 3);
            enumerator = manyElements.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(3, enumerator.Current);

            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void IEnumerator()
        {
            var empty = new ImmutableArray<int>.Builder(0);
            var enumerator = ((IEnumerable<int>)empty).GetEnumerator();
            Assert.False(enumerator.MoveNext());

            var manyElements = new ImmutableArray<int>.Builder(3);
            manyElements.AddRange(1, 2, 3);
            enumerator = ((IEnumerable<int>)manyElements).GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(3, enumerator.Current);

            Assert.False(enumerator.MoveNext());
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            var builder = new ImmutableArray<T>.Builder(contents.Length);
            builder.AddRange(contents);
            return builder;
        }
    }
}
