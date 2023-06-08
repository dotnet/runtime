// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.SpanTests
{
    public class IndexOfAnyExceptTests_Byte : IndexOfAnyExceptTests<byte> { protected override byte Create(int value) => (byte)value; }
    public class IndexOfAnyExceptTests_Char : IndexOfAnyExceptTests<char> { protected override char Create(int value) => (char)value; }
    public class IndexOfAnyExceptTests_Int32 : IndexOfAnyExceptTests<int> { protected override int Create(int value) => value; }
    public class IndexOfAnyExceptTests_Int64 : IndexOfAnyExceptTests<long> { protected override long Create(int value) => value; }
    public class IndexOfAnyExceptTests_Record : IndexOfAnyExceptTests<SimpleRecord> { protected override SimpleRecord Create(int value) => new SimpleRecord(value); }
    public class IndexOfAnyExceptTests_String : IndexOfAnyExceptTests<string>
    {
        protected override string Create(int value) => ((char)value).ToString();

        [Theory]

        [InlineData(new string[] { null, "a", "a", "a", "a" }, new string[] { "a" }, 0)]
        [InlineData(new string[] { "a", "a", null, "a", "a" }, new string[] { "a" }, 2)]
        [InlineData(new string[] { "a", "a", "a", "a", "a" }, new string[] { null }, 0)]
        [InlineData(new string[] { null, null, null, null, null }, new string[] { null }, -1)]
        [InlineData(new string[] { null, null, null, null, "a" }, new string[] { null }, 4)]

        [InlineData(new string[] { null, "a", "a", "a", "a" }, new string[] { "a", null }, -1)]
        [InlineData(new string[] { null, null, null, null, "a" }, new string[] { null, "a" }, -1)]
        [InlineData(new string[] { "a", "a", null, "a", "a" }, new string[] { "a", "b" }, 2)]
        [InlineData(new string[] { "a", "a", "a", "a", "a" }, new string[] { null, null, }, 0)]
        [InlineData(new string[] { null, null, null, null, null }, new string[] { null, null }, -1)]

        [InlineData(new string[] { null, "a", "a", "a", "a" }, new string[] { "a", null, null }, -1)]
        [InlineData(new string[] { null, null, null, null, "a" }, new string[] { null, null, "a" }, -1)]
        [InlineData(new string[] { "a", "a", null, "a", "a" }, new string[] { "a", "b", null }, -1)]
        [InlineData(new string[] { "a", "a", null, "a", "a" }, new string[] { "a", "a", "a" }, 2)]
        [InlineData(new string[] { "a", "a", "a", "a", "a" }, new string[] { null, null, null }, 0)]
        [InlineData(new string[] { null, null, null, null, null }, new string[] { null, null, null }, -1)]

        public void SearchingNulls(string[] input, string[] targets, int expected)
        {
            Assert.Equal(expected, input.AsSpan().IndexOfAnyExcept(targets));
            switch (targets.Length)
            {
                case 1:
                    Assert.Equal(expected, input.AsSpan().IndexOfAnyExcept(targets[0]));
                    break;
                case 2:
                    Assert.Equal(expected, input.AsSpan().IndexOfAnyExcept(targets[0], targets[1]));
                    break;
                case 3:
                    Assert.Equal(expected, input.AsSpan().IndexOfAnyExcept(targets[0], targets[1], targets[2]));
                    break;
            }
        }
    }

    public record SimpleRecord(int Value);

    public abstract class IndexOfAnyExceptTests<T> where T : IEquatable<T>
    {
        private readonly T _a, _b, _c, _d, _e;

        public IndexOfAnyExceptTests()
        {
            _a = Create('a');
            _b = Create('b');
            _c = Create('c');
            _d = Create('d');
            _e = Create('e');
        }

        /// <summary>Validate that the methods return -1 when the source span is empty.</summary>
        [Fact]
        public void ZeroLengthSpan_ReturnNegative1()
        {
            Assert.Equal(-1, IndexOfAnyExcept(Span<T>.Empty));
            Assert.Equal(-1, IndexOfAnyExcept(Span<T>.Empty, _a));
            Assert.Equal(-1, IndexOfAnyExcept(Span<T>.Empty, _a, _b));
            Assert.Equal(-1, IndexOfAnyExcept(Span<T>.Empty, _a, _b, _c));
            Assert.Equal(-1, IndexOfAnyExcept(Span<T>.Empty, new[] { _a, _b, _c, _d }));

            Assert.Equal(-1, LastIndexOfAnyExcept(Span<T>.Empty));
            Assert.Equal(-1, LastIndexOfAnyExcept(Span<T>.Empty, _a));
            Assert.Equal(-1, LastIndexOfAnyExcept(Span<T>.Empty, _a, _b));
            Assert.Equal(-1, LastIndexOfAnyExcept(Span<T>.Empty, _a, _b, _c));
            Assert.Equal(-1, LastIndexOfAnyExcept(Span<T>.Empty, new[] { _a, _b, _c, _d }));
        }

        public static IEnumerable<object[]> AllElementsMatch_ReturnsNegative1_MemberData()
        {
            foreach (int length in new[] { 1, 2, 4, 7, 15, 16, 17, 31, 32, 33, 100 })
            {
                yield return new object[] { length };
            }
        }

        /// <summary>Validate that the methods return -1 when the source span contains only the values being excepted.</summary>
        [Theory]
        [MemberData(nameof(AllElementsMatch_ReturnsNegative1_MemberData))]
        public void AllElementsMatch_ReturnsNegative1(int length)
        {
            Assert.Equal(-1, IndexOfAnyExcept(CreateArray(length, _a), _a));
            Assert.Equal(-1, IndexOfAnyExcept(CreateArray(length, _a, _b), _a, _b));
            Assert.Equal(-1, IndexOfAnyExcept(CreateArray(length, _a, _b, _c), _a, _b, _c));
            Assert.Equal(-1, IndexOfAnyExcept(CreateArray(length, _a, _b, _c, _d), _a, _b, _c, _d));

            Assert.Equal(-1, LastIndexOfAnyExcept(CreateArray(length, _a), _a));
            Assert.Equal(-1, LastIndexOfAnyExcept(CreateArray(length, _a, _b), _a, _b));
            Assert.Equal(-1, LastIndexOfAnyExcept(CreateArray(length, _a, _b, _c), _a, _b, _c));
            Assert.Equal(-1, LastIndexOfAnyExcept(CreateArray(length, _a, _b, _c, _d), _a, _b, _c, _d));
        }

        public static IEnumerable<object[]> SomeElementsDontMatch_ReturnsOffset_MemberData()
        {
            yield return new object[] { 1, new[] { 0 } };
            yield return new object[] { 2, new[] { 0, 1 } };
            yield return new object[] { 4, new[] { 2 } };
            yield return new object[] { 5, new[] { 2 } };
            yield return new object[] { 31, new[] { 30 } };
            yield return new object[] { 10, new[] { 1, 7 } };
            yield return new object[] { 100, new[] { 19, 20, 21, 80 } };
        }

        /// <summary>Validate that the methods return the expected position when the source span contains some data other than the excepted.</summary>
        [Theory]
        [MemberData(nameof(SomeElementsDontMatch_ReturnsOffset_MemberData))]
        public void SomeElementsDontMatch_ReturnsOffset(int length, int[] matchPositions)
        {
            Assert.Equal(matchPositions[0], IndexOfAnyExcept(Set(CreateArray(length, _a), _e, matchPositions), _a));
            Assert.Equal(matchPositions[0], IndexOfAnyExcept(Set(CreateArray(length, _a, _b), _e, matchPositions), _a, _b));
            Assert.Equal(matchPositions[0], IndexOfAnyExcept(Set(CreateArray(length, _a, _b, _c), _e, matchPositions), _a, _b, _c));
            Assert.Equal(matchPositions[0], IndexOfAnyExcept(Set(CreateArray(length, _a, _b, _c, _d), _e, matchPositions), _a, _b, _c, _d));

            Assert.Equal(matchPositions[^1], LastIndexOfAnyExcept(Set(CreateArray(length, _a), _e, matchPositions), _a));
            Assert.Equal(matchPositions[^1], LastIndexOfAnyExcept(Set(CreateArray(length, _a, _b), _e, matchPositions), _a, _b));
            Assert.Equal(matchPositions[^1], LastIndexOfAnyExcept(Set(CreateArray(length, _a, _b, _c), _e, matchPositions), _a, _b, _c));
            Assert.Equal(matchPositions[^1], LastIndexOfAnyExcept(Set(CreateArray(length, _a, _b, _c, _d), _e, matchPositions), _a, _b, _c, _d));
        }

        [Fact]
        public void EmptyValues_ReturnsFirstValueFromSpan()
        {
            Assert.Equal(-1, IndexOfAnyExcept(Span<T>.Empty, Array.Empty<T>()));
            Assert.Equal(-1, LastIndexOfAnyExcept(Span<T>.Empty, Array.Empty<T>()));

            for (int i = 1; i <= 3; i++)
            {
                Assert.Equal(0, IndexOfAnyExcept(new T[i], Array.Empty<T>()));
                Assert.Equal(i - 1, LastIndexOfAnyExcept(new T[i], Array.Empty<T>()));
            }
        }

        protected abstract T Create(int value);

        private T[] CreateArray(int length, params T[] values)
        {
            var arr = new T[length];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = values[i % values.Length];
            }
            return arr;
        }

        private T[] Set(T[] arr, T value, params int[] valuePositions)
        {
            foreach (int pos in valuePositions)
            {
                arr[pos] = value;
            }
            return arr;
        }

        // Wrappers for {Last}IndexOfAnyExcept that invoke both the Span and ReadOnlySpan overloads,
        // as well as the values overloads, ensuring they all produce the same result, and returning that result.
        // This avoids needing to code the same call sites twice in all the above tests.
        private static int IndexOfAnyExcept(Span<T> span, T value)
        {
            int result = MemoryExtensions.IndexOfAnyExcept(span, value);
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, value));
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((Span<T>)span, new[] { value }));
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, new[] { value }));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).IndexOfAnyExcept(SearchValues.Create(Cast<byte>(new T[] { value }))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).IndexOfAnyExcept(SearchValues.Create(Cast<char>(new T[] { value }))));
            return result;
        }
        private static int IndexOfAnyExcept(Span<T> span, T value0, T value1)
        {
            int result = MemoryExtensions.IndexOfAnyExcept(span, value0, value1);
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1));
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((Span<T>)span, new[] { value0, value1 }));
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, new[] { value0, value1 }));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).IndexOfAnyExcept(SearchValues.Create(Cast<byte>(new T[] { value0, value1 }))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).IndexOfAnyExcept(SearchValues.Create(Cast<char>(new T[] { value0, value1 }))));
            return result;
        }
        private static int IndexOfAnyExcept(Span<T> span, T value0, T value1, T value2)
        {
            int result = MemoryExtensions.IndexOfAnyExcept(span, value0, value1, value2);
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1, value2));
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((Span<T>)span, new[] { value0, value1, value2 }));
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, new[] { value0, value1, value2 }));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).IndexOfAnyExcept(SearchValues.Create(Cast<byte>(new T[] { value0, value1, value2 }))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).IndexOfAnyExcept(SearchValues.Create(Cast<char>(new T[] { value0, value1, value2 }))));
            return result;
        }
        private static int IndexOfAnyExcept(Span<T> span, params T[] values)
        {
            int result = MemoryExtensions.IndexOfAnyExcept(span, values);
            Assert.Equal(result, MemoryExtensions.IndexOfAnyExcept((ReadOnlySpan<T>)span, values));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).IndexOfAnyExcept(SearchValues.Create(Cast<byte>(values))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).IndexOfAnyExcept(SearchValues.Create(Cast<char>(values))));
            return result;
        }
        private static int LastIndexOfAnyExcept(Span<T> span, T value)
        {
            int result = MemoryExtensions.LastIndexOfAnyExcept(span, value);
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, value));
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((Span<T>)span, new[] { value }));
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, new[] { value }));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<byte>(new T[] { value }))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<char>(new T[] { value }))));
            return result;
        }
        private static int LastIndexOfAnyExcept(Span<T> span, T value0, T value1)
        {
            int result = MemoryExtensions.LastIndexOfAnyExcept(span, value0, value1);
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1));
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((Span<T>)span, new[] { value0, value1 }));
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, new[] { value0, value1 }));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<byte>(new T[] { value0, value1 }))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<char>(new T[] { value0, value1 }))));
            return result;
        }
        private static int LastIndexOfAnyExcept(Span<T> span, T value0, T value1, T value2)
        {
            int result = MemoryExtensions.LastIndexOfAnyExcept(span, value0, value1, value2);
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, value0, value1, value2));
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((Span<T>)span, new[] { value0, value1, value2 }));
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, new[] { value0, value1, value2 }));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<byte>(new T[] { value0, value1, value2 }))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<char>(new T[] { value0, value1, value2 }))));
            return result;
        }
        private static int LastIndexOfAnyExcept(Span<T> span, params T[] values)
        {
            int result = MemoryExtensions.LastIndexOfAnyExcept(span, values);
            Assert.Equal(result, MemoryExtensions.LastIndexOfAnyExcept((ReadOnlySpan<T>)span, values));
            if (typeof(T) == typeof(byte)) Assert.Equal(result, Cast<byte>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<byte>(values))));
            if (typeof(T) == typeof(char)) Assert.Equal(result, Cast<char>(span).LastIndexOfAnyExcept(SearchValues.Create(Cast<char>(values))));
            return result;
        }

        private static ReadOnlySpan<TTo> Cast<TTo>(ReadOnlySpan<T> span) =>
            MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, TTo>(ref MemoryMarshal.GetReference(span)), span.Length);
    }
}
