// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public static class BitVector32Tests
    {
        /// <summary>
        /// Data used for testing setting/unsetting multiple bits at a time.
        /// </summary>
        /// Format is:
        ///  1. Set data
        ///  2. Unset data
        ///  3. Bits to flip for transformation.
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> Mask_SetUnset_Multiple_Data()
        {
            yield return new object[] { 0, 0, new int[] { } };
            yield return new object[] { 1, 0, new int[] { 1 } };
            yield return new object[] { 2, 0, new int[] { 2 } };
            yield return new object[] { int.MinValue, 0, new int[] { 32 } };
            yield return new object[] { 6, 0, new int[] { 2, 3 } };
            yield return new object[] { 6, 6, new int[] { 2, 3 } };
            yield return new object[] { 31, 15, new int[] { 4 } };
            yield return new object[] { 22, 16, new int[] { 2, 3 } };
            yield return new object[] { -1, 0, new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 } };
        }

        /// <summary>
        /// Data used for testing creating sections.
        /// </summary>
        /// Format is:
        ///  1. maximum value allowed
        ///  2. resulting mask
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> Section_Create_Data()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 2, 3 };
            yield return new object[] { 3, 3 };
            yield return new object[] { 16, 31 };
            yield return new object[] { byte.MaxValue, byte.MaxValue };
            yield return new object[] { short.MaxValue, short.MaxValue };
            yield return new object[] { short.MaxValue - 1, short.MaxValue };
        }

        /// <summary>
        /// Data used for testing setting/unsetting via sections.
        /// </summary>
        /// Format is:
        ///  1. value
        ///  2. section
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> Section_Set_Data()
        {
            yield return new object[] { 0, BitVector32.CreateSection(1) };
            yield return new object[] { 1, BitVector32.CreateSection(1) };
            yield return new object[] { 0, BitVector32.CreateSection(short.MaxValue) };
            yield return new object[] { short.MaxValue, BitVector32.CreateSection(short.MaxValue) };
            yield return new object[] { 0, BitVector32.CreateSection(1, BitVector32.CreateSection(byte.MaxValue)) };
            yield return new object[] { 1, BitVector32.CreateSection(1, BitVector32.CreateSection(byte.MaxValue)) };
            yield return new object[] { 0, BitVector32.CreateSection(short.MaxValue, BitVector32.CreateSection(byte.MaxValue)) };
            yield return new object[] { short.MaxValue, BitVector32.CreateSection(short.MaxValue, BitVector32.CreateSection(byte.MaxValue)) };
            yield return new object[] { 16, BitVector32.CreateSection(short.MaxValue) };
            yield return new object[] { 16, BitVector32.CreateSection(short.MaxValue, BitVector32.CreateSection(byte.MaxValue)) };
            yield return new object[] { 31, BitVector32.CreateSection(short.MaxValue) };
            yield return new object[] { 31, BitVector32.CreateSection(short.MaxValue, BitVector32.CreateSection(byte.MaxValue)) };
            yield return new object[] { 16, BitVector32.CreateSection(byte.MaxValue) };
            yield return new object[] { 16, BitVector32.CreateSection(byte.MaxValue, BitVector32.CreateSection(byte.MaxValue, BitVector32.CreateSection(short.MaxValue))) };
            yield return new object[] { 31, BitVector32.CreateSection(byte.MaxValue) };
            yield return new object[] { 31, BitVector32.CreateSection(byte.MaxValue, BitVector32.CreateSection(byte.MaxValue, BitVector32.CreateSection(short.MaxValue))) };
        }

        /// <summary>
        /// Data used for testing equal sections.
        /// </summary>
        /// Format is:
        ///  1. Section left
        ///  2. Section right
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> Section_Equal_Data()
        {
            BitVector32.Section original = BitVector32.CreateSection(16);
            BitVector32.Section nested = BitVector32.CreateSection(16, original);

            yield return new object[] { original, original };
            yield return new object[] { original, BitVector32.CreateSection(16) };
            yield return new object[] { BitVector32.CreateSection(16), original };
            // Since the max value is changed to an inclusive mask, equal to mask max value
            yield return new object[] { original, BitVector32.CreateSection(31) };
            yield return new object[] { nested, nested };
            yield return new object[] { nested, BitVector32.CreateSection(16, original) };
            yield return new object[] { BitVector32.CreateSection(16, original), nested };
            yield return new object[] { nested, BitVector32.CreateSection(31, original) };
            yield return new object[] { nested, BitVector32.CreateSection(16, BitVector32.CreateSection(16)) };
            yield return new object[] { BitVector32.CreateSection(16, BitVector32.CreateSection(16)), nested };
            yield return new object[] { nested, BitVector32.CreateSection(31, BitVector32.CreateSection(16)) };
            // Because it only stores the offset, and not the previous section, later sections may be equal
            yield return new object[] { nested, BitVector32.CreateSection(16, BitVector32.CreateSection(8, BitVector32.CreateSection(1))) };
            yield return new object[] { BitVector32.CreateSection(16, BitVector32.CreateSection(8, BitVector32.CreateSection(1))), nested };
        }

        /// <summary>
        /// Data used for testing unequal sections.
        /// </summary>
        /// Format is:
        ///  1. Section left
        ///  2. Section right
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> Section_Unequal_Data()
        {
            BitVector32.Section original = BitVector32.CreateSection(16);
            BitVector32.Section nested = BitVector32.CreateSection(16, original);

            yield return new object[] { original, BitVector32.CreateSection(1) };
            yield return new object[] { BitVector32.CreateSection(1), original };
            yield return new object[] { original, nested };
            yield return new object[] { nested, original };
            yield return new object[] { nested, BitVector32.CreateSection(1, BitVector32.CreateSection(short.MaxValue)) };
            yield return new object[] { BitVector32.CreateSection(1, BitVector32.CreateSection(short.MaxValue)), nested };
            yield return new object[] { nested, BitVector32.CreateSection(16, BitVector32.CreateSection(short.MaxValue)) };
            yield return new object[] { BitVector32.CreateSection(16, BitVector32.CreateSection(short.MaxValue)), nested };
            yield return new object[] { nested, BitVector32.CreateSection(1, original) };
            yield return new object[] { BitVector32.CreateSection(1, original), nested };
        }

        [Fact]
        public static void Constructor_DefaultTest()
        {
            BitVector32 bv = new BitVector32();
            Assert.NotNull(bv);
            Assert.Equal(0, bv.Data);

            // Copy constructor results in item with same data.
            BitVector32 copied = new BitVector32(bv);
            Assert.NotNull(bv);
            Assert.Equal(0, copied.Data);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(99)]
        [InlineData(byte.MaxValue)]
        [InlineData(int.MaxValue / 2)]
        [InlineData(int.MaxValue - 1)]
        [InlineData(int.MaxValue)]
        [InlineData(-1)]
        [InlineData(-2)]
        [InlineData(-7)]
        [InlineData(-99)]
        [InlineData(byte.MinValue)]
        [InlineData(int.MinValue / 2)]
        [InlineData(int.MinValue + 1)]
        [InlineData(int.MinValue)]
        public static void Constructor_DataTest(int data)
        {
            BitVector32 bv = new BitVector32(data);
            Assert.NotNull(bv);
            Assert.Equal(data, bv.Data);

            // Copy constructor results in item with same data.
            BitVector32 copied = new BitVector32(bv);
            Assert.NotNull(bv);
            Assert.Equal(data, copied.Data);
        }

        [Fact]
        public static void Mask_DefaultTest()
        {
            Assert.Equal(1, BitVector32.CreateMask());
            Assert.Equal(1, BitVector32.CreateMask(0));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(6, 3)]
        [InlineData(short.MaxValue + 1, 1 << 14)]
        [InlineData(-2, int.MaxValue)]
        [InlineData(-2, -1)]
        // Works even if the mask has multiple bits set, which probably wasn't the intended use.
        public static void Mask_SeriesTest(int expected, int actual)
        {
            while (actual != int.MinValue)
            {
                actual = BitVector32.CreateMask(actual);
                Assert.Equal(expected, actual);
                expected <<= 1;
            }

            Assert.Equal(int.MinValue, actual);
        }

        [Fact]
        public static void Mask_LastTest()
        {
            Assert.Throws<InvalidOperationException>(() => BitVector32.CreateMask(int.MinValue));
        }

        [Fact]
        public static void Get_Mask_AllSetTest()
        {
            BitVector32 all = new BitVector32(-1);
            int mask = 0;
            for (int count = 0; count < 32; count++)
            {
                mask = BitVector32.CreateMask(mask);
                Assert.True(all[mask]);
            }
            Assert.Equal(int.MinValue, mask);
        }

        [Fact]
        public static void Get_Mask_NoneSetTest()
        {
            BitVector32 none = new BitVector32();
            int mask = 0;
            for (int count = 0; count < 32; count++)
            {
                mask = BitVector32.CreateMask(mask);
                Assert.False(none[mask]);
            }
            Assert.Equal(int.MinValue, mask);
        }

        [Fact]
        public static void Get_Mask_SomeSetTest()
        {
            // Constructs data with every even bit set.
            int data = Enumerable.Range(0, 16).Sum(x => 1 << (x * 2));
            BitVector32 some = new BitVector32(data);
            int mask = 0;
            for (int index = 0; index < 32; index++)
            {
                mask = BitVector32.CreateMask(mask);
                Assert.Equal(index % 2 == 0, some[mask]);
            }
            Assert.Equal(int.MinValue, mask);
        }

        [Fact]
        public static void Set_Mask_EmptyTest()
        {
            BitVector32 nothing = new BitVector32();
            nothing[0] = true;
            Assert.Equal(0, nothing.Data);

            BitVector32 all = new BitVector32(-1);
            all[0] = false;
            Assert.Equal(-1, all.Data);
        }

        [Fact]
        public static void Set_Mask_AllTest()
        {
            BitVector32 flip = new BitVector32();
            int mask = 0;
            for (int bit = 1; bit < 32 + 1; bit++)
            {
                mask = BitVector32.CreateMask(mask);

                BitVector32 single = new BitVector32();
                Assert.False(single[mask]);
                single[mask] = true;
                Assert.True(single[mask]);
                Assert.Equal(1 << (bit - 1), single.Data);

                flip[mask] = true;
            }
            Assert.Equal(-1, flip.Data);
            Assert.Equal(int.MinValue, mask);
        }

        [Fact]
        public static void Set_Mask_UnsetAllTest()
        {
            BitVector32 flip = new BitVector32(-1);
            int mask = 0;
            for (int bit = 1; bit < 32 + 1; bit++)
            {
                mask = BitVector32.CreateMask(mask);

                BitVector32 single = new BitVector32(1 << (bit - 1));
                Assert.True(single[mask]);
                single[mask] = false;
                Assert.False(single[mask]);
                Assert.Equal(0, single.Data);

                flip[mask] = false;
            }
            Assert.Equal(0, flip.Data);
            Assert.Equal(int.MinValue, mask);
        }

        [Theory]
        [MemberData(nameof(Mask_SetUnset_Multiple_Data))]
        public static void Set_Mask_MultipleTest(int expected, int start, int[] maskPositions)
        {
            int mask = maskPositions.Sum(x => 1 << (x - 1));

            BitVector32 blank = new BitVector32();

            BitVector32 set = new BitVector32();
            set[mask] = true;

            for (int bit = 0; bit < 32; bit++)
            {
                Assert.False(blank[1 << bit]);
                bool willSet = maskPositions.Contains(bit + 1);
                blank[1 << bit] = willSet;
                Assert.Equal(willSet, blank[1 << bit]);
                Assert.Equal(willSet, set[1 << bit]);
            }
            Assert.Equal(set, blank);
        }

        [Theory]
        [MemberData(nameof(Mask_SetUnset_Multiple_Data))]
        public static void Set_Mask_Multiple_UnsetTest(int start, int expected, int[] maskPositions)
        {
            int mask = maskPositions.Sum(x => 1 << (x - 1));

            BitVector32 set = new BitVector32();
            set[mask] = true;

            for (int bit = 0; bit < 32; bit++)
            {
                bool willUnset = maskPositions.Contains(bit + 1);
                Assert.Equal(willUnset, set[1 << bit]);
                set[1 << bit] = false;
                Assert.False(set[1 << bit]);
            }
            Assert.Equal(set, new BitVector32());
        }

        [Theory]
        [MemberData(nameof(Section_Set_Data))]
        public static void Set_SectionTest(int value, BitVector32.Section section)
        {
            BitVector32 empty = new BitVector32();
            empty[section] = value;
            Assert.Equal(value, empty[section]);
            Assert.Equal(value << section.Offset, empty.Data);

            BitVector32 full = new BitVector32(-1);
            full[section] = value;
            Assert.Equal(value, full[section]);
            int offsetMask = section.Mask << section.Offset;
            Assert.Equal((-1 & ~offsetMask) | (value << section.Offset), full.Data);
        }

        [Theory]
        [InlineData(short.MaxValue, int.MaxValue)]
        [InlineData(1, 2)]
        [InlineData(1, short.MaxValue)]
        [InlineData(1, -1)]
        [InlineData(short.MaxValue, short.MinValue)]
        public static void Set_Section_OutOfRangeTest(short maximum, int value)
        {
            {
                BitVector32 data = new BitVector32();
                BitVector32.Section section = BitVector32.CreateSection(maximum);

                data[section] = value;
                Assert.Equal(maximum & value, data.Data);
                Assert.NotEqual(value, data.Data);
                Assert.Equal(maximum & value, data[section]);
                Assert.NotEqual(value, data[section]);
            }
            {
                BitVector32 data = new BitVector32();
                BitVector32.Section nested = BitVector32.CreateSection(maximum, BitVector32.CreateSection(short.MaxValue));

                data[nested] = value;
                Assert.Equal((maximum & value) << 15, data.Data);
                Assert.NotEqual(value << 15, data.Data);
                Assert.Equal(maximum & value, data[nested]);
                Assert.NotEqual(value, data[nested]);
            }
        }

        [Theory]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(short.MaxValue)]
        [InlineData(byte.MaxValue)]
        // Regardless of mask size, values will be truncated if they hang off the end
        public static void Set_Section_OverflowTest(int value)
        {
            BitVector32 data = new BitVector32();
            BitVector32.Section offset = BitVector32.CreateSection(short.MaxValue, BitVector32.CreateSection(short.MaxValue));
            BitVector32.Section final = BitVector32.CreateSection(short.MaxValue, offset);

            data[final] = value;
            Assert.Equal((3 & value) << 30, data.Data);
            Assert.NotEqual(value, data.Data);
            Assert.Equal(3 & value, data[final]);
            Assert.NotEqual(value, data[final]);
        }

        [Theory]
        [MemberData(nameof(Section_Create_Data))]
        public static void CreateSectionTest(short maximum, short mask)
        {
            BitVector32.Section section = BitVector32.CreateSection(maximum);
            Assert.Equal(0, section.Offset);
            Assert.Equal(mask, section.Mask);
        }

        [Theory]
        [MemberData(nameof(Section_Create_Data))]
        public static void CreateSection_NextTest(short maximum, short mask)
        {
            BitVector32.Section initial = BitVector32.CreateSection(short.MaxValue);
            BitVector32.Section section = BitVector32.CreateSection(maximum, initial);
            Assert.Equal(15, section.Offset);
            Assert.Equal(mask, section.Mask);
        }

        [Theory]
        [InlineData(short.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        public static void CreateSection_InvalidMaximumTest(short maxvalue)
        {
            AssertExtensions.Throws<ArgumentException>("maxValue", () => BitVector32.CreateSection(maxvalue));
            BitVector32.Section valid = BitVector32.CreateSection(1);
            AssertExtensions.Throws<ArgumentException>("maxValue", () => BitVector32.CreateSection(maxvalue, valid));
        }

        [Theory]
        [InlineData(7, short.MaxValue)]
        [InlineData(short.MaxValue, 7)]
        [InlineData(short.MaxValue, short.MaxValue)]
        [InlineData(byte.MaxValue, byte.MaxValue)]
        public static void CreateSection_FullTest(short prior, short invalid)
        {
            // BV32 can hold just over 2 shorts, so fill most of the way first....
            BitVector32.Section initial = BitVector32.CreateSection(short.MaxValue, BitVector32.CreateSection(short.MaxValue));
            BitVector32.Section overflow = BitVector32.CreateSection(prior, initial);
            // Final masked value can hang off the end
            Assert.Equal(prior, overflow.Mask);
            Assert.Equal(30, overflow.Offset);
            // The next section would be created "off the end"
            //     - the current offset is 30, and the current mask requires more than the remaining 1 bit.
            Assert.InRange(CountBitsRequired(overflow.Mask), 2, 15);
            
            Assert.Throws<InvalidOperationException>(() => BitVector32.CreateSection(invalid, overflow));
        }

        [Theory]
        [MemberData(nameof(Section_Equal_Data))]
        public static void Section_EqualsTest(BitVector32.Section left, BitVector32.Section right)
        {
            Assert.True(left.Equals(left));
            Assert.True(left.Equals(right));
            Assert.True(right.Equals(left));
            Assert.True(left.Equals((object)left));
            Assert.True(left.Equals((object)right));
            Assert.True(right.Equals((object)left));

            Assert.True(left == right);
            Assert.True(right == left);
            Assert.False(left != right);
            Assert.False(right != left);
        }

        [Theory]
        [MemberData(nameof(Section_Unequal_Data))]
        public static void Section_Unequal_EqualsTest(BitVector32.Section left, BitVector32.Section right)
        {
            Assert.False(left.Equals(right));
            Assert.False(right.Equals(left));
            Assert.False(left.Equals((object)right));
            Assert.False(right.Equals((object)left));
            Assert.False(left.Equals(new object()));

            Assert.False(left == right);
            Assert.False(right == left);
            Assert.True(left != right);
            Assert.True(right != left);
        }

        [Theory]
        [MemberData(nameof(Section_Equal_Data))]
        public static void Section_GetHashCodeTest(BitVector32.Section left, BitVector32.Section right)
        {
            Assert.Equal(left.GetHashCode(), left.GetHashCode());
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        public static void Section_ToStringTest()
        {
            Random random = new Random(-55);
            
            short maxValue = (short)random.Next(1, short.MaxValue);
            BitVector32.Section section1 = BitVector32.CreateSection(maxValue);
            BitVector32.Section section2 = BitVector32.CreateSection(maxValue);

            Assert.Equal(section1.ToString(), section2.ToString());
            Assert.Equal(section1.ToString(), BitVector32.Section.ToString(section2));
        }

        [Fact]
        public static void EqualsTest()
        {
            BitVector32 original = new BitVector32();
            Assert.True(original.Equals(original));
            Assert.True(new BitVector32().Equals(original));
            Assert.True(original.Equals(new BitVector32()));
            Assert.True(new BitVector32(0).Equals(original));
            Assert.True(original.Equals(new BitVector32(0)));

            BitVector32 other = new BitVector32(int.MaxValue / 2 - 1);
            Assert.True(other.Equals(other));
            Assert.True(new BitVector32(int.MaxValue / 2 - 1).Equals(other));
            Assert.True(other.Equals(new BitVector32(int.MaxValue / 2 - 1)));

            Assert.False(other.Equals(original));
            Assert.False(original.Equals(other));
            Assert.False(other.Equals(null));
            Assert.False(original.Equals(null));
            Assert.False(other.Equals(int.MaxValue / 2 - 1));
            Assert.False(original.Equals(0));
        }

        [Fact]
        public static void GetHashCodeTest()
        {
            BitVector32 original = new BitVector32();
            Assert.Equal(original.GetHashCode(), original.GetHashCode());
            Assert.Equal(new BitVector32().GetHashCode(), original.GetHashCode());
            Assert.Equal(new BitVector32(0).GetHashCode(), original.GetHashCode());

            BitVector32 other = new BitVector32(int.MaxValue / 2 - 1);
            Assert.Equal(other.GetHashCode(), other.GetHashCode());
            Assert.Equal(new BitVector32(int.MaxValue / 2 - 1).GetHashCode(), other.GetHashCode());
        }

        [Theory]
        [InlineData(0, "BitVector32{00000000000000000000000000000000}")]
        [InlineData(1, "BitVector32{00000000000000000000000000000001}")]
        [InlineData(-1, "BitVector32{11111111111111111111111111111111}")]
        [InlineData(16 - 2, "BitVector32{00000000000000000000000000001110}")]
        [InlineData(-(16 - 2), "BitVector32{11111111111111111111111111110010}")]
        [InlineData(int.MaxValue, "BitVector32{01111111111111111111111111111111}")]
        [InlineData(int.MinValue, "BitVector32{10000000000000000000000000000000}")]
        [InlineData(short.MaxValue, "BitVector32{00000000000000000111111111111111}")]
        [InlineData(short.MinValue, "BitVector32{11111111111111111000000000000000}")]
        public static void ToStringTest(int data, string expected)
        {
            Assert.Equal(expected, new BitVector32(data).ToString());
            Assert.Equal(expected, BitVector32.ToString(new BitVector32(data)));
        }

        private static short CountBitsRequired(short value)
        {
            short required = 16;
            while ((value & 0x8000) == 0)
            {
                required--;
                value <<= 1;
            }
            return required;
        }
    }
}
