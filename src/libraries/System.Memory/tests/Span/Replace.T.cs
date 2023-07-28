// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.SpanTests
{
    public class ReplaceTests_Byte : ReplaceTests<byte> { protected override byte Create(int value) => (byte)value; }
    public class ReplaceTests_Int16 : ReplaceTests<short> { protected override short Create(int value) => (short)value; }
    public class ReplaceTests_Int32 : ReplaceTests<int> { protected override int Create(int value) => value; }
    public class ReplaceTests_Int64 : ReplaceTests<long> { protected override long Create(int value) => value; }
    public class ReplaceTests_Char : ReplaceTests<char> { protected override char Create(int value) => (char)value; }
    public class ReplaceTests_Double : ReplaceTests<double> { protected override double Create(int value) => (double)value; }
    public class ReplaceTests_Record : ReplaceTests<SimpleRecord> { protected override SimpleRecord Create(int value) => new SimpleRecord(value); }
    public class ReplaceTests_CustomEquatable : ReplaceTests<CustomEquatable> { protected override CustomEquatable Create(int value) => new CustomEquatable((byte)value); }

    public class ReplaceTests_String : ReplaceTests<string>
    {
        protected override string Create(int value) => value.ToString();

        [Fact]
        public void NullOld_NonNullOriginal_CopiedCorrectly()
        {
            string[] orig = new string[] { "a", "b", "c" };
            string[] actual = new string[orig.Length];
            ((ReadOnlySpan<string>)orig).Replace(actual, null, "d");
            Assert.Equal(orig, actual);
        }

        [Fact]
        public void NullOld_NullOriginal_CopiedCorrectly()
        {
            string[] orig = new string[] { "a", null, "c" };
            string[] actual = new string[orig.Length];
            ((ReadOnlySpan<string>)orig).Replace(actual, null, "b");
            Assert.Equal(new string[] { "a", "b", "c" }, actual);
        }

        [Fact]
        public void NonNullOld_NullOriginal_CopiedCorrectly()
        {
            string[] orig = new string[] { "a", null, "c" };
            string[] actual = new string[orig.Length];
            ((ReadOnlySpan<string>)orig).Replace(actual, "d", "b");
            Assert.Equal(orig, actual);
        }
    }

    public readonly struct CustomEquatable : IEquatable<CustomEquatable>
    {
        public byte Value { get; }

        public CustomEquatable(byte value) => Value = value;

        public bool Equals(CustomEquatable other) => other.Value == Value;
    }

    public abstract class ReplaceTests<T> where T : IEquatable<T>
    {
        private readonly T _oldValue;
        private readonly T _newValue;

        protected ReplaceTests()
        {
            _oldValue = Create('a');
            _newValue = Create('b');
        }

        [Fact]
        public void ZeroLengthSpan_InPlace()
        {
            Span<T>.Empty.Replace(_oldValue, _newValue);
        }

        [Fact]
        public void ZeroLengthSpan_Copy()
        {
            ReadOnlySpan<T>.Empty.Replace(Span<T>.Empty, _oldValue, _newValue);
        }

        [Fact]
        public void ArgumentValidation_Copy()
        {
            AssertExtensions.Throws<ArgumentException>("destination", () => ((ReadOnlySpan<T>)new T[] { _oldValue }).Replace(Span<T>.Empty, _oldValue, _newValue));

            T[] values = new T[] { _oldValue, _oldValue, _oldValue };
            AssertExtensions.Throws<ArgumentException>(null, () => new ReadOnlySpan<T>(values, 0, 2).Replace(new Span<T>(values, 1, 2), _oldValue, _newValue));
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void AllElementsNeedToBeReplaced_InPlace(int length)
        {
            Span<T> span = CreateArray(length, _oldValue);

            span.Replace(_oldValue, _newValue);

            Assert.Equal(CreateArray(length, _newValue), span.ToArray());
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void AllElementsNeedToBeReplaced_Copy(int length)
        {
            ReadOnlySpan<T> span = CreateArray(length, _oldValue);
            T[] original = span.ToArray();

            T[] destination = new T[span.Length];
            span.Replace(destination, _oldValue, _newValue);
            Assert.Equal(CreateArray(length, _newValue), destination);
            Assert.Equal(original, span.ToArray());

            destination = new T[span.Length + 1];
            span.Replace(destination, _oldValue, _newValue);
            Assert.Equal(CreateArray(length, _newValue), destination[0..^1]);
            Assert.Equal(default, destination[^1]);
            Assert.Equal(original, span.ToArray());
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void DefaultToBeReplaced_InPlace(int length)
        {
            Span<T> span = CreateArray(length);

            span.Replace(default, _newValue);

            Assert.Equal(CreateArray(length, _newValue), span.ToArray());
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void DefaultToBeReplaced_Copy(int length)
        {
            ReadOnlySpan<T> span = CreateArray(length);
            T[] original = span.ToArray();

            T[] destination = new T[span.Length];
            span.Replace(destination, default, _newValue);

            Assert.Equal(original, span.ToArray());
            Assert.Equal(CreateArray(length, _newValue), destination);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void NoElementsNeedToBeReplaced_InPlace(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            T[] original = span.ToArray();

            span.Replace(_oldValue, _newValue);

            Assert.Equal(original, span.ToArray());
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void NoElementsNeedToBeReplaced_Copy(int length)
        {
            T[] values = { Create('0'), Create('1') };

            ReadOnlySpan<T> span = CreateArray(length, values);
            T[] original = span.ToArray();

            T[] destination = span.ToArray();
            span.Replace(destination, _oldValue, _newValue);

            Assert.Equal(original, destination);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void SomeElementsNeedToBeReplaced_InPlace(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            span[0] = _oldValue;
            span[^1] = _oldValue;

            T[] expected = CreateArray(length, values);
            expected[0] = _newValue;
            expected[^1] = _newValue;

            span.Replace(_oldValue, _newValue);
            T[] actual = span.ToArray();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void SomeElementsNeedToBeReplaced_Copy(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            span[0] = _oldValue;
            span[^1] = _oldValue;
            T[] original = span.ToArray();

            T[] expected = CreateArray(length, values);
            expected[0] = _newValue;
            expected[^1] = _newValue;

            T[] destination = new T[expected.Length];
            ((ReadOnlySpan<T>)span).Replace(destination, _oldValue, _newValue);

            Assert.Equal(original, span.ToArray());
            Assert.Equal(expected, destination);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void OldAndNewValueAreSame_InPlace(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            span[0] = _oldValue;
            span[^1] = _oldValue;
            T[] expected = span.ToArray();

            span.Replace(_oldValue, _oldValue);

            Assert.Equal(expected, span.ToArray());
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void OldAndNewValueAreSame_Copy(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            span[0] = _oldValue;
            span[^1] = _oldValue;
            T[] expected = span.ToArray();

            T[] destination = new T[expected.Length];
            ((ReadOnlySpan<T>)span).Replace(destination, _oldValue, _oldValue);

            Assert.Equal(expected, span.ToArray());
            Assert.Equal(expected, destination);
        }

        public static IEnumerable<object[]> Length_MemberData()
        {
            foreach (int length in new[] { 1, 2, 4, 7, 15, 16, 17, 31, 32, 33, 100 })
            {
                yield return new object[] { length };
            }
        }

        protected abstract T Create(int value);

        private T[] CreateArray(int length, params T[] values)
        {
            var arr = new T[length];

            if (values.Length > 0)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = values[i % values.Length];
                }
            }

            return arr;
        }
    }
}
