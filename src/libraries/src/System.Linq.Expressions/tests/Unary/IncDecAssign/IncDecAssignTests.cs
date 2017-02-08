// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    public abstract class IncDecAssignTests
    {
        protected static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        public static IEnumerable<T?> NullableSequence<T>(IEnumerable<T> source) where T : struct
        {
            return source.Select(i => (T?)i).Concat(Enumerable.Repeat(default(T?), 1));
        }

        public static IEnumerable<short> Int16s
        {
            get
            {
                return new short[] { 0, 1, 2, short.MinValue, short.MaxValue };
            }
        }

        public static IEnumerable<object[]> Int16sAndDecrements()
        {
            return Int16s.Select(i => new object[] { typeof(short), i, unchecked((short)(i - 1)) });
        }

        public static IEnumerable<object[]> Int16sAndIncrements()
        {
            return Int16s.Select(i => new object[] { typeof(short), i, unchecked((short)(i + 1)) });
        }

        public static IEnumerable<short?> NullableInt16s
        {
            get { return NullableSequence(Int16s); }
        }

        public static IEnumerable<object[]> NullableInt16sAndDecrements()
        {
            return NullableInt16s.Select(i => new object[] { typeof(short?), i, unchecked((short?)(i - 1)) });
        }

        public static IEnumerable<object[]> NullableInt16sAndIncrements()
        {
            return NullableInt16s.Select(i => new object[] { typeof(short?), i, unchecked((short?)(i + 1)) });
        }

        public static IEnumerable<ushort> UInt16s
        {
            get
            {
                return new ushort[] { 0, 1, ushort.MaxValue };
            }
        }

        public static IEnumerable<object[]> UInt16sAndDecrements()
        {
            return UInt16s.Select(i => new object[] { typeof(ushort), i, unchecked((ushort)(i - 1)) });
        }

        public static IEnumerable<object[]> UInt16sAndIncrements()
        {
            return UInt16s.Select(i => new object[] { typeof(ushort), i, unchecked((ushort)(i + 1)) });
        }

        public static IEnumerable<ushort?> NullableUInt16s
        {
            get { return NullableSequence(UInt16s); }
        }

        public static IEnumerable<object[]> NullableUInt16sAndDecrements()
        {
            return NullableUInt16s.Select(i => new object[] { typeof(ushort?), i, unchecked((ushort?)(i - 1)) });
        }

        public static IEnumerable<object[]> NullableUInt16sAndIncrements()
        {
            return NullableUInt16s.Select(i => new object[] { typeof(ushort?), i, unchecked((ushort?)(i + 1)) });
        }

        public static IEnumerable<int> Int32s
        {
            get
            {
                return new[] { 0, 1, 2, int.MinValue, int.MaxValue };
            }
        }

        public static IEnumerable<object[]> Int32sAndDecrements()
        {
            return Int32s.Select(i => new object[] { typeof(int), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> Int32sAndIncrements()
        {
            return Int32s.Select(i => new object[] { typeof(int), i, unchecked(i + 1) });
        }

        public static IEnumerable<int?> NullableInt32s
        {
            get { return NullableSequence(Int32s); }
        }

        public static IEnumerable<object[]> NullableInt32sAndDecrements()
        {
            return NullableInt32s.Select(i => new object[] { typeof(int?), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> NullableInt32sAndIncrements()
        {
            return NullableInt32s.Select(i => new object[] { typeof(int?), i, unchecked(i + 1) });
        }

        public static IEnumerable<uint> UInt32s
        {
            get
            {
                return new[] { 0U, 1U, 2U, (uint)int.MaxValue, 1U + int.MaxValue, uint.MaxValue };
            }
        }

        public static IEnumerable<object[]> UInt32sAndDecrements()
        {
            return UInt32s.Select(i => new object[] { typeof(uint), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> UInt32sAndIncrements()
        {
            return UInt32s.Select(i => new object[] { typeof(uint), i, unchecked(i + 1) });
        }

        public static IEnumerable<uint?> NullableUInt32s
        {
            get { return NullableSequence(UInt32s); }
        }

        public static IEnumerable<object[]> NullableUInt32sAndDecrements()
        {
            return NullableUInt32s.Select(i => new object[] { typeof(uint?), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> NullableUInt32sAndIncrements()
        {
            return NullableUInt32s.Select(i => new object[] { typeof(uint?), i, unchecked(i + 1) });
        }

        public static IEnumerable<long> Int64s
        {
            get
            {
                return new[] { 0L, 1L, 2L, long.MinValue, long.MaxValue };
            }
        }

        public static IEnumerable<object[]> Int64sAndDecrements()
        {
            return Int64s.Select(i => new object[] { typeof(long), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> Int64sAndIncrements()
        {
            return Int64s.Select(i => new object[] { typeof(long), i, unchecked(i + 1) });
        }

        public static IEnumerable<long?> NullableInt64s
        {
            get { return NullableSequence(Int64s); }
        }

        public static IEnumerable<object[]> NullableInt64sAndDecrements()
        {
            return NullableInt64s.Select(i => new object[] { typeof(long?), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> NullableInt64sAndIncrements()
        {
            return NullableInt64s.Select(i => new object[] { typeof(long?), i, unchecked(i + 1) });
        }

        public static IEnumerable<ulong> UInt64s
        {
            get
            {
                return new[] { 0UL, 1UL, 2U, (ulong)long.MaxValue, 1UL + long.MaxValue, ulong.MaxValue };
            }
        }

        public static IEnumerable<object[]> UInt64sAndDecrements()
        {
            return UInt64s.Select(i => new object[] { typeof(ulong), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> UInt64sAndIncrements()
        {
            return UInt64s.Select(i => new object[] { typeof(ulong), i, unchecked(i + 1) });
        }

        public static IEnumerable<ulong?> NullableUInt64s
        {
            get { return NullableSequence(UInt64s); }
        }

        public static IEnumerable<object[]> NullableUInt64sAndDecrements()
        {
            return NullableUInt64s.Select(i => new object[] { typeof(ulong?), i, unchecked(i - 1) });
        }

        public static IEnumerable<object[]> NullableUInt64sAndIncrements()
        {
            return NullableUInt64s.Select(i => new object[] { typeof(ulong?), i, unchecked(i + 1) });
        }

        public static IEnumerable<decimal> Decimals
        {
            get
            {
                return new[] { 0m, 1m, -1m, decimal.MinValue + 1, decimal.MaxValue - 1 };
            }
        }

        public static IEnumerable<object[]> DecimalsAndDecrements()
        {
            return Decimals.Select(i => new object[] { typeof(decimal), i, i - 1 });
        }

        public static IEnumerable<object[]> DecimalsAndIncrements()
        {
            return Decimals.Select(i => new object[] { typeof(decimal), i, i + 1 });
        }

        public static IEnumerable<decimal?> NullableDecimals
        {
            get
            {
                return NullableSequence(Decimals);
            }
        }

        public static IEnumerable<object[]> NullableDecimalsAndDecrements()
        {
            return NullableDecimals.Select(i => new object[] { typeof(decimal?), i, i - 1 });
        }

        public static IEnumerable<object[]> NullableDecimalsAndIncrements()
        {
            return NullableDecimals.Select(i => new object[] { typeof(decimal?), i, i + 1 });
        }

        public static IEnumerable<float> Singles
        {
            get
            {
                return new[] { 0F, 1F, float.MinValue, float.MaxValue, float.NegativeInfinity, float.PositiveInfinity };
            }
        }

        public static IEnumerable<object[]> SinglesAndDecrements()
        {
            return Singles.Select(i => new object[] { typeof(float), i, i - 1 });
        }

        public static IEnumerable<object[]> SinglesAndIncrements()
        {
            return Singles.Select(i => new object[] { typeof(float), i, i + 1 });
        }

        public static IEnumerable<float?> NullableSingles
        {
            get { return NullableSequence(Singles); }
        }

        public static IEnumerable<object[]> NullableSinglesAndDecrements()
        {
            return NullableSingles.Select(i => new object[] { typeof(float?), i, i - 1 });
        }

        public static IEnumerable<object[]> NullableSinglesAndIncrements()
        {
            return NullableSingles.Select(i => new object[] { typeof(float?), i, i + 1 });
        }

        public static IEnumerable<double> Doubles
        {
            get
            {
                return new[] { 0F, 1F, double.MinValue, double.MaxValue, double.NegativeInfinity, double.PositiveInfinity };
            }
        }

        public static IEnumerable<object[]> DoublesAndDecrements()
        {
            return Doubles.Select(i => new object[] { typeof(double), i, i - 1 });
        }

        public static IEnumerable<object[]> DoublesAndIncrements()
        {
            return Doubles.Select(i => new object[] { typeof(double), i, i + 1 });
        }

        public static IEnumerable<double?> NullableDoubles
        {
            get { return NullableSequence(Doubles); }
        }

        public static IEnumerable<object[]> NullableDoublesAndDecrements()
        {
            return NullableDoubles.Select(i => new object[] { typeof(double?), i, i - 1 });
        }

        public static IEnumerable<object[]> NullableDoublesAndIncrements()
        {
            return NullableDoubles.Select(i => new object[] { typeof(double?), i, i + 1 });
        }

        public static IEnumerable<object[]> DecrementOverflowingValues()
        {
            yield return new object[] { decimal.MinValue };
        }

        public static IEnumerable<object[]> IncrementOverflowingValues()
        {
            yield return new object[] { decimal.MaxValue };
        }

        public static IEnumerable<object[]> UnincrementableAndUndecrementableTypes()
        {
            yield return new object[] { typeof(string) };
            yield return new object[] { typeof(DateTime) };
            yield return new object[] { typeof(Uri) };
            yield return new object[] { typeof(Tuple<string, int>) };
        }

        protected static string SillyMethod(string value)
        {
            return value == null ? null : "Eggplant";
        }

        protected static string GetString(int x)
        {
            return x.ToString();
        }

        protected class TestPropertyClass<T>
        {
            public static T TestStatic { get; set; }
            public T TestInstance { get; set; }
        }
    }
}
