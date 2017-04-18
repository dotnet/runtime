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
        #region Shift Tests

        public static IEnumerable<object> Shift_Data()
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

        public static IEnumerable<object> RightShift_Hidden_Data()
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

