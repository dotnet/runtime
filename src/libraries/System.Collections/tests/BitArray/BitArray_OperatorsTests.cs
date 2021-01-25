// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public static class BitArray_OperatorsTests
    {
        private const int BitsPerByte = 8;
        private const int BitsPerInt32 = 32;

        public static IEnumerable<object[]> Not_Operator_Data()
        {
            foreach (int size in new[] { 0, 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, BitsPerInt32 * 3, BitsPerInt32 * 4, BitsPerInt32 * 5, BitsPerInt32 * 6, BitsPerInt32 * 7, BitsPerInt32 * 8, BitsPerInt32 * 8 + BitsPerInt32 - 1, short.MaxValue })
            {
                yield return new object[] { Enumerable.Repeat(true, size).ToArray() };
                yield return new object[] { Enumerable.Repeat(false, size).ToArray() };
                yield return new object[] { Enumerable.Range(0, size).Select(i => i % 2 == 1).ToArray() };
            }
        }

        [Theory]
        [MemberData(nameof(Not_Operator_Data))]
        public static void Not(bool[] data)
        {
            BitArray bitArray = new BitArray(data);

            BitArray bitArrayNot = bitArray.Not();
            Assert.Equal(bitArray.Length, bitArrayNot.Length);
            Assert.Same(bitArray, bitArrayNot);
            for (int i = 0; i < bitArray.Length; i++)
            {
                Assert.Equal(!data[i], bitArrayNot[i]);
            }
        }

        public static IEnumerable<object[]> And_Operator_Data()
        {
            yield return new object[] { new bool[0], new bool[0], new bool[0] };
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, BitsPerInt32 * 3, BitsPerInt32 * 4, BitsPerInt32 * 5, BitsPerInt32 * 6, BitsPerInt32 * 7, BitsPerInt32 * 8, BitsPerInt32 * 8 + BitsPerInt32 - 1, short.MaxValue })
            {
                bool[] allTrue = Enumerable.Repeat(true, size).ToArray();
                bool[] allFalse = Enumerable.Repeat(false, size).ToArray();
                bool[] alternating = Enumerable.Range(0, size).Select(i => i % 2 == 1).ToArray();
                yield return new object[] { allTrue, allTrue, allTrue };
                yield return new object[] { allTrue, allFalse, allFalse };
                yield return new object[] { allFalse, allTrue, allFalse };
                yield return new object[] { allFalse, allFalse, allFalse };
                yield return new object[] { allTrue, alternating, alternating };
                yield return new object[] { alternating, allTrue, alternating };
                yield return new object[] { allFalse, alternating, allFalse };
                yield return new object[] { alternating, allFalse, allFalse };
            }
        }

        [Theory]
        [MemberData(nameof(And_Operator_Data))]
        public static void And_Operator(bool[] l, bool[] r, bool[] expected)
        {
            BitArray left = new BitArray(l);
            BitArray right = new BitArray(r);

            BitArray actual = left.And(right);
            Assert.Same(left, actual);
            Assert.Equal(actual.Length, expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        public static IEnumerable<object[]> Or_Operator_Data()
        {
            yield return new object[] { new bool[0], new bool[0], new bool[0] };
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, BitsPerInt32 * 3, BitsPerInt32 * 4, BitsPerInt32 * 5, BitsPerInt32 * 6, BitsPerInt32 * 7, BitsPerInt32 * 8, BitsPerInt32 * 8 + BitsPerInt32 - 1, short.MaxValue })
            {
                bool[] allTrue = Enumerable.Repeat(true, size).ToArray();
                bool[] allFalse = Enumerable.Repeat(false, size).ToArray();
                bool[] alternating = Enumerable.Range(0, size).Select(i => i % 2 == 1).ToArray();
                yield return new object[] { allTrue, allTrue, allTrue };
                yield return new object[] { allTrue, allFalse, allTrue };
                yield return new object[] { allFalse, allTrue, allTrue };
                yield return new object[] { allFalse, allFalse, allFalse };
                yield return new object[] { allTrue, alternating, allTrue };
                yield return new object[] { alternating, allTrue, allTrue };
                yield return new object[] { allFalse, alternating, alternating };
                yield return new object[] { alternating, allFalse, alternating };
            }
        }

        [Theory]
        [MemberData(nameof(Or_Operator_Data))]
        public static void Or_Operator(bool[] l, bool[] r, bool[] expected)
        {
            BitArray left = new BitArray(l);
            BitArray right = new BitArray(r);

            BitArray actual = left.Or(right);
            Assert.Same(left, actual);
            Assert.Equal(actual.Length, expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        public static IEnumerable<object[]> Xor_Operator_Data()
        {
            yield return new object[] { new bool[0], new bool[0], new bool[0] };
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, BitsPerInt32 * 3, BitsPerInt32 * 4, BitsPerInt32 * 5, BitsPerInt32 * 6, BitsPerInt32 * 7, BitsPerInt32 * 8, BitsPerInt32 * 8 + BitsPerInt32 - 1, short.MaxValue })
            {
                bool[] allTrue = Enumerable.Repeat(true, size).ToArray();
                bool[] allFalse = Enumerable.Repeat(false, size).ToArray();
                bool[] alternating = Enumerable.Range(0, size).Select(i => i % 2 == 1).ToArray();
                bool[] inverse = Enumerable.Range(0, size).Select(i => i % 2 == 0).ToArray();
                yield return new object[] { allTrue, allTrue, allFalse };
                yield return new object[] { allTrue, allFalse, allTrue };
                yield return new object[] { allFalse, allTrue, allTrue };
                yield return new object[] { allFalse, allFalse, allFalse };
                yield return new object[] { allTrue, alternating, inverse };
                yield return new object[] { alternating, allTrue, inverse };
                yield return new object[] { allFalse, alternating, alternating };
                yield return new object[] { alternating, allFalse, alternating };
                yield return new object[] { alternating, inverse, allTrue };
                yield return new object[] { inverse, alternating, allTrue };
            }
        }

        [Theory]
        [MemberData(nameof(Xor_Operator_Data))]
        public static void Xor_Operator(bool[] l, bool[] r, bool[] expected)
        {
            BitArray left = new BitArray(l);
            BitArray right = new BitArray(r);

            BitArray actual = left.Xor(right);
            Assert.Same(left, actual);
            Assert.Equal(actual.Length, expected.Length);

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public static void And_Invalid()
        {
            BitArray bitArray1 = new BitArray(11, false);
            BitArray bitArray2 = new BitArray(6, false);

            // Different lengths
            AssertExtensions.Throws<ArgumentException>(null, () => bitArray1.And(bitArray2));
            AssertExtensions.Throws<ArgumentException>(null, () => bitArray2.And(bitArray1));

            AssertExtensions.Throws<ArgumentNullException>("value", () => bitArray1.And(null));
        }

        [Fact]
        public static void Or_Invalid()
        {
            BitArray bitArray1 = new BitArray(11, false);
            BitArray bitArray2 = new BitArray(6, false);

            // Different lengths
            AssertExtensions.Throws<ArgumentException>(null, () => bitArray1.Or(bitArray2));
            AssertExtensions.Throws<ArgumentException>(null, () => bitArray2.Or(bitArray1));

            AssertExtensions.Throws<ArgumentNullException>("value", () => bitArray1.Or(null));
        }

        [Fact]
        public static void Xor_Invalid()
        {
            BitArray bitArray1 = new BitArray(11, false);
            BitArray bitArray2 = new BitArray(6, false);

            // Different lengths
            AssertExtensions.Throws<ArgumentException>(null, () => bitArray1.Xor(bitArray2));
            AssertExtensions.Throws<ArgumentException>(null, () => bitArray2.Xor(bitArray1));

            AssertExtensions.Throws<ArgumentNullException>("value", () => bitArray1.Xor(null));
        }

        public static IEnumerable<object[]> Resize_TestData()
        {
            yield return new object[] { new BitArray(32), new BitArray(64), 32, 32 };
            yield return new object[] { new BitArray(32), new BitArray(64), 64, 64 };
            yield return new object[] { new BitArray(64), new BitArray(32), 32, 32 };
            yield return new object[] { new BitArray(64), new BitArray(32), 64, 64 };
            yield return new object[] { new BitArray(288), new BitArray(32), 288, 288 };
            yield return new object[] { new BitArray(288), new BitArray(32), 32, 32 };
            yield return new object[] { new BitArray(32), new BitArray(288), 288, 288 };
            yield return new object[] { new BitArray(32), new BitArray(288), 32, 32 };
            yield return new object[] { new BitArray(1), new BitArray(9), 9, 9 };
            yield return new object[] { new BitArray(1), new BitArray(9), 1, 1 };
            yield return new object[] { new BitArray(9), new BitArray(1), 9, 9 };
            yield return new object[] { new BitArray(9), new BitArray(1), 1, 1 };
            yield return new object[] { new BitArray(287), new BitArray(32), 32, 32 };
            yield return new object[] { new BitArray(287), new BitArray(32), 287, 287 };
            yield return new object[] { new BitArray(32), new BitArray(287), 32, 32 };
            yield return new object[] { new BitArray(32), new BitArray(287), 287, 287 };
            yield return new object[] { new BitArray(289), new BitArray(32), 289, 289 };
            yield return new object[] { new BitArray(289), new BitArray(32), 32, 32 };
            yield return new object[] { new BitArray(32), new BitArray(289), 289, 289 };
            yield return new object[] { new BitArray(32), new BitArray(289), 32, 32 };
        }

        [Theory]
        [MemberData(nameof(Resize_TestData))]
        public static void And_With_Resize(BitArray left, BitArray right, int newLeftLength, int newRightLength)
        {
            left.Length = newLeftLength;
            right.Length = newRightLength;

            left.And(right);
        }

        [Theory]
        [MemberData(nameof(Resize_TestData))]
        public static void Or_With_Resize(BitArray left, BitArray right, int newLeftLength, int newRightLength)
        {
            left.Length = newLeftLength;
            right.Length = newRightLength;

            left.Or(right);
        }

        [Theory]
        [MemberData(nameof(Resize_TestData))]
        public static void Xor_With_Resize(BitArray left, BitArray right, int newLeftLength, int newRightLength)
        {
            left.Length = newLeftLength;
            right.Length = newRightLength;

            left.Xor(right);
        }

        #region Shift Tests

        public static IEnumerable<object[]> Shift_Data()
        {
            foreach (int size in new[] { 0, 1, BitsPerInt32 / 2, BitsPerInt32, BitsPerInt32 + 1, 2 * BitsPerInt32, 2 * BitsPerInt32 + 1 })
            {
                foreach (int shift in new[] { 0, 1, size / 2, size - 1, size }.Where(s => s >= 0).Distinct())
                {
                    yield return new object[] { size, new int[] { /* deliberately empty */ }, shift };
                    yield return new object[] { size, Enumerable.Range(0, size), shift };

                    if (size > 1)
                    {
                        foreach (int position in new[] { 0, size / 2, size - 1 })
                        {
                            yield return new object[] { size, new[] { position }, shift };
                        }
                        yield return new object[] { size, new[] { 0, size / 2, size - 1 }, shift };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(Shift_Data))]
        public static void RightShift(int length, IEnumerable<int> set, int shift)
        {
            BitArray ba = new BitArray(GetBoolArray(length, set));
            bool[] expected = GetBoolArray(length, set.Select(i => i - shift).Where(i => i >= 0));

            BitArray returned = ba.RightShift(shift);
            Assert.Same(ba, returned);

            int index = 0;
            Assert.All(ba.Cast<bool>(), bit => Assert.Equal(expected[index++], bit));
        }

        [Theory]
        [MemberData(nameof(Shift_Data))]
        public static void LeftShift(int length, IEnumerable<int> set, int shift)
        {
            BitArray ba = new BitArray(GetBoolArray(length, set));
            bool[] expected = GetBoolArray(length, set.Select(i => i + shift).Where(i => i < length));

            BitArray returned = ba.LeftShift(shift);
            Assert.Same(ba, returned);

            int index = 0;
            Assert.All(ba.Cast<bool>(), bit => Assert.Equal(expected[index++], bit));
        }

        private static bool[] GetBoolArray(int length, IEnumerable<int> set)
        {
            bool[] b = new bool[length];
            foreach (int position in set)
            {
                b[position] = true;
            }
            return b;
        }

        [Fact]
        public static void LeftShift_Iterator()
        {
            BitArray ba = new BitArray(BitsPerInt32 / 2);
            IEnumerator enumerator = ba.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            ba.LeftShift(1);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        [Fact]
        public static void LeftShift_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            BitArray bitArray = new BitArray(1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => bitArray.LeftShift(-1));
        }

        [Fact]
        public static void RightShift_Iterator()
        {
            BitArray ba = new BitArray(BitsPerInt32 / 2);
            IEnumerator enumerator = ba.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            ba.RightShift(1);
            Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        }

        public static IEnumerable<object[]> RightShift_Hidden_Data()
        {
            yield return new object[] { "Constructor", Unset_Visible_Bits(new BitArray(BitsPerInt32 / 2, true)) };
            yield return new object[] { "Not", Unset_Visible_Bits(new BitArray(BitsPerInt32 / 2, false).Not()) };
            BitArray setAll = new BitArray(BitsPerInt32 / 2, false);
            setAll.SetAll(true);
            yield return new object[] { "SetAll", Unset_Visible_Bits(setAll) };
            BitArray lengthShort = new BitArray(BitsPerInt32, true);
            lengthShort.Length = BitsPerInt32 / 2;
            yield return new object[] { "Length-Short", Unset_Visible_Bits(lengthShort) };
            BitArray lengthLong = new BitArray(2 * BitsPerInt32, true);
            lengthLong.Length = BitsPerInt32;
            yield return new object[] { "Length-Long", Unset_Visible_Bits(lengthLong) };
            BitArray leftShift = new BitArray(BitsPerInt32 / 2);
            for (int i = 0; i < leftShift.Length; i++) leftShift[i] = true;
            yield return new object[] { "LeftShift", leftShift.LeftShift(BitsPerInt32 / 2) };
        }

        private static BitArray Unset_Visible_Bits(BitArray ba)
        {
            for (int i = 0; i < ba.Length; i++) ba[i] = false;
            return ba;
        }

        [Theory]
        [MemberData(nameof(RightShift_Hidden_Data))]
        public static void RightShift_Hidden(string label, BitArray bits)
        {
            _ = label;

            Assert.All(bits.Cast<bool>(), bit => Assert.False(bit));
            bits.RightShift(1);
            Assert.All(bits.Cast<bool>(), bit => Assert.False(bit));
        }

        [Fact]
        public static void RightShift_NegativeCount_ThrowsArgumentOutOfRangeException()
        {
            BitArray bitArray = new BitArray(1);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => bitArray.RightShift(-1));
        }

        #endregion
    }
}
