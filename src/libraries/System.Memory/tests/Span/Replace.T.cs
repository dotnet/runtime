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
        public void ZeroLengthSpan()
        {
            Exception actual = Record.Exception(() => Span<T>.Empty.Replace(_oldValue, _newValue));

            Assert.Null(actual);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void AllElementsNeedToBeReplaced(int length)
        {
            Span<T> span = CreateArray(length, _oldValue);
            T[] expected = CreateArray(length, _newValue);

            span.Replace(_oldValue, _newValue);
            T[] actual = span.ToArray();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void DefaultToBeReplaced(int length)
        {
            Span<T> span = CreateArray(length);
            T[] expected = CreateArray(length, _newValue);

            span.Replace(default, _newValue);
            T[] actual = span.ToArray();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void NoElementsNeedToBeReplaced(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            T[] expected = span.ToArray();

            span.Replace(_oldValue, _newValue);
            T[] actual = span.ToArray();

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(Length_MemberData))]
        public void SomeElementsNeedToBeReplaced(int length)
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
        public void OldAndNewValueAreSame(int length)
        {
            T[] values = { Create('0'), Create('1') };

            Span<T> span = CreateArray(length, values);
            span[0] = _oldValue;
            span[^1] = _oldValue;
            T[] expected = span.ToArray();

            span.Replace(_oldValue, _oldValue);
            T[] actual = span.ToArray();

            Assert.Equal(expected, actual);
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
