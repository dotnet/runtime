// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableArrayBuilderTest : SimpleElementImmutablesTestBase
    {
        public static IEnumerable<object[]> BuilderAddRangeData()
        {
            yield return new object[] { new[] { "a", "b" }, Array.Empty<string>(), new[] { "a", "b" } };
            yield return new object[] { Array.Empty<string>(), new[] { "a", "b" }, new[] { "a", "b" } };
            yield return new object[] { new[] { "a", "b" }, new[] { "c", "d" }, new[] { "a", "b", "c", "d" } };
        }

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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("capacity", () => ImmutableArray.CreateBuilder<int>(-1));
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

            builder.AddRange((IEnumerable<int>)new int[0]);
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

            AssertExtensions.Throws<ArgumentNullException>("items", () => builder1.AddRange((int[])null));
            AssertExtensions.Throws<ArgumentNullException>("items", () => builder1.AddRange(null, 42));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder1.AddRange(new int[0], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder1.AddRange(new int[0], 42));

            AssertExtensions.Throws<ArgumentNullException>("items", () => builder1.AddRange((ImmutableArray<int>.Builder)null));
            AssertExtensions.Throws<ArgumentNullException>("items", () => builder1.AddRange((IEnumerable<int>)null));

            Assert.Throws<NullReferenceException>(() => builder1.AddRange(default(ImmutableArray<int>)));
            builder1.AddRange(default(ImmutableArray<int>), 42);

            var builder2 = new ImmutableArray<object>.Builder();
            builder2.AddRange(default(ImmutableArray<string>));
            AssertExtensions.Throws<ArgumentNullException>("items", () => builder2.AddRange((ImmutableArray<string>.Builder)null));
        }

        [Theory]
        [MemberData(nameof(BuilderAddRangeData))]
        public void AddRangeDerivedArray(string[] builderElements, string[] rangeElements, string[] expectedResult)
        {
            // Initialize builder
            var builder = new ImmutableArray<object>.Builder();
            builder.AddRange(builderElements);

            // AddRange
            builder.AddRange(rangeElements);

            // Assert
            Assert.Equal(expectedResult, builder);
        }

        [Theory]
        [MemberData(nameof(BuilderAddRangeData))]
        public void AddRangeSpan(string[] builderElements, string[] rangeElements, string[] expectedResult)
        {
            // Initialize builder
            var builder = new ImmutableArray<string>.Builder();
            builder.AddRange(builderElements);

            // AddRange
            builder.AddRange(new ReadOnlySpan<string>(rangeElements));

            // Assert
            Assert.Equal(expectedResult, builder);
        }

        [Theory]
        [MemberData(nameof(BuilderAddRangeData))]
        public void AddRangeDerivedSpan(string[] builderElements, string[] rangeElements, string[] expectedResult)
        {
            // Initialize builder
            var builder = new ImmutableArray<object>.Builder();
            builder.AddRange(builderElements);

            // AddRange
            builder.AddRange(new ReadOnlySpan<string>(rangeElements));
            
            // Assert
            Assert.Equal(expectedResult, builder);
        }

        [Theory]
        [MemberData(nameof(BuilderAddRangeData))]
        public void AddRangeDerivedImmutableArray(string[] builderElements, string[] rangeElements, string[] expectedResult)
        {
            // Initialize builder
            var builder = new ImmutableArray<object>.Builder();
            builder.AddRange(builderElements);

            // AddRange
            builder.AddRange(rangeElements.ToImmutableArray());
            
            // Assert
            Assert.Equal(expectedResult, builder);
        }

        [Theory]
        [MemberData(nameof(BuilderAddRangeData))]
        public void AddRangeDerivedBuilder(string[] builderElements, string[] rangeElements, string[] expectedResult)
        {
            // Initialize builder
            var builderBase = new ImmutableArray<object>.Builder();
            builderBase.AddRange(builderElements);
            
            // Prepare another builder to add
            var builder = new ImmutableArray<string>.Builder();
            builder.AddRange(rangeElements);

            // AddRange
            builderBase.AddRange(builder);

            // Assert
            Assert.Equal(expectedResult, builderBase);
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
        public void IndexOf_WithoutCountParam()
        {
            var builder = ImmutableArray.Create(2, 5, 8).ToBuilder();
            var absComparer = new DelegateEqualityComparer<int>(equals: (x, y) => Math.Abs(x) == Math.Abs(y));

            Assert.Equal(1, builder.IndexOf(-5, 0, absComparer));
            Assert.Equal(-1, builder.IndexOf(-5, 2, absComparer));
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
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(-1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Insert(builder.Count + 1, 0));
        }

        [Fact]
        public void InsertRange()
        {
            var builder = new ImmutableArray<int>.Builder();

            builder.InsertRange(0, Enumerable.Range(1, 4));
            Assert.Equal(new[] { 1, 2, 3, 4 }, builder);

            builder.InsertRange(1, Enumerable.Range(5, 2));
            Assert.Equal(new[] { 1, 5, 6, 2, 3, 4 }, builder);

            builder.InsertRange(0, new ImmutableArray<int>(new int[] { 7, 8 }));
            Assert.Equal(new[] { 7, 8, 1, 5, 6, 2, 3, 4 }, builder);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.InsertRange(-1, Enumerable.Range(1, 2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.InsertRange(100, Enumerable.Range(1, 2)));
            AssertExtensions.Throws<ArgumentNullException>("items", () => builder.InsertRange(2, null));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.InsertRange(-1, new ImmutableArray<int>()));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.InsertRange(100, new ImmutableArray<int>()));
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
        public void Remove_EqualityComparer()
        {
            var builder = ImmutableArray.Create(1.5, 2.5, 3.5).ToBuilder();
            var absComparer = new DelegateEqualityComparer<double>(equals: (x, y) => Math.Abs(x) == Math.Abs(y));

            Assert.True(builder.Remove(-1.5, absComparer));
            Assert.Equal(new[] { 2.5, 3.5 }, builder);

            Assert.False(builder.Remove(5, absComparer));
            Assert.False(builder.Remove(4, null));
        }

        [Fact]
        public void RemoveAt()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3, 4);
            builder.RemoveAt(0);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.RemoveAt(3));
            Assert.Equal(new[] { 2, 3, 4 }, builder);
            builder.RemoveAt(1);
            Assert.Equal(new[] { 2, 4 }, builder);
            builder.RemoveAt(1);
            Assert.Equal(new[] { 2 }, builder);
            builder.RemoveAt(0);
            Assert.Equal(0, builder.Count);
        }

        [Fact]
        public void RemoveRange_ValueType()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3, 4, 5);

            builder.RemoveRange(1, 2);
            Assert.Equal(new[] { 1, 4, 5 }, builder);

            builder.RemoveRange(new int[] { 4, 6 });
            Assert.Equal(new[] { 1, 5 }, builder);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.RemoveRange(-1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.RemoveRange(1, 10));
            AssertExtensions.Throws<ArgumentNullException>("items", () => builder.RemoveRange(null));
        }

        [Fact]
        public void RemoveRange_ReferenceType()
        {
            var builder = new ImmutableArray<GenericParameterHelper>.Builder();
            builder.AddRange(new GenericParameterHelper(1), new GenericParameterHelper(2), new GenericParameterHelper(3), new GenericParameterHelper(4));

            builder.RemoveRange(1, 2);

            Assert.Equal(new[] { new GenericParameterHelper(1), new GenericParameterHelper(4) }, builder);
        }

        [Fact]
        public void RemoveRange_EqualityComparer()
        {
            var builder = ImmutableArray.Create(1.5, 2.5, 3.5, 4.5, 5.6).ToBuilder();
            var absComparer = new DelegateEqualityComparer<double>(equals: (x, y) => Math.Abs(x) == Math.Abs(y));

            builder.RemoveRange(new[] { -2.5, -4.5, 6.2 }, absComparer);
            Assert.Equal(new[] { 1.5, 3.5, 5.6 }, builder);
            AssertExtensions.Throws<ArgumentNullException>("items", () => builder.RemoveRange(null, absComparer));
        }

        [Fact]
        public void RemoveAll()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(Enumerable.Range(1, 8));
            builder.RemoveAll(n => n % 2 == 0);

            Assert.Equal(new[] { 1, 3, 5, 7 }, builder);
        }

        [Fact]
        public void ReverseContents()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(1, 2, 3, 4);
            builder.Reverse();
            Assert.Equal(new[] { 4, 3, 2, 1 }, builder);

            builder.RemoveAt(0);
            builder.Reverse();
            Assert.Equal(new[] { 1, 2, 3 }, builder);

            builder.RemoveAt(0);
            builder.Reverse();
            Assert.Equal(new[] { 3, 2 }, builder);

            builder.RemoveAt(0);
            builder.Reverse();
            Assert.Equal(new[] { 2 }, builder);

            builder.RemoveAt(0);
            builder.Reverse();
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
        public void Sort_Comparison()
        {
            var builder = new ImmutableArray<int>.Builder(4);

            builder.Sort((x, y) => y.CompareTo(x));
            Assert.Equal(Array.Empty<int>(), builder);

            builder.AddRange(2, 4, 1, 3);
            builder.Sort((x, y) => y.CompareTo(x));
            Assert.Equal(new[] { 4, 3, 2, 1 }, builder);

            builder.Add(5);
            builder.Sort((x, y) => x.CompareTo(y));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, builder);
        }

        [Fact]
        public void Sort_NullComparison_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("comparison", () => ImmutableArray.CreateBuilder<int>().Sort((Comparison<int>)null));
        }

        [Fact]
        public void SortNullComparer()
        {
            var template = ImmutableArray.Create(2, 4, 1, 3);

            var builder = template.ToBuilder();
            builder.Sort((IComparer<int>)null);
            Assert.Equal(new[] { 1, 2, 3, 4 }, builder);

            builder = template.ToBuilder();
            builder.Sort(1, 2, null);
            Assert.Equal(new[] { 2, 1, 4, 3 }, builder);
        }

        [Fact]
        public void SortOneElementArray()
        {
            int[] resultantArray = new[] { 4 };

            var builder1 = new ImmutableArray<int>.Builder();
            builder1.Add(4);
            builder1.Sort();
            Assert.Equal(resultantArray, builder1);

            var builder2 = new ImmutableArray<int>.Builder();
            builder2.Add(4);
            builder2.Sort(Comparer<int>.Default);
            Assert.Equal(resultantArray, builder2);

            var builder3 = new ImmutableArray<int>.Builder();
            builder3.Add(4);
            builder3.Sort(0, 1, Comparer<int>.Default);
            Assert.Equal(resultantArray, builder3);
        }

        [Fact]
        public void SortRange()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(2, 4, 1, 3);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.Sort(-1, 2, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Sort(1, 4, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => builder.Sort(0, -1, Comparer<int>.Default));

            builder.Sort(builder.Count, 0, Comparer<int>.Default);
            Assert.Equal(new int[] { 2, 4, 1, 3 }, builder);

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
        public void ToImmutableArray()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.AddRange(0, 1, 2);

            var array = builder.ToImmutableArray();
            Assert.Equal(0, array[0]);
            Assert.Equal(1, array[1]);
            Assert.Equal(2, array[2]);

            builder[1] = 5;
            Assert.Equal(5, builder[1]);
            Assert.Equal(1, array[1]);

            builder.Clear();
            Assert.True(builder.ToImmutableArray().IsEmpty);
            Assert.False(array.IsEmpty);

            ImmutableArray<int>.Builder nullBuilder = null;
            AssertExtensions.Throws<ArgumentNullException>("builder", () => nullBuilder.ToImmutableArray());
        }

        [Fact]
        public void CopyToArray()
        {
            var builder = ImmutableArray.Create(1, 2, 3).ToBuilder();
            var target = new int[4];

            builder.CopyTo(target, 1);
            Assert.Equal(new[] { 0, 1, 2, 3 }, target);

            AssertExtensions.Throws<ArgumentNullException>("array", () => builder.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.CopyTo(target, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => builder.CopyTo(target, 2));
        }

        [Fact]
        public void CopyTo_DestinationArray()
        {
            var builder = ImmutableArray.Create(1, 2, 3).ToBuilder();
            var target = new int[4];

            builder.CopyTo(target);
            Assert.Equal(new[] { 1, 2, 3, 0 }, target);

            AssertExtensions.Throws<ArgumentNullException>("destination", () => builder.CopyTo(null));
        }

        [Fact]
        public void CopyTo_SourceIdx_DestinationArr_DestinationIdx_Length()
        {
            var builder = ImmutableArray.Create(1, 2, 3).ToBuilder();
            var target = new int[4];

            builder.CopyTo(1, target, 1, 2);
            Assert.Equal(new[] { 0, 2, 3, 0 }, target);

            AssertExtensions.Throws<ArgumentNullException>("destination", () => builder.CopyTo(1, null, 2, 3));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.CopyTo(1, target, 2, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", () => builder.CopyTo(1, target, 2, 8));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", () => builder.CopyTo(1, target, 5, 2));
        }

        [Fact]
        public void CopyToSpan()
        {
            var builder = ImmutableArray.Create(1, 2, 3).ToBuilder();
            Span<int> span;
            int[] target = new int[4];

            // Span is longer than immutableArray
            span = new Span<int>(target);
            builder.CopyTo(span);
            Assert.Equal(new[] { 1, 2, 3, 0 }, target);
            span.Fill(0);

            // Span has same length as immutableArray
            span = new Span<int>(target, 0, 3);
            builder.CopyTo(span);
            Assert.Equal(new[] { 1, 2, 3, 0 }, target);
            span.Fill(0);

            // Span is shorter than immutableArray
            span = new Span<int>(target, 0, 2);
            AssertExtensions.Throws<ArgumentOutOfRangeException, int>("destination", span, s => builder.CopyTo(s));
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

        [Fact]
        public void MoveToImmutableNormal()
        {
            var builder = CreateBuilderWithCount<string>(2);
            Assert.Equal(2, builder.Count);
            Assert.Equal(2, builder.Capacity);
            builder[1] = "b";
            builder[0] = "a";
            var array = builder.MoveToImmutable();
            Assert.Equal(new[] { "a", "b" }, array);
            Assert.Equal(0, builder.Count);
            Assert.Equal(0, builder.Capacity);
        }

        [Fact]
        public void MoveToImmutableRepeat()
        {
            var builder = CreateBuilderWithCount<string>(2);
            builder[0] = "a";
            builder[1] = "b";
            var array1 = builder.MoveToImmutable();
            var array2 = builder.MoveToImmutable();
            Assert.Equal(new[] { "a", "b" }, array1);
            Assert.Equal(0, array2.Length);
        }

        [Fact]
        public void MoveToImmutablePartialFill()
        {
            var builder = ImmutableArray.CreateBuilder<int>(4);
            builder.Add(42);
            builder.Add(13);
            Assert.Equal(4, builder.Capacity);
            Assert.Equal(2, builder.Count);
            Assert.Throws<InvalidOperationException>(() => builder.MoveToImmutable());
        }

        [Fact]
        public void MoveToImmutablePartialFillWithCountUpdate()
        {
            var builder = ImmutableArray.CreateBuilder<int>(4);
            builder.Add(42);
            builder.Add(13);
            Assert.Equal(4, builder.Capacity);
            Assert.Equal(2, builder.Count);
            builder.Count = builder.Capacity;
            var array = builder.MoveToImmutable();
            Assert.Equal(new[] { 42, 13, 0, 0 }, array);
        }

        [Fact]
        public void MoveToImmutableThenUse()
        {
            var builder = CreateBuilderWithCount<string>(2);
            Assert.Equal(2, builder.MoveToImmutable().Length);
            Assert.Equal(0, builder.Capacity);
            builder.Add("a");
            builder.Add("b");
            Assert.Equal(2, builder.Count);
            Assert.True(builder.Capacity >= 2);
            Assert.Equal(new[] { "a", "b" }, builder.MoveToImmutable());
        }

        [Fact]
        public void MoveToImmutableAfterClear()
        {
            var builder = CreateBuilderWithCount<string>(2);
            builder[0] = "a";
            builder[1] = "b";
            builder.Clear();
            Assert.Throws<InvalidOperationException>(() => builder.MoveToImmutable());
        }

        [Fact]
        public void MoveToImmutableAddToCapacity()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);
            for (int i = 0; i < builder.Capacity; i++)
            {
                builder.Add(i);
            }

            Assert.Equal(new[] { 0, 1, 2 }, builder.MoveToImmutable());
        }

        [Fact]
        public void MoveToImmutableInsertToCapacity()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);
            for (int i = 0; i < builder.Capacity; i++)
            {
                builder.Insert(i, i);
            }

            Assert.Equal(new[] { 0, 1, 2 }, builder.MoveToImmutable());
        }

        [Fact]
        public void MoveToImmutableAddRangeToCapcity()
        {
            var array = new[] { 1, 2, 3, 4, 5 };
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: array.Length);
            builder.AddRange(array);
            Assert.Equal(array, builder.MoveToImmutable());
        }

        [Fact]
        public void MoveToImmutableAddRemoveAddToCapacity()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 3);
            for (int i = 0; i < builder.Capacity; i++)
            {
                builder.Add(i);
                builder.RemoveAt(i);
                builder.Add(i);
            }

            Assert.Equal(new[] { 0, 1, 2 }, builder.MoveToImmutable());
        }

        [Fact]
        public void CapacitySetToZero()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 10);
            builder.Capacity = 0;
            Assert.Equal(0, builder.Capacity);
            Assert.Equal(new int[] { }, builder.ToArray());
        }

        [Fact]
        public void CapacitySetToLessThanCount()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 10);
            builder.Add(1);
            builder.Add(1);
            Assert.Throws<ArgumentException>(() => builder.Capacity = 1);
        }

        [Fact]
        public void CapacitySetToCount()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 10);
            builder.Add(1);
            builder.Add(2);
            builder.Capacity = builder.Count;
            Assert.Equal(2, builder.Capacity);
            Assert.Equal(new[] { 1, 2 }, builder.ToArray());
        }

        [Fact]
        public void CapacitySetToCapacity()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 10);
            builder.Add(1);
            builder.Add(2);
            builder.Capacity = builder.Capacity;
            Assert.Equal(10, builder.Capacity);
            Assert.Equal(new[] { 1, 2 }, builder.ToArray());
        }

        [Fact]
        public void CapacitySetToBiggerCapacity()
        {
            var builder = ImmutableArray.CreateBuilder<int>(initialCapacity: 10);
            builder.Add(1);
            builder.Add(2);
            builder.Capacity = 20;
            Assert.Equal(20, builder.Capacity);
            Assert.Equal(2, builder.Count);
            Assert.Equal(new[] { 1, 2 }, builder.ToArray());
        }

        [Fact]
        public void Replace()
        {
            var builder = ImmutableArray.Create(1.5, 2.5, 3.5).ToBuilder();

            builder.Replace(1.5, 1.6);

            Assert.Equal(new[] { 1.6, 2.5, 3.5 }, builder);

            var absComparer = new DelegateEqualityComparer<double>(equals: (x, y) => Math.Abs(x) == Math.Abs(y));

            builder.Replace(-3.5, 4.2, absComparer);

            Assert.Equal(new[] { 1.6, 2.5, 4.2 }, builder);
        }

        [Fact]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableArray.CreateBuilder<int>());
            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(4);
            builder.AddRange("One", "Two", "Three", "Four");
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(builder);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            string[] items = itemProperty.GetValue(info.Instance) as string[];
            Assert.Equal(builder, items);
        }

        [Fact]
        public static void TestDebuggerAttributes_Null()
        {
            Type proxyType = DebuggerAttributes.GetProxyType(ImmutableArray.CreateBuilder<string>(4));
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        [Fact]
        public void ItemRef()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.Add(1);
            builder.Add(2);
            builder.Add(3);

            ref readonly var safeRef = ref builder.ItemRef(1);
            ref var unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(2, builder.ItemRef(1));

            unsafeRef = 4;

            Assert.Equal(4, builder.ItemRef(1));
        }

        [Fact]
        public void ItemRef_OutOfBounds()
        {
            var builder = new ImmutableArray<int>.Builder();
            builder.Add(1);
            builder.Add(2);
            builder.Add(3);

            Assert.Throws<IndexOutOfRangeException>(() => builder.ItemRef(5));
        }

        private static ImmutableArray<T>.Builder CreateBuilderWithCount<T>(int count)
        {
            var builder = ImmutableArray.CreateBuilder<T>(count);
            builder.Count = count;
            return builder;
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            var builder = new ImmutableArray<T>.Builder(contents.Length);
            builder.AddRange(contents);
            return builder;
        }
    }
}
