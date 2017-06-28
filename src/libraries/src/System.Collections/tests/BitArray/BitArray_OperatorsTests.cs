// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public static partial class BitArray_OperatorsTests
    {
        private const int BitsPerByte = 8;
        private const int BitsPerInt32 = 32;

        public static IEnumerable<object[]> Not_Operator_Data()
        {
            foreach (int size in new[] { 0, 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, short.MaxValue })
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
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, short.MaxValue })
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
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, short.MaxValue })
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
            foreach (int size in new[] { 1, BitsPerByte, BitsPerByte * 2, BitsPerInt32, BitsPerInt32 * 2, short.MaxValue })
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
    }
}

