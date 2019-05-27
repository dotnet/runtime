// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Collections.Immutable.Tests
{
    public class ImmutableArrayTest : SimpleElementImmutablesTestBase
    {
        private static readonly ImmutableArray<int> s_emptyDefault;
        private static readonly ImmutableArray<int> s_empty = ImmutableArray.Create<int>();
        private static readonly ImmutableArray<int> s_oneElement = ImmutableArray.Create(1);
        private static readonly ImmutableArray<int> s_manyElements = ImmutableArray.Create(1, 2, 3);
        private static readonly ImmutableArray<GenericParameterHelper> s_oneElementRefType = ImmutableArray.Create(new GenericParameterHelper(1));
        private static readonly ImmutableArray<string> s_twoElementRefTypeWithNull = ImmutableArray.Create("1", null);

        public static IEnumerable<object[]> Int32EnumerableData()
        {
            yield return new object[] { new int[0] };
            yield return new object[] { new[] { 1 } };
            yield return new object[] { Enumerable.Range(1, 3) };
            yield return new object[] { Enumerable.Range(4, 4) };
            yield return new object[] { new[] { 2, 3, 5 } };
        }

        public static IEnumerable<object[]> SpecialInt32ImmutableArrayData()
        {
            yield return new object[] { s_emptyDefault };
            yield return new object[] { s_empty };
        }

        public static IEnumerable<object[]> StringImmutableArrayData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new[] { "a" } };
            yield return new object[] { new[] { "a", "b", "c" } };
            yield return new object[] { new[] { string.Empty } };
            yield return new object[] { new[] { (string)null } };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void Clear(IEnumerable<int> source)
        {
            Assert.True(s_empty == source.ToImmutableArray().Clear());
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void AsSpanRoundTripTests(IEnumerable<int> source)
        {
            ImmutableArray<int> immutableArray = source.ToImmutableArray();
            ReadOnlySpan<int> span = immutableArray.AsSpan();
            Assert.Equal(immutableArray, span.ToArray());
            Assert.Equal(immutableArray.Length, span.Length);
        }

        [Fact]
        public void AsSpanRoundTripEmptyArrayTests()
        {
            ImmutableArray<int> immutableArray = ImmutableArray.Create(Array.Empty<int>());
            ReadOnlySpan<int> span = immutableArray.AsSpan();
            Assert.Equal(immutableArray, span.ToArray());
            Assert.Equal(immutableArray.Length, span.Length);
        }

        [Fact]
        public void AsSpanRoundTripDefaultArrayTests()
        {
            ImmutableArray<int> immutableArray = new ImmutableArray<int>();
            ReadOnlySpan<int> span = immutableArray.AsSpan();
            Assert.True(immutableArray.IsDefault);
            Assert.Equal(0, span.Length);
            Assert.True(span.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(StringImmutableArrayData))]
        public void AsSpanRoundTripStringTests(IEnumerable<string> source)
        {
            ImmutableArray<string> immutableArray = source.ToImmutableArray();
            ReadOnlySpan<string> span = immutableArray.AsSpan();
            Assert.Equal(immutableArray, span.ToArray());
            Assert.Equal(immutableArray.Length, span.Length);
        }

        [Fact]
        public void AsSpanRoundTripDefaultArrayStringTests()
        {
            ImmutableArray<string> immutableArray = new ImmutableArray<string>();
            ReadOnlySpan<string> span = immutableArray.AsSpan();
            Assert.True(immutableArray.IsDefault);
            Assert.Equal(0, span.Length);
            Assert.True(span.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void AsMemoryRoundTripTests(IEnumerable<int> source)
        {
            ImmutableArray<int> immutableArray = source.ToImmutableArray();
            ReadOnlyMemory<int> memory = immutableArray.AsMemory();
            Assert.Equal(immutableArray, memory.ToArray());
            Assert.Equal(immutableArray.Length, memory.Length);
        }

        [Fact]
        public void AsMemoryRoundTripEmptyArrayTests()
        {
            ImmutableArray<int> immutableArray = ImmutableArray.Create(Array.Empty<int>());
            ReadOnlyMemory<int> memory = immutableArray.AsMemory();
            Assert.Equal(immutableArray, memory.ToArray());
            Assert.Equal(immutableArray.Length, memory.Length);
        }

        [Fact]
        public void AsMemoryRoundTripDefaultArrayTests()
        {
            ImmutableArray<int> immutableArray = new ImmutableArray<int>();
            ReadOnlyMemory<int> memory = immutableArray.AsMemory();
            Assert.True(immutableArray.IsDefault);
            Assert.Equal(0, memory.Length);
            Assert.True(memory.IsEmpty);
        }

        [Theory]
        [MemberData(nameof(StringImmutableArrayData))]
        public void AsMemoryRoundTripStringTests(IEnumerable<string> source)
        {
            ImmutableArray<string> immutableArray = source.ToImmutableArray();
            ReadOnlyMemory<string> memory = immutableArray.AsMemory();
            Assert.Equal(immutableArray, memory.ToArray());
            Assert.Equal(immutableArray.Length, memory.Length);
        }

        [Fact]
        public void AsMemoryRoundTripDefaultArrayStringTests()
        {
            ImmutableArray<string> immutableArray = new ImmutableArray<string>();
            ReadOnlyMemory<string> memory = immutableArray.AsMemory();
            Assert.True(immutableArray.IsDefault);
            Assert.Equal(0, memory.Length);
            Assert.True(memory.IsEmpty);
        }

        [Fact]
        public void CreateEnumerableElementType()
        {
            // Create should not have the same semantics as CreateRange, except for arrays.
            // If you pass in an IEnumerable<T> to Create, you should get an
            // ImmutableArray<IEnumerable<T>>. However, if you pass a T[] in, you should get
            // a ImmutableArray<T>.

            var array = new int[0];
            Assert.IsType<ImmutableArray<int>>(ImmutableArray.Create(array));

            var immutable = ImmutableArray<int>.Empty;
            Assert.IsType<ImmutableArray<ImmutableArray<int>>>(ImmutableArray.Create(immutable));

            var enumerable = Enumerable.Empty<int>();
            Assert.IsType<ImmutableArray<IEnumerable<int>>>(ImmutableArray.Create(enumerable));
        }

        [Fact]
        public void CreateEmpty()
        {
            Assert.True(s_empty == ImmutableArray.Create<int>());
            Assert.True(s_empty == ImmutableArray.Create(new int[0]));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateRange(IEnumerable<int> source)
        {
            Assert.Equal(source, ImmutableArray.CreateRange(source));
        }

        [Fact]
        public void CreateRangeInvalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("items", () => ImmutableArray.CreateRange((IEnumerable<int>)null));
        }

        [Fact]
        public void CreateRangeEmptyReturnsSingleton()
        {
            var empty = ImmutableArray.CreateRange(new int[0]);
            // This equality check returns true if the underlying arrays are the same instance.
            Assert.True(s_empty == empty);
        }

        [Theory]
        [ActiveIssue("https://github.com/xunit/xunit/issues/1794")]
        [MemberData(nameof(CreateRangeWithSelectorData))]
        public void CreateRangeWithSelector<TResult>(IEnumerable<int> source, Func<int, TResult> selector)
        {
            Assert.Equal(source.Select(selector), ImmutableArray.CreateRange(source.ToImmutableArray(), selector));
        }

        public static IEnumerable<object[]> CreateRangeWithSelectorData()
        {
            yield return new object[] { new int[] { }, new Func<int, int>(i => i) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, new Func<int, float>(i => i + 0.5f) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, new Func<int, int>(i => i + 1) };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateRangeWithSelectorInvalid(IEnumerable<int> source)
        {
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArray.CreateRange(source.ToImmutableArray(), (Func<int, int>)null));
            // If both parameters are invalid, the selector should be validated first.
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArray.CreateRange(s_emptyDefault, (Func<int, int>)null));
            Assert.Throws<NullReferenceException>(() => ImmutableArray.CreateRange(s_emptyDefault, i => i));
        }

        [Theory]
        [ActiveIssue("https://github.com/xunit/xunit/issues/1794")]
        [MemberData(nameof(CreateRangeWithSelectorAndArgumentData))]
        public void CreateRangeWithSelectorAndArgument<TArg, TResult>(IEnumerable<int> source, Func<int, TArg, TResult> selector, TArg arg)
        {
            var expected = source.Zip(Enumerable.Repeat(arg, source.Count()), selector);
            Assert.Equal(expected, ImmutableArray.CreateRange(source.ToImmutableArray(), selector, arg));
        }

        public static IEnumerable<object[]> CreateRangeWithSelectorAndArgumentData()
        {
            yield return new object[] { new int[] { }, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, new Func<int, float, float>((x, y) => x + y), 0.5f };
            yield return new object[] { new[] { 4, 5, 6, 7 }, new Func<int, int, int>((x, y) => x + y), 1 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, new Func<int, object, int>((x, y) => x), null };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateRangeWithSelectorAndArgumentInvalid(IEnumerable<int> source)
        {
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArray.CreateRange(source.ToImmutableArray(), (Func<int, int, int>)null, 0));
            // If both parameters are invalid, the selector should be validated first.
            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArray.CreateRange(s_emptyDefault, (Func<int, int, int>)null, 0));
            Assert.Throws<NullReferenceException>(() => ImmutableArray.CreateRange(s_emptyDefault, (x, y) => 0, 0));
        }

        [Theory]
        [ActiveIssue("https://github.com/xunit/xunit/issues/1794")]
        [MemberData(nameof(CreateRangeSliceWithSelectorData))]
        public void CreateRangeSliceWithSelector<TResult>(IEnumerable<int> source, int start, int length, Func<int, TResult> selector)
        {
            var expected = source.Skip(start).Take(length).Select(selector);
            Assert.Equal(expected, ImmutableArray.CreateRange(source.ToImmutableArray(), start, length, selector));
        }

        public static IEnumerable<object[]> CreateRangeSliceWithSelectorData()
        {
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 0, new Func<int, float>(i => i + 0.5f) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 0, new Func<int, double>(i => i + 0.5d) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 0, new Func<int, int>(i => i) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 1, new Func<int, int>(i => i * 2) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 2, new Func<int, int>(i => i + 1) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 4, new Func<int, int>(i => i) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 3, 1, new Func<int, int>(i => i) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 3, 0, new Func<int, int>(i => i) };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 4, 0, new Func<int, int>(i => i) };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateRangeSliceWithSelectorInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArray.CreateRange(array, 0, 0, (Func<int, int>)null));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.CreateRange(array, -1, 1, (Func<int, int>)null));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.CreateRange(array, -1, 1, i => i));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, 0, array.Length + 1, i => i));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, array.Length, 1, i => i));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, Math.Max(0, array.Length - 1), 2, i => i));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, 0, -1, i => i));

            Assert.Throws<NullReferenceException>(() => ImmutableArray.CreateRange(s_emptyDefault, 0, 0, i => i));
        }

        [Theory]
        [ActiveIssue("https://github.com/xunit/xunit/issues/1794")]
        [MemberData(nameof(CreateRangeSliceWithSelectorAndArgumentData))]
        public void CreateRangeSliceWithSelectorAndArgument<TArg, TResult>(IEnumerable<int> source, int start, int length, Func<int, TArg, TResult> selector, TArg arg)
        {
            var expected = source.Skip(start).Take(length).Zip(Enumerable.Repeat(arg, length), selector);
            Assert.Equal(expected, ImmutableArray.CreateRange(source.ToImmutableArray(), start, length, selector, arg));
        }

        public static IEnumerable<object[]> CreateRangeSliceWithSelectorAndArgumentData()
        {
            yield return new object[] { new int[] { }, 0, 0, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 0, new Func<int, float, float>((x, y) => x + y), 0.5f };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 0, new Func<int, double, double>((x, y) => x + y), 0.5d };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 0, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 1, new Func<int, int, int>((x, y) => x * y), 2 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 2, new Func<int, int, int>((x, y) => x + y), 1 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 4, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 3, 1, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 3, 0, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 4, 0, new Func<int, int, int>((x, y) => x + y), 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 1, new Func<int, object, int>((x, y) => x), null };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateRangeSliceWithSelectorAndArgumentInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentNullException>("selector", () => ImmutableArray.CreateRange(array, 0, 0, (Func<int, int, int>)null, 0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.CreateRange(s_empty, -1, 1, (Func<int, int, int>)null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.CreateRange(array, -1, 1, (i, j) => i + j, 0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, 0, array.Length + 1, (i, j) => i + j, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, array.Length, 1, (i, j) => i + j, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, Math.Max(0, array.Length - 1), 2, (i, j) => i + j, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.CreateRange(array, 0, -1, (i, j) => i + j, 0));

            Assert.Throws<NullReferenceException>(() => ImmutableArray.CreateRange(s_emptyDefault, 0, 0, (x, y) => 0, 0));
        }

        [Theory]
        [MemberData(nameof(CreateFromSliceData))]
        public void CreateFromSlice(IEnumerable<int> source, int start, int length)
        {
            Assert.Equal(source.Skip(start).Take(length), ImmutableArray.Create(source.ToImmutableArray(), start, length));
            Assert.Equal(source.Skip(start).Take(length), ImmutableArray.Create(source.ToArray(), start, length));
        }

        public static IEnumerable<object[]> CreateFromSliceData()
        {
            yield return new object[] { new int[] { }, 0, 0 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 0, 2 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 1, 2 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 2, 2 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 3, 1 };
            yield return new object[] { new[] { 4, 5, 6, 7 }, 4, 0 };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateFromSliceOfImmutableArrayInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.Create(array, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.Create(array, array.Length + 1, 0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, 0, array.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, Math.Max(0, array.Length - 1), 2));

            if (array.Length > 0)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, 1, array.Length));
            }
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateFromSliceOfImmutableArrayOptimizations(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            var slice = ImmutableArray.Create(array, 0, array.Length);
            Assert.True(array == slice); // Verify that the underlying arrays are reference-equal.
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateFromSliceOfImmutableArrayEmptyReturnsSingleton(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            var slice = ImmutableArray.Create(array, Math.Min(1, array.Length), 0);
            Assert.True(s_empty == slice);
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateFromSliceOfArrayInvalid(IEnumerable<int> source)
        {
            var array = source.ToArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.Create(array, -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("start", () => ImmutableArray.Create(array, array.Length + 1, 0));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, 0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, 0, array.Length + 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, Math.Max(0, array.Length - 1), 2));

            if (array.Length > 0)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => ImmutableArray.Create(array, 1, array.Length));
            }
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateFromSliceOfArrayEmptyReturnsSingleton(IEnumerable<int> source)
        {
            var array = source.ToArray();
            var slice = ImmutableArray.Create(array, Math.Min(1, array.Length), 0);
            Assert.True(s_empty == slice);
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CreateFromArray(IEnumerable<int> source)
        {
            Assert.Equal(source, ImmutableArray.Create(source.ToArray()));
        }

        [Fact]
        public void CreateFromArrayNull()
        {
            var immutable = ImmutableArray.Create(default(int[]));
            Assert.False(immutable.IsDefault);
            Assert.True(immutable.IsEmpty);
        }

        [Fact]
        public void Covariance()
        {
            ImmutableArray<string> derivedImmutable = ImmutableArray.Create("a", "b", "c");
            ImmutableArray<object> baseImmutable = derivedImmutable.As<object>();
            Assert.False(baseImmutable.IsDefault);
            // Must cast to object or the IEnumerable<object> overload of Assert.Equal would be used
            Assert.Equal((object)derivedImmutable, baseImmutable, EqualityComparer<object>.Default);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.False(derivedImmutable2.IsDefault);
            Assert.True(derivedImmutable == derivedImmutable2);

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

        [Fact]
        public void CovarianceImplicit()
        {
            // Verify that CreateRange is smart enough to reuse the underlying array when possible.

            ImmutableArray<string> derivedImmutable = ImmutableArray.Create("a", "b", "c");
            ImmutableArray<object> baseImmutable = ImmutableArray.CreateRange<object>(derivedImmutable);
            // Must cast to object or the IEnumerable<object> overload of Equals would be used
            Assert.Equal((object)derivedImmutable, baseImmutable, EqualityComparer<object>.Default);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.True(derivedImmutable == derivedImmutable2);
        }

        [Fact]
        public void CastUpReference()
        {
            ImmutableArray<string> derivedImmutable = ImmutableArray.Create("a", "b", "c");
            ImmutableArray<object> baseImmutable = ImmutableArray<object>.CastUp(derivedImmutable);
            // Must cast to object or the IEnumerable<object> overload of Equals would be used
            Assert.Equal((object)derivedImmutable, baseImmutable, EqualityComparer<object>.Default);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            Assert.True(derivedImmutable == baseImmutable.As<string>());
            Assert.True(derivedImmutable == baseImmutable.CastArray<string>());
        }

        [Fact]
        public void CastUpReferenceDefaultValue()
        {
            ImmutableArray<string> derivedImmutable = default(ImmutableArray<string>);
            ImmutableArray<object> baseImmutable = ImmutableArray<object>.CastUp(derivedImmutable);
            Assert.True(baseImmutable.IsDefault);
            Assert.True(derivedImmutable.IsDefault);

            // Make sure we can reverse that, as a means to verify the underlying array is the same instance.
            ImmutableArray<string> derivedImmutable2 = baseImmutable.As<string>();
            Assert.True(derivedImmutable2.IsDefault);
            Assert.True(derivedImmutable == derivedImmutable2);
        }

        [Fact]
        public void CastUpReferenceToInterface()
        {
            var stringArray = ImmutableArray.Create("a", "b");
            var enumArray = ImmutableArray<IEnumerable>.CastUp(stringArray);
            Assert.Equal(2, enumArray.Length);
            Assert.True(stringArray == enumArray.CastArray<string>());
            Assert.True(stringArray == enumArray.As<string>());
        }

        [Fact]
        public void CastUpInterfaceToInterface()
        {
            var genericEnumArray = ImmutableArray.Create<IEnumerable<int>>(new List<int>(), new List<int>());
            var legacyEnumArray = ImmutableArray<IEnumerable>.CastUp(genericEnumArray);
            Assert.Equal(2, legacyEnumArray.Length);
            Assert.True(genericEnumArray == legacyEnumArray.As<IEnumerable<int>>());
            Assert.True(genericEnumArray == legacyEnumArray.CastArray<IEnumerable<int>>());
        }

        [Fact]
        public void CastUpArrayToSystemArray()
        {
            var arrayArray = ImmutableArray.Create(new int[] { 1, 2 }, new int[] { 3, 4 });
            var sysArray = ImmutableArray<Array>.CastUp(arrayArray);
            Assert.Equal(2, sysArray.Length);
            Assert.True(arrayArray == sysArray.As<int[]>());
            Assert.True(arrayArray == sysArray.CastArray<int[]>());
        }

        [Fact]
        public void CastUpArrayToObject()
        {
            var arrayArray = ImmutableArray.Create(new int[] { 1, 2 }, new int[] { 3, 4 });
            var objectArray = ImmutableArray<object>.CastUp(arrayArray);
            Assert.Equal(2, objectArray.Length);
            Assert.True(arrayArray == objectArray.As<int[]>());
            Assert.True(arrayArray == objectArray.CastArray<int[]>());
        }

        [Fact]
        public void CastUpDelegateToSystemDelegate()
        {
            var delArray = ImmutableArray.Create<Action>(() => { }, () => { });
            var sysDelArray = ImmutableArray<Delegate>.CastUp(delArray);
            Assert.Equal(2, sysDelArray.Length);
            Assert.True(delArray == sysDelArray.As<Action>());
            Assert.True(delArray == sysDelArray.CastArray<Action>());
        }

        [Fact]
        public void CastArrayUnrelatedInterface()
        {
            var stringArray = ImmutableArray.Create("cat", "dog");
            var comparableArray = ImmutableArray<IComparable>.CastUp(stringArray);
            var enumArray = comparableArray.CastArray<IEnumerable>();
            Assert.Equal(2, enumArray.Length);
            Assert.True(stringArray == enumArray.As<string>());
            Assert.True(stringArray == enumArray.CastArray<string>());
        }

        [Fact]
        public void CastArrayBadInterface()
        {
            var formattableArray = ImmutableArray.Create<IFormattable>(1, 2);
            Assert.Throws<InvalidCastException>(() => formattableArray.CastArray<IComparable>());
        }

        [Fact]
        public void CastArrayBadReference()
        {
            var objectArray = ImmutableArray.Create<object>("cat", "dog");
            Assert.Throws<InvalidCastException>(() => objectArray.CastArray<string>());
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void ToImmutableArray(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            Assert.Equal(source, array);
            Assert.True(array == array.ToImmutableArray());
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void Count(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            Assert.Equal(source.Count(), array.Length);
            Assert.Equal(source.Count(), ((ICollection)array).Count);
            Assert.Equal(source.Count(), ((ICollection<int>)array).Count);
            Assert.Equal(source.Count(), ((IReadOnlyCollection<int>)array).Count);
        }

        [Fact]
        public void CountInvalid()
        {
            Assert.Throws<NullReferenceException>(() => s_emptyDefault.Length);
            Assert.Throws<InvalidOperationException>(() => ((ICollection)s_emptyDefault).Count);
            Assert.Throws<InvalidOperationException>(() => ((ICollection<int>)s_emptyDefault).Count);
            Assert.Throws<InvalidOperationException>(() => ((IReadOnlyCollection<int>)s_emptyDefault).Count);
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void IsEmpty(IEnumerable<int> source)
        {
            Assert.Equal(!source.Any(), source.ToImmutableArray().IsEmpty);
        }

        [Fact]
        public void IsEmptyInvalid()
        {
            Assert.Throws<NullReferenceException>(() => s_emptyDefault.IsEmpty);
        }

        [Fact]
        public void IndexOfInvalid()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.IndexOf(5));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.IndexOf(5, 0));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.IndexOf(5, 0, 0));
        }

        [Fact]
        public void LastIndexOfInvalid()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.LastIndexOf(5));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.LastIndexOf(5, 0));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.LastIndexOf(5, 0, 0));
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

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void ContainsInt32(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            if (source.Any(i => i >= 0))
            {
                int contained = Enumerable.Range(0, int.MaxValue).First(i => source.Contains(i));
                Assert.True(array.Contains(contained));
                Assert.True(((ICollection<int>)array).Contains(contained));
            }

            int notContained = Enumerable.Range(0, int.MaxValue).First(i => !source.Contains(i));
            Assert.False(array.Contains(notContained));
            Assert.False(((ICollection<int>)array).Contains(notContained));
        }

        [Theory]
        [MemberData(nameof(ContainsNullData))]
        public void ContainsNull<T>(IEnumerable<T> source) where T : class
        {
            bool expected = source.Contains(null, EqualityComparer<T>.Default);
            Assert.Equal(expected, source.ToImmutableArray().Contains(null));
        }

        public static IEnumerable<object[]> ContainsNullData()
        {
            yield return new object[] { s_oneElementRefType };
            yield return new object[] { s_twoElementRefTypeWithNull };
            yield return new object[] { new[] { new object() } };
        }

        [Fact]
        public void ContainsInvalid()
        {
            Assert.Throws<NullReferenceException>(() => s_emptyDefault.Contains(0));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void GetEnumerator(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            var enumeratorStruct = array.GetEnumerator();

            Assert.IsType<ImmutableArray<int>.Enumerator>(enumeratorStruct);
            AssertNotAssignableFrom<IDisposable>(enumeratorStruct);
            AssertNotAssignableFrom<IEnumerator>(enumeratorStruct);
            AssertNotAssignableFrom<IEnumerator<int>>(enumeratorStruct);

            var set = new HashSet<IEnumerator>();

            set.Add(((IEnumerable<int>)array).GetEnumerator());
            set.Add(((IEnumerable<int>)array).GetEnumerator());

            set.Add(((IEnumerable)array).GetEnumerator());
            set.Add(((IEnumerable)array).GetEnumerator());

            int expected = array.IsEmpty ? 1 : 4; // Empty ImmutableArrays should cache their enumerators.
            Assert.Equal(expected, set.Count);
            Assert.DoesNotContain(null, set);

            Assert.All(set, enumerator =>
            {
                Assert.NotEqual(enumeratorStruct.GetType(), enumerator.GetType());
                Assert.Equal(set.First().GetType(), enumerator.GetType());
            });
        }

        private static void AssertNotAssignableFrom<T>(object obj)
        {
            var typeInfo = obj.GetType().GetTypeInfo();
            Assert.False(typeof(T).GetTypeInfo().IsAssignableFrom(typeInfo));
        }

        [Fact]
        public void GetEnumeratorObjectEmptyReturnsSingleton()
        {
            var empty = (IEnumerable<int>)s_empty;
            Assert.Same(empty.GetEnumerator(), empty.GetEnumerator());
        }

        [Fact]
        public void GetEnumeratorInvalid()
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.GetEnumerator());
            Assert.Throws<InvalidOperationException>(() => ((IEnumerable)s_emptyDefault).GetEnumerator());
            Assert.Throws<InvalidOperationException>(() => ((IEnumerable<int>)s_emptyDefault).GetEnumerator());
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void EnumeratorTraversal(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            var enumeratorStruct = array.GetEnumerator();
            var enumeratorObject = ((IEnumerable<int>)array).GetEnumerator();

            Assert.Throws<IndexOutOfRangeException>(() => enumeratorStruct.Current);
            Assert.Throws<InvalidOperationException>(() => enumeratorObject.Current);

            int count = source.Count();

            for (int i = 0; i < count; i++)
            {
                Assert.True(enumeratorStruct.MoveNext());
                Assert.True(enumeratorObject.MoveNext());

                int element = source.ElementAt(i);
                Assert.Equal(element, enumeratorStruct.Current);
                Assert.Equal(element, enumeratorObject.Current);
                Assert.Equal(element, ((IEnumerator)enumeratorObject).Current);
            }

            Assert.False(enumeratorStruct.MoveNext());
            Assert.False(enumeratorObject.MoveNext());

            Assert.Throws<IndexOutOfRangeException>(() => enumeratorStruct.Current);
            Assert.Throws<InvalidOperationException>(() => enumeratorObject.Current);
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void EnumeratorObjectTraversalDisposeReset(IEnumerable<int> source)
        {
            var array = (IEnumerable<int>)source.ToImmutableArray();
            var enumerator = array.GetEnumerator();

            Assert.All(Enumerable.Range(0, source.Count()), bound =>
            {
                enumerator.Reset();
                enumerator.Dispose(); // This should have no effect.

                for (int i = 0; i < bound; i++)
                {
                    int element = source.ElementAt(i);

                    enumerator.Dispose(); // This should have no effect.
                    Assert.True(enumerator.MoveNext());
                    Assert.Equal(element, enumerator.Current);
                    Assert.Equal(element, ((IEnumerator)enumerator).Current);
                }
            });
        }

        [Fact]
        public void EnumeratorStructTraversalDefaultInvalid()
        {
            var enumerator = default(ImmutableArray<int>.Enumerator);
            Assert.Throws<NullReferenceException>(() => enumerator.Current);
            Assert.Throws<NullReferenceException>(() => enumerator.MoveNext());
        }

        [Theory]
        [MemberData(nameof(EnumeratorTraversalNullData))]
        public void EnumeratorTraversalNull<T>(IEnumerable<T> source) where T : class
        {
            var array = ForceLazy(source.ToImmutableArray()).ToArray();
            Assert.Equal(source, array);
            Assert.Contains(null, array);
        }

        public static IEnumerable<object[]> EnumeratorTraversalNullData()
        {
            yield return new object[] { s_twoElementRefTypeWithNull };
            yield return new object[] { new[] { default(object) } };
            yield return new object[] { new[] { null, new object() } };
            yield return new object[] { new[] { null, string.Empty } };
        }

        [Theory]
        [MemberData(nameof(EqualsData))]
        public void Equals(ImmutableArray<int> first, ImmutableArray<int> second, bool expected)
        {
            Assert.Equal(expected, first == second);
            Assert.NotEqual(expected, first != second);

            Assert.Equal(expected, first.Equals(second));
            Assert.Equal(expected, AsEquatable(first).Equals(second));
            Assert.Equal(expected, first.Equals((object)second));

            Assert.Equal(expected, second == first);
            Assert.NotEqual(expected, second != first);

            Assert.Equal(expected, second.Equals(first));
            Assert.Equal(expected, AsEquatable(second).Equals(first));
            Assert.Equal(expected, second.Equals((object)first));
        }

        public static IEnumerable<object[]> EqualsData()
        {
            var enumerables = Int32EnumerableData()
                .Select(array => array[0])
                .Cast<IEnumerable<int>>();

            foreach (var enumerable in enumerables)
            {
                var array = enumerable.ToImmutableArray();

                yield return new object[]
                {
                    array,
                    array,
                    true
                };

                // Reference equality, not content equality, should be compared.
                yield return new object[]
                {
                    array,
                    enumerable.ToImmutableArray(),
                    !enumerable.Any() || enumerable is ImmutableArray<int>
                };
            }

            // Empty and default ImmutableArrays should not be seen as equal.
            yield return new object[]
            {
                s_empty,
                s_emptyDefault,
                false
            };

            yield return new object[]
            {
                s_empty,
                s_oneElement,
                false
            };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        [MemberData(nameof(SpecialInt32ImmutableArrayData))]
        public void EqualsSelf(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

#pragma warning disable CS1718 // Comparison made to same variable
            Assert.True(array == array);
            Assert.False(array != array);
#pragma warning restore CS1718 // Comparison made to same variable

            Assert.True(array.Equals(array));
            Assert.True(AsEquatable(array).Equals(array));
            Assert.True(array.Equals((object)array));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        [MemberData(nameof(SpecialInt32ImmutableArrayData))]
        public void EqualsNull(IEnumerable<int> source)
        {
            Assert.False(source.ToImmutableArray().Equals(null));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        [MemberData(nameof(SpecialInt32ImmutableArrayData))]
        public void EqualsNullable(IEnumerable<int> source)
        {
            // ImmutableArray<T> overrides the equality operators for ImmutableArray<T>?.
            // If one nullable with HasValue = false is compared to a nullable with HasValue = true,
            // but Value.IsDefault = true, the nullables will compare as equal.

            var array = source.ToImmutableArray();
            ImmutableArray<int>? nullable = array;

            Assert.Equal(array.IsDefault, null == nullable);
            Assert.NotEqual(array.IsDefault, null != nullable);

            Assert.Equal(array.IsDefault, nullable == null);
            Assert.NotEqual(array.IsDefault, nullable != null);
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        [MemberData(nameof(SpecialInt32ImmutableArrayData))]
        public void GetHashCode(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            // We must box once. Otherwise, the following assert would not have much purpose since
            // RuntimeHelpers.GetHashCode returns different values for boxed objects that are not
            // reference-equal.
            object boxed = array;

            // The default implementation of object.GetHashCode is a call to RuntimeHelpers.GetHashCode.
            // This assert effectively ensures that ImmutableArray overrides GetHashCode.
            Assert.NotEqual(RuntimeHelpers.GetHashCode(boxed), boxed.GetHashCode());

            // Ensure that the hash is consistent.
            Assert.Equal(array.GetHashCode(), array.GetHashCode());

            if (array.IsDefault)
            {
                Assert.Equal(0, array.GetHashCode());
            }
            else if (array.IsEmpty)
            {
                // Empty array instances should be cached.
                var same = ImmutableArray.Create(new int[0]);
                Assert.Equal(array.GetHashCode(), same.GetHashCode());
            }

            // Use reflection to retrieve the underlying array, and ensure that the ImmutableArray's
            // hash code is equivalent to the array's hash code.

            int[] underlyingArray = GetUnderlyingArray(array);
            Assert.Equal(underlyingArray?.GetHashCode() ?? 0, array.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(AddData))]
        public void Add(IEnumerable<int> source, IEnumerable<int> items)
        {
            var array = source.ToImmutableArray();

            var list = new List<Tuple<int[], ImmutableArray<int>>>();

            int index = 0;
            foreach (int item in items)
            {
                // Take a snapshot of the ImmutableArray before the Add.
                list.Add(Tuple.Create(array.ToArray(), array));

                // Add the next item.
                array = array.Add(item);

                var expected = source.Concat(items.Take(++index));
                Assert.Equal(expected, array);

                // Go back to previous ImmutableArrays and make sure their contents
                // didn't change by comparing them against their snapshots.
                foreach (var tuple in list)
                {
                    Assert.Equal(tuple.Item1, tuple.Item2);
                }
            }
        }

        [Theory]
        [MemberData(nameof(AddData))]
        public void AddRange(IEnumerable<int> source, IEnumerable<int> items)
        {
            Assert.All(ChangeType(items), it =>
            {
                var array = source.ToImmutableArray();

                Assert.Equal(source.Concat(items), array.AddRange(it)); // Enumerable overload
                Assert.Equal(source.Concat(items), array.AddRange(it.ToImmutableArray())); // Struct overload
                Assert.Equal(source, array); // Make sure the original array wasn't affected.
            });
        }

        public static IEnumerable<object[]> AddData()
        {
            yield return new object[] { new int[] { }, new[] { 1 } };
            yield return new object[] { new[] { 1, 2 }, new[] { 3 } };
            yield return new object[] { s_empty, Enumerable.Empty<int>() };
            yield return new object[] { s_empty, Enumerable.Range(1, 2) };
            yield return new object[] { s_empty, new[] { 1, 2 } };
            yield return new object[] { s_manyElements, new[] { 4 } };
            yield return new object[] { s_manyElements, new[] { 4, 5 } };
            yield return new object[] { s_manyElements, new[] { 4 } };
            yield return new object[] { s_manyElements, new[] { 4, 5 } };
            yield return new object[] { s_empty, s_empty };
            yield return new object[] { s_empty, s_oneElement };
            yield return new object[] { s_oneElement, s_empty };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void AddRangeInvalid(IEnumerable<int> source)
        {
            // If the lhs or the rhs is a default ImmutableArray, AddRange should throw.

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.AddRange(source)); // Enumerable overload
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.AddRange(source.ToImmutableArray())); // Struct overload
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => source.ToImmutableArray().AddRange(s_emptyDefault)); // Struct overload
            Assert.Throws<InvalidOperationException>(() => source.ToImmutableArray().AddRange((IEnumerable<int>)s_emptyDefault)); // Enumerable overload

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.AddRange(s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.AddRange((IEnumerable<int>)s_emptyDefault));
        }

        [Theory]
        [MemberData(nameof(InsertData))]
        public void Insert<T>(IEnumerable<T> source, int index, T item)
        {
            var expected = source.Take(index)
                .Concat(new[] { item })
                .Concat(source.Skip(index));
            var array = source.ToImmutableArray();

            Assert.Equal(expected, array.Insert(index, item));
            Assert.Equal(source, array); // Make sure the original array wasn't affected.
        }

        public static IEnumerable<object[]> InsertData()
        {
            yield return new object[] { new char[] { }, 0, 'c' };
            yield return new object[] { new[] { 'c' }, 0, 'a' };
            yield return new object[] { new[] { 'c' }, 1, 'e' };
            yield return new object[] { new[] { 'a', 'c' }, 1, 'b' };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void InsertInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.Insert(-1, 0x61));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.Insert(array.Length + 1, 0x61));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1)]
        [InlineData(0)]
        public void InsertDefaultInvalid(int index)
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.Insert(index, 10));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void InsertRangeInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.InsertRange(array.Length + 1, s_oneElement));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.InsertRange(-1, s_oneElement));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.InsertRange(array.Length + 1, (IEnumerable<int>)s_oneElement));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.InsertRange(-1, (IEnumerable<int>)s_oneElement));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void InsertRangeDefaultInvalid(IEnumerable<int> items)
        {
            var array = items.ToImmutableArray();

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.InsertRange(1, items));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.InsertRange(-1, items));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.InsertRange(0, items));

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.InsertRange(1, array));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.InsertRange(-1, array));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.InsertRange(0, array));

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => array.InsertRange(1, s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => array.InsertRange(-1, s_emptyDefault));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => array.InsertRange(0, s_emptyDefault));

            if (array.Length > 0)
            {
                Assert.Throws<InvalidOperationException>(() => array.InsertRange(1, (IEnumerable<int>)s_emptyDefault));
            }

            Assert.Throws<InvalidOperationException>(() => array.InsertRange(0, (IEnumerable<int>)s_emptyDefault));
        }

        [Theory]
        [MemberData(nameof(InsertRangeData))]
        public void InsertRange(IEnumerable<int> source, int index, IEnumerable<int> items)
        {
            var array = source.ToImmutableArray();

            Assert.All(ChangeType(items), it =>
            {
                var expected = source.Take(index)
                    .Concat(items)
                    .Concat(source.Skip(index));

                Assert.Equal(expected, array.InsertRange(index, it)); // Enumerable overload
                Assert.Equal(expected, array.InsertRange(index, it.ToImmutableArray())); // Struct overload

                if (index == array.Length)
                {
                    // Insertion at the end is equivalent to adding.
                    Assert.Equal(expected, array.InsertRange(index, it)); // Enumerable overload
                    Assert.Equal(expected, array.InsertRange(index, it.ToImmutableArray())); // Struct overload
                }
            });
        }

        public static IEnumerable<object[]> InsertRangeData()
        {
            yield return new object[] { s_manyElements, 0, new[] { 7 } };
            yield return new object[] { s_manyElements, 0, new[] { 7, 8 } };
            yield return new object[] { s_manyElements, 1, new[] { 7 } };
            yield return new object[] { s_manyElements, 1, new[] { 7, 8 } };
            yield return new object[] { s_manyElements, 3, new[] { 7 } };
            yield return new object[] { s_manyElements, 3, new[] { 7, 8 } };
            yield return new object[] { s_empty, 0, new[] { 1 } };
            yield return new object[] { s_empty, 0, new[] { 2, 3, 4 } };
            yield return new object[] { s_manyElements, 0, new int[0] };
            yield return new object[] { s_empty, 0, s_empty };
            yield return new object[] { s_empty, 0, s_oneElement };
            yield return new object[] { s_oneElement, 0, s_empty };
            yield return new object[] { s_empty, 0, new uint[] { 1, 2, 3 } };
            yield return new object[] { s_manyElements, 0, new uint[] { 4, 5, 6 } };
            yield return new object[] { s_manyElements, 3, new uint[] { 4, 5, 6 } };
        }

        [Theory]
        [MemberData(nameof(RemoveAtData))]
        public void RemoveAt(IEnumerable<int> source, int index)
        {
            var array = source.ToImmutableArray();
            var expected = source.Take(index).Concat(source.Skip(index + 1));
            Assert.Equal(expected, array.RemoveAt(index));
        }

        public static IEnumerable<object[]> RemoveAtData()
        {
            yield return new object[] { s_oneElement, 0 };
            yield return new object[] { s_manyElements, 0 };
            yield return new object[] { s_manyElements, 1 };
            yield return new object[] { s_manyElements, 2 };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void RemoveAtInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.RemoveAt(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => array.RemoveAt(array.Length));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.RemoveAt(array.Length + 1));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void RemoveAtDefaultInvalid(int index)
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveAt(index));
        }

        [Theory]
        [MemberData(nameof(RemoveData))]
        public void Remove<T>(IEnumerable<T> source, T item, IEqualityComparer<T> comparer)
        {
            var array = source.ToImmutableArray();

            var comparerOrDefault = comparer ?? EqualityComparer<T>.Default;
            var expected = source
                .TakeWhile(x => !comparerOrDefault.Equals(x, item))
                .Concat(source.SkipWhile(x => !comparerOrDefault.Equals(x, item)).Skip(1));

            Assert.Equal(expected, array.Remove(item, comparer));
            Assert.Equal(expected, ((IImmutableList<T>)array).Remove(item, comparer));
            
            if (comparer == null || comparer == EqualityComparer<T>.Default)
            {
                Assert.Equal(expected, array.Remove(item));
                Assert.Equal(expected, ((IImmutableList<T>)array).Remove(item));
            }
        }

        public static IEnumerable<object[]> RemoveData()
        {
            return SharedEqualityComparers<int>().SelectMany(comparer =>
                new[]
                {
                    new object[] { s_manyElements, 1, comparer },
                    new object[] { s_manyElements, 2, comparer },
                    new object[] { s_manyElements, 3, comparer },
                    new object[] { s_manyElements, 4, comparer },
                    new object[] { new int[0], 4, comparer },
                    new object[] { new int[] { 1, 4 }, 4, comparer },
                    new object[] { s_oneElement, 1, comparer }
                });
        }

        [Fact]
        public void RemoveDefaultInvalid()
        {
            Assert.All(SharedEqualityComparers<int>(), comparer =>
            {
                Assert.Throws<NullReferenceException>(() => s_emptyDefault.Remove(5));
                Assert.Throws<NullReferenceException>(() => s_emptyDefault.Remove(5, comparer));

                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).Remove(5));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).Remove(5, comparer));
            });
        }

        [Theory]
        [MemberData(nameof(RemoveRangeIndexLengthData))]
        public void RemoveRangeIndexLength(IEnumerable<int> source, int index, int length)
        {
            var array = source.ToImmutableArray();
            var expected = source.Take(index).Concat(source.Skip(index + length));
            Assert.Equal(expected, array.RemoveRange(index, length));
        }

        public static IEnumerable<object[]> RemoveRangeIndexLengthData()
        {
            yield return new object[] { s_empty, 0, 0 };
            yield return new object[] { s_oneElement, 1, 0 };
            yield return new object[] { s_oneElement, 0, 1 };
            yield return new object[] { s_oneElement, 0, 0 };
            yield return new object[] { new[] { 1, 2, 3, 4 }, 0, 2 };
            yield return new object[] { new[] { 1, 2, 3, 4 }, 1, 2 };
            yield return new object[] { new[] { 1, 2, 3, 4 }, 2, 2 };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void RemoveRangeIndexLengthInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.RemoveRange(-1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.RemoveRange(array.Length + 1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => array.RemoveRange(0, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => array.RemoveRange(0, array.Length + 1));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, -1)]
        [InlineData(0, 0)]
        [InlineData(1, -1)]
        public void RemoveRangeIndexLengthDefaultInvalid(int index, int length)
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(index, length));
        }

        [Theory]
        [MemberData(nameof(RemoveRangeEnumerableData))]
        public void RemoveRangeEnumerable(IEnumerable<int> source, IEnumerable<int> items, IEqualityComparer<int> comparer)
        {
            var array = source.ToImmutableArray();
            IEnumerable<int> expected = items.Aggregate(
                seed: source.ToImmutableArray(),
                func: (a, i) => a.Remove(i, comparer));

            Assert.Equal(expected, array.RemoveRange(items, comparer)); // Enumerable overload
            Assert.Equal(expected, array.RemoveRange(items.ToImmutableArray(), comparer)); // Struct overload
            Assert.Equal(expected, ((IImmutableList<int>)array).RemoveRange(items, comparer));

            if (comparer == null || comparer == EqualityComparer<int>.Default)
            {
                Assert.Equal(expected, array.RemoveRange(items)); // Enumerable overload
                Assert.Equal(expected, array.RemoveRange(items.ToImmutableArray())); // Struct overload
                Assert.Equal(expected, ((IImmutableList<int>)array).RemoveRange(items));
            }
        }

        public static IEnumerable<object[]> RemoveRangeEnumerableData()
        {
            return SharedEqualityComparers<int>().SelectMany(comparer =>
                new[]
                {
                    new object[] { s_empty, s_empty, comparer },
                    new object[] { s_empty, s_oneElement, comparer },
                    new object[] { s_oneElement, s_empty, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 2, 3, 4 }, comparer },
                    new object[] { Enumerable.Range(1, 5), Enumerable.Range(6, 5), comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 2 }, comparer },
                    new object[] { s_empty, new int[] { }, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 2 }, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 1, 3, 5 }, comparer },
                    new object[] { Enumerable.Range(1, 10), new[] { 2, 4, 5, 7, 10 }, comparer },
                    new object[] { Enumerable.Range(1, 10), new[] { 1, 2, 4, 5, 7, 10 }, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 5 }, comparer },
                    new object[] { new[] { 1, 2, 2, 3 }, new[] { 2 }, comparer },
                    new object[] { new[] { 1, 2, 2, 3 }, new[] { 2, 2 }, comparer },
                    new object[] { new[] { 1, 2, 2, 3 }, new[] { 2, 2, 2 }, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 42 }, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 42, 42 }, comparer },
                    new object[] { new[] { 1, 2, 3 }, new[] { 42, 42, 42 }, comparer },
                });
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void RemoveRangeEnumerableInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            Assert.All(SharedEqualityComparers<int>(), comparer =>
            {
                // Enumerable overloads, lhs is default
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(source));
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(source, comparer));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).RemoveRange(source));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).RemoveRange(source, comparer));

                // Struct overloads, lhs is default
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(array));
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(array, comparer));

                // Struct overloads, rhs is default
                AssertExtensions.Throws<ArgumentNullException>("items", () => array.RemoveRange(s_emptyDefault));
                AssertExtensions.Throws<ArgumentNullException>("items", () => array.RemoveRange(s_emptyDefault, comparer));

                // Enumerable overloads, rhs is default
                Assert.Throws<InvalidOperationException>(() => array.RemoveRange((IEnumerable<int>)s_emptyDefault));
                Assert.Throws<InvalidOperationException>(() => array.RemoveRange((IEnumerable<int>)s_emptyDefault, comparer));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)array).RemoveRange(s_emptyDefault));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)array).RemoveRange(s_emptyDefault, comparer));

                // Struct overloads, both sides are default
                AssertExtensions.Throws<ArgumentNullException>("items", () => s_emptyDefault.RemoveRange(s_emptyDefault));
                AssertExtensions.Throws<ArgumentNullException>("items", () => s_emptyDefault.RemoveRange(s_emptyDefault, comparer));

                // Enumerable overloads, both sides are default
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange((IEnumerable<int>)s_emptyDefault));
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange((IEnumerable<int>)s_emptyDefault, comparer));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).RemoveRange(s_emptyDefault));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).RemoveRange(s_emptyDefault, comparer));

                // Enumerable overloads, rhs is null
                AssertExtensions.Throws<ArgumentNullException>("items", () => array.RemoveRange(items: null));
                AssertExtensions.Throws<ArgumentNullException>("items", () => array.RemoveRange(items: null, equalityComparer: comparer));
                AssertExtensions.Throws<ArgumentNullException>("items", () => ((IImmutableList<int>)array).RemoveRange(items: null));
                AssertExtensions.Throws<ArgumentNullException>("items", () => ((IImmutableList<int>)array).RemoveRange(items: null, equalityComparer: comparer));

                // Enumerable overloads, lhs is default and rhs is null
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(items: null));
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(items: null, equalityComparer: comparer));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).RemoveRange(items: null));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).RemoveRange(items: null, equalityComparer: comparer));
            });
        }

        [Fact]
        public void RemoveRangeEnumerableRegression()
        {
            // Validates that a fixed bug in the inappropriate adding of the Empty
            // singleton enumerator to the reusable instances bag does not regress.
            
            IEnumerable<int> oneElementBoxed = s_oneElement;
            IEnumerable<int> emptyBoxed = s_empty;
            IEnumerable<int> emptyDefaultBoxed = s_emptyDefault;

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(emptyBoxed));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.RemoveRange(emptyDefaultBoxed));
            Assert.Throws<InvalidOperationException>(() => s_empty.RemoveRange(emptyDefaultBoxed));

            Assert.Equal(oneElementBoxed, oneElementBoxed);
        }

        [Theory]
        [MemberData(nameof(RemoveAllData))]
        public void RemoveAll(IEnumerable<int> source, Predicate<int> match)
        {
            var array = source.ToImmutableArray();
            var expected = source.Where(i => !match(i));
            Assert.Equal(expected, array.RemoveAll(match));
        }

        public static IEnumerable<object[]> RemoveAllData()
        {
            yield return new object[] { Enumerable.Range(1, 10), new Predicate<int>(i => i % 2 == 0) };
            yield return new object[] { Enumerable.Range(1, 10), new Predicate<int>(i => i % 2 == 1) };
            yield return new object[] { Enumerable.Range(1, 10), new Predicate<int>(i => true) };
            yield return new object[] { Enumerable.Range(1, 10), new Predicate<int>(i => false) };
            yield return new object[] { s_empty, new Predicate<int>(i => false) };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void RemoveAllInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentNullException>("match", () => array.RemoveAll(match: null));
        }

        [Fact]
        public void RemoveAllDefaultInvalid()
        {
            Assert.Throws<NullReferenceException>(() => s_emptyDefault.RemoveAll(i => false));
        }

        [Theory]
        [MemberData(nameof(ReplaceData))]
        public void Replace<T>(IEnumerable<T> source, T oldValue, T newValue, IEqualityComparer<T> comparer)
        {
            var array = source.ToImmutableArray();

            var comparerOrDefault = comparer ?? EqualityComparer<T>.Default;
            var expected = source
                .TakeWhile(x => !comparerOrDefault.Equals(x, oldValue))
                .Concat(new[] { newValue })
                .Concat(source.SkipWhile(x => !comparerOrDefault.Equals(x, oldValue)).Skip(1));

            // If the comparer is a faulty implementation that says nothing is equal,
            // an exception will be thrown here. Check that the comparer says the source contains
            // this value first.

            if (source.Contains(oldValue, comparer))
            {
                Assert.Equal(expected, array.Replace(oldValue, newValue, comparer));
                Assert.Equal(expected, ((IImmutableList<T>)array).Replace(oldValue, newValue, comparer));
            }

            if (comparer == null || comparer == EqualityComparer<T>.Default)
            {
                Assert.Equal(expected, array.Replace(oldValue, newValue));
                Assert.Equal(expected, ((IImmutableList<T>)array).Replace(oldValue, newValue));
            }
        }

        public static IEnumerable<object[]> ReplaceData()
        {
            return SharedEqualityComparers<int>().SelectMany(comparer =>
                new[]
                {
                    new object[] { s_oneElement, 1, 5, comparer },
                    new object[] { s_manyElements, 1, 6, comparer },
                    new object[] { s_manyElements, 2, 6, comparer },
                    new object[] { s_manyElements, 3, 6, comparer },
                    new object[] { new[] { 1, 3, 3, 4 }, 3, 2, comparer },
                    new object[] { s_manyElements, 2, 10, comparer }
                });
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void ReplaceInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            int notContained = Enumerable.Range(0, int.MaxValue).First(i => !source.Contains(i));

            Assert.All(SharedEqualityComparers<int>(), comparer =>
            {
                AssertExtensions.Throws<ArgumentException>("oldValue", () => array.Replace(notContained, 123));
                AssertExtensions.Throws<ArgumentException>("oldValue", () => ((IImmutableList<int>)array).Replace(notContained, 123));

                // If the comparer is a faulty implementation that says everything is equal,
                // an exception won't be thrown here. Check that the comparer says the source does
                // not contain this value first.
                if (!source.Contains(notContained, comparer))
                {
                    AssertExtensions.Throws<ArgumentException>("oldValue", () => array.Replace(notContained, 123, comparer));
                    AssertExtensions.Throws<ArgumentException>("oldValue", () => ((IImmutableList<int>)array).Replace(notContained, 123, comparer));
                }
            });
        }

        [Fact]
        public void ReplaceDefaultInvalid()
        {
            Assert.All(SharedEqualityComparers<int>(), comparer =>
            {
                Assert.Throws<NullReferenceException>(() => s_emptyDefault.Replace(123, 123));
                Assert.Throws<NullReferenceException>(() => s_emptyDefault.Replace(123, 123, comparer));

                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).Replace(123, 123));
                Assert.Throws<InvalidOperationException>(() => ((IImmutableList<int>)s_emptyDefault).Replace(123, 123, comparer));
            });
        }

        [Theory]
        [MemberData(nameof(SetItemData))]
        public void SetItem<T>(IEnumerable<T> source, int index, T item)
        {
            var array = source.ToImmutableArray();
            var expected = source.ToArray();
            expected[index] = item;
            Assert.Equal(expected, array.SetItem(index, item));
        }

        public static IEnumerable<object[]> SetItemData()
        {
            yield return new object[] { s_oneElement, 0, 12345 };
            yield return new object[] { s_manyElements, 0, 12345 };
            yield return new object[] { s_manyElements, 1, 12345 };
            yield return new object[] { s_manyElements, 2, 12345 };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void SetItemInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.SetItem(index: -1, item: 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.SetItem(index: array.Length, item: 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.SetItem(index: array.Length + 1, item: 0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void SetItemDefaultInvalid(int index)
        {
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.SetItem(index, item: 0));
        }

        [Theory]
        [MemberData(nameof(CopyToData))]
        public void CopyTo(IEnumerable<int> source, int sourceIndex, IEnumerable<int> destination, int destinationIndex, int length)
        {
            var array = source.ToImmutableArray();

            // Take a snapshot of the destination array before calling CopyTo.
            // Afterwards, ensure that the range we copied to was overwritten, and check
            // that other areas were unaffected.

            CopyAndInvoke(destination, destinationArray =>
            {
                array.CopyTo(sourceIndex, destinationArray, destinationIndex, length);

                Assert.Equal(destination.Take(destinationIndex), destinationArray.Take(destinationIndex));
                Assert.Equal(source.Skip(sourceIndex).Take(length), destinationArray.Skip(destinationIndex).Take(length));
                Assert.Equal(destination.Skip(destinationIndex + length), destinationArray.Skip(destinationIndex + length));
            });

            if (sourceIndex == 0 && length == array.Length)
            {
                CopyAndInvoke(destination, destinationArray =>
                {
                    array.CopyTo(destinationArray, destinationIndex);

                    Assert.Equal(destination.Take(destinationIndex), destinationArray.Take(destinationIndex));
                    Assert.Equal(source, destinationArray.Skip(destinationIndex).Take(array.Length));
                    Assert.Equal(destination.Skip(destinationIndex + array.Length), destinationArray.Skip(destinationIndex + array.Length));
                });

                if (destinationIndex == 0)
                {
                    CopyAndInvoke(destination, destinationArray =>
                    {
                        array.CopyTo(destinationArray);

                        Assert.Equal(source, destinationArray.Take(array.Length));
                        Assert.Equal(destination.Skip(array.Length), destinationArray.Skip(array.Length));
                    });
                }
            }
        }

        private static void CopyAndInvoke<T>(IEnumerable<T> source, Action<T[]> action) => action(source.ToArray());

        public static IEnumerable<object[]> CopyToData()
        {
            yield return new object[] { s_manyElements, 0, new int[3], 0, 3 };
            yield return new object[] { new[] { 1, 2, 3 }, 0, new int[4], 1, 3 };
            yield return new object[] { new[] { 1, 2, 3 }, 0, Enumerable.Range(1, 4), 1, 3 };
            yield return new object[] { new[] { 1, 2, 3 }, 1, new int[4], 3, 1 };
            yield return new object[] { new[] { 1, 2, 3 }, 1, Enumerable.Range(1, 4), 3, 1 };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void CopyToInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            // ImmutableArray<T>.CopyTo defers to Array.Copy for argument validation, so
            // the parameter names here come from Array.Copy.

            AssertExtensions.Throws<ArgumentNullException>("destinationArray", "dest", () => array.CopyTo(null));
            AssertExtensions.Throws<ArgumentNullException>("destinationArray", "dest", () => array.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentNullException>("destinationArray", "dest", () => array.CopyTo(0, null, 0, 0));
            AssertExtensions.Throws<ArgumentNullException>("destinationArray", "dest", () => array.CopyTo(-1, null, -1, -1)); // The destination should be validated first.

            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => array.CopyTo(-1, new int[0], -1, -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sourceIndex", "srcIndex", () => array.CopyTo(-1, new int[0], -1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", "dstIndex", () => array.CopyTo(0, new int[0], -1, 0));

            AssertExtensions.Throws<ArgumentException>("sourceArray", string.Empty, () => array.CopyTo(array.Length, new int[1], 0, 1)); // Not enough room in the source.

            if (array.Length > 0)
            {
                AssertExtensions.Throws<ArgumentException>("destinationArray", string.Empty, () => array.CopyTo(array.Length - 1, new int[1], 1, 1)); // Not enough room in the destination.
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(2, 2)]
        [InlineData(3, 1)]
        public void CopyToDefaultInvalid(int destinationLength, int destinationIndex)
        {
            var destination = new int[destinationLength];

            if (destinationIndex == 0)
            {
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.CopyTo(destination));
            }

            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.CopyTo(destination, destinationIndex));
            TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.CopyTo(0, destination, destinationIndex, 0));
        }

        [Theory]
        [MemberData(nameof(IsDefaultOrEmptyData))]
        public void IsDefault(IEnumerable<int> source, bool isDefault, bool isEmpty)
        {
            var array = source.ToImmutableArray();

            Assert.Equal(isDefault, array.IsDefault);
        }

        [Theory]
        [MemberData(nameof(IsDefaultOrEmptyData))]
        public void IsDefaultOrEmpty(IEnumerable<int> source, bool isDefault, bool isEmpty)
        {
            var array = source.ToImmutableArray();

            Assert.Equal(isDefault || isEmpty, array.IsDefaultOrEmpty);
        }

        public static IEnumerable<object[]> IsDefaultOrEmptyData()
        {
            yield return new object[] { s_emptyDefault, true, false };
            yield return new object[] { s_empty, false, true };
            yield return new object[] { s_oneElement, false, false };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void GetIndexer(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            
            for (int i = 0; i < array.Length; i++)
            {
                int expected = source.ElementAt(i);

                Assert.Equal(expected, array[i]);
                Assert.Equal(expected, ((IList)array)[i]);
                Assert.Equal(expected, ((IList<int>)array)[i]);
                Assert.Equal(expected, ((IReadOnlyList<int>)array)[i]);
            }
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void GetIndexerInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            Assert.Throws<IndexOutOfRangeException>(() => array[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList)array)[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList<int>)array)[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<int>)array)[-1]);

            Assert.Throws<IndexOutOfRangeException>(() => array[array.Length]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList)array)[array.Length]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList<int>)array)[array.Length]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<int>)array)[array.Length]);

            Assert.Throws<IndexOutOfRangeException>(() => array[array.Length + 1]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList)array)[array.Length + 1]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IList<int>)array)[array.Length + 1]);
            Assert.Throws<IndexOutOfRangeException>(() => ((IReadOnlyList<int>)array)[array.Length + 1]);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        public void GetIndexerDefaultInvalid(int index)
        {
            Assert.Throws<NullReferenceException>(() => s_emptyDefault[index]);
            Assert.Throws<InvalidOperationException>(() => ((IList)s_emptyDefault)[index]);
            Assert.Throws<InvalidOperationException>(() => ((IList<int>)s_emptyDefault)[index]);
            Assert.Throws<InvalidOperationException>(() => ((IReadOnlyList<int>)s_emptyDefault)[index]);
        }

        [Theory]
        [MemberData(nameof(SortData))]
        public void Sort<T>(IEnumerable<T> source, int index, int count, IComparer<T> comparer)
        {
            var array = source.ToImmutableArray();

            var expected = source.ToArray();
            Array.Sort(expected, index, count, comparer);

            Assert.Equal(expected, array.Sort(index, count, comparer));
            Assert.Equal(source, array); // Make sure the original array is unaffected.

            if (index == 0 && count == array.Length)
            {
                Assert.Equal(expected, array.Sort(comparer));
                Assert.Equal(source, array); // Make sure the original array is unaffected.

                if (comparer != null)
                {
                    Assert.Equal(expected, array.Sort(comparer.Compare));
                    Assert.Equal(source, array); // Make sure the original array is unaffected.
                }

                if (comparer == null || comparer == Comparer<T>.Default)
                {
                    Assert.Equal(expected, array.Sort());
                    Assert.Equal(source, array); // Make sure the original array is unaffected.
                }
            }
        }

        public static IEnumerable<object[]> SortData()
        {
            return SharedComparers<int>().SelectMany(comparer =>
                new[]
                {
                    new object[] { new[] { 2, 4, 1, 3 }, 0, 4, comparer },
                    new object[] { new[] { 1 }, 0, 1, comparer },
                    new object[] { new int[0], 0, 0, comparer },
                    new object[] { new[] { 2, 4, 1, 3 }, 1, 2, comparer },
                    new object[] { new[] { 2, 4, 1, 3 }, 4, 0, comparer },
                    new object[] { new[] { "c", "B", "a" }, 0, 3, StringComparer.OrdinalIgnoreCase },
                    new object[] { new[] { "c", "B", "a" }, 0, 3, StringComparer.Ordinal },
                    new object[] { new[] { 1, 2, 3, 4, 6, 5, 7, 8, 9, 10 }, 4, 2, comparer }
                });
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void SortComparisonInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentNullException>("comparison", () => array.Sort(comparison: null));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void SortComparerInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.Sort(-1, -1, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => array.Sort(-1, 0, Comparer<int>.Default));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => array.Sort(0, -1, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => array.Sort(array.Length + 1, 0, Comparer<int>.Default));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => array.Sort(0, array.Length + 1, Comparer<int>.Default));
        }

        [Theory]
        [InlineData(-1, -1)]
        [InlineData(0, 0)]
        [InlineData(1, 1)]
        public void SortDefaultInvalid(int index, int count)
        {
            Assert.All(SharedComparers<int>(), comparer =>
            {
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.Sort());
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.Sort(comparer));
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.Sort(comparer.Compare));
                TestExtensionsMethods.ValidateDefaultThisBehavior(() => s_emptyDefault.Sort(index, count, comparer));
            });
        }

        [Theory]
        [MemberData(nameof(SortAlreadySortedData))]
        public void SortAlreadySorted(IEnumerable<int> source, int index, int count, IComparer<int> comparer)
        {
            // If ImmutableArray<T>.Sort is called when the array is already sorted,
            // it should just return the original array rather than allocating a new one.

            var array = source.ToImmutableArray();

            Assert.True(array == array.Sort(index, count, comparer));

            if (index == 0 && count == array.Length)
            {
                Assert.True(array == array.Sort(comparer));

                if (comparer != null)
                {
                    Assert.True(array == array.Sort(comparer.Compare));
                }

                if (comparer == null || comparer == Comparer<int>.Default)
                {
                    Assert.True(array == array.Sort());
                }
            }
        }

        public static IEnumerable<object[]> SortAlreadySortedData()
        {
            yield return new object[] { new[] { 1, 2, 3, 4 }, 0, 4, null };
            yield return new object[] { new[] { 1, 2, 3, 4, 6, 5, 7, 8, 9, 10 }, 0, 5, null };
            yield return new object[] { new[] { 1, 2, 3, 4, 6, 5, 7, 8, 9, 10 }, 5, 5, null };

            yield return new object[] { new[] { 1, 2, 3, 4 }, 0, 4, Comparer<int>.Default };
            yield return new object[] { new[] { 1, 2, 3, 4, 6, 5, 7, 8, 9, 10 }, 0, 5, Comparer<int>.Default };
            yield return new object[] { new[] { 1, 2, 3, 4, 6, 5, 7, 8, 9, 10 }, 5, 5, Comparer<int>.Default };

            yield return new object[] { new[] { 1, 5, 2 }, 0, 3, Comparer<int>.Create((x, y) => 0) };
            yield return new object[] { new[] { 1, 5, 2 }, 1, 2, Comparer<int>.Create((x, y) => 0) };
            yield return new object[] { new[] { 1, 5, 2 }, 1, 1, Comparer<int>.Create((x, y) => 0) };
            yield return new object[] { new[] { 1, 5, 2 }, 0, 2, Comparer<int>.Create((x, y) => 0) };
            yield return new object[] { new[] { 1, 5, 2, 4 }, 1, 2, Comparer<int>.Create((x, y) => 0) };
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void ToBuilder(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            ImmutableArray<int>.Builder builder = array.ToBuilder();

            Assert.Equal(array, builder);
            Assert.Equal(array.Length, builder.Count);

            // Make sure that mutating the builder doesn't change the ImmutableArray.
            if (array.Length > 0)
            {
                builder[0] += 1;
                Assert.Equal(source.First(), array[0]);
                Assert.Equal(source.First() + 1, builder[0]);
                builder[0] -= 1;
            }

            builder.Add(int.MinValue);
            Assert.Equal(array.Length + 1, builder.Count);
            Assert.Equal(array.Add(int.MinValue), builder);
        }

        [Theory]
        [MemberData(nameof(IStructuralEquatableEqualsData))]
        [MemberData(nameof(IStructuralEquatableEqualsNullComparerData))]
        public void IStructuralEquatableEquals(IEnumerable<int> source, object second, IEqualityComparer comparer, bool expected)
        {
            ImmutableArray<int> first = source.ToImmutableArray();

            Assert.Equal(expected, ((IStructuralEquatable)first).Equals(second, comparer));

            if (!first.IsDefault)
            {
                int[] firstArray = first.ToArray();
                Assert.Equal(!IsImmutableArray(second) && expected, ((IStructuralEquatable)firstArray).Equals(second, comparer));

                var secondEquatable = second as IStructuralEquatable;
                if (secondEquatable != null)
                {
                    Assert.Equal(expected, secondEquatable.Equals(firstArray, comparer));
                }
            }
        }

        public static IEnumerable<object[]> IStructuralEquatableEqualsData()
        {
            // The comparers here must consider two arrays structurally equal if the default comparer does.
            var optimisticComparers = new IEqualityComparer[]
            {
                EqualityComparer<int>.Default,
                new DelegateEqualityComparer<object>(objectEquals: (x, y) => true)
            };

            // The comparers here must not consider two arrays structurally equal if the default comparer doesn't.
            var pessimisticComparers = new IEqualityComparer[]
            {
                EqualityComparer<int>.Default,
                new DelegateEqualityComparer<object>(objectEquals: (x, y) => false)
            };

            foreach (IEqualityComparer comparer in optimisticComparers)
            {
                yield return new object[] { s_empty, s_empty, comparer, true };
                yield return new object[] { s_emptyDefault, s_emptyDefault, comparer, true };
                yield return new object[] { new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, comparer, true };
                yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(1, 2, 3), comparer, true };
                yield return new object[] { new[] { 1, 2, 3 }, new object[] { 1, 2, 3 }, comparer, true };
                yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create<object>(1, 2, 3), comparer, true };
            }

            foreach (IEqualityComparer comparer in pessimisticComparers)
            {
                yield return new object[] { s_emptyDefault, s_empty, comparer, false };
                yield return new object[] { s_emptyDefault, s_oneElement, comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, new List<int> { 1, 2, 3 }, comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, new object(), comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, null, comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, new[] { 1, 2, 4 }, comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(1, 2, 4), comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, new string[3], comparer, false };
                yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(new string[3]), comparer, false };
            }
        }

        public static IEnumerable<object[]> IStructuralEquatableEqualsNullComparerData()
        {
            // Unlike other methods on ImmutableArray, null comparers are invalid inputs for IStructuralEquatable.Equals.
            // However, it will not throw for a null comparer if the array is default and `other` is an ImmutableArray, or
            // if Array's IStructuralEquatable.Equals implementation (which it calls under the cover) short-circuits before
            // trying to use the comparer.

            yield return new object[] { s_emptyDefault, s_emptyDefault, null, true };
            yield return new object[] { s_emptyDefault, ImmutableArray.Create(1, 2, 3), null, false };

            yield return new object[] { new int[0], null, null, false }; // Array short-circuits because `other` is null
            yield return new object[] { s_empty, s_empty, null, true }; // Array short-circuits because the arrays are reference-equal
            yield return new object[] { new int[0], new List<int>(), null, false }; // Array short-circuits because `other` is not an array
            yield return new object[] { new int[0], new int[1], null, false }; // Array short-circuits because `other.Length` isn't equal
            yield return new object[] { new int[0], new int[0], null, true }; // For zero-element arrays, Array doesn't have to use the comparer
        }

        [Fact]
        public void IStructuralEquatableEqualsNullComparerInvalid()
        {
            // This was not fixed for compatability reasons. See https://github.com/dotnet/corefx/issues/13410
            Assert.Throws<NullReferenceException>(() => ((IStructuralEquatable)ImmutableArray.Create(1, 2, 3)).Equals(ImmutableArray.Create(1, 2, 3), comparer: null));
            Assert.Throws<NullReferenceException>(() => ((IStructuralEquatable)s_emptyDefault).Equals(other: null, comparer: null));
        }

        [Theory]
        [MemberData(nameof(IStructuralEquatableGetHashCodeData))]
        public void IStructuralEquatableGetHashCode(IEnumerable<int> source, IEqualityComparer comparer)
        {
            var array = source.ToImmutableArray();
            int expected = ((IStructuralEquatable)source.ToArray()).GetHashCode(comparer);
            Assert.Equal(expected, ((IStructuralEquatable)array).GetHashCode(comparer));
        }

        public static IEnumerable<object[]> IStructuralEquatableGetHashCodeData()
        {
            var enumerables = Int32EnumerableData()
                .Select(array => array[0])
                .Cast<IEnumerable<int>>();

            return SharedComparers<int>()
                .OfType<IEqualityComparer>()
                .Except(new IEqualityComparer[] { null })
                .SelectMany(comparer => enumerables.Select(enumerable => new object[] { enumerable, comparer }));
        }

        [Fact]
        public void IStructuralEquatableGetHashCodeDefault()
        {
            Assert.All(SharedComparers<int>().OfType<IEqualityComparer>(), comparer =>
            {
                // A default ImmutableArray should always hash to the same value, regardless of comparer.
                // This includes null, which is included in the set of shared comparers.
                Assert.Equal(0, ((IStructuralEquatable)s_emptyDefault).GetHashCode(comparer));
            });
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void IStructuralEquatableGetHashCodeNullComparerNonNullUnderlyingArrayInvalid(IEnumerable<int> source)
        {
            var array = source.ToImmutableArray();
            AssertExtensions.Throws<ArgumentNullException>("comparer", () => ((IStructuralEquatable)array).GetHashCode(comparer: null));
        }

        [Fact]
        public void IStructuralComparableCompareToDefaultAndDefault()
        {
            Assert.All(SharedComparers<int>().OfType<IComparer>(), comparer =>
            {
                // Default ImmutableArrays are always considered the same as other default ImmutableArrays, no matter
                // what the comparer is. (Even if the comparer is null.)
                Assert.Equal(0, ((IStructuralComparable)s_emptyDefault).CompareTo(s_emptyDefault, comparer));
            });
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void IStructuralComparableCompareToDefaultAndNonDefaultInvalid(IEnumerable<int> source)
        {
            object other = source.ToImmutableArray();
            var comparers = SharedComparers<int>().OfType<IComparer>().Except(new IComparer[] { null });

            Assert.All(comparers, comparer =>
            {
                // CompareTo should throw if the arrays are of different lengths. The default ImmutableArray is considered to have
                // a different length from every other array, including empty ones.
                AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)s_emptyDefault).CompareTo(other, comparer));
                AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)other).CompareTo(s_emptyDefault, comparer));
            });
        }

        [Theory]
        [MemberData(nameof(IStructuralComparableCompareToNullComparerArgumentInvalidData))]
        public void IStructuralComparableCompareToNullComparerArgumentInvalid(IEnumerable<int> source, object other)
        {
            var array = source.ToImmutableArray();
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)array).CompareTo(other, comparer: null));

            if (other is Array || IsImmutableArray(other))
            {
                AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)other).CompareTo(array, comparer: null));
            }
        }

        public static IEnumerable<object[]> IStructuralComparableCompareToNullComparerArgumentInvalidData()
        {
            yield return new object[] { ImmutableArray.Create<int>(), null };
            yield return new object[] { ImmutableArray.Create<int>(), ImmutableArray.Create(1, 2, 3) };
            yield return new object[] { new[] { 1, 2, 3 }, null };
        }

        [Theory]
        [MemberData(nameof(IStructuralComparableCompareToNullComparerNullReferenceInvalidData))]
        public void IStructuralComparableCompareToNullComparerNullReferenceInvalid(IEnumerable<int> source, object other)
        {
            var array = source.ToImmutableArray();
            Assert.Throws<NullReferenceException>(() => ((IStructuralComparable)array).CompareTo(other, comparer: null));

            if (other == null)
            {
                Assert.Throws<NullReferenceException>(() => ((IStructuralComparable)array).CompareTo(s_emptyDefault, comparer: null));
            }
        }

        public static IEnumerable<object[]> IStructuralComparableCompareToNullComparerNullReferenceInvalidData()
        {
            // This was not fixed for compatability reasons. See https://github.com/dotnet/corefx/issues/13410
            yield return new object[] { new[] { 1, 2, 3 }, new[] { 1, 2, 3 } };
            yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(1, 2, 3) };
            // Cache this into a local so the comparands are reference-equal.
            var oneTwoThree = ImmutableArray.Create(1, 2, 3);
            yield return new object[] { oneTwoThree, oneTwoThree };
        }

        [Theory]
        [MemberData(nameof(IStructuralComparableCompareToData))]
        public void IStructuralComparableCompareTo(IEnumerable<int> source, object other, IComparer comparer, int expected)
        {
            var array = source?.ToImmutableArray() ?? s_emptyDefault;
            Assert.Equal(expected, ((IStructuralComparable)array).CompareTo(other ?? s_emptyDefault, comparer));

            if (other is Array)
            {
                Assert.Equal(expected, ((IStructuralComparable)source.ToArray()).CompareTo(other ?? s_emptyDefault, comparer));
            }
        }

        public static IEnumerable<object[]> IStructuralComparableCompareToData()
        {
            yield return new object[] { new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, Comparer<int>.Default, 0 };
            yield return new object[] { new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, Comparer<object>.Default, 0 };

            yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(1, 2, 3), Comparer<int>.Default, 0 };
            yield return new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(1, 2, 3), Comparer<object>.Default, 0 };

            // The comparands are the same instance, so Array can short-circuit.
            yield return new object[] { s_empty, s_empty, Comparer<int>.Default, 0 };
            yield return new object[] { s_empty, s_empty, Comparer<object>.Default, 0 };

            // Normally, a null comparer is an invalid input. However, if both comparands are default ImmutableArrays
            // then CompareTo will short-circuit before it validates the comparer.
            yield return new object[] { null, null, null, 0 };
        }

        [Theory]
        [MemberData(nameof(IStructuralComparableCompareToInvalidData))]
        public void IStructuralComparableCompareToInvalid(IEnumerable<int> source, object other, IComparer comparer)
        {
            var array = source.ToImmutableArray();

            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)array).CompareTo(other, comparer));
            AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)source.ToArray()).CompareTo(other, comparer));

            if (other is Array || IsImmutableArray(other))
            {
                AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)other).CompareTo(array, comparer));
                AssertExtensions.Throws<ArgumentException>("other", () => ((IStructuralComparable)other).CompareTo(source.ToArray(), comparer));
            }
        }

        public static IEnumerable<object[]> IStructuralComparableCompareToInvalidData()
        {
            return SharedComparers<int>()
                .OfType<IComparer>()
                .Except(new IComparer[] { null })
                .SelectMany(comparer => new[]
                {
                    new object[] { new[] { 1, 2, 3 }, new[] { 1, 2, 3, 4 }, comparer },
                    new object[] { new[] { 1, 2, 3 }, ImmutableArray.Create(1, 2, 3, 4), comparer },
                    new object[] { new[] { 1, 2, 3 }, new List<int> { 1, 2, 3 }, comparer }
                });
        }

        [Theory]
        [MemberData(nameof(BinarySearchData))]
        public void BinarySearch(IEnumerable<int> source, int value)
        {
            var array = source.ToArray();

            Assert.Equal(
                Array.BinarySearch(array, value),
                ImmutableArray.BinarySearch(ImmutableArray.Create(array), value));

            Assert.Equal(
                Array.BinarySearch(array, value, Comparer<int>.Default),
                ImmutableArray.BinarySearch(ImmutableArray.Create(array), value, Comparer<int>.Default));

            Assert.Equal(
                Array.BinarySearch(array, 0, array.Length, value),
                ImmutableArray.BinarySearch(ImmutableArray.Create(array), 0, array.Length, value));

            if (array.Length > 0)
            {
                Assert.Equal(
                    Array.BinarySearch(array, 1, array.Length - 1, value),
                    ImmutableArray.BinarySearch(ImmutableArray.Create(array), 1, array.Length - 1, value));
            }

            Assert.Equal(
                Array.BinarySearch(array, 0, array.Length, value, Comparer<int>.Default),
                ImmutableArray.BinarySearch(ImmutableArray.Create(array), 0, array.Length, value, Comparer<int>.Default));
        }

        public static IEnumerable<object[]> BinarySearchData()
        {
            yield return new object[] { new int[0], 5 };
            yield return new object[] { new[] { 3 }, 5 };
            yield return new object[] { new[] { 5 }, 5 };
            yield return new object[] { new[] { 1, 2, 3 }, 1 };
            yield return new object[] { new[] { 1, 2, 3 }, 2 };
            yield return new object[] { new[] { 1, 2, 3 }, 3 };
            yield return new object[] { new[] { 1, 2, 3, 4 }, 4 };
        }

        [Theory]
        [MemberData(nameof(BinarySearchData))]
        public void BinarySearchDefaultInvalid(IEnumerable<int> source, int value)
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => ImmutableArray.BinarySearch(s_emptyDefault, value));
        }

        [Fact]
        public void OfType()
        {
            Assert.Equal(new int[0], s_emptyDefault.OfType<int>());
            Assert.Equal(new int[0], s_empty.OfType<int>());
            Assert.Equal(s_oneElement, s_oneElement.OfType<int>());
            Assert.Equal(new[] { "1" }, s_twoElementRefTypeWithNull.OfType<string>());
        }

        [Fact]
        public void AddThreadSafety()
        {
            // Note the point of this thread-safety test is *not* to test the thread-safety of the test itself.
            // This test has a known issue where the two threads will stomp on each others updates, but that's not the point.
            // The point is that ImmutableArray`1.Add should *never* throw. But if it reads its own T[] field more than once,
            // it *can* throw because the field can be replaced with an array of another length. 
            // In fact, much worse can happen where we corrupt data if we are for example copying data out of the array
            // in (for example) a CopyTo method and we read from the field more than once.
            // Also noteworthy: this method only tests the thread-safety of the Add method.
            // While it proves the general point, any method that reads 'this' more than once is vulnerable.
            var array = ImmutableArray.Create<int>();
            Action mutator = () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    ImmutableInterlocked.InterlockedExchange(ref array, array.Add(1));
                }
            };
            Task.WaitAll(Task.Run(mutator), Task.Run(mutator));
        }

        [Theory]
        [MemberData(nameof(Int32EnumerableData))]
        public void DebuggerAttributesValid(IEnumerable<int> source)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(source.ToImmutableArray());
        }

        [Fact]
        public void DebuggerAttributesValid()
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(ImmutableArray.Create<int>());
            ImmutableArray<int> array = ImmutableArray.Create(1, 2, 3, 4);
            FieldInfo itemField = typeof(ImmutableArray<int>).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Single(fi => fi.GetCustomAttribute<DebuggerBrowsableAttribute>()?.State == DebuggerBrowsableState.RootHidden);
            int[] items = itemField.GetValue(array) as int[];
            Assert.Equal(array, items);
        }

        [Fact]
        public void ItemRef()
        {
            var array = new[] { 1, 2, 3 }.ToImmutableArray();

            ref readonly var safeRef = ref array.ItemRef(1);
            ref var unsafeRef = ref Unsafe.AsRef(safeRef);

            Assert.Equal(2, array.ItemRef(1));

            unsafeRef = 4;

            Assert.Equal(4, array.ItemRef(1));
        }

        [Fact]
        public void ItemRef_OutOfBounds()
        {
            var array = new[] { 1, 2, 3 }.ToImmutableArray();

            Assert.Throws<IndexOutOfRangeException>(() => array.ItemRef(5));
        }

        protected override IEnumerable<T> GetEnumerableOf<T>(params T[] contents)
        {
            return ImmutableArray.Create(contents);
        }

        /// <summary>
        /// Returns an object typed as an <see cref="IEquatable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object.</param>
        private static IEquatable<T> AsEquatable<T>(T obj) where T : IEquatable<T> => obj;

        /// <summary>
        /// Given an enumerable, produces a list of enumerables that have the same contents,
        /// but have different underlying types.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source enumerable.</param>
        private static IEnumerable<IEnumerable<T>> ChangeType<T>(IEnumerable<T> source)
        {
            yield return source;
            // Implements IList<T>, but isn't a type we're likely to explicitly optimize for.
            yield return new LinkedList<T>(source);
            yield return source.Select(x => x); // A lazy enumerable.

            // Constructing these types will be problematic if the source is a T[], but
            // its underlying type is not typeof(T[]).
            // The reason is since they are contiguous in memory, they call Array.Copy
            // if the source is an array as an optimization, which throws if the types
            // of the arrays do not exactly match up.
            // More info here: https://github.com/dotnet/corefx/issues/2241

            if (!(source is T[]) || source.GetType() == typeof(T[]))
            {
                yield return source.ToArray();
                yield return source.ToList();
                yield return source.ToImmutableArray();
                // Is not an ICollection<T>, but does implement ICollection and IReadOnlyCollection<T>.
                yield return new Queue<T>(source);
            }
        }

        /// <summary>
        /// Wraps an enumerable in an iterator, so that it does not implement interfaces such as <see cref="IList{T}"/>.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The source enumerable.</param>
        private static IEnumerable<T> ForceLazy<T>(IEnumerable<T> source)
        {
            foreach (T element in source)
            {
                yield return element;
            }
        }

        /// <summary>
        /// Gets the underlying array of an <see cref="ImmutableArray{T}"/>. For testing purposes only.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="array">The immutable array.</param>
        /// <returns>The underlying array.</returns>
        private static T[] GetUnderlyingArray<T>(ImmutableArray<T> array)
        {
            //
            // There is no documented way of doing this so this helper is inherently fragile.
            //
            // The prior version of this used Reflection to get at the private "array" field directly. This will not work on .NET Native
            // due to the prohibition on internal framework Reflection.
            //
            // This alternate method is despicable but ImmutableArray`1 has a documented contract of being exactly one reference-type field in size
            // (for example, ImmutableInterlocked depends on it.) 
            //
            // That leaves precious few candidates for which field is the "array" field...
            //
            T[] underlyingArray = Unsafe.As<ImmutableArray<T>, ImmutableArrayLayout<T>>(ref array).array;
            if (underlyingArray != null && !(((object)underlyingArray) is T[]))
                throw new Exception("ImmutableArrayTest.GetUnderlyingArray's sneaky trick of getting the underlying array out is no longer valid. This helper needs to be updated or scrapped.");
            return underlyingArray;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ImmutableArrayLayout<T>
        {
            public T[] array;
        }

        /// <summary>
        /// Returns whether the object is an instance of an <see cref="ImmutableArray{T}"/>.
        /// </summary>
        /// <param name="obj">The object.</param>
        private static bool IsImmutableArray(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var typeInfo = obj.GetType().GetTypeInfo();
            return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(ImmutableArray<>);
        }

        /// <summary>
        /// Returns a list of comparers that are shared between tests.
        /// </summary>
        /// <typeparam name="T">The comparand type.</typeparam>
        private static IEnumerable<IComparer<T>> SharedComparers<T>()
            where T : IComparable<T>
        {
            // Null comparers should be accepted and translated to the default comparer.
            yield return null;
            yield return Comparer<T>.Default;
            yield return Comparer<T>.Create((x, y) => y.CompareTo(x));
            yield return Comparer<T>.Create((x, y) => 0);
        }

        /// <summary>
        /// Returns a list of equality comparers that are shared between tests.
        /// </summary>
        /// <typeparam name="T">The comparand type.</typeparam>
        private static IEnumerable<IEqualityComparer<T>> SharedEqualityComparers<T>()
        {
            // Null comparers should be accepted and translated to the default comparer.
            yield return null;
            yield return EqualityComparer<T>.Default;
            yield return new DelegateEqualityComparer<T>(equals: (x, y) => true, objectGetHashCode: obj => 0);
            yield return new DelegateEqualityComparer<T>(equals: (x, y) => false, objectGetHashCode: obj => 0);
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
