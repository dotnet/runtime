// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Collections.Immutable.Test
{
    public class ImmutableArrayTest : SimpleElementImmutablesTestBase
    {
        private static readonly ImmutableArray<int> emptyDefault;
        private static readonly ImmutableArray<int> empty = ImmutableArray.Create<int>();
        private static readonly ImmutableArray<int> oneElement = ImmutableArray.Create(1);
        private static readonly ImmutableArray<int> manyElements = ImmutableArray.Create(1, 2, 3);
        private static readonly ImmutableArray<GenericParameterHelper> oneElementRefType = ImmutableArray.Create(new GenericParameterHelper(1));
        private static readonly ImmutableArray<string> twoElementRefTypeWithNull = ImmutableArray.Create("1", null);

        [Fact]
        public void CreateEmpty()
        {
            Assert.Equal(ImmutableArray.Create<int>(), ImmutableArray<int>.Empty);
        }

        [Fact]
        public void CreateFromEnumerable()
        {
            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange((IEnumerable<int>)null));

            IEnumerable<int> source = new[] { 1, 2, 3 };
            var array = ImmutableArray.CreateRange(source);
            Assert.Equal(3, array.Length);
        }

        [Fact]
        public void CreateFromEmptyEnumerableReturnsSingleton()
        {
            IEnumerable<int> emptySource1 = new int[0];
            var immutable = ImmutableArray.CreateRange(emptySource1);

            // This equality check returns true if the underlying arrays are the same instance.
            Assert.Equal(empty, immutable);
        }

        [Fact]
        public void CreateRangeFromImmutableArrayWithSelector()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);

            var copy1 = ImmutableArray.CreateRange(array, i => i + 0.5);
            Assert.Equal(new[] { 4.5, 5.5, 6.5, 7.5 }, copy1);

            var copy2 = ImmutableArray.CreateRange(array, i => i + 1);
            Assert.Equal(new[] { 5, 6, 7, 8 }, copy2);

            Assert.Equal(new int[] { }, ImmutableArray.CreateRange(empty, i => i));

            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange(array, (Func<int, int>)null));
        }

        [Fact]
        public void CreateRangeFromImmutableArrayWithSelectorAndArgument()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);

            var copy1 = ImmutableArray.CreateRange(array, (i, j) => i + j, 0.5);
            Assert.Equal(new[] { 4.5, 5.5, 6.5, 7.5 }, copy1);

            var copy2 = ImmutableArray.CreateRange(array, (i, j) => i + j, 1);
            Assert.Equal(new[] { 5, 6, 7, 8 }, copy2);

            var copy3 = ImmutableArray.CreateRange(array, (int i, object j) => i, null);
            Assert.Equal(new[] { 4, 5, 6, 7 }, copy3);

            Assert.Equal(new int[] { }, ImmutableArray.CreateRange(empty, (i, j) => i + j, 0));

            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange(array, (Func<int, int, int>)null, 0));
        }

        [Fact]
        public void CreateRangeSliceFromImmutableArrayWithSelector()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);

            var copy1 = ImmutableArray.CreateRange(array, 0, 0, i => i + 0.5);
            Assert.Equal(new double[] { }, copy1);

            var copy2 = ImmutableArray.CreateRange(array, 0, 0, i => i);
            Assert.Equal(new int[] { }, copy2);

            var copy3 = ImmutableArray.CreateRange(array, 0, 1, i => i * 2);
            Assert.Equal(new int[] { 8 }, copy3);

            var copy4 = ImmutableArray.CreateRange(array, 0, 2, i => i + 1);
            Assert.Equal(new int[] { 5, 6 }, copy4);

            var copy5 = ImmutableArray.CreateRange(array, 0, 4, i => i);
            Assert.Equal(new int[] { 4, 5, 6, 7 }, copy5);

            var copy6 = ImmutableArray.CreateRange(array, 3, 1, i => i);
            Assert.Equal(new int[] { 7 }, copy6);

            var copy7 = ImmutableArray.CreateRange(array, 3, 0, i => i);
            Assert.Equal(new int[] { }, copy7);

            var copy8 = ImmutableArray.CreateRange(array, 4, 0, i => i);
            Assert.Equal(new int[] { }, copy8);

            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange(array, 0, 0, (Func<int, int>)null));
            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange(empty, 0, 0, (Func<int, int>)null));

            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, -1, 1, (Func<int, int>)null));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, -1, 1, i => i));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 0, 5, i => i));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 4, 1, i => i));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 3, 2, i => i));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 1, -1, i => i));
        }

        [Fact]
        public void CreateRangeSliceFromImmutableArrayWithSelectorAndArgument()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);

            var copy1 = ImmutableArray.CreateRange(array, 0, 0, (i, j) => i + j, 0.5);
            Assert.Equal(new double[] { }, copy1);

            var copy2 = ImmutableArray.CreateRange(array, 0, 0, (i, j) => i + j, 0);
            Assert.Equal(new int[] { }, copy2);

            var copy3 = ImmutableArray.CreateRange(array, 0, 1, (i, j) => i * j, 2);
            Assert.Equal(new int[] { 8 }, copy3);

            var copy4 = ImmutableArray.CreateRange(array, 0, 2, (i, j) => i + j, 1);
            Assert.Equal(new int[] { 5, 6 }, copy4);

            var copy5 = ImmutableArray.CreateRange(array, 0, 4, (i, j) => i + j, 0);
            Assert.Equal(new int[] { 4, 5, 6, 7 }, copy5);

            var copy6 = ImmutableArray.CreateRange(array, 3, 1, (i, j) => i + j, 0);
            Assert.Equal(new int[] { 7 }, copy6);

            var copy7 = ImmutableArray.CreateRange(array, 3, 0, (i, j) => i + j, 0);
            Assert.Equal(new int[] { }, copy7);

            var copy8 = ImmutableArray.CreateRange(array, 4, 0, (i, j) => i + j, 0);
            Assert.Equal(new int[] { }, copy8);

            var copy9 = ImmutableArray.CreateRange(array, 0, 1, (int i, object j) => i, null);
            Assert.Equal(new int[] { 4 }, copy9);

            Assert.Equal(new int[] { }, ImmutableArray.CreateRange(empty, 0, 0, (i, j) => i + j, 0));

            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange(array, 0, 0, (Func<int, int, int>)null, 0));
            Assert.Throws<ArgumentNullException>(() => ImmutableArray.CreateRange(empty, 0, 0, (Func<int, int, int>)null, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(empty, -1, 1, (Func<int, int, int>)null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, -1, 1, (i, j) => i + j, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 0, 5, (i, j) => i + j, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 4, 1, (i, j) => i + j, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 3, 2, (i, j) => i + j, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.CreateRange(array, 1, -1, (i, j) => i + j, 0));
        }

        [Fact]
        public void CreateFromSliceOfImmutableArray()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);
            Assert.Equal(new[] { 4, 5 }, ImmutableArray.Create(array, 0, 2));
            Assert.Equal(new[] { 5, 6 }, ImmutableArray.Create(array, 1, 2));
            Assert.Equal(new[] { 6, 7 }, ImmutableArray.Create(array, 2, 2));
            Assert.Equal(new[] { 7 }, ImmutableArray.Create(array, 3, 1));
            Assert.Equal(new int[0], ImmutableArray.Create(array, 4, 0));

            Assert.Equal(new int[] { }, ImmutableArray.Create(empty, 0, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(empty, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, 0, array.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, 1, array.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, array.Length + 1, 0));
        }

        [Fact]
        public void CreateFromSliceOfImmutableArrayOptimizations()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);
            var slice = ImmutableArray.Create(array, 0, array.Length);
            Assert.Equal(array, slice); // array instance actually shared between the two
        }

        [Fact]
        public void CreateFromSliceOfImmutableArrayEmptyReturnsSingleton()
        {
            var array = ImmutableArray.Create(4, 5, 6, 7);
            var slice = ImmutableArray.Create(array, 1, 0);
            Assert.Equal(empty, slice);
        }

        [Fact]
        public void CreateFromSliceOfArray()
        {
            var array = new int[] { 4, 5, 6, 7 };
            Assert.Equal(new[] { 4, 5 }, ImmutableArray.Create(array, 0, 2));
            Assert.Equal(new[] { 5, 6 }, ImmutableArray.Create(array, 1, 2));
            Assert.Equal(new[] { 6, 7 }, ImmutableArray.Create(array, 2, 2));
            Assert.Equal(new[] { 7 }, ImmutableArray.Create(array, 3, 1));
            Assert.Equal(new int[0], ImmutableArray.Create(array, 4, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, 0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, 0, array.Length + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, 1, array.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImmutableArray.Create(array, array.Length + 1, 0));
        }

        [Fact]
        public void CreateFromSliceOfArrayEmptyReturnsSingleton()
        {
            var array = new int[] { 4, 5, 6, 7 };
            var slice = ImmutableArray.Create(array, 1, 0);
            Assert.Equal(empty, slice);
            slice = ImmutableArray.Create(array, array.Length, 0);
            Assert.Equal(empty, slice);
        }

        [Fact]
        public void CreateFromArray()
        {
            var source = new[] { 1, 2, 3 };
            var immutable = ImmutableArray.Create(source);
            Assert.Equal(source, immutable);
        }

        [Fact]
        public void CreateFromNullArray()
        {
            int[] nullArray = null;
            ImmutableArray<int> immutable = ImmutableArray.Create(nullArray);
            Assert.False(immutable.IsDefault);
            Assert.Equal(0, immutable.Length);
        }

        [Fact]
        public void Covariance()
        {
            ImmutableArray<string> derivedImmutable = ImmutableArray.Create("a", "b", "c");
            ImmutableArray<object> baseImmutable = derivedImmutable.As<object>();
            Assert.False(baseImmutable.IsDefault);
            Assert.Equal(derivedImmutable, baseImmutable);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.False(derivedImmutable2.IsDefault);
            Assert.Equal(derivedImmutable, derivedImmutable2);

            // Try a cast that would fail.
            Assert.True(baseImmutable.As<Encoder>().IsDefault);
        }

        [Fact]
        public void DowncastOfDefaultStructs()
        {
            ImmutableArray<string> derivedImmutable = default(ImmutableArray<string>);
            ImmutableArray<object> baseImmutable = derivedImmutable.As<object>();
            Assert.True(baseImmutable.IsDefault);
            Assert.True(derivedImmutable.IsDefault);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.True(derivedImmutable2.IsDefault);
            Assert.True(derivedImmutable == derivedImmutable2);
        }

        /// <summary>
        /// Verifies that using an ordinary Create factory method is smart enough to reuse
        /// an underlying array when possible.
        /// </summary>
        [Fact]
        public void CovarianceImplicit()
        {
            ImmutableArray<string> derivedImmutable = ImmutableArray.Create("a", "b", "c");
            ImmutableArray<object> baseImmutable = ImmutableArray.CreateRange<object>(derivedImmutable);
            Assert.Equal(derivedImmutable, baseImmutable);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.Equal(derivedImmutable, derivedImmutable2);
        }

        [Fact]
        public void CreateByCovariantStaticCast()
        {
            ImmutableArray<string> derivedImmutable = ImmutableArray.Create("a", "b", "c");
            ImmutableArray<object> baseImmutable = ImmutableArray.Create<object, string>(derivedImmutable);
            Assert.Equal(derivedImmutable, baseImmutable);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.Equal(derivedImmutable, derivedImmutable2);
        }

        [Fact]
        public void CreateByCovariantStaticCastDefault()
        {
            ImmutableArray<string> derivedImmutable = default(ImmutableArray<string>);
            ImmutableArray<object> baseImmutable = ImmutableArray.Create<object, string>(derivedImmutable);
            Assert.True(baseImmutable.IsDefault);
            Assert.True(derivedImmutable.IsDefault);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.True(derivedImmutable2.IsDefault);
            Assert.True(derivedImmutable == derivedImmutable2);
        }

        [Fact]
        public void ToImmutableArray()
        {
            IEnumerable<int> source = new[] { 1, 2, 3 };
            ImmutableArray<int> immutable = source.ToImmutableArray();
            Assert.Equal(source, immutable);

            ImmutableArray<int> immutable2 = immutable.ToImmutableArray();
            Assert.Equal(immutable, immutable2); // this will compare array reference equality.
        }

        [Fact]
        public void Count()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.Length);
            Assert.Throws<InvalidOperationException>(() => ((ICollection)emptyDefault).Count);
            Assert.Throws<InvalidOperationException>(() => ((ICollection<int>)emptyDefault).Count);
            Assert.Throws<InvalidOperationException>(() => ((IReadOnlyCollection<int>)emptyDefault).Count);

            Assert.Equal(0, empty.Length);
            Assert.Equal(0, ((ICollection)empty).Count);
            Assert.Equal(0, ((ICollection<int>)empty).Count);
            Assert.Equal(0, ((IReadOnlyCollection<int>)empty).Count);

            Assert.Equal(1, oneElement.Length);
            Assert.Equal(1, ((ICollection)oneElement).Count);
            Assert.Equal(1, ((ICollection<int>)oneElement).Count);
            Assert.Equal(1, ((IReadOnlyCollection<int>)oneElement).Count);
        }

        [Fact]
        public void IsEmpty()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.IsEmpty);
            Assert.True(empty.IsEmpty);
            Assert.False(oneElement.IsEmpty);
        }

        [Fact]
        public void IndexOfDefault()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.IndexOf(5));
            Assert.Throws<NullReferenceException>(() => emptyDefault.IndexOf(5, 0));
            Assert.Throws<NullReferenceException>(() => emptyDefault.IndexOf(5, 0, 0));
        }

        [Fact]
        public void LastIndexOfDefault()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.LastIndexOf(5));
            Assert.Throws<NullReferenceException>(() => emptyDefault.LastIndexOf(5, 0));
            Assert.Throws<NullReferenceException>(() => emptyDefault.LastIndexOf(5, 0, 0));
        }

        [Fact]
        public void IndexOf()
        {
            IndexOfTests.IndexOfTest(
                seq => ImmutableArray.CreateRange(seq),
                (b, v) => b.IndexOf(v),
                (b, v, i) => b.IndexOf(v, i),
                (b, v, i, c) => b.IndexOf(v, i, c),
                (b, v, i, c, eq) => b.IndexOf(v, i, c, eq));
        }

        [Fact]
        public void LastIndexOf()
        {
            IndexOfTests.LastIndexOfTest(
                seq => ImmutableArray.CreateRange(seq),
                (b, v) => b.LastIndexOf(v),
                (b, v, eq) => b.LastIndexOf(v, eq), 
                (b, v, i) => b.LastIndexOf(v, i),
                (b, v, i, c) => b.LastIndexOf(v, i, c),
                (b, v, i, c, eq) => b.LastIndexOf(v, i, c, eq));
        }

        [Fact]
        public void Contains()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.Contains(0));
            Assert.False(empty.Contains(0));
            Assert.True(oneElement.Contains(1));
            Assert.False(oneElement.Contains(2));
            Assert.True(manyElements.Contains(3));
            Assert.False(oneElementRefType.Contains(null));
            Assert.True(twoElementRefTypeWithNull.Contains(null));
        }

        [Fact]
        public void ContainsEqualityComparer()
        {
            var array = ImmutableArray.Create("a", "B");
            Assert.False(array.Contains("A", StringComparer.Ordinal));
            Assert.True(array.Contains("A", StringComparer.OrdinalIgnoreCase));
            Assert.False(array.Contains("b", StringComparer.Ordinal));
            Assert.True(array.Contains("b", StringComparer.OrdinalIgnoreCase));
        }

        [Fact]
        public void Enumerator()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.GetEnumerator());

            ImmutableArray<int>.Enumerator enumerator = default(ImmutableArray<int>.Enumerator);
            Assert.Throws<NullReferenceException>(() => enumerator.Current);
            Assert.Throws<NullReferenceException>(() => enumerator.MoveNext());

            enumerator = empty.GetEnumerator();
            Assert.Throws<IndexOutOfRangeException>(() => enumerator.Current);
            Assert.False(enumerator.MoveNext());

            enumerator = manyElements.GetEnumerator();
            Assert.Throws<IndexOutOfRangeException>(() => enumerator.Current);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(3, enumerator.Current);

            Assert.False(enumerator.MoveNext());
            Assert.Throws<IndexOutOfRangeException>(() => enumerator.Current);
        }

        [Fact]
        public void ObjectEnumerator()
        {
            Assert.Throws<InvalidOperationException>(() => ((IEnumerable<int>)emptyDefault).GetEnumerator());

            IEnumerator<int> enumerator = ((IEnumerable<int>)empty).GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.False(enumerator.MoveNext());

            enumerator = ((IEnumerable<int>)manyElements).GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(1, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(2, enumerator.Current);
            Assert.True(enumerator.MoveNext());
            Assert.Equal(3, enumerator.Current);

            Assert.False(enumerator.MoveNext());
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        [Fact]
        public void EnumeratorWithNullValues()
        {
            var enumerationResult = System.Linq.Enumerable.ToArray(twoElementRefTypeWithNull);
            Assert.Equal("1", enumerationResult[0]);
            Assert.Null(enumerationResult[1]);
        }

        [Fact]
        public void EqualityCheckComparesInternalArrayByReference()
        {
            var immutable1 = ImmutableArray.Create(1);
            var immutable2 = ImmutableArray.Create(1);
            Assert.NotEqual(immutable1, immutable2);

            Assert.True(immutable1.Equals(immutable1));
            Assert.True(immutable1.Equals((object)immutable1));
        }

        [Fact]
        public void EqualsObjectNull()
        {
            Assert.False(empty.Equals((object)null));
        }

        [Fact]
        public void OperatorsAndEquality()
        {
            Assert.True(empty.Equals(empty));
            var emptySame = empty;
            Assert.True(empty == emptySame);
            Assert.False(empty != emptySame);

            // empty and default should not be seen as equal
            Assert.False(empty.Equals(emptyDefault));
            Assert.False(empty == emptyDefault);
            Assert.True(empty != emptyDefault);
            Assert.False(emptyDefault == empty);
            Assert.True(emptyDefault != empty);

            Assert.False(empty.Equals(oneElement));
            Assert.False(empty == oneElement);
            Assert.True(empty != oneElement);
            Assert.False(oneElement == empty);
            Assert.True(oneElement != empty);
        }

        [Fact]
        public void NullableOperators()
        {
            ImmutableArray<int>? nullArray = null;
            ImmutableArray<int>? nonNullDefault = emptyDefault;
            ImmutableArray<int>? nonNullEmpty = empty;

            Assert.True(nullArray == nonNullDefault);
            Assert.False(nullArray != nonNullDefault);
            Assert.True(nonNullDefault == nullArray);
            Assert.False(nonNullDefault != nullArray);

            Assert.False(nullArray == nonNullEmpty);
            Assert.True(nullArray != nonNullEmpty);
            Assert.False(nonNullEmpty == nullArray);
            Assert.True(nonNullEmpty != nullArray);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            Assert.Equal(0, emptyDefault.GetHashCode());
            Assert.NotEqual(0, empty.GetHashCode());
            Assert.NotEqual(0, oneElement.GetHashCode());
        }

        [Fact]
        public void Add()
        {
            var source = new[] { 1, 2 };
            var array1 = ImmutableArray.Create(source);
            var array2 = array1.Add(3);
            Assert.Equal(source, array1);
            Assert.Equal(new[] { 1, 2, 3 }, array2);
            Assert.Equal(new[] { 1 }, empty.Add(1));
        }

        [Fact]
        public void AddRange()
        {
            var nothingToEmpty = empty.AddRange(Enumerable.Empty<int>());
            Assert.False(nothingToEmpty.IsDefault);
            Assert.True(nothingToEmpty.IsEmpty);

            Assert.Equal(new[] { 1, 2 }, empty.AddRange(Enumerable.Range(1, 2)));
            Assert.Equal(new[] { 1, 2 }, empty.AddRange(new[] { 1, 2 }));

            Assert.Equal(new[] { 1, 2, 3, 4 }, manyElements.AddRange(new[] { 4 }));
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, manyElements.AddRange(new[] { 4, 5 }));
        }

        [Fact]
        public void AddRangeDefaultEnumerable()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(Enumerable.Empty<int>()));
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(Enumerable.Range(1, 2)));
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(new[] { 1, 2 }));
        }

        [Fact]
        public void AddRangeDefaultStruct()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(empty));
            Assert.Throws<NullReferenceException>(() => empty.AddRange(emptyDefault));
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(oneElement));
            Assert.Throws<NullReferenceException>(() => oneElement.AddRange(emptyDefault));

            IEnumerable<int> emptyBoxed = empty;
            IEnumerable<int> emptyDefaultBoxed = emptyDefault;
            IEnumerable<int> oneElementBoxed = oneElement;
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(emptyBoxed));
            Assert.Throws<InvalidOperationException>(() => empty.AddRange(emptyDefaultBoxed));
            Assert.Throws<NullReferenceException>(() => emptyDefault.AddRange(oneElementBoxed));
            Assert.Throws<InvalidOperationException>(() => oneElement.AddRange(emptyDefaultBoxed));
        }

        [Fact]
        public void AddRangeNoOpIdentity()
        {
            Assert.Equal(empty, empty.AddRange(empty));
            Assert.Equal(oneElement, empty.AddRange(oneElement)); // struct overload
            Assert.Equal(oneElement, empty.AddRange((IEnumerable<int>)oneElement)); // enumerable overload
            Assert.Equal(oneElement, oneElement.AddRange(empty));
        }

        [Fact]
        public void Insert()
        {
            var array1 = ImmutableArray.Create<char>();
            Assert.Throws<ArgumentOutOfRangeException>(() => array1.Insert(-1, 'a'));
            Assert.Throws<ArgumentOutOfRangeException>(() => array1.Insert(1, 'a'));

            var insertFirst = array1.Insert(0, 'c');
            Assert.Equal(new[] { 'c' }, insertFirst);

            var insertLeft = insertFirst.Insert(0, 'a');
            Assert.Equal(new[] { 'a', 'c' }, insertLeft);

            var insertRight = insertFirst.Insert(1, 'e');
            Assert.Equal(new[] { 'c', 'e' }, insertRight);

            var insertBetween = insertLeft.Insert(1, 'b');
            Assert.Equal(new[] { 'a', 'b', 'c' }, insertBetween);
        }

        [Fact]
        public void InsertDefault()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.Insert(-1, 10));
            Assert.Throws<NullReferenceException>(() => emptyDefault.Insert(1, 10));
            Assert.Throws<NullReferenceException>(() => emptyDefault.Insert(0, 10));
        }

        [Fact]
        public void InsertRangeNoOpIdentity()
        {
            Assert.Equal(empty, empty.InsertRange(0, empty));
            Assert.Equal(oneElement, empty.InsertRange(0, oneElement)); // struct overload
            Assert.Equal(oneElement, empty.InsertRange(0, (IEnumerable<int>)oneElement)); // enumerable overload
            Assert.Equal(oneElement, oneElement.InsertRange(0, empty));
        }

        [Fact]
        public void InsertRangeEmpty()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.Insert(-1, 10));
            Assert.Throws<NullReferenceException>(() => emptyDefault.Insert(1, 10));
            Assert.Equal(new int[0], empty.InsertRange(0, Enumerable.Empty<int>()));
            Assert.Equal(empty, empty.InsertRange(0, Enumerable.Empty<int>()));
            Assert.Equal(new[] { 1 }, empty.InsertRange(0, new[] { 1 }));
            Assert.Equal(new[] { 2, 3, 4 }, empty.InsertRange(0, new[] { 2, 3, 4 }));
            Assert.Equal(new[] { 2, 3, 4 }, empty.InsertRange(0, Enumerable.Range(2, 3)));
            Assert.Equal(manyElements, manyElements.InsertRange(0, Enumerable.Empty<int>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.InsertRange(1, oneElement));
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.InsertRange(-1, oneElement));
        }

        [Fact]
        public void InsertRangeDefault()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(1, Enumerable.Empty<int>()));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(-1, Enumerable.Empty<int>()));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, Enumerable.Empty<int>()));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, new[] { 1 }));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, new[] { 2, 3, 4 }));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, Enumerable.Range(2, 3)));
        }

        /// <summary>
        /// Validates that a fixed bug in the inappropriate adding of the
        /// Empty singleton enumerator to the reusable instances bag does not regress.
        /// </summary>
        [Fact]
        public void EmptyEnumeratorReuseRegressionTest()
        {
            IEnumerable<int> oneElementBoxed = oneElement;
            IEnumerable<int> emptyBoxed = empty;
            IEnumerable<int> emptyDefaultBoxed = emptyDefault;

            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(emptyBoxed));
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(emptyDefaultBoxed));
            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(emptyDefaultBoxed));
            Assert.Equal(oneElementBoxed, oneElementBoxed);
        }

        [Fact]
        public void InsertRangeDefaultStruct()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, empty));
            Assert.Throws<NullReferenceException>(() => empty.InsertRange(0, emptyDefault));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, oneElement));
            Assert.Throws<NullReferenceException>(() => oneElement.InsertRange(0, emptyDefault));

            IEnumerable<int> emptyBoxed = empty;
            IEnumerable<int> emptyDefaultBoxed = emptyDefault;
            IEnumerable<int> oneElementBoxed = oneElement;
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, emptyBoxed));
            Assert.Throws<InvalidOperationException>(() => empty.InsertRange(0, emptyDefaultBoxed));
            Assert.Throws<NullReferenceException>(() => emptyDefault.InsertRange(0, oneElementBoxed));
            Assert.Throws<InvalidOperationException>(() => oneElement.InsertRange(0, emptyDefaultBoxed));
        }

        [Fact]
        public void InsertRangeLeft()
        {
            Assert.Equal(new[] { 7, 1, 2, 3 }, manyElements.InsertRange(0, new[] { 7 }));
            Assert.Equal(new[] { 7, 8, 1, 2, 3 }, manyElements.InsertRange(0, new[] { 7, 8 }));
        }

        [Fact]
        public void InsertRangeMid()
        {
            Assert.Equal(new[] { 1, 7, 2, 3 }, manyElements.InsertRange(1, new[] { 7 }));
            Assert.Equal(new[] { 1, 7, 8, 2, 3 }, manyElements.InsertRange(1, new[] { 7, 8 }));
        }

        [Fact]
        public void InsertRangeRight()
        {
            Assert.Equal(new[] { 1, 2, 3, 7 }, manyElements.InsertRange(3, new[] { 7 }));
            Assert.Equal(new[] { 1, 2, 3, 7, 8 }, manyElements.InsertRange(3, new[] { 7, 8 }));
        }

        [Fact]
        public void RemoveAt()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.RemoveAt(0));
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => oneElement.RemoveAt(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.RemoveAt(-1));

            Assert.Equal(new int[0], oneElement.RemoveAt(0));
            Assert.Equal(new[] { 2, 3 }, manyElements.RemoveAt(0));
            Assert.Equal(new[] { 1, 3 }, manyElements.RemoveAt(1));
            Assert.Equal(new[] { 1, 2 }, manyElements.RemoveAt(2));
        }

        [Fact]
        public void Remove()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.Remove(5));
            Assert.False(empty.Remove(5).IsDefault);

            Assert.True(oneElement.Remove(1).IsEmpty);
            Assert.Equal(new[] { 2, 3 }, manyElements.Remove(1));
            Assert.Equal(new[] { 1, 3 }, manyElements.Remove(2));
            Assert.Equal(new[] { 1, 2 }, manyElements.Remove(3));
        }

        [Fact]
        public void RemoveRange()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.RemoveRange(0, 0));
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => oneElement.RemoveRange(1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.RemoveRange(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => oneElement.RemoveRange(0, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => oneElement.RemoveRange(0, -1));

            var fourElements = ImmutableArray.Create(1, 2, 3, 4);
            Assert.Equal(new int[0], oneElement.RemoveRange(0, 1));
            Assert.Equal(oneElement.ToArray(), oneElement.RemoveRange(0, 0));
            Assert.Equal(new[] { 3, 4 }, fourElements.RemoveRange(0, 2));
            Assert.Equal(new[] { 1, 4 }, fourElements.RemoveRange(1, 2));
            Assert.Equal(new[] { 1, 2 }, fourElements.RemoveRange(2, 2));
        }

        [Fact]
        public void RemoveRangeDefaultStruct()
        {
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(empty));
            Assert.Throws<ArgumentNullException>(() => Assert.Equal(empty, empty.RemoveRange(emptyDefault)));
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(oneElement));
            Assert.Throws<ArgumentNullException>(() => Assert.Equal(oneElement, oneElement.RemoveRange(emptyDefault)));

            IEnumerable<int> emptyBoxed = empty;
            IEnumerable<int> emptyDefaultBoxed = emptyDefault;
            IEnumerable<int> oneElementBoxed = oneElement;
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(emptyBoxed));
            Assert.Throws<InvalidOperationException>(() => empty.RemoveRange(emptyDefaultBoxed));
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(oneElementBoxed));
            Assert.Throws<InvalidOperationException>(() => oneElement.RemoveRange(emptyDefaultBoxed));
        }

        [Fact]
        public void RemoveRangeNoOpIdentity()
        {
            Assert.Equal(empty, empty.RemoveRange(empty));
            Assert.Equal(empty, empty.RemoveRange(oneElement)); // struct overload
            Assert.Equal(empty, empty.RemoveRange((IEnumerable<int>)oneElement)); // enumerable overload
            Assert.Equal(oneElement, oneElement.RemoveRange(empty));
        }

        [Fact]
        public void RemoveAll()
        {
            Assert.Throws<ArgumentNullException>(() => oneElement.RemoveAll(null));

            var array = ImmutableArray.CreateRange(Enumerable.Range(1, 10));
            var removedEvens = array.RemoveAll(n => n % 2 == 0);
            var removedOdds = array.RemoveAll(n => n % 2 == 1);
            var removedAll = array.RemoveAll(n => true);
            var removedNone = array.RemoveAll(n => false);

            Assert.Equal(new[] { 1, 3, 5, 7, 9 }, removedEvens);
            Assert.Equal(new[] { 2, 4, 6, 8, 10 }, removedOdds);
            Assert.True(removedAll.IsEmpty);
            Assert.Equal(Enumerable.Range(1, 10), removedNone);

            Assert.False(empty.RemoveAll(n => false).IsDefault);
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveAll(n => false));
        }

        [Fact]
        public void RemoveRangeEnumerableTest()
        {
            var list = ImmutableArray.Create(1, 2, 3);
            Assert.Throws<ArgumentNullException>(() => list.RemoveRange(null));
            Assert.Throws<NullReferenceException>(() => emptyDefault.RemoveRange(new int[0]).IsDefault);
            Assert.False(empty.RemoveRange(new int[0]).IsDefault);

            ImmutableArray<int> removed2 = list.RemoveRange(new[] { 2 });
            Assert.Equal(2, removed2.Length);
            Assert.Equal(new[] { 1, 3 }, removed2);

            ImmutableArray<int> removed13 = list.RemoveRange(new[] { 1, 3, 5 });
            Assert.Equal(1, removed13.Length);
            Assert.Equal(new[] { 2 }, removed13);

            Assert.Equal(new[] { 1, 3, 6, 8, 9 }, ImmutableArray.CreateRange(Enumerable.Range(1, 10)).RemoveRange(new[] { 2, 4, 5, 7, 10 }));
            Assert.Equal(new[] { 3, 6, 8, 9 }, ImmutableArray.CreateRange(Enumerable.Range(1, 10)).RemoveRange(new[] { 1, 2, 4, 5, 7, 10 }));

            Assert.Equal(list, list.RemoveRange(new[] { 5 }));
            Assert.Equal(ImmutableArray.Create<int>(), ImmutableArray.Create<int>().RemoveRange(new[] { 1 }));

            var listWithDuplicates = ImmutableArray.Create(1, 2, 2, 3);
            Assert.Equal(new[] { 1, 2, 3 }, listWithDuplicates.RemoveRange(new[] { 2 }));
            Assert.Equal(new[] { 1, 3 }, listWithDuplicates.RemoveRange(new[] { 2, 2 }));
            Assert.Equal(new[] { 1, 3 }, listWithDuplicates.RemoveRange(new[] { 2, 2, 2 }));
        }

        [Fact]
        public void Replace()
        {
            Assert.Equal(new[] { 5 }, oneElement.Replace(1, 5));

            Assert.Equal(new[] { 6, 2, 3 }, manyElements.Replace(1, 6));
            Assert.Equal(new[] { 1, 6, 3 }, manyElements.Replace(2, 6));
            Assert.Equal(new[] { 1, 2, 6 }, manyElements.Replace(3, 6));

            Assert.Equal(new[] { 1, 2, 3, 4 }, ImmutableArray.Create(1, 3, 3, 4).Replace(3, 2));
        }

        [Fact]
        public void ReplaceMissingThrowsTest()
        {
            Assert.Throws<ArgumentException>(() => empty.Replace(5, 3));
        }

        [Fact]
        public void SetItem()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.SetItem(0, 10));
            Assert.Throws<NullReferenceException>(() => emptyDefault.SetItem(0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => oneElement.SetItem(1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => empty.SetItem(-1, 10));

            Assert.Equal(new[] { 12345 }, oneElement.SetItem(0, 12345));
            Assert.Equal(new[] { 12345, 2, 3 }, manyElements.SetItem(0, 12345));
            Assert.Equal(new[] { 1, 12345, 3 }, manyElements.SetItem(1, 12345));
            Assert.Equal(new[] { 1, 2, 12345 }, manyElements.SetItem(2, 12345));
        }

        [Fact]
        public void CopyToArray()
        {
            {
                var target = new int[manyElements.Length];
                manyElements.CopyTo(target);
                Assert.Equal(target, manyElements);
            }

            {
                var target = new int[0];
                Assert.Throws<NullReferenceException>(() => emptyDefault.CopyTo(target));
            }
        }

        [Fact]
        public void CopyToIntArrayIntInt()
        {
            var source = ImmutableArray.Create(1, 2, 3);
            var target = new int[4];
            source.CopyTo(1, target, 3, 1);
            Assert.Equal(new[] { 0, 0, 0, 2 }, target);
        }

        [Fact]
        public void Concat()
        {
            var array1 = ImmutableArray.Create(1, 2, 3);
            var array2 = ImmutableArray.Create(4, 5, 6);

            var concat = array1.Concat(array2);
            Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, concat);
        }

        /// <summary>
        /// Verifies reuse of the original array when concatenated to an empty array.
        /// </summary>
        [Fact]
        public void ConcatEdgeCases()
        {
            // empty arrays
            Assert.Equal(manyElements, manyElements.Concat(empty));
            Assert.Equal(manyElements, empty.Concat(manyElements));

            // default arrays
            manyElements.Concat(emptyDefault);
            Assert.Throws<InvalidOperationException>(() => manyElements.Concat(emptyDefault).Count());
            Assert.Throws<InvalidOperationException>(() => emptyDefault.Concat(manyElements).Count());
        }

        [Fact]
        public void IsDefault()
        {
            Assert.True(emptyDefault.IsDefault);
            Assert.False(empty.IsDefault);
            Assert.False(oneElement.IsDefault);
        }

        [Fact]
        public void IsDefaultOrEmpty()
        {
            Assert.True(empty.IsDefaultOrEmpty);
            Assert.True(emptyDefault.IsDefaultOrEmpty);
            Assert.False(oneElement.IsDefaultOrEmpty);
        }

        [Fact]
        public void IndexGetter()
        {
            Assert.Equal(1, oneElement[0]);
            Assert.Equal(1, ((IList)oneElement)[0]);
            Assert.Equal(1, ((IList<int>)oneElement)[0]);
            Assert.Equal(1, ((IReadOnlyList<int>)oneElement)[0]);

            Assert.Throws<IndexOutOfRangeException>(() => oneElement[1]);
            Assert.Throws<IndexOutOfRangeException>(() => oneElement[-1]);

            Assert.Throws<NullReferenceException>(() => emptyDefault[0]);
            Assert.Throws<InvalidOperationException>(() => ((IList)emptyDefault)[0]);
            Assert.Throws<InvalidOperationException>(() => ((IList<int>)emptyDefault)[0]);
            Assert.Throws<InvalidOperationException>(() => ((IReadOnlyList<int>)emptyDefault)[0]);
        }

        [Fact]
        public void ExplicitMethods()
        {
            IList<int> c = oneElement;
            Assert.Throws<NotSupportedException>(() => c.Add(3));
            Assert.Throws<NotSupportedException>(() => c.Clear());
            Assert.Throws<NotSupportedException>(() => c.Remove(3));
            Assert.True(c.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => c.Insert(0, 2));
            Assert.Throws<NotSupportedException>(() => c.RemoveAt(0));
            Assert.Equal(oneElement[0], c[0]);
            Assert.Throws<NotSupportedException>(() => c[0] = 8);

            var enumerator = c.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(oneElement[0], enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void Sort()
        {
            var array = ImmutableArray.Create(2, 4, 1, 3);
            Assert.Equal(new[] { 1, 2, 3, 4 }, array.Sort());
            Assert.Equal(new[] { 2, 4, 1, 3 }, array); // original array uneffected.
        }

        [Fact]
        public void SortRange()
        {
            var array = ImmutableArray.Create(2, 4, 1, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => array.Sort(-1, 2, Comparer<int>.Default));
            Assert.Throws<ArgumentOutOfRangeException>(() => array.Sort(1, 4, Comparer<int>.Default));
            Assert.Equal(new int[] { 2, 4, 1, 3 }, array.Sort(array.Length, 0, Comparer<int>.Default));
            Assert.Throws<ArgumentNullException>(() => array.Sort(1, 2, null));
            Assert.Equal(new[] { 2, 1, 4, 3 }, array.Sort(1, 2, Comparer<int>.Default));
        }

        [Fact]
        public void SortComparer()
        {
            var array = ImmutableArray.Create("c", "B", "a");
            Assert.Equal(new[] { "a", "B", "c" }, array.Sort(StringComparer.OrdinalIgnoreCase));
            Assert.Equal(new[] { "B", "a", "c" }, array.Sort(StringComparer.Ordinal));
        }

        [Fact]
        public void SortPreservesArrayWhenAlreadySorted()
        {
            var sortedArray = ImmutableArray.Create(1, 2, 3, 4);
            Assert.Equal(sortedArray, sortedArray.Sort());

            var mostlySorted = ImmutableArray.Create(1, 2, 3, 4, 6, 5, 7, 8, 9, 10);
            Assert.Equal(mostlySorted, mostlySorted.Sort(0, 5, Comparer<int>.Default));
            Assert.Equal(mostlySorted, mostlySorted.Sort(5, 5, Comparer<int>.Default));
            Assert.Equal(Enumerable.Range(1, 10), mostlySorted.Sort(4, 2, Comparer<int>.Default));
        }

        [Fact]
        public void ToBuilder()
        {
            Assert.Equal(0, empty.ToBuilder().Count);
            Assert.Throws<NullReferenceException>(() => emptyDefault.ToBuilder().Count);

            var builder = oneElement.ToBuilder();
            Assert.Equal(oneElement.ToArray(), builder);

            builder = manyElements.ToBuilder();
            Assert.Equal(manyElements.ToArray(), builder);

            // Make sure that changing the builder doesn't change the original immutable array.
            int expected = manyElements[0];
            builder[0] = expected + 1;
            Assert.Equal(expected, manyElements[0]);
            Assert.Equal(expected + 1, builder[0]);
        }

        [Fact]
        public void StructuralEquatableEqualsDefault()
        {
            IStructuralEquatable eq = emptyDefault;

            Assert.True(eq.Equals(emptyDefault, EqualityComparer<int>.Default));
            Assert.False(eq.Equals(empty, EqualityComparer<int>.Default));
            Assert.False(eq.Equals(oneElement, EqualityComparer<int>.Default));
        }

        [Fact]
        public void StructuralEquatableEquals()
        {
            IStructuralEquatable array = new int[3] { 1, 2, 3 };
            IStructuralEquatable immArray = ImmutableArray.Create(1, 2, 3);

            var otherArray = new object[] { 1, 2, 3 };
            var otherImmArray = ImmutableArray.Create(otherArray);
            var unequalArray = new int[] { 1, 2, 4 };
            var unequalImmArray = ImmutableArray.Create(unequalArray);
            var unrelatedArray = new string[3];
            var unrelatedImmArray = ImmutableArray.Create(unrelatedArray);
            var otherList = new List<int> { 1, 2, 3 };
            Assert.Equal(array.Equals(otherArray, EqualityComparer<int>.Default), immArray.Equals(otherImmArray, EqualityComparer<int>.Default));
            Assert.Equal(array.Equals(otherList, EqualityComparer<int>.Default), immArray.Equals(otherList, EqualityComparer<int>.Default));
            Assert.Equal(array.Equals(unrelatedArray, EverythingEqual<object>.Default), immArray.Equals(unrelatedImmArray, EverythingEqual<object>.Default));
            Assert.Equal(array.Equals(new object(), EqualityComparer<int>.Default), immArray.Equals(new object(), EqualityComparer<int>.Default));
            Assert.Equal(array.Equals(null, EqualityComparer<int>.Default), immArray.Equals(null, EqualityComparer<int>.Default));
            Assert.Equal(array.Equals(unequalArray, EqualityComparer<int>.Default), immArray.Equals(unequalImmArray, EqualityComparer<int>.Default));
        }

        [Fact]
        public void StructuralEquatableEqualsArrayInterop()
        {
            IStructuralEquatable array = new int[3] { 1, 2, 3 };
            IStructuralEquatable immArray = ImmutableArray.Create(1, 2, 3);
            var unequalArray = new int[] { 1, 2, 4 };

            Assert.True(immArray.Equals(array, EqualityComparer<int>.Default));
            Assert.False(immArray.Equals(unequalArray, EqualityComparer<int>.Default));
        }

        [Fact]
        public void StructuralEquatableGetHashCodeDefault()
        {
            IStructuralEquatable defaultImmArray = emptyDefault;
            Assert.Equal(0, defaultImmArray.GetHashCode(EqualityComparer<int>.Default));
        }

        [Fact]
        public void StructuralEquatableGetHashCode()
        {
            IStructuralEquatable emptyArray = new int[0];
            IStructuralEquatable emptyImmArray = empty;
            IStructuralEquatable array = new int[3] { 1, 2, 3 };
            IStructuralEquatable immArray = ImmutableArray.Create(1, 2, 3);

            Assert.Equal(emptyArray.GetHashCode(EqualityComparer<int>.Default), emptyImmArray.GetHashCode(EqualityComparer<int>.Default));
            Assert.Equal(array.GetHashCode(EqualityComparer<int>.Default), immArray.GetHashCode(EqualityComparer<int>.Default));
            Assert.Equal(array.GetHashCode(EverythingEqual<int>.Default), immArray.GetHashCode(EverythingEqual<int>.Default));
        }

        [Fact]
        public void StructuralComparableDefault()
        {
            IStructuralComparable def = emptyDefault;
            IStructuralComparable mt = empty;

            // default to default is fine, and should be seen as equal.
            Assert.Equal(0, def.CompareTo(emptyDefault, Comparer<int>.Default));

            // default to empty and vice versa should throw, on the basis that 
            // arrays compared that are of different lengths throw. Empty vs. default aren't really compatible.
            Assert.Throws<ArgumentException>(() => def.CompareTo(empty, Comparer<int>.Default));
            Assert.Throws<ArgumentException>(() => mt.CompareTo(emptyDefault, Comparer<int>.Default));
        }

        [Fact]
        public void StructuralComparable()
        {
            IStructuralComparable array = new int[3] { 1, 2, 3 };
            IStructuralComparable equalArray = new int[3] { 1, 2, 3 };
            IStructuralComparable immArray = ImmutableArray.Create((int[])array);
            IStructuralComparable equalImmArray = ImmutableArray.Create((int[])equalArray);

            IStructuralComparable longerArray = new int[] { 1, 2, 3, 4 };
            IStructuralComparable longerImmArray = ImmutableArray.Create((int[])longerArray);

            Assert.Equal(array.CompareTo(equalArray, Comparer<int>.Default), immArray.CompareTo(equalImmArray, Comparer<int>.Default));

            Assert.Throws<ArgumentException>(() => array.CompareTo(longerArray, Comparer<int>.Default));
            Assert.Throws<ArgumentException>(() => immArray.CompareTo(longerImmArray, Comparer<int>.Default));

            var list = new List<int> { 1, 2, 3 };
            Assert.Throws<ArgumentException>(() => array.CompareTo(list, Comparer<int>.Default));
            Assert.Throws<ArgumentException>(() => immArray.CompareTo(list, Comparer<int>.Default));
        }

        [Fact]
        public void StructuralComparableArrayInterop()
        {
            IStructuralComparable array = new int[3] { 1, 2, 3 };
            IStructuralComparable equalArray = new int[3] { 1, 2, 3 };
            IStructuralComparable immArray = ImmutableArray.Create((int[])array);
            IStructuralComparable equalImmArray = ImmutableArray.Create((int[])equalArray);

            Assert.Equal(array.CompareTo(equalArray, Comparer<int>.Default), immArray.CompareTo(equalArray, Comparer<int>.Default));
        }

        [Fact]
        public void BinarySearch()
        {
            Assert.Throws<ArgumentNullException>(() => Assert.Equal(Array.BinarySearch(new int[0], 5), ImmutableArray.BinarySearch(default(ImmutableArray<int>), 5)));
            Assert.Equal(Array.BinarySearch(new int[0], 5), ImmutableArray.BinarySearch(ImmutableArray.Create<int>(), 5));
            Assert.Equal(Array.BinarySearch(new int[] { 3 }, 5), ImmutableArray.BinarySearch(ImmutableArray.Create(3), 5));
            Assert.Equal(Array.BinarySearch(new int[] { 5 }, 5), ImmutableArray.BinarySearch(ImmutableArray.Create(5), 5));
        }

        [Fact]
        public void OfType()
        {
            Assert.Equal(0, emptyDefault.OfType<int>().Count());
            Assert.Equal(0, empty.OfType<int>().Count());
            Assert.Equal(1, oneElement.OfType<int>().Count());
            Assert.Equal(1, twoElementRefTypeWithNull.OfType<string>().Count());
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            return ImmutableArray.Create(contents);
        }

        /// <summary>
        /// A structure that takes exactly 3 bytes of memory.
        /// </summary>
        private struct ThreeByteStruct : IEquatable<ThreeByteStruct>
        {
            public ThreeByteStruct(byte first, byte second, byte third)
            {
                this.Field1 = first;
                this.Field2 = second;
                this.Field3 = third;
            }

            public byte Field1;
            public byte Field2;
            public byte Field3;

            public bool Equals(ThreeByteStruct other)
            {
                return this.Field1 == other.Field1
                    && this.Field2 == other.Field2
                    && this.Field3 == other.Field3;
            }

            public override bool Equals(object obj)
            {
                if (obj is ThreeByteStruct)
                {
                    return this.Equals((ThreeByteStruct)obj);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return this.Field1;
            }
        }

        /// <summary>
        /// A structure that takes exactly 9 bytes of memory.
        /// </summary>
        private struct NineByteStruct : IEquatable<NineByteStruct>
        {
            public NineByteStruct(int first, int second, int third, int fourth, int fifth, int sixth, int seventh, int eighth, int ninth)
            {
                this.Field1 = (byte)first;
                this.Field2 = (byte)second;
                this.Field3 = (byte)third;
                this.Field4 = (byte)fourth;
                this.Field5 = (byte)fifth;
                this.Field6 = (byte)sixth;
                this.Field7 = (byte)seventh;
                this.Field8 = (byte)eighth;
                this.Field9 = (byte)ninth;
            }

            public byte Field1;
            public byte Field2;
            public byte Field3;
            public byte Field4;
            public byte Field5;
            public byte Field6;
            public byte Field7;
            public byte Field8;
            public byte Field9;

            public bool Equals(NineByteStruct other)
            {
                return this.Field1 == other.Field1
                    && this.Field2 == other.Field2
                    && this.Field3 == other.Field3
                    && this.Field4 == other.Field4
                    && this.Field5 == other.Field5
                    && this.Field6 == other.Field6
                    && this.Field7 == other.Field7
                    && this.Field8 == other.Field8
                    && this.Field9 == other.Field9;
            }

            public override bool Equals(object obj)
            {
                if (obj is NineByteStruct)
                {
                    return this.Equals((NineByteStruct)obj);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return this.Field1;
            }
        }

        /// <summary>
        /// A structure that requires 9 bytes of memory but occupies 12 because of memory alignment.
        /// </summary>
        private struct TwelveByteStruct : IEquatable<TwelveByteStruct>
        {
            public TwelveByteStruct(int first, int second, byte third)
            {
                this.Field1 = first;
                this.Field2 = second;
                this.Field3 = third;
            }

            public int Field1;
            public int Field2;
            public byte Field3;

            public bool Equals(TwelveByteStruct other)
            {
                return this.Field1 == other.Field1
                    && this.Field2 == other.Field2
                    && this.Field3 == other.Field3;
            }

            public override bool Equals(object obj)
            {
                if (obj is TwelveByteStruct)
                {
                    return this.Equals((TwelveByteStruct)obj);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return this.Field1;
            }
        }

        private struct StructWithReferenceTypeField
        {
            public string foo;

            public StructWithReferenceTypeField(string foo)
            {
                this.foo = foo;
            }
        }
    }
}
