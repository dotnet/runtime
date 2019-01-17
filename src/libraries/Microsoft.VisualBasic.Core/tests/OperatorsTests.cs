// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public partial class OperatorsTests
    {
        public static IEnumerable<object[]> AddObject_Idempotent_TestData()
        {
            // byte + primitives.
            yield return new object[] { (byte)1, (byte)2, (byte)3 };
            yield return new object[] { (byte)2, (sbyte)2, (short)4 };
            yield return new object[] { (byte)3, (ushort)2, (ushort)5 };
            yield return new object[] { (byte)4, (short)2, (short)6 };
            yield return new object[] { (byte)5, (uint)2, (uint)7 };
            yield return new object[] { (byte)6, 2, 8 };
            yield return new object[] { (byte)7, (long)2, (long)9 };
            yield return new object[] { (byte)8, (ulong)2, (ulong)10 };
            yield return new object[] { (byte)9, (float)2, (float)11 };
            yield return new object[] { (byte)10, (double)2, (double)12 };
            yield return new object[] { (byte)11, (decimal)2, (decimal)13 };
            yield return new object[] { (byte)12, "2", (double)14 };
            yield return new object[] { (byte)13, true, (short)12 };
            yield return new object[] { (byte)14, null, (byte)14 };
            yield return new object[] { (byte)15, byte.MaxValue, (short)270 };

            // sbyte + primitives.
            yield return new object[] { (sbyte)1, (sbyte)2, (sbyte)3 };
            yield return new object[] { (sbyte)3, (ushort)2, 5 };
            yield return new object[] { (sbyte)4, (short)2, (short)6 };
            yield return new object[] { (sbyte)5, (uint)2, (long)7 };
            yield return new object[] { (sbyte)6, 2, 8 };
            yield return new object[] { (sbyte)7, (long)2, (long)9 };
            yield return new object[] { (sbyte)9, (float)2, (float)11 };
            yield return new object[] { (sbyte)8, (ulong)2, (decimal)10 };
            yield return new object[] { (sbyte)10, (double)2, (double)12 };
            yield return new object[] { (sbyte)11, (decimal)2, (decimal)13 };
            yield return new object[] { (sbyte)12, "2", (double)14 };
            yield return new object[] { (sbyte)13, true, (sbyte)12 };
            yield return new object[] { (sbyte)14, null, (sbyte)14 };
            yield return new object[] { (sbyte)15, sbyte.MaxValue, (short)142 };

            // ushort + primitives.
            yield return new object[] { (ushort)3, (ushort)2, (ushort)5 };
            yield return new object[] { (ushort)4, (short)2, 6 };
            yield return new object[] { (ushort)5, (uint)2, (uint)7 };
            yield return new object[] { (ushort)6, 2, 8 };
            yield return new object[] { (ushort)7, (long)2, (long)9 };
            yield return new object[] { (ushort)8, (ulong)2, (ulong)10 };
            yield return new object[] { (ushort)9, (float)2, (float)11 };
            yield return new object[] { (ushort)10, (double)2, (double)12 };
            yield return new object[] { (ushort)11, (decimal)2, (decimal)13 };
            yield return new object[] { (ushort)12, "2", (double)14 };
            yield return new object[] { (ushort)13, true, 12 };
            yield return new object[] { (ushort)14, null, (ushort)14 };
            yield return new object[] { (ushort)15, ushort.MaxValue, 65550 };

            // short + primitives.
            yield return new object[] { (short)4, (short)2, (short)6 };
            yield return new object[] { (short)5, (uint)2, (long)7 };
            yield return new object[] { (short)6, 2, 8 };
            yield return new object[] { (short)7, (long)2, (long)9 };
            yield return new object[] { (short)8, (ulong)2, (decimal)10 };
            yield return new object[] { (short)9, (float)2, (float)11 };
            yield return new object[] { (short)10, (double)2, (double)12 };
            yield return new object[] { (short)11, (decimal)2, (decimal)13 };
            yield return new object[] { (short)12, "2", (double)14 };
            yield return new object[] { (short)13, true, (short)12 };
            yield return new object[] { (short)14, null, (short)14 };
            yield return new object[] { (short)15, short.MaxValue, 32782 };

            // uint + primitives.
            yield return new object[] { (uint)4, (short)2, (long)6 };
            yield return new object[] { (uint)5, (uint)2, (uint)7 };
            yield return new object[] { (uint)6, 2, (long)8 };
            yield return new object[] { (uint)7, (ulong)2, (ulong)9 };
            yield return new object[] { (uint)8, (long)2, (long)10 };
            yield return new object[] { (uint)9, (float)2, (float)11 };
            yield return new object[] { (uint)10, (double)2, (double)12 };
            yield return new object[] { (uint)11, (decimal)2, (decimal)13 };
            yield return new object[] { (uint)12, "2", (double)14 };
            yield return new object[] { (uint)13, true, (long)12 };
            yield return new object[] { (uint)14, null, (uint)14 };
            yield return new object[] { (uint)15, uint.MaxValue, 4294967310 };

            // int + primitives.
            yield return new object[] { 6, 2, 8 };
            yield return new object[] { 7, (ulong)2, (decimal)9 };
            yield return new object[] { 8, (long)2, (long)10 };
            yield return new object[] { 9, (float)2, (float)11 };
            yield return new object[] { 10, (double)2, (double)12 };
            yield return new object[] { 11, (decimal)2, (decimal)13 };
            yield return new object[] { 12, "2", (double)14 };
            yield return new object[] { 13, true, 12 };
            yield return new object[] { 14, null, 14 };
            yield return new object[] { 15, int.MaxValue, (long)2147483662 };

            // ulong + primitives.
            yield return new object[] { (ulong)7, (ulong)2, (ulong)9 };
            yield return new object[] { (ulong)8, (long)2, (decimal)10 };
            yield return new object[] { (ulong)9, (float)2, (float)11 };
            yield return new object[] { (ulong)10, (double)2, (double)12 };
            yield return new object[] { (ulong)11, (decimal)2, (decimal)13 };
            yield return new object[] { (ulong)12, "2", (double)14 };
            yield return new object[] { (ulong)13, true, (decimal)12 };
            yield return new object[] { (ulong)14, null, (ulong)14 };
            yield return new object[] { (ulong)15, ulong.MaxValue, decimal.Parse("18446744073709551630", CultureInfo.InvariantCulture) };

            // long + primitives.
            yield return new object[] { (long)8, (long)2, (long)10 };
            yield return new object[] { (long)9, (float)2, (float)11 };
            yield return new object[] { (long)10, (double)2, (double)12 };
            yield return new object[] { (long)11, (decimal)2, (decimal)13 };
            yield return new object[] { (long)12, "2", (double)14 };
            yield return new object[] { (long)13, true, (long)12 };
            yield return new object[] { (long)14, null, (long)14 };
            yield return new object[] { (long)15, long.MaxValue, decimal.Parse("9223372036854775822", CultureInfo.InvariantCulture) };

            // float + primitives
            yield return new object[] { (float)9, (float)2, (float)11 };
            yield return new object[] { (float)10, (double)2, (double)12 };
            yield return new object[] { (float)11, (decimal)2, (float)13 };
            yield return new object[] { (float)12, "2", (double)14 };
            yield return new object[] { (float)13, true, (float)12 };
            yield return new object[] { (float)14, null, (float)14 };
            yield return new object[] { (float)15, float.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { (float)15, float.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { (float)15, float.NaN, double.NaN };

            // double + primitives
            yield return new object[] { (double)10, (double)2, (double)12 };
            yield return new object[] { (double)11, (decimal)2, (double)13 };
            yield return new object[] { (double)12, "2", (double)14 };
            yield return new object[] { (double)13, true, (double)12 };
            yield return new object[] { (double)14, null, (double)14 };
            yield return new object[] { (double)15, double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { (double)15, double.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { (double)15, double.NaN, double.NaN };

            // decimal + primitives
            yield return new object[] { (decimal)11, (decimal)2, (decimal)13 };
            yield return new object[] { (decimal)12, "2", (double)14 };
            yield return new object[] { (decimal)13, true, (decimal)12 };
            yield return new object[] { (decimal)14, null, (decimal)14 };

            // string + primitives
            yield return new object[] { "1", "2", "12" };
            yield return new object[] { "2", '2', "22" };
            yield return new object[] { "2", new char[] { '2' }, "22" };
            yield return new object[] { "3", true, (double)2 };
            yield return new object[] { "5", DBNull.Value, "5" };
            yield return new object[] { "5", null, "5" };

            // chars + primitives.
            yield return new object[] { new char[] { '1' }, "2", "12" };
            yield return new object[] { new char[] { '2' }, new char[] { '2' }, "22" };
            yield return new object[] { new char[] { '5' }, null, "5" };

            // bool + primitives
            yield return new object[] { true, "2", (double)1 };
            yield return new object[] { true, true, (short)-2 };
            yield return new object[] { true, false, (short)-1 };
            yield return new object[] { true, null, (short)-1 };

            // char + primitives
            yield return new object[] { 'a', null, "a\0" };
            yield return new object[] { 'a', 'b', "ab" };

            // DBNull.
            yield return new object[] { DBNull.Value, "1", "1" };

            // null + null
            yield return new object[] { null, null, 0 };

            // object + object
            yield return new object[] { new AddObject(), 2, "custom" };
            yield return new object[] { new AddObject(), new OperatorsTests(), "customobject" };
        }

        [Theory]
        [MemberData(nameof(AddObject_Idempotent_TestData))]
        public void AddObject_Convertible_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.AddObject(left, right));

            if (expected is string expectedString)
            {
                string reversed = new string(expectedString.Reverse().ToArray());
                Assert.Equal(reversed, Operators.AddObject(right, left));
            }
            else
            {
                Assert.Equal(expected, Operators.AddObject(right, left));
            }
        }

        [Fact]
        public void AddObject_DateString_ReturnsExpected()
        {
            string expected = Assert.IsType<string>(Operators.AddObject("String", new DateTime(2017, 10, 10)));
            Assert.StartsWith("String", expected);
            Assert.Contains("17", expected);
        }

        [Fact]
        public void AddObject_DateDate_ReturnsExpected()
        {
            string expected = Assert.IsType<string>(Operators.AddObject(new DateTime(2018, 10, 10), new DateTime(2017, 10, 10)));
            Assert.Contains("17", expected);
            Assert.Contains("18", expected);
        }

        [Fact]
        public void AddObject_DateNull_ReturnsExpected()
        {
            string expected = Assert.IsType<string>(Operators.AddObject(new DateTime(2018, 10, 10), null));
            Assert.Contains("18", expected);
        }

        [Fact]
        public void AddObject_NullDate_ReturnsExpected()
        {
            string expected = Assert.IsType<string>(Operators.AddObject(null, new DateTime(2018, 10, 10)));
            Assert.Contains("18", expected);
        }

        [Fact]
        public void AddObject_StringDate_ReturnsExpected()
        {
            string expected = Assert.IsType<string>(Operators.AddObject(new DateTime(2017, 10, 10), "String"));
            Assert.EndsWith("String", expected);
            Assert.Contains("17", expected);
        }

        [Fact]
        public void AddObject_FloatDoubleDecimalOverflow_ReturnsMax()
        {
            Assert.Equal(float.MaxValue, Operators.AddObject((float)15, float.MaxValue));
            Assert.Equal(double.MaxValue, Operators.AddObject((double)15, double.MaxValue));
            Assert.NotEqual(decimal.MaxValue, Operators.AddObject((decimal)15, decimal.MaxValue));
        }

        public static IEnumerable<object[]> AddObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { '2', 1 };
            yield return new object[] { '3', new object() };
            yield return new object[] { new object(), '3' };

            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { DBNull.Value, 2 };
            yield return new object[] { null, DBNull.Value };
            yield return new object[] { DBNull.Value, null };
            yield return new object[] { DBNull.Value, DBNull.Value };

            yield return new object[] { new char[] { '8' }, 10 };
            yield return new object[] { 10, new char[] { '8' } };
            yield return new object[] { new char[] { '8' }, DBNull.Value };
            yield return new object[] { DBNull.Value, new char[] { '8' } };
            yield return new object[] { new char[] { '8' }, new object() };
            yield return new object[] { new object(), new char[] { '8' } };
        }

        [Theory]
        [MemberData(nameof(AddObject_InvalidObjects_TestData))]
        public void AddObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.AddObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.AddObject(right, left));
        }

        public static IEnumerable<object[]> AddObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new AddObject(), new object() };
            yield return new object[] { new object(), new AddObject() };

            yield return new object[] { new AddObject(), new AddObject() };
        }

        [Theory]
        [MemberData(nameof(AddObject_MismatchingObjects_TestData))]
        public void AddObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.AddObject(left, right));
        }

        public class AddObject
        {
            public static string operator +(AddObject left, int right) => "custom";
            public static string operator +(int left, AddObject right) => "motsuc";

            public static string operator +(AddObject left, OperatorsTests right) => "customobject";
            public static string operator +(OperatorsTests left, AddObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> AndObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)10, (byte)14, (byte)10 };
            yield return new object[] { (byte)10, (ByteEnum)14, (byte)10 };
            yield return new object[] { (byte)10, (sbyte)14, (short)10 };
            yield return new object[] { (byte)10, (SByteEnum)14, (short)10 };
            yield return new object[] { (byte)10, (ushort)14, (ushort)10 };
            yield return new object[] { (byte)10, (UShortEnum)14, (ushort)10 };
            yield return new object[] { (byte)10, (short)14, (short)10 };
            yield return new object[] { (byte)10, (ShortEnum)14, (short)10 };
            yield return new object[] { (byte)10, (uint)14, (uint)10 };
            yield return new object[] { (byte)10, (UIntEnum)14, (uint)10 };
            yield return new object[] { (byte)10, 14, 10 };
            yield return new object[] { (byte)10, (IntEnum)14, 10 };
            yield return new object[] { (byte)10, (ulong)14, (ulong)10 };
            yield return new object[] { (byte)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (byte)10, (long)14, (long)10 };
            yield return new object[] { (byte)10, (LongEnum)14, (long)10 };
            yield return new object[] { (byte)10, (float)14, (long)10 };
            yield return new object[] { (byte)10, (double)14, (long)10 };
            yield return new object[] { (byte)10, (decimal)14, (long)10 };
            yield return new object[] { (byte)10, "14", (long)10 };
            yield return new object[] { (byte)10, true, (short)10 };
            yield return new object[] { (byte)10, null, (byte)0 };

            yield return new object[] { (ByteEnum)10, (byte)14, (byte)10 };
            yield return new object[] { (ByteEnum)10, (ByteEnum)14, (ByteEnum)10 };
            yield return new object[] { (ByteEnum)10, (sbyte)14, (short)10 };
            yield return new object[] { (ByteEnum)10, (SByteEnum)14, (short)10 };
            yield return new object[] { (ByteEnum)10, (ushort)14, (ushort)10 };
            yield return new object[] { (ByteEnum)10, (UShortEnum)14, (ushort)10 };
            yield return new object[] { (ByteEnum)10, (short)14, (short)10 };
            yield return new object[] { (ByteEnum)10, (ShortEnum)14, (short)10 };
            yield return new object[] { (ByteEnum)10, (uint)14, (uint)10 };
            yield return new object[] { (ByteEnum)10, (UIntEnum)14, (uint)10 };
            yield return new object[] { (ByteEnum)10, 14, 10 };
            yield return new object[] { (ByteEnum)10, (IntEnum)14, 10 };
            yield return new object[] { (ByteEnum)10, (ulong)14, (ulong)10 };
            yield return new object[] { (ByteEnum)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (ByteEnum)10, (long)14, (long)10 };
            yield return new object[] { (ByteEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (ByteEnum)10, (float)14, (long)10 };
            yield return new object[] { (ByteEnum)10, (double)14, (long)10 };
            yield return new object[] { (ByteEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (ByteEnum)10, "14", (long)10 };
            yield return new object[] { (ByteEnum)10, true, (short)10 };
            yield return new object[] { (ByteEnum)10, null, (ByteEnum)0 };

            // sbyte.
            yield return new object[] { (sbyte)10, (byte)14, (short)10 };
            yield return new object[] { (sbyte)10, (ByteEnum)14, (short)10 };
            yield return new object[] { (sbyte)10, (sbyte)14, (sbyte)10 };
            yield return new object[] { (sbyte)10, (SByteEnum)14, (sbyte)10 };
            yield return new object[] { (sbyte)10, (ushort)14, 10 };
            yield return new object[] { (sbyte)10, (UShortEnum)14, 10 };
            yield return new object[] { (sbyte)10, (short)14, (short)10 };
            yield return new object[] { (sbyte)10, (ShortEnum)14, (short)10 };
            yield return new object[] { (sbyte)10, (uint)14, (long)10 };
            yield return new object[] { (sbyte)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (sbyte)10, 14, 10 };
            yield return new object[] { (sbyte)10, (IntEnum)14, 10 };
            yield return new object[] { (sbyte)10, (ulong)14, (long)10 };
            yield return new object[] { (sbyte)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (sbyte)10, (long)14, (long)10 };
            yield return new object[] { (sbyte)10, (LongEnum)14, (long)10 };
            yield return new object[] { (sbyte)10, (float)14, (long)10 };
            yield return new object[] { (sbyte)10, (double)14, (long)10 };
            yield return new object[] { (sbyte)10, (decimal)14, (long)10 };
            yield return new object[] { (sbyte)10, "14", (long)10 };
            yield return new object[] { (sbyte)10, true, (sbyte)10 };
            yield return new object[] { (sbyte)10, null, (sbyte)0 };

            yield return new object[] { (SByteEnum)10, (byte)14, (short)10 };
            yield return new object[] { (SByteEnum)10, (ByteEnum)14, (short)10 };
            yield return new object[] { (SByteEnum)10, (sbyte)14, (sbyte)10 };
            yield return new object[] { (SByteEnum)10, (SByteEnum)14, (SByteEnum)10 };
            yield return new object[] { (SByteEnum)10, (ushort)14, 10 };
            yield return new object[] { (SByteEnum)10, (UShortEnum)14, 10 };
            yield return new object[] { (SByteEnum)10, (short)14, (short)10 };
            yield return new object[] { (SByteEnum)10, (ShortEnum)14, (short)10 };
            yield return new object[] { (SByteEnum)10, (uint)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (SByteEnum)10, 14, 10 };
            yield return new object[] { (SByteEnum)10, (IntEnum)14, 10 };
            yield return new object[] { (SByteEnum)10, (ulong)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (long)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (float)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (double)14, (long)10 };
            yield return new object[] { (SByteEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (SByteEnum)10, "14", (long)10 };
            yield return new object[] { (SByteEnum)10, true, (sbyte)10 };
            yield return new object[] { (SByteEnum)10, null, (SByteEnum)0 };

            // ushort.
            yield return new object[] { (ushort)10, (byte)14, (ushort)10 };
            yield return new object[] { (ushort)10, (ByteEnum)14, (ushort)10 };
            yield return new object[] { (ushort)10, (sbyte)14, 10 };
            yield return new object[] { (ushort)10, (SByteEnum)14, 10 };
            yield return new object[] { (ushort)10, (ushort)14, (ushort)10 };
            yield return new object[] { (ushort)10, (UShortEnum)14, (ushort)10 };
            yield return new object[] { (ushort)10, (short)14, 10 };
            yield return new object[] { (ushort)10, (ShortEnum)14, 10 };
            yield return new object[] { (ushort)10, (uint)14, (uint)10 };
            yield return new object[] { (ushort)10, (UIntEnum)14, (uint)10 };
            yield return new object[] { (ushort)10, 14, 10 };
            yield return new object[] { (ushort)10, (IntEnum)14, 10 };
            yield return new object[] { (ushort)10, (ulong)14, (ulong)10 };
            yield return new object[] { (ushort)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (ushort)10, (long)14, (long)10 };
            yield return new object[] { (ushort)10, (LongEnum)14, (long)10 };
            yield return new object[] { (ushort)10, (float)14, (long)10 };
            yield return new object[] { (ushort)10, (double)14, (long)10 };
            yield return new object[] { (ushort)10, (decimal)14, (long)10 };
            yield return new object[] { (ushort)10, "14", (long)10 };
            yield return new object[] { (ushort)10, true, 10 };
            yield return new object[] { (ushort)10, null, (ushort)0 };

            yield return new object[] { (UShortEnum)10, (byte)14, (ushort)10 };
            yield return new object[] { (UShortEnum)10, (ByteEnum)14, (ushort)10 };
            yield return new object[] { (UShortEnum)10, (sbyte)14, 10 };
            yield return new object[] { (UShortEnum)10, (SByteEnum)14, 10 };
            yield return new object[] { (UShortEnum)10, (ushort)14, (ushort)10 };
            yield return new object[] { (UShortEnum)10, (UShortEnum)14, (UShortEnum)10 };
            yield return new object[] { (UShortEnum)10, (short)14, 10 };
            yield return new object[] { (UShortEnum)10, (ShortEnum)14, 10 };
            yield return new object[] { (UShortEnum)10, (uint)14, (uint)10 };
            yield return new object[] { (UShortEnum)10, (UIntEnum)14, (uint)10 };
            yield return new object[] { (UShortEnum)10, 14, 10 };
            yield return new object[] { (UShortEnum)10, (IntEnum)14, 10 };
            yield return new object[] { (UShortEnum)10, (ulong)14, (ulong)10 };
            yield return new object[] { (UShortEnum)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (UShortEnum)10, (long)14, (long)10 };
            yield return new object[] { (UShortEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (UShortEnum)10, (float)14, (long)10 };
            yield return new object[] { (UShortEnum)10, (double)14, (long)10 };
            yield return new object[] { (UShortEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (UShortEnum)10, "14", (long)10 };
            yield return new object[] { (UShortEnum)10, true, 10 };
            yield return new object[] { (UShortEnum)10, null, (UShortEnum)0 };

            // short.
            yield return new object[] { (short)10, (byte)14, (short)10 };
            yield return new object[] { (short)10, (ByteEnum)14, (short)10 };
            yield return new object[] { (short)10, (sbyte)14, (short)10 };
            yield return new object[] { (short)10, (SByteEnum)14, (short)10 };
            yield return new object[] { (short)10, (ushort)14, 10 };
            yield return new object[] { (short)10, (UShortEnum)14, 10 };
            yield return new object[] { (short)10, (short)14, (short)10 };
            yield return new object[] { (short)10, (ShortEnum)14, (short)10 };
            yield return new object[] { (short)10, (uint)14, (long)10 };
            yield return new object[] { (short)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (short)10, 14, 10 };
            yield return new object[] { (short)10, (IntEnum)14, 10 };
            yield return new object[] { (short)10, (ulong)14, (long)10 };
            yield return new object[] { (short)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (short)10, (long)14, (long)10 };
            yield return new object[] { (short)10, (LongEnum)14, (long)10 };
            yield return new object[] { (short)10, (float)14, (long)10 };
            yield return new object[] { (short)10, (double)14, (long)10 };
            yield return new object[] { (short)10, (decimal)14, (long)10 };
            yield return new object[] { (short)10, "14", (long)10 };
            yield return new object[] { (short)10, true, (short)10 };
            yield return new object[] { (short)10, null, (short)0 };

            yield return new object[] { (ShortEnum)10, (byte)14, (short)10 };
            yield return new object[] { (ShortEnum)10, (ByteEnum)14, (short)10 };
            yield return new object[] { (ShortEnum)10, (sbyte)14, (short)10 };
            yield return new object[] { (ShortEnum)10, (SByteEnum)14, (short)10 };
            yield return new object[] { (ShortEnum)10, (ushort)14, 10 };
            yield return new object[] { (ShortEnum)10, (UShortEnum)14, 10 };
            yield return new object[] { (ShortEnum)10, (short)14, (short)10 };
            yield return new object[] { (ShortEnum)10, (ShortEnum)14, (ShortEnum)10 };
            yield return new object[] { (ShortEnum)10, (uint)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (ShortEnum)10, 14, 10 };
            yield return new object[] { (ShortEnum)10, (IntEnum)14, 10 };
            yield return new object[] { (ShortEnum)10, (ulong)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (long)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (float)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (double)14, (long)10 };
            yield return new object[] { (ShortEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (ShortEnum)10, "14", (long)10 };
            yield return new object[] { (ShortEnum)10, true, (short)10 };
            yield return new object[] { (ShortEnum)10, null, (ShortEnum)0 };

            // uint.
            yield return new object[] { (uint)10, (byte)14, (uint)10 };
            yield return new object[] { (uint)10, (ByteEnum)14, (uint)10 };
            yield return new object[] { (uint)10, (sbyte)14, (long)10 };
            yield return new object[] { (uint)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (uint)10, (ushort)14, (uint)10 };
            yield return new object[] { (uint)10, (UShortEnum)14, (uint)10 };
            yield return new object[] { (uint)10, (short)14, (long)10 };
            yield return new object[] { (uint)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (uint)10, (uint)14, (uint)10 };
            yield return new object[] { (uint)10, (UIntEnum)14, (uint)10 };
            yield return new object[] { (uint)10, 14, (long)10 };
            yield return new object[] { (uint)10, (IntEnum)14, (long)10 };
            yield return new object[] { (uint)10, (ulong)14, (ulong)10 };
            yield return new object[] { (uint)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (uint)10, (long)14, (long)10 };
            yield return new object[] { (uint)10, (LongEnum)14, (long)10 };
            yield return new object[] { (uint)10, (float)14, (long)10 };
            yield return new object[] { (uint)10, (double)14, (long)10 };
            yield return new object[] { (uint)10, (decimal)14, (long)10 };
            yield return new object[] { (uint)10, "14", (long)10 };
            yield return new object[] { (uint)10, true, (long)10 };
            yield return new object[] { (uint)10, null, (uint)0 };

            yield return new object[] { (UIntEnum)10, (byte)14, (uint)10 };
            yield return new object[] { (UIntEnum)10, (ByteEnum)14, (uint)10 };
            yield return new object[] { (UIntEnum)10, (sbyte)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (ushort)14, (uint)10 };
            yield return new object[] { (UIntEnum)10, (UShortEnum)14, (uint)10 };
            yield return new object[] { (UIntEnum)10, (short)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (uint)14, (uint)10 };
            yield return new object[] { (UIntEnum)10, (UIntEnum)14, (UIntEnum)10 };
            yield return new object[] { (UIntEnum)10, 14, (long)10 };
            yield return new object[] { (UIntEnum)10, (IntEnum)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (ulong)14, (ulong)10 };
            yield return new object[] { (UIntEnum)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (UIntEnum)10, (long)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (float)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (double)14, (long)10 };
            yield return new object[] { (UIntEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (UIntEnum)10, "14", (long)10 };
            yield return new object[] { (UIntEnum)10, true, (long)10 };
            yield return new object[] { (UIntEnum)10, null, (UIntEnum)0 };

            // int.
            yield return new object[] { 10, (byte)14, 10 };
            yield return new object[] { 10, (ByteEnum)14, 10 };
            yield return new object[] { 10, (sbyte)14, 10 };
            yield return new object[] { 10, (SByteEnum)14, 10 };
            yield return new object[] { 10, (ushort)14, 10 };
            yield return new object[] { 10, (UShortEnum)14, 10 };
            yield return new object[] { 10, (short)14, 10 };
            yield return new object[] { 10, (ShortEnum)14, 10 };
            yield return new object[] { 10, (uint)14, (long)10 };
            yield return new object[] { 10, (UIntEnum)14, (long)10 };
            yield return new object[] { 10, 14, 10 };
            yield return new object[] { 10, (IntEnum)14, 10 };
            yield return new object[] { 10, (ulong)14, (long)10 };
            yield return new object[] { 10, (ULongEnum)14, (long)10 };
            yield return new object[] { 10, (long)14, (long)10 };
            yield return new object[] { 10, (LongEnum)14, (long)10 };
            yield return new object[] { 10, (float)14, (long)10 };
            yield return new object[] { 10, (double)14, (long)10 };
            yield return new object[] { 10, (decimal)14, (long)10 };
            yield return new object[] { 10, "14", (long)10 };
            yield return new object[] { 10, true, 10 };
            yield return new object[] { 10, null, 0 };

            yield return new object[] { (IntEnum)10, (byte)14, 10 };
            yield return new object[] { (IntEnum)10, (ByteEnum)14, 10 };
            yield return new object[] { (IntEnum)10, (sbyte)14, 10 };
            yield return new object[] { (IntEnum)10, (SByteEnum)14, 10 };
            yield return new object[] { (IntEnum)10, (ushort)14, 10 };
            yield return new object[] { (IntEnum)10, (UShortEnum)14, 10 };
            yield return new object[] { (IntEnum)10, (short)14, 10 };
            yield return new object[] { (IntEnum)10, (ShortEnum)14, 10 };
            yield return new object[] { (IntEnum)10, (uint)14, (long)10 };
            yield return new object[] { (IntEnum)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (IntEnum)10, 14, 10 };
            yield return new object[] { (IntEnum)10, (IntEnum)14, (IntEnum)10 };
            yield return new object[] { (IntEnum)10, (ulong)14, (long)10 };
            yield return new object[] { (IntEnum)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (IntEnum)10, (long)14, (long)10 };
            yield return new object[] { (IntEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (IntEnum)10, (float)14, (long)10 };
            yield return new object[] { (IntEnum)10, (double)14, (long)10 };
            yield return new object[] { (IntEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (IntEnum)10, "14", (long)10 };
            yield return new object[] { (IntEnum)10, true, 10 };
            yield return new object[] { (IntEnum)10, null, (IntEnum)0 };

            // ulong.
            yield return new object[] { (ulong)10, (byte)14, (ulong)10 };
            yield return new object[] { (ulong)10, (ByteEnum)14, (ulong)10 };
            yield return new object[] { (ulong)10, (sbyte)14, (long)10 };
            yield return new object[] { (ulong)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (ulong)10, (ushort)14, (ulong)10 };
            yield return new object[] { (ulong)10, (UShortEnum)14, (ulong)10 };
            yield return new object[] { (ulong)10, (short)14, (long)10 };
            yield return new object[] { (ulong)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (ulong)10, (uint)14, (ulong)10 };
            yield return new object[] { (ulong)10, (UIntEnum)14, (ulong)10 };
            yield return new object[] { (ulong)10, 14, (long)10 };
            yield return new object[] { (ulong)10, (IntEnum)14, (long)10 };
            yield return new object[] { (ulong)10, (ulong)14, (ulong)10 };
            yield return new object[] { (ulong)10, (ULongEnum)14, (ulong)10 };
            yield return new object[] { (ulong)10, (long)14, (long)10 };
            yield return new object[] { (ulong)10, (LongEnum)14, (long)10 };
            yield return new object[] { (ulong)10, (float)14, (long)10 };
            yield return new object[] { (ulong)10, (double)14, (long)10 };
            yield return new object[] { (ulong)10, (decimal)14, (long)10 };
            yield return new object[] { (ulong)10, "14", (long)10 };
            yield return new object[] { (ulong)10, true, (long)10 };
            yield return new object[] { (ulong)10, null, (ulong)0 };

            yield return new object[] { (ULongEnum)10, (byte)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, (ByteEnum)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, (sbyte)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (ushort)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, (UShortEnum)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, (short)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (uint)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, (UIntEnum)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, 14, (long)10 };
            yield return new object[] { (ULongEnum)10, (IntEnum)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (ulong)14, (ulong)10 };
            yield return new object[] { (ULongEnum)10, (ULongEnum)14, (ULongEnum)10 };
            yield return new object[] { (ULongEnum)10, (long)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (LongEnum)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (float)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (double)14, (long)10 };
            yield return new object[] { (ULongEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (ULongEnum)10, "14", (long)10 };
            yield return new object[] { (ULongEnum)10, true, (long)10 };
            yield return new object[] { (ULongEnum)10, null, (ULongEnum)0 };

            // long.
            yield return new object[] { (long)10, (byte)14, (long)10 };
            yield return new object[] { (long)10, (ByteEnum)14, (long)10 };
            yield return new object[] { (long)10, (sbyte)14, (long)10 };
            yield return new object[] { (long)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (long)10, (ushort)14, (long)10 };
            yield return new object[] { (long)10, (UShortEnum)14, (long)10 };
            yield return new object[] { (long)10, (short)14, (long)10 };
            yield return new object[] { (long)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (long)10, (uint)14, (long)10 };
            yield return new object[] { (long)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (long)10, 14, (long)10 };
            yield return new object[] { (long)10, (IntEnum)14, (long)10 };
            yield return new object[] { (long)10, (ulong)14, (long)10 };
            yield return new object[] { (long)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (long)10, (long)14, (long)10 };
            yield return new object[] { (long)10, (LongEnum)14, (long)10 };
            yield return new object[] { (long)10, (float)14, (long)10 };
            yield return new object[] { (long)10, (double)14, (long)10 };
            yield return new object[] { (long)10, (decimal)14, (long)10 };
            yield return new object[] { (long)10, "14", (long)10 };
            yield return new object[] { (long)10, true, (long)10 };
            yield return new object[] { (long)10, null, (long)0 };

            yield return new object[] { (LongEnum)10, (byte)14, (long)10 };
            yield return new object[] { (LongEnum)10, (ByteEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, (sbyte)14, (long)10 };
            yield return new object[] { (LongEnum)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, (ushort)14, (long)10 };
            yield return new object[] { (LongEnum)10, (UShortEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, (short)14, (long)10 };
            yield return new object[] { (LongEnum)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, (uint)14, (long)10 };
            yield return new object[] { (LongEnum)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, 14, (long)10 };
            yield return new object[] { (LongEnum)10, (IntEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, (ulong)14, (long)10 };
            yield return new object[] { (LongEnum)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (LongEnum)10, (long)14, (long)10 };
            yield return new object[] { (LongEnum)10, (LongEnum)14, (LongEnum)10 };
            yield return new object[] { (LongEnum)10, (float)14, (long)10 };
            yield return new object[] { (LongEnum)10, (double)14, (long)10 };
            yield return new object[] { (LongEnum)10, (decimal)14, (long)10 };
            yield return new object[] { (LongEnum)10, "14", (long)10 };
            yield return new object[] { (LongEnum)10, true, (long)10 };
            yield return new object[] { (LongEnum)10, null, (LongEnum)0 };

            // float.
            yield return new object[] { (float)10, (byte)14, (long)10 };
            yield return new object[] { (float)10, (ByteEnum)14, (long)10 };
            yield return new object[] { (float)10, (sbyte)14, (long)10 };
            yield return new object[] { (float)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (float)10, (ushort)14, (long)10 };
            yield return new object[] { (float)10, (UShortEnum)14, (long)10 };
            yield return new object[] { (float)10, (short)14, (long)10 };
            yield return new object[] { (float)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (float)10, (uint)14, (long)10 };
            yield return new object[] { (float)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (float)10, 14, (long)10 };
            yield return new object[] { (float)10, (IntEnum)14, (long)10 };
            yield return new object[] { (float)10, (ulong)14, (long)10 };
            yield return new object[] { (float)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (float)10, (long)14, (long)10 };
            yield return new object[] { (float)10, (LongEnum)14, (long)10 };
            yield return new object[] { (float)10, (float)14, (long)10 };
            yield return new object[] { (float)10, (double)14, (long)10 };
            yield return new object[] { (float)10, (decimal)14, (long)10 };
            yield return new object[] { (float)10, "14", (long)10 };
            yield return new object[] { (float)10, true, (long)10 };
            yield return new object[] { (float)10, null, (long)0 };

            // double.
            yield return new object[] { (double)10, (byte)14, (long)10 };
            yield return new object[] { (double)10, (ByteEnum)14, (long)10 };
            yield return new object[] { (double)10, (sbyte)14, (long)10 };
            yield return new object[] { (double)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (double)10, (ushort)14, (long)10 };
            yield return new object[] { (double)10, (UShortEnum)14, (long)10 };
            yield return new object[] { (double)10, (short)14, (long)10 };
            yield return new object[] { (double)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (double)10, (uint)14, (long)10 };
            yield return new object[] { (double)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (double)10, 14, (long)10 };
            yield return new object[] { (double)10, (IntEnum)14, (long)10 };
            yield return new object[] { (double)10, (ulong)14, (long)10 };
            yield return new object[] { (double)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (double)10, (long)14, (long)10 };
            yield return new object[] { (double)10, (LongEnum)14, (long)10 };
            yield return new object[] { (double)10, (float)14, (long)10 };
            yield return new object[] { (double)10, (double)14, (long)10 };
            yield return new object[] { (double)10, (decimal)14, (long)10 };
            yield return new object[] { (double)10, "14", (long)10 };
            yield return new object[] { (double)10, true, (long)10 };
            yield return new object[] { (double)10, null, (long)0 };

            // decimal.
            yield return new object[] { (decimal)10, (byte)14, (long)10 };
            yield return new object[] { (decimal)10, (ByteEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (sbyte)14, (long)10 };
            yield return new object[] { (decimal)10, (SByteEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (ushort)14, (long)10 };
            yield return new object[] { (decimal)10, (UShortEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (short)14, (long)10 };
            yield return new object[] { (decimal)10, (ShortEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (uint)14, (long)10 };
            yield return new object[] { (decimal)10, (UIntEnum)14, (long)10 };
            yield return new object[] { (decimal)10, 14, (long)10 };
            yield return new object[] { (decimal)10, (IntEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (ulong)14, (long)10 };
            yield return new object[] { (decimal)10, (ULongEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (long)14, (long)10 };
            yield return new object[] { (decimal)10, (LongEnum)14, (long)10 };
            yield return new object[] { (decimal)10, (float)14, (long)10 };
            yield return new object[] { (decimal)10, (double)14, (long)10 };
            yield return new object[] { (decimal)10, (decimal)14, (long)10 };
            yield return new object[] { (decimal)10, "14", (long)10 };
            yield return new object[] { (decimal)10, true, (long)10 };
            yield return new object[] { (decimal)10, null, (long)0 };

            // string.
            yield return new object[] { "10", (byte)14, (long)10 };
            yield return new object[] { "10", (ByteEnum)14, (long)10 };
            yield return new object[] { "10", (sbyte)14, (long)10 };
            yield return new object[] { "10", (SByteEnum)14, (long)10 };
            yield return new object[] { "10", (ushort)14, (long)10 };
            yield return new object[] { "10", (UShortEnum)14, (long)10 };
            yield return new object[] { "10", (short)14, (long)10 };
            yield return new object[] { "10", (ShortEnum)14, (long)10 };
            yield return new object[] { "10", (uint)14, (long)10 };
            yield return new object[] { "10", (UIntEnum)14, (long)10 };
            yield return new object[] { "10", 14, (long)10 };
            yield return new object[] { "10", (IntEnum)14, (long)10 };
            yield return new object[] { "10", (ulong)14, (long)10 };
            yield return new object[] { "10", (ULongEnum)14, (long)10 };
            yield return new object[] { "10", (long)14, (long)10 };
            yield return new object[] { "10", (LongEnum)14, (long)10 };
            yield return new object[] { "10", (float)14, (long)10 };
            yield return new object[] { "10", (double)14, (long)10 };
            yield return new object[] { "10", (decimal)14, (long)10 };
            yield return new object[] { "10", "14", (long)10 };
            yield return new object[] { "10", true, true };
            yield return new object[] { "10", null, (long)0 };

            // bool.
            yield return new object[] { true, (byte)14, (short)14 };
            yield return new object[] { true, (ByteEnum)14, (short)14 };
            yield return new object[] { true, (sbyte)14, (sbyte)14 };
            yield return new object[] { true, (SByteEnum)14, (sbyte)14 };
            yield return new object[] { true, (ushort)14, 14 };
            yield return new object[] { true, (UShortEnum)14, 14 };
            yield return new object[] { true, (short)14, (short)14 };
            yield return new object[] { true, (ShortEnum)14, (short)14 };
            yield return new object[] { true, (uint)14, (long)14 };
            yield return new object[] { true, (UIntEnum)14, (long)14 };
            yield return new object[] { true, 14, 14 };
            yield return new object[] { true, (IntEnum)14, 14 };
            yield return new object[] { true, (ulong)14, (long)14 };
            yield return new object[] { true, (ULongEnum)14, (long)14 };
            yield return new object[] { true, (long)14, (long)14 };
            yield return new object[] { true, (LongEnum)14, (long)14 };
            yield return new object[] { true, (float)14, (long)14 };
            yield return new object[] { true, (double)14, (long)14 };
            yield return new object[] { true, (decimal)14, (long)14 };
            yield return new object[] { true, "14", true };
            yield return new object[] { true, true, true };
            yield return new object[] { true, null, false };

            // null.
            yield return new object[] { null, (byte)14, (byte)0 };
            yield return new object[] { null, (ByteEnum)14, (ByteEnum)0 };
            yield return new object[] { null, (sbyte)14, (sbyte)0 };
            yield return new object[] { null, (SByteEnum)14, (SByteEnum)0 };
            yield return new object[] { null, (ushort)14, (ushort)0 };
            yield return new object[] { null, (UShortEnum)14, (UShortEnum)0 };
            yield return new object[] { null, (short)14, (short)0 };
            yield return new object[] { null, (ShortEnum)14, (ShortEnum)0 };
            yield return new object[] { null, (uint)14, (uint)0 };
            yield return new object[] { null, (UIntEnum)14, (UIntEnum)0 };
            yield return new object[] { null, 14, 0 };
            yield return new object[] { null, (IntEnum)14, (IntEnum)0 };
            yield return new object[] { null, (ulong)14, (ulong)0 };
            yield return new object[] { null, (ULongEnum)14, (ULongEnum)0 };
            yield return new object[] { null, (long)14, (long)0 };
            yield return new object[] { null, (LongEnum)14, (LongEnum)0 };
            yield return new object[] { null, (float)14, (long)0 };
            yield return new object[] { null, (double)14, (long)0 };
            yield return new object[] { null, (decimal)14, (long)0 };
            yield return new object[] { null, "14", (long)0 };
            yield return new object[] { null, true, false };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new AndObject(), 2, "custom" };
            yield return new object[] { 2, new AndObject(), "motsuc" };
            yield return new object[] { new AndObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new AndObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(AndObject_TestData))]
        public void AndObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.AndObject(left, right));
        }

        public static IEnumerable<object[]> AndObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(AndObject_InvalidObjects_TestData))]
        public void AndObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.AndObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.AndObject(right, left));
        }

        public static IEnumerable<object[]> AndObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new AndObject(), new object() };
            yield return new object[] { new object(), new AndObject() };

            yield return new object[] { new AndObject(), new AndObject() };
        }

        [Theory]
        [MemberData(nameof(AndObject_MismatchingObjects_TestData))]
        public void AndObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.AndObject(left, right));
        }

        public class AndObject
        {
            public static string operator &(AndObject left, int right) => "custom";
            public static string operator &(int left, AndObject right) => "motsuc";

            public static string operator &(AndObject left, OperatorsTests right) => "customobject";
            public static string operator &(OperatorsTests left, AndObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> ConcatenateObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)10, (byte)2, "102" };
            yield return new object[] { (byte)10, (sbyte)2, "102" };
            yield return new object[] { (byte)10, (ushort)2, "102" };
            yield return new object[] { (byte)10, (short)2, "102" };
            yield return new object[] { (byte)10, (uint)2, "102" };
            yield return new object[] { (byte)10, 2, "102" };
            yield return new object[] { (byte)10, (ulong)2, "102" };
            yield return new object[] { (byte)10, (long)2, "102" };
            yield return new object[] { (byte)10, (float)2, "102" };
            yield return new object[] { (byte)10, (double)2, "102" };
            yield return new object[] { (byte)10, (decimal)2, "102" };
            yield return new object[] { (byte)10, "2", "102" };
            yield return new object[] { (byte)10, new char[] { '2' }, "102" };
            yield return new object[] { (byte)10, true, "10True" };
            yield return new object[] { (byte)10, DBNull.Value, "10" };
            yield return new object[] { (byte)10, null, "10" };

            // sbyte.
            yield return new object[] { (sbyte)10, (byte)2, "102" };
            yield return new object[] { (sbyte)10, (sbyte)2, "102" };
            yield return new object[] { (sbyte)10, (ushort)2, "102" };
            yield return new object[] { (sbyte)10, (short)2, "102" };
            yield return new object[] { (sbyte)10, (uint)2, "102" };
            yield return new object[] { (sbyte)10, 2, "102" };
            yield return new object[] { (sbyte)10, (ulong)2, "102" };
            yield return new object[] { (sbyte)10, (long)2, "102" };
            yield return new object[] { (sbyte)10, (float)2, "102" };
            yield return new object[] { (sbyte)10, (double)2, "102" };
            yield return new object[] { (sbyte)10, (decimal)2, "102" };
            yield return new object[] { (sbyte)10, "2", "102" };
            yield return new object[] { (sbyte)10, new char[] { '2' }, "102" };
            yield return new object[] { (sbyte)10, true, "10True" };
            yield return new object[] { (sbyte)10, DBNull.Value, "10" };
            yield return new object[] { (sbyte)10, null, "10" };

            // ushort.
            yield return new object[] { (ushort)10, (byte)2, "102" };
            yield return new object[] { (ushort)10, (sbyte)2, "102" };
            yield return new object[] { (ushort)10, (ushort)2, "102" };
            yield return new object[] { (ushort)10, (short)2, "102" };
            yield return new object[] { (ushort)10, (uint)2, "102" };
            yield return new object[] { (ushort)10, 2, "102" };
            yield return new object[] { (ushort)10, (ulong)2, "102" };
            yield return new object[] { (ushort)10, (long)2, "102" };
            yield return new object[] { (ushort)10, (float)2, "102" };
            yield return new object[] { (ushort)10, (double)2, "102" };
            yield return new object[] { (ushort)10, (decimal)2, "102" };
            yield return new object[] { (ushort)10, "2", "102" };
            yield return new object[] { (ushort)10, new char[] { '2' }, "102" };
            yield return new object[] { (ushort)10, true, "10True" };
            yield return new object[] { (ushort)10, DBNull.Value, "10" };
            yield return new object[] { (ushort)10, null, "10" };

            // short.
            yield return new object[] { (short)10, (byte)2, "102" };
            yield return new object[] { (short)10, (sbyte)2, "102" };
            yield return new object[] { (short)10, (ushort)2, "102" };
            yield return new object[] { (short)10, (short)2, "102" };
            yield return new object[] { (short)10, (uint)2, "102" };
            yield return new object[] { (short)10, 2, "102" };
            yield return new object[] { (short)10, (ulong)2, "102" };
            yield return new object[] { (short)10, (long)2, "102" };
            yield return new object[] { (short)10, (float)2, "102" };
            yield return new object[] { (short)10, (double)2, "102" };
            yield return new object[] { (short)10, (decimal)2, "102" };
            yield return new object[] { (short)10, "2", "102" };
            yield return new object[] { (short)10, new char[] { '2' }, "102" };
            yield return new object[] { (short)10, true, "10True" };
            yield return new object[] { (short)10, DBNull.Value, "10" };
            yield return new object[] { (short)10, null, "10" };

            // uint.
            yield return new object[] { (uint)10, (byte)2, "102" };
            yield return new object[] { (uint)10, (sbyte)2, "102" };
            yield return new object[] { (uint)10, (ushort)2, "102" };
            yield return new object[] { (uint)10, (short)2, "102" };
            yield return new object[] { (uint)10, (uint)2, "102" };
            yield return new object[] { (uint)10, 2, "102" };
            yield return new object[] { (uint)10, (ulong)2, "102" };
            yield return new object[] { (uint)10, (long)2, "102" };
            yield return new object[] { (uint)10, (float)2, "102" };
            yield return new object[] { (uint)10, (double)2, "102" };
            yield return new object[] { (uint)10, (decimal)2, "102" };
            yield return new object[] { (uint)10, "2", "102" };
            yield return new object[] { (uint)10, new char[] { '2' }, "102" };
            yield return new object[] { (uint)10, true, "10True" };
            yield return new object[] { (uint)10, DBNull.Value, "10" };
            yield return new object[] { (uint)10, null, "10" };

            // int.
            yield return new object[] { 10, (byte)2, "102" };
            yield return new object[] { 10, (sbyte)2, "102" };
            yield return new object[] { 10, (ushort)2, "102" };
            yield return new object[] { 10, (short)2, "102" };
            yield return new object[] { 10, (uint)2, "102" };
            yield return new object[] { 10, 2, "102" };
            yield return new object[] { 10, (ulong)2, "102" };
            yield return new object[] { 10, (long)2, "102" };
            yield return new object[] { 10, (float)2, "102" };
            yield return new object[] { 10, (double)2, "102" };
            yield return new object[] { 10, (decimal)2, "102" };
            yield return new object[] { 10, "2", "102" };
            yield return new object[] { 10, new char[] { '2' }, "102" };
            yield return new object[] { 10, true, "10True" };
            yield return new object[] { 10, DBNull.Value, "10" };
            yield return new object[] { 10, null, "10" };

            // ulong.
            yield return new object[] { (ulong)10, (byte)2, "102" };
            yield return new object[] { (ulong)10, (sbyte)2, "102" };
            yield return new object[] { (ulong)10, (ushort)2, "102" };
            yield return new object[] { (ulong)10, (short)2, "102" };
            yield return new object[] { (ulong)10, (uint)2, "102" };
            yield return new object[] { (ulong)10, 2, "102" };
            yield return new object[] { (ulong)10, (ulong)2, "102" };
            yield return new object[] { (ulong)10, (long)2, "102" };
            yield return new object[] { (ulong)10, (float)2, "102" };
            yield return new object[] { (ulong)10, (double)2, "102" };
            yield return new object[] { (ulong)10, (decimal)2, "102" };
            yield return new object[] { (ulong)10, "2", "102" };
            yield return new object[] { (ulong)10, new char[] { '2' }, "102" };
            yield return new object[] { (ulong)10, true, "10True" };
            yield return new object[] { (ulong)10, DBNull.Value, "10" };
            yield return new object[] { (ulong)10, null, "10" };

            // long.
            yield return new object[] { (long)10, (byte)2, "102" };
            yield return new object[] { (long)10, (sbyte)2, "102" };
            yield return new object[] { (long)10, (ushort)2, "102" };
            yield return new object[] { (long)10, (short)2, "102" };
            yield return new object[] { (long)10, (uint)2, "102" };
            yield return new object[] { (long)10, 2, "102" };
            yield return new object[] { (long)10, (ulong)2, "102" };
            yield return new object[] { (long)10, (long)2, "102" };
            yield return new object[] { (long)10, (float)2, "102" };
            yield return new object[] { (long)10, (double)2, "102" };
            yield return new object[] { (long)10, (decimal)2, "102" };
            yield return new object[] { (long)10, "2", "102" };
            yield return new object[] { (long)10, new char[] { '2' }, "102" };
            yield return new object[] { (long)10, true, "10True" };
            yield return new object[] { (long)10, DBNull.Value, "10" };
            yield return new object[] { (long)10, null, "10" };

            // float.
            yield return new object[] { (float)10, (byte)2, "102" };
            yield return new object[] { (float)10, (sbyte)2, "102" };
            yield return new object[] { (float)10, (ushort)2, "102" };
            yield return new object[] { (float)10, (short)2, "102" };
            yield return new object[] { (float)10, (uint)2, "102" };
            yield return new object[] { (float)10, 2, "102" };
            yield return new object[] { (float)10, (ulong)2, "102" };
            yield return new object[] { (float)10, (long)2, "102" };
            yield return new object[] { (float)10, (float)2, "102" };
            yield return new object[] { (float)10, (double)2, "102" };
            yield return new object[] { (float)10, (decimal)2, "102" };
            yield return new object[] { (float)10, "2", "102" };
            yield return new object[] { (float)10, new char[] { '2' }, "102" };
            yield return new object[] { (float)10, true, "10True" };
            yield return new object[] { (float)10, DBNull.Value, "10" };
            yield return new object[] { (float)10, null, "10" };

            // double.
            yield return new object[] { (double)10, (byte)2, "102" };
            yield return new object[] { (double)10, (sbyte)2, "102" };
            yield return new object[] { (double)10, (ushort)2, "102" };
            yield return new object[] { (double)10, (short)2, "102" };
            yield return new object[] { (double)10, (uint)2, "102" };
            yield return new object[] { (double)10, 2, "102" };
            yield return new object[] { (double)10, (ulong)2, "102" };
            yield return new object[] { (double)10, (long)2, "102" };
            yield return new object[] { (double)10, (float)2, "102" };
            yield return new object[] { (double)10, (double)2, "102" };
            yield return new object[] { (double)10, (decimal)2, "102" };
            yield return new object[] { (double)10, "2", "102" };
            yield return new object[] { (double)10, new char[] { '2' }, "102" };
            yield return new object[] { (double)10, true, "10True" };
            yield return new object[] { (double)10, DBNull.Value, "10" };
            yield return new object[] { (double)10, null, "10" };

            // decimal.
            yield return new object[] { (decimal)10, (byte)2, "102" };
            yield return new object[] { (decimal)10, (sbyte)2, "102" };
            yield return new object[] { (decimal)10, (ushort)2, "102" };
            yield return new object[] { (decimal)10, (short)2, "102" };
            yield return new object[] { (decimal)10, (uint)2, "102" };
            yield return new object[] { (decimal)10, 2, "102" };
            yield return new object[] { (decimal)10, (ulong)2, "102" };
            yield return new object[] { (decimal)10, (long)2, "102" };
            yield return new object[] { (decimal)10, (float)2, "102" };
            yield return new object[] { (decimal)10, (double)2, "102" };
            yield return new object[] { (decimal)10, (decimal)2, "102" };
            yield return new object[] { (decimal)10, "2", "102" };
            yield return new object[] { (decimal)10, new char[] { '2' }, "102" };
            yield return new object[] { (decimal)10, true, "10True" };
            yield return new object[] { (decimal)10, DBNull.Value, "10" };
            yield return new object[] { (decimal)10, null, "10" };

            // string.
            yield return new object[] { "10", (byte)2, "102" };
            yield return new object[] { "10", (sbyte)2, "102" };
            yield return new object[] { "10", (ushort)2, "102" };
            yield return new object[] { "10", (short)2, "102" };
            yield return new object[] { "10", (uint)2, "102" };
            yield return new object[] { "10", 2, "102" };
            yield return new object[] { "10", (ulong)2, "102" };
            yield return new object[] { "10", (long)2, "102" };
            yield return new object[] { "10", (float)2, "102" };
            yield return new object[] { "10", (double)2, "102" };
            yield return new object[] { "10", (decimal)2, "102" };
            yield return new object[] { "10", "2", "102" };
            yield return new object[] { "10", new char[] { '2' }, "102" };
            yield return new object[] { "10", true, "10True" };
            yield return new object[] { "10", DBNull.Value, "10" };
            yield return new object[] { "10", null, "10" };

            // chars.
            yield return new object[] { new char[] { '1', '0' }, (byte)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (sbyte)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (ushort)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (short)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (uint)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, 2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (ulong)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (long)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (float)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (double)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, (decimal)2, "102" };
            yield return new object[] { new char[] { '1', '0' }, "2", "102" };
            yield return new object[] { new char[] { '1', '0' }, new char[] { '2' }, "102" };
            yield return new object[] { new char[] { '1', '0' }, true, "10True" };
            yield return new object[] { new char[] { '1', '0' }, DBNull.Value, "10" };
            yield return new object[] { new char[] { '1', '0' }, null, "10" };

            // bool.
            yield return new object[] { true, (byte)2, "True2" };
            yield return new object[] { true, (sbyte)2, "True2" };
            yield return new object[] { true, (ushort)2, "True2" };
            yield return new object[] { true, (short)2, "True2" };
            yield return new object[] { true, (uint)2, "True2" };
            yield return new object[] { true, 2, "True2" };
            yield return new object[] { true, (ulong)2, "True2" };
            yield return new object[] { true, (long)2, "True2" };
            yield return new object[] { true, (float)2, "True2" };
            yield return new object[] { true, (double)2, "True2" };
            yield return new object[] { true, (decimal)2, "True2" };
            yield return new object[] { true, "2", "True2" };
            yield return new object[] { true, new char[] { '2' }, "True2" };
            yield return new object[] { true, true, "TrueTrue" };
            yield return new object[] { true, DBNull.Value, "True" };
            yield return new object[] { true, null, "True" };

            // DBNull.Value.
            yield return new object[] { DBNull.Value, (byte)2, "2" };
            yield return new object[] { DBNull.Value, (sbyte)2, "2" };
            yield return new object[] { DBNull.Value, (ushort)2, "2" };
            yield return new object[] { DBNull.Value, (short)2, "2" };
            yield return new object[] { DBNull.Value, (uint)2, "2" };
            yield return new object[] { DBNull.Value, 2, "2" };
            yield return new object[] { DBNull.Value, (ulong)2, "2" };
            yield return new object[] { DBNull.Value, (long)2, "2" };
            yield return new object[] { DBNull.Value, (float)2, "2" };
            yield return new object[] { DBNull.Value, (double)2, "2" };
            yield return new object[] { DBNull.Value, (decimal)2, "2" };
            yield return new object[] { DBNull.Value, "2", "2" };
            yield return new object[] { DBNull.Value, new char[] { '2' }, "2" };
            yield return new object[] { DBNull.Value, true, "True" };
            yield return new object[] { DBNull.Value, DBNull.Value, DBNull.Value };
            yield return new object[] { DBNull.Value, null, "" };

            // null.
            yield return new object[] { null, (byte)2, "2" };
            yield return new object[] { null, (sbyte)2, "2" };
            yield return new object[] { null, (ushort)2, "2" };
            yield return new object[] { null, (short)2, "2" };
            yield return new object[] { null, (uint)2, "2" };
            yield return new object[] { null, 2, "2" };
            yield return new object[] { null, (ulong)2, "2" };
            yield return new object[] { null, (long)2, "2" };
            yield return new object[] { null, (float)2, "2" };
            yield return new object[] { null, (double)2, "2" };
            yield return new object[] { null, (decimal)2, "2" };
            yield return new object[] { null, "2", "2" };
            yield return new object[] { null, new char[] { '2' }, "2" };
            yield return new object[] { null, true, "True" };
            yield return new object[] { null, DBNull.Value, "" };
            yield return new object[] { null, null, "" };

            // object.
            yield return new object[] { new ConcatenateObject(), 2, "custom" };
            yield return new object[] { 2, new ConcatenateObject(), "motsuc" };
            yield return new object[] { new ConcatenateObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new ConcatenateObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(ConcatenateObject_TestData))]
        public void ConcatenateObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.ConcatenateObject(left, right));
        }

        public static IEnumerable<object[]> ConcatenateObject_InvalidObjects_TestData()
        {
            yield return new object[] { '3', new object() };

            yield return new object[] { new char[] { '8' }, new object() };
            yield return new object[] { new object(), new char[] { '8' } };
        }

        [Theory]
        [MemberData(nameof(ConcatenateObject_InvalidObjects_TestData))]
        public void ConcatenateObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ConcatenateObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.ConcatenateObject(right, left));
        }

        public static IEnumerable<object[]> ConcatenateObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new ConcatenateObject(), new object() };
            yield return new object[] { new object(), new ConcatenateObject() };

            yield return new object[] { new ConcatenateObject(), new ConcatenateObject() };
        }

        [Theory]
        [MemberData(nameof(ConcatenateObject_MismatchingObjects_TestData))]
        public void ConcatenateObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ConcatenateObject(left, right));
        }

        public class ConcatenateObject
        {
            [SpecialName]
            public static string op_Concatenate(ConcatenateObject left, int right) => "custom";

            [SpecialName]
            public static string op_Concatenate(int left, ConcatenateObject right) => "motsuc";

            [SpecialName]
            public static string op_Concatenate(ConcatenateObject left, OperatorsTests right) => "customobject";

            [SpecialName]
            public static string op_Concatenate(OperatorsTests left, ConcatenateObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> DivideObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)4, (byte)2, (double)2 };
            yield return new object[] { (byte)8, (sbyte)2, (double)4 };
            yield return new object[] { (byte)12, (ushort)2, (double)6 };
            yield return new object[] { (byte)16, (short)2, (double)8 };
            yield return new object[] { (byte)20, (uint)2, (double)10 };
            yield return new object[] { (byte)24, 2, (double)12 };
            yield return new object[] { (byte)28, (long)2, (double)14 };
            yield return new object[] { (byte)32, (ulong)2, (double)16 };
            yield return new object[] { (byte)36, (float)2, (float)18 };
            yield return new object[] { (byte)40, (double)2, (double)20 };
            yield return new object[] { (byte)44, (decimal)2, (decimal)22 };
            yield return new object[] { (byte)48, "2", (double)24 };
            yield return new object[] { (byte)52, true, (double)(-52) };
            yield return new object[] { (byte)56, null, double.PositiveInfinity };

            // sbyte.
            yield return new object[] { (sbyte)4, (byte)2, (double)2 };
            yield return new object[] { (sbyte)8, (sbyte)2, (double)4 };
            yield return new object[] { (sbyte)12, (ushort)2, (double)6 };
            yield return new object[] { (sbyte)16, (short)2, (double)8 };
            yield return new object[] { (sbyte)20, (uint)2, (double)10 };
            yield return new object[] { (sbyte)24, 2, (double)12 };
            yield return new object[] { (sbyte)28, (long)2, (double)14 };
            yield return new object[] { (sbyte)32, (ulong)2, (double)16 };
            yield return new object[] { (sbyte)36, (float)2, (float)18 };
            yield return new object[] { (sbyte)40, (double)2, (double)20 };
            yield return new object[] { (sbyte)44, (decimal)2, (decimal)22 };
            yield return new object[] { (sbyte)48, "2", (double)24 };
            yield return new object[] { (sbyte)52, true, (double)(-52) };
            yield return new object[] { (sbyte)56, null, double.PositiveInfinity };

            // ushort.
            yield return new object[] { (ushort)4, (byte)2, (double)2 };
            yield return new object[] { (ushort)8, (sbyte)2, (double)4 };
            yield return new object[] { (ushort)12, (ushort)2, (double)6 };
            yield return new object[] { (ushort)16, (short)2, (double)8 };
            yield return new object[] { (ushort)20, (uint)2, (double)10 };
            yield return new object[] { (ushort)24, 2, (double)12 };
            yield return new object[] { (ushort)28, (long)2, (double)14 };
            yield return new object[] { (ushort)32, (ulong)2, (double)16 };
            yield return new object[] { (ushort)36, (float)2, (float)18 };
            yield return new object[] { (ushort)40, (double)2, (double)20 };
            yield return new object[] { (ushort)44, (decimal)2, (decimal)22 };
            yield return new object[] { (ushort)48, "2", (double)24 };
            yield return new object[] { (ushort)52, true, (double)(-52) };
            yield return new object[] { (ushort)56, null, double.PositiveInfinity };

            // short.
            yield return new object[] { (short)4, (byte)2, (double)2 };
            yield return new object[] { (short)8, (sbyte)2, (double)4 };
            yield return new object[] { (short)12, (ushort)2, (double)6 };
            yield return new object[] { (short)16, (short)2, (double)8 };
            yield return new object[] { (short)20, (uint)2, (double)10 };
            yield return new object[] { (short)24, 2, (double)12 };
            yield return new object[] { (short)28, (long)2, (double)14 };
            yield return new object[] { (short)32, (ulong)2, (double)16 };
            yield return new object[] { (short)36, (float)2, (float)18 };
            yield return new object[] { (short)40, (double)2, (double)20 };
            yield return new object[] { (short)44, (decimal)2, (decimal)22 };
            yield return new object[] { (short)48, "2", (double)24 };
            yield return new object[] { (short)52, true, (double)(-52) };
            yield return new object[] { (short)56, null, double.PositiveInfinity };

            // uint.
            yield return new object[] { (uint)4, (byte)2, (double)2 };
            yield return new object[] { (uint)8, (sbyte)2, (double)4 };
            yield return new object[] { (uint)12, (ushort)2, (double)6 };
            yield return new object[] { (uint)16, (short)2, (double)8 };
            yield return new object[] { (uint)20, (uint)2, (double)10 };
            yield return new object[] { (uint)24, 2, (double)12 };
            yield return new object[] { (uint)28, (long)2, (double)14 };
            yield return new object[] { (uint)32, (ulong)2, (double)16 };
            yield return new object[] { (uint)36, (float)2, (float)18 };
            yield return new object[] { (uint)40, (double)2, (double)20 };
            yield return new object[] { (uint)44, (decimal)2, (decimal)22 };
            yield return new object[] { (uint)48, "2", (double)24 };
            yield return new object[] { (uint)52, true, (double)(-52) };
            yield return new object[] { (uint)56, null, double.PositiveInfinity };

            // int.
            yield return new object[] { 4, (byte)2, (double)2 };
            yield return new object[] { 8, (sbyte)2, (double)4 };
            yield return new object[] { 12, (ushort)2, (double)6 };
            yield return new object[] { 16, (short)2, (double)8 };
            yield return new object[] { 20, (uint)2, (double)10 };
            yield return new object[] { 24, 2, (double)12 };
            yield return new object[] { 28, (long)2, (double)14 };
            yield return new object[] { 32, (ulong)2, (double)16 };
            yield return new object[] { 36, (float)2, (float)18 };
            yield return new object[] { 40, (double)2, (double)20 };
            yield return new object[] { 44, (decimal)2, (decimal)22 };
            yield return new object[] { 48, "2", (double)24 };
            yield return new object[] { 52, true, (double)(-52) };
            yield return new object[] { 56, null, double.PositiveInfinity };

            // ulong.
            yield return new object[] { (ulong)4, (byte)2, (double)2 };
            yield return new object[] { (ulong)8, (sbyte)2, (double)4 };
            yield return new object[] { (ulong)12, (ushort)2, (double)6 };
            yield return new object[] { (ulong)16, (short)2, (double)8 };
            yield return new object[] { (ulong)20, (uint)2, (double)10 };
            yield return new object[] { (ulong)24, 2, (double)12 };
            yield return new object[] { (ulong)28, (long)2, (double)14 };
            yield return new object[] { (ulong)32, (ulong)2, (double)16 };
            yield return new object[] { (ulong)36, (float)2, (float)18 };
            yield return new object[] { (ulong)40, (double)2, (double)20 };
            yield return new object[] { (ulong)44, (decimal)2, (decimal)22 };
            yield return new object[] { (ulong)48, "2", (double)24 };
            yield return new object[] { (ulong)52, true, (double)(-52) };
            yield return new object[] { (ulong)56, null, double.PositiveInfinity };

            // long + primitives.
            yield return new object[] { (long)4, (byte)2, (double)2 };
            yield return new object[] { (long)8, (sbyte)2, (double)4 };
            yield return new object[] { (long)12, (ushort)2, (double)6 };
            yield return new object[] { (long)16, (short)2, (double)8 };
            yield return new object[] { (long)20, (uint)2, (double)10 };
            yield return new object[] { (long)24, 2, (double)12 };
            yield return new object[] { (long)28, (long)2, (double)14 };
            yield return new object[] { (long)32, (ulong)2, (double)16 };
            yield return new object[] { (long)36, (float)2, (float)18 };
            yield return new object[] { (long)40, (double)2, (double)20 };
            yield return new object[] { (long)44, (decimal)2, (decimal)22 };
            yield return new object[] { (long)48, "2", (double)24 };
            yield return new object[] { (long)52, true, (double)(-52) };
            yield return new object[] { (long)56, null, double.PositiveInfinity };

            // float.
            yield return new object[] { (float)4, (byte)2, (float)2 };
            yield return new object[] { (float)8, (sbyte)2, (float)4 };
            yield return new object[] { (float)12, (ushort)2, (float)6 };
            yield return new object[] { (float)16, (short)2, (float)8 };
            yield return new object[] { (float)20, (uint)2, (float)10 };
            yield return new object[] { (float)24, 2, (float)12 };
            yield return new object[] { (float)28, (long)2, (float)14 };
            yield return new object[] { (float)32, (ulong)2, (float)16 };
            yield return new object[] { (float)36, (float)2, (float)18 };
            yield return new object[] { (float)40, (double)2, (double)20 };
            yield return new object[] { (float)44, (decimal)2, (float)22 };
            yield return new object[] { (float)48, "2", (double)24 };
            yield return new object[] { (float)52, true, (float)(-52) };
            yield return new object[] { (float)56, null, double.PositiveInfinity };
            yield return new object[] { (float)58, float.PositiveInfinity, (float)0 };
            yield return new object[] { (float)58, float.NegativeInfinity, (float)(-0.0f) };
            yield return new object[] { (float)58, float.NaN, float.NaN };
            yield return new object[] { float.PositiveInfinity, (float)2, float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, (float)2, float.NegativeInfinity };
            yield return new object[] { float.NaN, (float)2, float.NaN };

            // double.
            yield return new object[] { (double)4, (byte)2, (double)2 };
            yield return new object[] { (double)8, (sbyte)2, (double)4 };
            yield return new object[] { (double)12, (ushort)2, (double)6 };
            yield return new object[] { (double)16, (short)2, (double)8 };
            yield return new object[] { (double)20, (uint)2, (double)10 };
            yield return new object[] { (double)24, 2, (double)12 };
            yield return new object[] { (double)28, (long)2, (double)14 };
            yield return new object[] { (double)32, (ulong)2, (double)16 };
            yield return new object[] { (double)36, (float)2, (double)18 };
            yield return new object[] { (double)40, (double)2, (double)20 };
            yield return new object[] { (double)44, (decimal)2, (double)22 };
            yield return new object[] { (double)48, "2", (double)24 };
            yield return new object[] { (double)52, true, (double)(-52) };
            yield return new object[] { (double)56, null, double.PositiveInfinity };
            yield return new object[] { (double)58, double.PositiveInfinity, (double)0 };
            yield return new object[] { (double)58, double.NegativeInfinity, (double)(-0.0) };
            yield return new object[] { (double)58, double.NaN, double.NaN };
            yield return new object[] { double.PositiveInfinity, (double)2, double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, (double)2, double.NegativeInfinity };
            yield return new object[] { double.NaN, (double)2, double.NaN };

            // decimal.
            yield return new object[] { (decimal)4, (byte)2, (decimal)2 };
            yield return new object[] { (decimal)8, (sbyte)2, (decimal)4 };
            yield return new object[] { (decimal)12, (ushort)2, (decimal)6 };
            yield return new object[] { (decimal)16, (short)2, (decimal)8 };
            yield return new object[] { (decimal)20, (uint)2, (decimal)10 };
            yield return new object[] { (decimal)24, 2, (decimal)12 };
            yield return new object[] { (decimal)28, (long)2, (decimal)14 };
            yield return new object[] { (decimal)32, (ulong)2, (decimal)16 };
            yield return new object[] { (decimal)36, (float)2, (float)18 };
            yield return new object[] { (decimal)40, (double)2, (double)20 };
            yield return new object[] { (decimal)44, (decimal)2, (decimal)22 };
            yield return new object[] { (decimal)48, "2", (double)24 };
            yield return new object[] { (decimal)52, true, (decimal)(-52) };
            yield return new object[] { decimal.MaxValue, (decimal)0.5, float.Parse("1.58456325028529E+29", NumberStyles.Any, CultureInfo.InvariantCulture) };

            // string + primitives
            yield return new object[] { "4", (byte)2, (double)2 };
            yield return new object[] { "8", (sbyte)2, (double)4 };
            yield return new object[] { "12", (ushort)2, (double)6 };
            yield return new object[] { "16", (short)2, (double)8 };
            yield return new object[] { "20", (uint)2, (double)10 };
            yield return new object[] { "24", 2, (double)12 };
            yield return new object[] { "28", (long)2, (double)14 };
            yield return new object[] { "32", (ulong)2, (double)16 };
            yield return new object[] { "36", (float)2, (double)18 };
            yield return new object[] { "40", (double)2, (double)20 };
            yield return new object[] { "44", (decimal)2, (double)22 };
            yield return new object[] { "48", "2", (double)24 };
            yield return new object[] { "52", true, (double)(-52) };
            yield return new object[] { "56", null, double.PositiveInfinity };

            // bool.
            yield return new object[] { true, (byte)2, (double)(-0.5) };
            yield return new object[] { true, (sbyte)2, (double)(-0.5) };
            yield return new object[] { true, (ushort)2, (double)(-0.5) };
            yield return new object[] { true, (short)2, (double)(-0.5) };
            yield return new object[] { true, (uint)2, (double)(-0.5) };
            yield return new object[] { true, 2, (double)(-0.5) };
            yield return new object[] { true, (long)2, (double)(-0.5) };
            yield return new object[] { true, (ulong)2, (double)(-0.5) };
            yield return new object[] { true, (float)2, (float)(-0.5) };
            yield return new object[] { true, (double)2, (double)(-0.5) };
            yield return new object[] { true, (decimal)2, (decimal)(-0.5) };
            yield return new object[] { true, "2", (double)(-0.5) };
            yield return new object[] { true, true, (double)1 };
            yield return new object[] { true, null, double.NegativeInfinity };

            // null.
            yield return new object[] { null, (byte)2, (double)0 };
            yield return new object[] { null, (sbyte)2, (double)0 };
            yield return new object[] { null, (ushort)2, (double)0 };
            yield return new object[] { null, (short)2, (double)0 };
            yield return new object[] { null, (uint)2, (double)0 };
            yield return new object[] { null, 2, (double)0 };
            yield return new object[] { null, (long)2, (double)0 };
            yield return new object[] { null, (ulong)2, (double)0 };
            yield return new object[] { null, (float)2, (float)0 };
            yield return new object[] { null, (double)2, (double)0 };
            yield return new object[] { null, (decimal)2, (decimal)0 };
            yield return new object[] { null, "2", (double)0 };
            yield return new object[] { null, false, double.NaN };
            yield return new object[] { null, null, double.NaN };

            // object.
            yield return new object[] { new DivideObject(), 2, "custom" };
            yield return new object[] { 2, new DivideObject(), "motsuc" };
            yield return new object[] { new DivideObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new DivideObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(DivideObject_TestData))]
        public void DivideObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.DivideObject(left, right));
        }

        [Fact]
        public void DivideObject_DecimalWithNull_ThrowsDivideByZeroException()
        {
            Assert.Throws<DivideByZeroException>(() => Operators.DivideObject((decimal)56, null));
        }

        public static IEnumerable<object[]> DivideObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(DivideObject_InvalidObjects_TestData))]
        public void DivideObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.DivideObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.DivideObject(right, left));
        }

        public static IEnumerable<object[]> DivideObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new DivideObject(), new object() };
            yield return new object[] { new object(), new DivideObject() };

            yield return new object[] { new DivideObject(), new DivideObject() };
        }

        [Theory]
        [MemberData(nameof(DivideObject_MismatchingObjects_TestData))]
        public void DivideObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.DivideObject(left, right));
        }

        public class DivideObject
        {
            public static string operator /(DivideObject left, int right) => "custom";
            public static string operator /(int left, DivideObject right) => "motsuc";

            public static string operator /(DivideObject left, OperatorsTests right) => "customobject";
            public static string operator /(OperatorsTests left, DivideObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> ExponentObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)2, (byte)4, (double)16 };
            yield return new object[] { (byte)2, (sbyte)4, (double)16 };
            yield return new object[] { (byte)2, (ushort)4, (double)16 };
            yield return new object[] { (byte)2, (short)4, (double)16 };
            yield return new object[] { (byte)2, (uint)4, (double)16 };
            yield return new object[] { (byte)2, 4, (double)16 };
            yield return new object[] { (byte)2, (ulong)4, (double)16 };
            yield return new object[] { (byte)2, (long)4, (double)16 };
            yield return new object[] { (byte)2, (float)4, (double)16 };
            yield return new object[] { (byte)2, (double)4, (double)16 };
            yield return new object[] { (byte)2, (decimal)4, (double)16 };
            yield return new object[] { (byte)2, "4", (double)16 };
            yield return new object[] { (byte)2, true, (double)0.5 };
            yield return new object[] { (byte)2, null, (double)1 };

            // sbyte.
            yield return new object[] { (sbyte)2, (byte)4, (double)16 };
            yield return new object[] { (sbyte)2, (sbyte)4, (double)16 };
            yield return new object[] { (sbyte)2, (ushort)4, (double)16 };
            yield return new object[] { (sbyte)2, (short)4, (double)16 };
            yield return new object[] { (sbyte)2, (uint)4, (double)16 };
            yield return new object[] { (sbyte)2, 4, (double)16 };
            yield return new object[] { (sbyte)2, (ulong)4, (double)16 };
            yield return new object[] { (sbyte)2, (long)4, (double)16 };
            yield return new object[] { (sbyte)2, (float)4, (double)16 };
            yield return new object[] { (sbyte)2, (double)4, (double)16 };
            yield return new object[] { (sbyte)2, (decimal)4, (double)16 };
            yield return new object[] { (sbyte)2, "4", (double)16 };
            yield return new object[] { (sbyte)2, true, (double)0.5 };
            yield return new object[] { (sbyte)2, null, (double)1 };

            // uint.
            yield return new object[] { (uint)2, (byte)4, (double)16 };
            yield return new object[] { (uint)2, (sbyte)4, (double)16 };
            yield return new object[] { (uint)2, (ushort)4, (double)16 };
            yield return new object[] { (uint)2, (short)4, (double)16 };
            yield return new object[] { (uint)2, (uint)4, (double)16 };
            yield return new object[] { (uint)2, 4, (double)16 };
            yield return new object[] { (uint)2, (ulong)4, (double)16 };
            yield return new object[] { (uint)2, (long)4, (double)16 };
            yield return new object[] { (uint)2, (float)4, (double)16 };
            yield return new object[] { (uint)2, (double)4, (double)16 };
            yield return new object[] { (uint)2, (decimal)4, (double)16 };
            yield return new object[] { (uint)2, "4", (double)16 };
            yield return new object[] { (uint)2, true, (double)0.5 };
            yield return new object[] { (uint)2, null, (double)1 };

            // int.
            yield return new object[] { 2, (byte)4, (double)16 };
            yield return new object[] { 2, (sbyte)4, (double)16 };
            yield return new object[] { 2, (ushort)4, (double)16 };
            yield return new object[] { 2, (short)4, (double)16 };
            yield return new object[] { 2, (uint)4, (double)16 };
            yield return new object[] { 2, 4, (double)16 };
            yield return new object[] { 2, (ulong)4, (double)16 };
            yield return new object[] { 2, (long)4, (double)16 };
            yield return new object[] { 2, (float)4, (double)16 };
            yield return new object[] { 2, (double)4, (double)16 };
            yield return new object[] { 2, (decimal)4, (double)16 };
            yield return new object[] { 2, "4", (double)16 };
            yield return new object[] { 2, true, (double)0.5 };
            yield return new object[] { 2, null, (double)1 };

            // ulong.
            yield return new object[] { (ulong)2, (byte)4, (double)16 };
            yield return new object[] { (ulong)2, (sbyte)4, (double)16 };
            yield return new object[] { (ulong)2, (ushort)4, (double)16 };
            yield return new object[] { (ulong)2, (short)4, (double)16 };
            yield return new object[] { (ulong)2, (uint)4, (double)16 };
            yield return new object[] { (ulong)2, 4, (double)16 };
            yield return new object[] { (ulong)2, (ulong)4, (double)16 };
            yield return new object[] { (ulong)2, (long)4, (double)16 };
            yield return new object[] { (ulong)2, (float)4, (double)16 };
            yield return new object[] { (ulong)2, (double)4, (double)16 };
            yield return new object[] { (ulong)2, (decimal)4, (double)16 };
            yield return new object[] { (ulong)2, "4", (double)16 };
            yield return new object[] { (ulong)2, true, (double)0.5 };
            yield return new object[] { (ulong)2, null, (double)1 };

            // long.
            yield return new object[] { (long)2, (byte)4, (double)16 };
            yield return new object[] { (long)2, (sbyte)4, (double)16 };
            yield return new object[] { (long)2, (ushort)4, (double)16 };
            yield return new object[] { (long)2, (short)4, (double)16 };
            yield return new object[] { (long)2, (uint)4, (double)16 };
            yield return new object[] { (long)2, 4, (double)16 };
            yield return new object[] { (long)2, (ulong)4, (double)16 };
            yield return new object[] { (long)2, (long)4, (double)16 };
            yield return new object[] { (long)2, (float)4, (double)16 };
            yield return new object[] { (long)2, (double)4, (double)16 };
            yield return new object[] { (long)2, (decimal)4, (double)16 };
            yield return new object[] { (long)2, "4", (double)16 };
            yield return new object[] { (long)2, true, (double)0.5 };
            yield return new object[] { (long)2, null, (double)1 };

            // float.
            yield return new object[] { (float)2, (byte)4, (double)16 };
            yield return new object[] { (float)2, (sbyte)4, (double)16 };
            yield return new object[] { (float)2, (ushort)4, (double)16 };
            yield return new object[] { (float)2, (short)4, (double)16 };
            yield return new object[] { (float)2, (uint)4, (double)16 };
            yield return new object[] { (float)2, 4, (double)16 };
            yield return new object[] { (float)2, (ulong)4, (double)16 };
            yield return new object[] { (float)2, (long)4, (double)16 };
            yield return new object[] { (float)2, (float)4, (double)16 };
            yield return new object[] { (float)2, (double)4, (double)16 };
            yield return new object[] { (float)2, (decimal)4, (double)16 };
            yield return new object[] { (float)2, "4", (double)16 };
            yield return new object[] { (float)2, true, (double)0.5 };
            yield return new object[] { (float)2, null, (double)1 };

            // double.
            yield return new object[] { (double)2, (byte)4, (double)16 };
            yield return new object[] { (double)2, (sbyte)4, (double)16 };
            yield return new object[] { (double)2, (ushort)4, (double)16 };
            yield return new object[] { (double)2, (short)4, (double)16 };
            yield return new object[] { (double)2, (uint)4, (double)16 };
            yield return new object[] { (double)2, 4, (double)16 };
            yield return new object[] { (double)2, (ulong)4, (double)16 };
            yield return new object[] { (double)2, (long)4, (double)16 };
            yield return new object[] { (double)2, (float)4, (double)16 };
            yield return new object[] { (double)2, (double)4, (double)16 };
            yield return new object[] { (double)2, (decimal)4, (double)16 };
            yield return new object[] { (double)2, "4", (double)16 };
            yield return new object[] { (double)2, true, (double)0.5 };
            yield return new object[] { (double)2, null, (double)1 };

            // decimal.
            yield return new object[] { (decimal)2, (byte)4, (double)16 };
            yield return new object[] { (decimal)2, (sbyte)4, (double)16 };
            yield return new object[] { (decimal)2, (ushort)4, (double)16 };
            yield return new object[] { (decimal)2, (short)4, (double)16 };
            yield return new object[] { (decimal)2, (uint)4, (double)16 };
            yield return new object[] { (decimal)2, 4, (double)16 };
            yield return new object[] { (decimal)2, (ulong)4, (double)16 };
            yield return new object[] { (decimal)2, (long)4, (double)16 };
            yield return new object[] { (decimal)2, (float)4, (double)16 };
            yield return new object[] { (decimal)2, (double)4, (double)16 };
            yield return new object[] { (decimal)2, (decimal)4, (double)16 };
            yield return new object[] { (decimal)2, "4", (double)16 };
            yield return new object[] { (decimal)2, true, (double)0.5 };
            yield return new object[] { (decimal)2, null, (double)1 };

            // string.
            yield return new object[] { "2", (byte)4, (double)16 };
            yield return new object[] { "2", (sbyte)4, (double)16 };
            yield return new object[] { "2", (ushort)4, (double)16 };
            yield return new object[] { "2", (short)4, (double)16 };
            yield return new object[] { "2", (uint)4, (double)16 };
            yield return new object[] { "2", 4, (double)16 };
            yield return new object[] { "2", (ulong)4, (double)16 };
            yield return new object[] { "2", (long)4, (double)16 };
            yield return new object[] { "2", (float)4, (double)16 };
            yield return new object[] { "2", (double)4, (double)16 };
            yield return new object[] { "2", (decimal)4, (double)16 };
            yield return new object[] { "2", "4", (double)16 };
            yield return new object[] { "2", true, (double)0.5 };
            yield return new object[] { "2", null, (double)1 };

            // bool.
            yield return new object[] { true, (byte)4, (double)1 };
            yield return new object[] { true, (sbyte)4, (double)1 };
            yield return new object[] { true, (ushort)4, (double)1 };
            yield return new object[] { true, (short)4, (double)1 };
            yield return new object[] { true, (uint)4, (double)1 };
            yield return new object[] { true, 4, (double)1 };
            yield return new object[] { true, (ulong)4, (double)1 };
            yield return new object[] { true, (long)4, (double)1 };
            yield return new object[] { true, (float)4, (double)1 };
            yield return new object[] { true, (double)4, (double)1 };
            yield return new object[] { true, (decimal)4, (double)1 };
            yield return new object[] { true, "4", (double)1 };
            yield return new object[] { true, true, (double)(-1) };
            yield return new object[] { true, null, (double)1 };

            // null.
            yield return new object[] { null, (byte)4, (double)0 };
            yield return new object[] { null, (sbyte)4, (double)0 };
            yield return new object[] { null, (ushort)4, (double)0 };
            yield return new object[] { null, (short)4, (double)0 };
            yield return new object[] { null, (uint)4, (double)0 };
            yield return new object[] { null, 4, (double)0 };
            yield return new object[] { null, (ulong)4, (double)0 };
            yield return new object[] { null, (long)4, (double)0 };
            yield return new object[] { null, (float)4, (double)0 };
            yield return new object[] { null, (double)4, (double)0 };
            yield return new object[] { null, (decimal)4, (double)0 };
            yield return new object[] { null, "4", (double)0 };
            yield return new object[] { null, true, double.PositiveInfinity };
            yield return new object[] { null, null, (double)1 };

            // object.
            yield return new object[] { new ExponentObject(), 2, "custom" };
            yield return new object[] { 2, new ExponentObject(), "motsuc" };
            yield return new object[] { new ExponentObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new ExponentObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(ExponentObject_TestData))]
        public void ExponentObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.ExponentObject(left, right));
        }

        public static IEnumerable<object[]> ExponentObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(ExponentObject_InvalidObjects_TestData))]
        public void ExponentObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ExponentObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.ExponentObject(right, left));
        }

        public static IEnumerable<object[]> ExponentObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new ExponentObject(), new object() };
            yield return new object[] { new object(), new ExponentObject() };

            yield return new object[] { new ExponentObject(), new ExponentObject() };
        }

        [Theory]
        [MemberData(nameof(ExponentObject_MismatchingObjects_TestData))]
        public void ExponentObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ExponentObject(left, right));
        }

        public class ExponentObject
        {
            [SpecialName]
            public static string op_Exponent(ExponentObject left, int right) => "custom";

            [SpecialName]
            public static string op_Exponent(int left, ExponentObject right) => "motsuc";

            [SpecialName]
            public static string op_Exponent(ExponentObject left, OperatorsTests right) => "customobject";

            [SpecialName]
            public static string op_Exponent(OperatorsTests left, ExponentObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> IntDivideObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)4, (byte)2, (byte)2 };
            yield return new object[] { (byte)8, (sbyte)2, (short)4 };
            yield return new object[] { (byte)12, (ushort)2, (ushort)6 };
            yield return new object[] { (byte)16, (short)2, (short)8 };
            yield return new object[] { (byte)20, (uint)2, (uint)10 };
            yield return new object[] { (byte)24, 2, 12 };
            yield return new object[] { (byte)28, (long)2, (long)14 };
            yield return new object[] { (byte)32, (ulong)2, (ulong)16 };
            yield return new object[] { (byte)36, (float)2, (long)18 };
            yield return new object[] { (byte)40, (double)2, (long)20 };
            yield return new object[] { (byte)44, (decimal)2, (long)22 };
            yield return new object[] { (byte)48, "2", (long)24 };
            yield return new object[] { (byte)52, true, (short)(-52) };

            // sbyte.
            yield return new object[] { (sbyte)4, (byte)2, (short)2 };
            yield return new object[] { (sbyte)8, (sbyte)2, (sbyte)4 };
            yield return new object[] { (sbyte)12, (ushort)2, 6 };
            yield return new object[] { (sbyte)16, (short)2, (short)8 };
            yield return new object[] { (sbyte)20, (uint)2, (long)10 };
            yield return new object[] { (sbyte)24, 2, 12 };
            yield return new object[] { (sbyte)28, (long)2, (long)14 };
            yield return new object[] { (sbyte)32, (ulong)2, (long)16 };
            yield return new object[] { (sbyte)36, (float)2, (long)18 };
            yield return new object[] { (sbyte)40, (double)2, (long)20 };
            yield return new object[] { (sbyte)44, (decimal)2, (long)22 };
            yield return new object[] { (sbyte)48, "2", (long)24 };
            yield return new object[] { (sbyte)52, true, (sbyte)(-52) };
            yield return new object[] { sbyte.MinValue, (sbyte)(-1), (short)128 };

            // ushort.
            yield return new object[] { (ushort)4, (byte)2, (ushort)2 };
            yield return new object[] { (ushort)8, (sbyte)2, 4 };
            yield return new object[] { (ushort)12, (ushort)2, (ushort)6 };
            yield return new object[] { (ushort)16, (short)2, 8 };
            yield return new object[] { (ushort)20, (uint)2, (uint)10 };
            yield return new object[] { (ushort)24, 2, 12 };
            yield return new object[] { (ushort)28, (long)2, (long)14 };
            yield return new object[] { (ushort)32, (ulong)2, (ulong)16 };
            yield return new object[] { (ushort)36, (float)2, (long)18 };
            yield return new object[] { (ushort)40, (double)2, (long)20 };
            yield return new object[] { (ushort)44, (decimal)2, (long)22 };
            yield return new object[] { (ushort)48, "2", (long)24 };
            yield return new object[] { (ushort)52, true, -52 };

            // short.
            yield return new object[] { (short)4, (byte)2, (short)2 };
            yield return new object[] { (short)8, (sbyte)2, (short)4 };
            yield return new object[] { (short)12, (ushort)2, 6 };
            yield return new object[] { (short)16, (short)2, (short)8 };
            yield return new object[] { (short)20, (uint)2, (long)10 };
            yield return new object[] { (short)24, 2, 12 };
            yield return new object[] { (short)28, (long)2, (long)14 };
            yield return new object[] { (short)32, (ulong)2, (long)16 };
            yield return new object[] { (short)36, (float)2, (long)18 };
            yield return new object[] { (short)40, (double)2, (long)20 };
            yield return new object[] { (short)44, (decimal)2, (long)22 };
            yield return new object[] { (short)48, "2", (long)24 };
            yield return new object[] { (short)52, true, (short)(-52) };
            yield return new object[] { short.MinValue, (short)(-1), 32768 };

            // uint.
            yield return new object[] { (uint)4, (byte)2, (uint)2 };
            yield return new object[] { (uint)8, (sbyte)2, (long)4 };
            yield return new object[] { (uint)12, (ushort)2, (uint)6 };
            yield return new object[] { (uint)16, (short)2, (long)8 };
            yield return new object[] { (uint)20, (uint)2, (uint)10 };
            yield return new object[] { (uint)24, 2, (long)12 };
            yield return new object[] { (uint)28, (long)2, (long)14 };
            yield return new object[] { (uint)32, (ulong)2, (ulong)16 };
            yield return new object[] { (uint)36, (float)2, (long)18 };
            yield return new object[] { (uint)40, (double)2, (long)20 };
            yield return new object[] { (uint)44, (decimal)2, (long)22 };
            yield return new object[] { (uint)48, "2", (long)24 };
            yield return new object[] { (uint)52, true, (long)(-52) };

            // int.
            yield return new object[] { 4, (byte)2, 2 };
            yield return new object[] { 8, (sbyte)2, 4 };
            yield return new object[] { 12, (ushort)2, 6 };
            yield return new object[] { 16, (short)2, 8 };
            yield return new object[] { 20, (uint)2, (long)10 };
            yield return new object[] { 24, 2, 12 };
            yield return new object[] { 28, (long)2, (long)14 };
            yield return new object[] { 32, (ulong)2, (long)16 };
            yield return new object[] { 36, (float)2, (long)18 };
            yield return new object[] { 40, (double)2, (long)20 };
            yield return new object[] { 44, (decimal)2, (long)22 };
            yield return new object[] { 48, "2", (long)24 };
            yield return new object[] { 52, true, -52 };
            yield return new object[] { int.MinValue, -1, (long)2147483648 };

            // ulong.
            yield return new object[] { (ulong)4, (byte)2, (ulong)2 };
            yield return new object[] { (ulong)8, (sbyte)2, (long)4 };
            yield return new object[] { (ulong)12, (ushort)2, (ulong)6 };
            yield return new object[] { (ulong)16, (short)2, (long)8 };
            yield return new object[] { (ulong)20, (uint)2, (ulong)10 };
            yield return new object[] { (ulong)24, 2, (long)12 };
            yield return new object[] { (ulong)28, (long)2, (long)14 };
            yield return new object[] { (ulong)32, (ulong)2, (ulong)16 };
            yield return new object[] { (ulong)36, (float)2, (long)18 };
            yield return new object[] { (ulong)40, (double)2, (long)20 };
            yield return new object[] { (ulong)44, (decimal)2, (long)22 };
            yield return new object[] { (ulong)48, "2", (long)24 };
            yield return new object[] { (ulong)52, true, (long)(-52) };

            // long.
            yield return new object[] { (long)4, (byte)2, (long)2 };
            yield return new object[] { (long)8, (sbyte)2, (long)4 };
            yield return new object[] { (long)12, (ushort)2, (long)6 };
            yield return new object[] { (long)16, (short)2, (long)8 };
            yield return new object[] { (long)20, (uint)2, (long)10 };
            yield return new object[] { (long)24, 2, (long)12 };
            yield return new object[] { (long)28, (long)2, (long)14 };
            yield return new object[] { (long)32, (ulong)2, (long)16 };
            yield return new object[] { (long)36, (float)2, (long)18 };
            yield return new object[] { (long)40, (double)2, (long)20 };
            yield return new object[] { (long)44, (decimal)2, (long)22 };
            yield return new object[] { (long)48, "2", (long)24 };
            yield return new object[] { (long)52, true, (long)(-52) };

            // float.
            yield return new object[] { (float)4, (byte)2, (long)2 };
            yield return new object[] { (float)8, (sbyte)2, (long)4 };
            yield return new object[] { (float)12, (ushort)2, (long)6 };
            yield return new object[] { (float)16, (short)2, (long)8 };
            yield return new object[] { (float)20, (uint)2, (long)10 };
            yield return new object[] { (float)24, 2, (long)12 };
            yield return new object[] { (float)28, (long)2, (long)14 };
            yield return new object[] { (float)32, (ulong)2, (long)16 };
            yield return new object[] { (float)36, (float)2, (long)18 };
            yield return new object[] { (float)40, (double)2, (long)20 };
            yield return new object[] { (float)44, (decimal)2, (long)22 };
            yield return new object[] { (float)48, "2", (long)24 };
            yield return new object[] { (float)52, true, (long)(-52) };

            // double.
            yield return new object[] { (double)4, (byte)2, (long)2 };
            yield return new object[] { (double)8, (sbyte)2, (long)4 };
            yield return new object[] { (double)12, (ushort)2, (long)6 };
            yield return new object[] { (double)16, (short)2, (long)8 };
            yield return new object[] { (double)20, (uint)2, (long)10 };
            yield return new object[] { (double)24, 2, (long)12 };
            yield return new object[] { (double)28, (long)2, (long)14 };
            yield return new object[] { (double)32, (ulong)2, (long)16 };
            yield return new object[] { (double)36, (float)2, (long)18 };
            yield return new object[] { (double)40, (double)2, (long)20 };
            yield return new object[] { (double)44, (decimal)2, (long)22 };
            yield return new object[] { (double)48, "2", (long)24 };
            yield return new object[] { (double)52, true, (long)(-52) };

            // decimal.
            yield return new object[] { (decimal)4, (byte)2, (long)2 };
            yield return new object[] { (decimal)8, (sbyte)2, (long)4 };
            yield return new object[] { (decimal)12, (ushort)2, (long)6 };
            yield return new object[] { (decimal)16, (short)2, (long)8 };
            yield return new object[] { (decimal)20, (uint)2, (long)10 };
            yield return new object[] { (decimal)24, 2, (long)12 };
            yield return new object[] { (decimal)28, (long)2, (long)14 };
            yield return new object[] { (decimal)32, (ulong)2, (long)16 };
            yield return new object[] { (decimal)36, (float)2, (long)18 };
            yield return new object[] { (decimal)40, (double)2, (long)20 };
            yield return new object[] { (decimal)44, (decimal)2, (long)22 };
            yield return new object[] { (decimal)48, "2", (long)24 };
            yield return new object[] { (decimal)52, true, (long)(-52) };
            
            // string.
            yield return new object[] { "4", (byte)2, (long)2 };
            yield return new object[] { "8", (sbyte)2, (long)4 };
            yield return new object[] { "12", (ushort)2, (long)6 };
            yield return new object[] { "16", (short)2, (long)8 };
            yield return new object[] { "20", (uint)2, (long)10 };
            yield return new object[] { "24", 2, (long)12 };
            yield return new object[] { "28", (long)2, (long)14 };
            yield return new object[] { "32", (ulong)2, (long)16 };
            yield return new object[] { "36", (float)2, (long)18 };
            yield return new object[] { "40", (double)2, (long)20 };
            yield return new object[] { "44", (decimal)2, (long)22 };
            yield return new object[] { "48", "2", (long)24 };
            yield return new object[] { "52", true, (long)(-52) };
 
            // bool.
            yield return new object[] { true, (byte)2, (short)0 };
            yield return new object[] { true, (sbyte)2, (sbyte)0 };
            yield return new object[] { true, (ushort)2, 0 };
            yield return new object[] { true, (short)2, (short)0 };
            yield return new object[] { true, (uint)2, (long)0 };
            yield return new object[] { true, 2, 0 };
            yield return new object[] { true, (long)2, (long)0 };
            yield return new object[] { true, (ulong)2, (long)0 };
            yield return new object[] { true, (float)2, (long)0 };
            yield return new object[] { true, (double)2, (long)0 };
            yield return new object[] { true, (decimal)2, (long)0 };
            yield return new object[] { true, "2", (long)0 };
            yield return new object[] { true, true, (short)1 };

            // null.
            yield return new object[] { null, (byte)2, (byte)0 };
            yield return new object[] { null, (sbyte)2, (sbyte)0 };
            yield return new object[] { null, (ushort)2, (ushort)0 };
            yield return new object[] { null, (short)2, (short)0 };
            yield return new object[] { null, (uint)2, (uint)0 };
            yield return new object[] { null, 2, 0 };
            yield return new object[] { null, (long)2, (long)0 };
            yield return new object[] { null, (ulong)2, (ulong)0 };
            yield return new object[] { null, (float)2, (long)0 };
            yield return new object[] { null, (double)2, (long)0 };
            yield return new object[] { null, (decimal)2, (long)0 };
            yield return new object[] { null, "2", (long)0 };

            // object.
            yield return new object[] { new IntDivideObject(), 2, "custom" };
            yield return new object[] { 2, new IntDivideObject(), "motsuc" };
            yield return new object[] { new IntDivideObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new IntDivideObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(IntDivideObject_TestData))]
        public void IntDivideObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.IntDivideObject(left, right));
        }

        public static IEnumerable<object[]> IntDivideObject_DivideByZero_TestData()
        {
            yield return new object[] { (byte)1 };
            yield return new object[] { (sbyte)2 };
            yield return new object[] { (ushort)1 };
            yield return new object[] { (short)1 };
            yield return new object[] { (uint)1 };
            yield return new object[] { (int)1 };
            yield return new object[] { (ulong)1 };
            yield return new object[] { (long)1 };
            yield return new object[] { (float)1 };
            yield return new object[] { (double)1 };
            yield return new object[] { (decimal)1 };
            yield return new object[] { "1" };
            yield return new object[] { true };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(IntDivideObject_DivideByZero_TestData))]
        public void IntDivideObject_NullRight_ThrowsDivideByZeroException(object left)
        {
            Assert.Throws<DivideByZeroException>(() => Operators.IntDivideObject(left, null));
            Assert.Throws<DivideByZeroException>(() => Operators.IntDivideObject(left, false));
            Assert.Throws<DivideByZeroException>(() => Operators.IntDivideObject(left, 0));
        }

        public static IEnumerable<object[]> IntDivideObject_Overflow_TestData()
        {
            yield return new object[] { long.MinValue, -1 };

            yield return new object[] { (float)58, float.PositiveInfinity };
            yield return new object[] { (float)58, float.NegativeInfinity };
            yield return new object[] { (float)58, float.NaN };
            yield return new object[] { float.PositiveInfinity, (float)2 };
            yield return new object[] { float.NegativeInfinity, (float)2 };
            yield return new object[] { float.NaN, (float)2 };

            yield return new object[] { (double)58, double.PositiveInfinity };
            yield return new object[] { (double)58, double.NegativeInfinity };
            yield return new object[] { (double)58, double.NaN };
            yield return new object[] { double.PositiveInfinity, (double)2 };
            yield return new object[] { double.NegativeInfinity, (double)2 };
            yield return new object[] { double.NaN, (double)2 };
        }

        [Theory]
        [MemberData(nameof(IntDivideObject_Overflow_TestData))]
        public void IntDivideObject_ResultOverflows_ThrowsOverflowException(object left, object right)
        {
            Assert.Throws<OverflowException>(() => Operators.IntDivideObject(left, right));
        }

        public static IEnumerable<object[]> IntDivideObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(IntDivideObject_InvalidObjects_TestData))]
        public void IntDivideObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.IntDivideObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.IntDivideObject(right, left));
        }

        public static IEnumerable<object[]> IntDivideObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new IntDivideObject(), new object() };
            yield return new object[] { new object(), new IntDivideObject() };

            yield return new object[] { new IntDivideObject(), new IntDivideObject() };
        }

        [Theory]
        [MemberData(nameof(IntDivideObject_MismatchingObjects_TestData))]
        public void IntDivideObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.IntDivideObject(left, right));
        }

        public class IntDivideObject
        {
            [SpecialName]
            public static string op_IntegerDivision(IntDivideObject left, int right) => "custom";

            [SpecialName]
            public static string op_IntegerDivision(int left, IntDivideObject right) => "motsuc";

            [SpecialName]
            public static string op_IntegerDivision(IntDivideObject left, OperatorsTests right) => "customobject";

            [SpecialName]
            public static string op_IntegerDivision(OperatorsTests left, IntDivideObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> LeftShiftObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)10, (byte)2, (byte)40 };
            yield return new object[] { (byte)10, (sbyte)2, (byte)40 };
            yield return new object[] { (byte)10, (ushort)2, (byte)40 };
            yield return new object[] { (byte)10, (short)2, (byte)40 };
            yield return new object[] { (byte)10, (uint)2, (byte)40 };
            yield return new object[] { (byte)10, 2, (byte)40 };
            yield return new object[] { (byte)10, (ulong)2, (byte)40 };
            yield return new object[] { (byte)10, (long)2, (byte)40 };
            yield return new object[] { (byte)10, (float)2, (byte)40 };
            yield return new object[] { (byte)10, (double)2, (byte)40 };
            yield return new object[] { (byte)10, (decimal)2, (byte)40 };
            yield return new object[] { (byte)10, "2", (byte)40 };
            yield return new object[] { (byte)10, true, (byte)0 };
            yield return new object[] { (byte)10, null, (byte)10 };

            // sbyte.
            yield return new object[] { (sbyte)10, (byte)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (sbyte)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (ushort)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (short)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (uint)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, 2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (ulong)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (long)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (float)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (double)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, (decimal)2, (sbyte)40 };
            yield return new object[] { (sbyte)10, "2", (sbyte)40 };
            yield return new object[] { (sbyte)10, true, (sbyte)0 };
            yield return new object[] { (sbyte)10, null, (sbyte)10 };

            // ushort.
            yield return new object[] { (ushort)10, (byte)2, (ushort)40 };
            yield return new object[] { (ushort)10, (sbyte)2, (ushort)40 };
            yield return new object[] { (ushort)10, (ushort)2, (ushort)40 };
            yield return new object[] { (ushort)10, (short)2, (ushort)40 };
            yield return new object[] { (ushort)10, (uint)2, (ushort)40 };
            yield return new object[] { (ushort)10, 2, (ushort)40 };
            yield return new object[] { (ushort)10, (ulong)2, (ushort)40 };
            yield return new object[] { (ushort)10, (long)2, (ushort)40 };
            yield return new object[] { (ushort)10, (float)2, (ushort)40 };
            yield return new object[] { (ushort)10, (double)2, (ushort)40 };
            yield return new object[] { (ushort)10, (decimal)2, (ushort)40 };
            yield return new object[] { (ushort)10, "2", (ushort)40 };
            yield return new object[] { (ushort)10, true, (ushort)0 };
            yield return new object[] { (ushort)10, null, (ushort)10 };

            // short.
            yield return new object[] { (short)10, (byte)2, (short)40 };
            yield return new object[] { (short)10, (sbyte)2, (short)40 };
            yield return new object[] { (short)10, (ushort)2, (short)40 };
            yield return new object[] { (short)10, (short)2, (short)40 };
            yield return new object[] { (short)10, (uint)2, (short)40 };
            yield return new object[] { (short)10, 2, (short)40 };
            yield return new object[] { (short)10, (ulong)2, (short)40 };
            yield return new object[] { (short)10, (long)2, (short)40 };
            yield return new object[] { (short)10, (float)2, (short)40 };
            yield return new object[] { (short)10, (double)2, (short)40 };
            yield return new object[] { (short)10, (decimal)2, (short)40 };
            yield return new object[] { (short)10, "2", (short)40 };
            yield return new object[] { (short)10, true, (short)0 };
            yield return new object[] { (short)10, null, (short)10 };

            // uint.
            yield return new object[] { (uint)10, (byte)2, (uint)40 };
            yield return new object[] { (uint)10, (sbyte)2, (uint)40 };
            yield return new object[] { (uint)10, (ushort)2, (uint)40 };
            yield return new object[] { (uint)10, (short)2, (uint)40 };
            yield return new object[] { (uint)10, (uint)2, (uint)40 };
            yield return new object[] { (uint)10, 2, (uint)40 };
            yield return new object[] { (uint)10, (ulong)2, (uint)40 };
            yield return new object[] { (uint)10, (long)2, (uint)40 };
            yield return new object[] { (uint)10, (float)2, (uint)40 };
            yield return new object[] { (uint)10, (double)2, (uint)40 };
            yield return new object[] { (uint)10, (decimal)2, (uint)40 };
            yield return new object[] { (uint)10, "2", (uint)40 };
            yield return new object[] { (uint)10, true, (uint)0 };
            yield return new object[] { (uint)10, null, (uint)10 };

            // int.
            yield return new object[] { 10, (byte)2, 40 };
            yield return new object[] { 10, (sbyte)2, 40 };
            yield return new object[] { 10, (ushort)2, 40 };
            yield return new object[] { 10, (short)2, 40 };
            yield return new object[] { 10, (uint)2, 40 };
            yield return new object[] { 10, 2, 40 };
            yield return new object[] { 10, (ulong)2, 40 };
            yield return new object[] { 10, (long)2, 40 };
            yield return new object[] { 10, (float)2, 40 };
            yield return new object[] { 10, (double)2, 40 };
            yield return new object[] { 10, (decimal)2, 40 };
            yield return new object[] { 10, "2", 40 };
            yield return new object[] { 10, true, 0 };
            yield return new object[] { 10, null, 10 };

            // ulong.
            yield return new object[] { (ulong)10, (byte)2, (ulong)40 };
            yield return new object[] { (ulong)10, (sbyte)2, (ulong)40 };
            yield return new object[] { (ulong)10, (ushort)2, (ulong)40 };
            yield return new object[] { (ulong)10, (short)2, (ulong)40 };
            yield return new object[] { (ulong)10, (uint)2, (ulong)40 };
            yield return new object[] { (ulong)10, 2, (ulong)40 };
            yield return new object[] { (ulong)10, (ulong)2, (ulong)40 };
            yield return new object[] { (ulong)10, (long)2, (ulong)40 };
            yield return new object[] { (ulong)10, (float)2, (ulong)40 };
            yield return new object[] { (ulong)10, (double)2, (ulong)40 };
            yield return new object[] { (ulong)10, (decimal)2, (ulong)40 };
            yield return new object[] { (ulong)10, "2", (ulong)40 };
            yield return new object[] { (ulong)10, true, (ulong)0 };
            yield return new object[] { (ulong)10, null, (ulong)10 };

            // long.
            yield return new object[] { (long)10, (byte)2, (long)40 };
            yield return new object[] { (long)10, (sbyte)2, (long)40 };
            yield return new object[] { (long)10, (ushort)2, (long)40 };
            yield return new object[] { (long)10, (short)2, (long)40 };
            yield return new object[] { (long)10, (uint)2, (long)40 };
            yield return new object[] { (long)10, 2, (long)40 };
            yield return new object[] { (long)10, (ulong)2, (long)40 };
            yield return new object[] { (long)10, (long)2, (long)40 };
            yield return new object[] { (long)10, (float)2, (long)40 };
            yield return new object[] { (long)10, (double)2, (long)40 };
            yield return new object[] { (long)10, (decimal)2, (long)40 };
            yield return new object[] { (long)10, "2", (long)40 };
            yield return new object[] { (long)10, true, (long)0 };
            yield return new object[] { (long)10, null, (long)10 };

            // float.
            yield return new object[] { (float)10, (byte)2, (long)40 };
            yield return new object[] { (float)10, (sbyte)2, (long)40 };
            yield return new object[] { (float)10, (ushort)2, (long)40 };
            yield return new object[] { (float)10, (short)2, (long)40 };
            yield return new object[] { (float)10, (uint)2, (long)40 };
            yield return new object[] { (float)10, 2, (long)40 };
            yield return new object[] { (float)10, (ulong)2, (long)40 };
            yield return new object[] { (float)10, (long)2, (long)40 };
            yield return new object[] { (float)10, (float)2, (long)40 };
            yield return new object[] { (float)10, (double)2, (long)40 };
            yield return new object[] { (float)10, (decimal)2, (long)40 };
            yield return new object[] { (float)10, "2", (long)40 };
            yield return new object[] { (float)10, true, (long)0 };
            yield return new object[] { (float)10, null, (long)10 };

            // double.
            yield return new object[] { (double)10, (byte)2, (long)40 };
            yield return new object[] { (double)10, (sbyte)2, (long)40 };
            yield return new object[] { (double)10, (ushort)2, (long)40 };
            yield return new object[] { (double)10, (short)2, (long)40 };
            yield return new object[] { (double)10, (uint)2, (long)40 };
            yield return new object[] { (double)10, 2, (long)40 };
            yield return new object[] { (double)10, (ulong)2, (long)40 };
            yield return new object[] { (double)10, (long)2, (long)40 };
            yield return new object[] { (double)10, (float)2, (long)40 };
            yield return new object[] { (double)10, (double)2, (long)40 };
            yield return new object[] { (double)10, (decimal)2, (long)40 };
            yield return new object[] { (double)10, "2", (long)40 };
            yield return new object[] { (double)10, true, (long)0 };
            yield return new object[] { (double)10, null, (long)10 };

            // decimal.
            yield return new object[] { (decimal)10, (byte)2, (long)40 };
            yield return new object[] { (decimal)10, (sbyte)2, (long)40 };
            yield return new object[] { (decimal)10, (ushort)2, (long)40 };
            yield return new object[] { (decimal)10, (short)2, (long)40 };
            yield return new object[] { (decimal)10, (uint)2, (long)40 };
            yield return new object[] { (decimal)10, 2, (long)40 };
            yield return new object[] { (decimal)10, (ulong)2, (long)40 };
            yield return new object[] { (decimal)10, (long)2, (long)40 };
            yield return new object[] { (decimal)10, (float)2, (long)40 };
            yield return new object[] { (decimal)10, (double)2, (long)40 };
            yield return new object[] { (decimal)10, (decimal)2, (long)40 };
            yield return new object[] { (decimal)10, "2", (long)40 };
            yield return new object[] { (decimal)10, true, (long)0 };
            yield return new object[] { (decimal)10, null, (long)10 };

            // string.
            yield return new object[] { "10", (byte)2, (long)40 };
            yield return new object[] { "10", (sbyte)2, (long)40 };
            yield return new object[] { "10", (ushort)2, (long)40 };
            yield return new object[] { "10", (short)2, (long)40 };
            yield return new object[] { "10", (uint)2, (long)40 };
            yield return new object[] { "10", 2, (long)40 };
            yield return new object[] { "10", (ulong)2, (long)40 };
            yield return new object[] { "10", (long)2, (long)40 };
            yield return new object[] { "10", (float)2, (long)40 };
            yield return new object[] { "10", (double)2, (long)40 };
            yield return new object[] { "10", (decimal)2, (long)40 };
            yield return new object[] { "10", "2", (long)40 };
            yield return new object[] { "10", true, (long)0 };
            yield return new object[] { "10", null, (long)10 };

            // bool.
            yield return new object[] { true, (byte)2, (short)-4 };
            yield return new object[] { true, (sbyte)2, (short)-4 };
            yield return new object[] { true, (ushort)2, (short)-4 };
            yield return new object[] { true, (short)2, (short)-4 };
            yield return new object[] { true, (uint)2, (short)-4 };
            yield return new object[] { true, 2, (short)-4 };
            yield return new object[] { true, (ulong)2, (short)-4 };
            yield return new object[] { true, (long)2, (short)-4 };
            yield return new object[] { true, (float)2, (short)-4 };
            yield return new object[] { true, (double)2, (short)-4 };
            yield return new object[] { true, (decimal)2, (short)-4 };
            yield return new object[] { true, "2", (short)-4 };
            yield return new object[] { true, true, (short)-32768 };
            yield return new object[] { true, null, (short)-1 };

            // null.
            yield return new object[] { null, (byte)2, 0 };
            yield return new object[] { null, (sbyte)2, 0 };
            yield return new object[] { null, (ushort)2, 0 };
            yield return new object[] { null, (short)2, 0 };
            yield return new object[] { null, (uint)2, 0 };
            yield return new object[] { null, 2, 0 };
            yield return new object[] { null, (ulong)2, 0 };
            yield return new object[] { null, (long)2, 0 };
            yield return new object[] { null, (float)2, 0 };
            yield return new object[] { null, (double)2, 0 };
            yield return new object[] { null, (decimal)2, 0 };
            yield return new object[] { null, "2", 0 };
            yield return new object[] { null, true, 0 };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new LeftShiftObject(), 2, "custom" };
            yield return new object[] { 2, new LeftShiftObject(), "motsuc" };
            yield return new object[] { new LeftShiftObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new LeftShiftObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(LeftShiftObject_TestData))]
        public void LeftShiftObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.LeftShiftObject(left, right));
        }

        public static IEnumerable<object[]> LeftShiftObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(LeftShiftObject_InvalidObjects_TestData))]
        public void LeftShiftObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.LeftShiftObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.LeftShiftObject(right, left));
        }

        public static IEnumerable<object[]> LeftShiftObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new LeftShiftObject(), new object() };
            yield return new object[] { new object(), new LeftShiftObject() };

            yield return new object[] { new LeftShiftObject(), new LeftShiftObject() };
        }

        [Theory]
        [MemberData(nameof(LeftShiftObject_MismatchingObjects_TestData))]
        public void LeftShiftObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.LeftShiftObject(left, right));
        }

        public class LeftShiftObject
        {
            [SpecialName]
            public static string op_LeftShift(LeftShiftObject left, int right) => "custom";

            [SpecialName]
            public static string op_LeftShift(int left, LeftShiftObject right) => "motsuc";

            [SpecialName]
            public static string op_LeftShift(LeftShiftObject left, OperatorsTests right) => "customobject";

            [SpecialName]
            public static string op_LeftShift(OperatorsTests left, LeftShiftObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> MultiplyObject_Idempotent_TestData()
        {
            // byte.
            yield return new object[] { (byte)1, (byte)2, (byte)2 };
            yield return new object[] { (byte)2, (sbyte)2, (short)4 };
            yield return new object[] { (byte)3, (ushort)2, (ushort)6 };
            yield return new object[] { (byte)4, (short)2, (short)8 };
            yield return new object[] { (byte)5, (uint)2, (uint)10 };
            yield return new object[] { (byte)6, 2, 12 };
            yield return new object[] { (byte)7, (long)2, (long)14 };
            yield return new object[] { (byte)8, (ulong)2, (ulong)16 };
            yield return new object[] { (byte)9, (float)2, (float)18 };
            yield return new object[] { (byte)10, (double)2, (double)20 };
            yield return new object[] { (byte)11, (decimal)2, (decimal)22 };
            yield return new object[] { (byte)12, "2", (double)24 };
            yield return new object[] { (byte)13, true, (short)(-13) };
            yield return new object[] { (byte)14, null, (byte)0 };
            yield return new object[] { (byte)15, byte.MaxValue, (short)3825 };
            yield return new object[] { byte.MaxValue, byte.MaxValue, 65025 };

            // sbyte.
            yield return new object[] { (sbyte)1, (byte)2, (short)2 };
            yield return new object[] { (sbyte)2, (sbyte)2, (sbyte)4 };
            yield return new object[] { (sbyte)3, (ushort)2, 6 };
            yield return new object[] { (sbyte)4, (short)2, (short)8 };
            yield return new object[] { (sbyte)5, (uint)2, (long)10 };
            yield return new object[] { (sbyte)6, 2, 12 };
            yield return new object[] { (sbyte)7, (long)2, (long)14 };
            yield return new object[] { (sbyte)8, (float)2, (float)16 };
            yield return new object[] { (sbyte)9, (ulong)2, (decimal)18 };
            yield return new object[] { (sbyte)10, (double)2, (double)20 };
            yield return new object[] { (sbyte)11, (decimal)2, (decimal)22 };
            yield return new object[] { (sbyte)12, "2", (double)24 };
            yield return new object[] { (sbyte)13, true, (sbyte)(-13) };
            yield return new object[] { (sbyte)14, null, (sbyte)0 };
            yield return new object[] { (sbyte)15, sbyte.MaxValue, (short)1905 };
            yield return new object[] { sbyte.MaxValue, sbyte.MaxValue, (short)16129 };

            // ushort.
            yield return new object[] { (ushort)1, (byte)2, (ushort)2 };
            yield return new object[] { (ushort)2, (sbyte)2, 4 };
            yield return new object[] { (ushort)3, (ushort)2, (ushort)6 };
            yield return new object[] { (ushort)4, (short)2, 8 };
            yield return new object[] { (ushort)5, (uint)2, (uint)10 };
            yield return new object[] { (ushort)6, 2, 12 };
            yield return new object[] { (ushort)7, (long)2, (long)14 };
            yield return new object[] { (ushort)8, (ulong)2, (ulong)16 };
            yield return new object[] { (ushort)9, (float)2, (float)18 };
            yield return new object[] { (ushort)10, (double)2, (double)20 };
            yield return new object[] { (ushort)11, (decimal)2, (decimal)22 };
            yield return new object[] { (ushort)12, "2", (double)24 };
            yield return new object[] { (ushort)13, true, -13 };
            yield return new object[] { (ushort)14, null, (ushort)0 };
            yield return new object[] { (ushort)15, ushort.MaxValue, 983025 };
            yield return new object[] { ushort.MaxValue, ushort.MaxValue, (long)4294836225 };

            // short.
            yield return new object[] { (short)1, (byte)2, (short)2 };
            yield return new object[] { (short)2, (sbyte)2, (short)4 };
            yield return new object[] { (short)3, (ushort)2, 6 };
            yield return new object[] { (short)4, (short)2, (short)8 };
            yield return new object[] { (short)5, (uint)2, (long)10 };
            yield return new object[] { (short)6, 2, 12 };
            yield return new object[] { (short)7, (long)2, (long)14 };
            yield return new object[] { (short)8, (ulong)2, (decimal)16 };
            yield return new object[] { (short)9, (float)2, (float)18 };
            yield return new object[] { (short)10, (double)2, (double)20 };
            yield return new object[] { (short)11, (decimal)2, (decimal)22 };
            yield return new object[] { (short)12, "2", (double)24 };
            yield return new object[] { (short)13, true, (short)(-13) };
            yield return new object[] { (short)14, null, (short)0 };
            yield return new object[] { (short)15, short.MaxValue, 491505 };
            yield return new object[] { short.MaxValue, short.MaxValue, 1073676289 };

            // uint.
            yield return new object[] { (uint)1, (byte)2, (uint)2 };
            yield return new object[] { (uint)2, (sbyte)2, (long)4 };
            yield return new object[] { (uint)3, (ushort)2, (uint)6 };
            yield return new object[] { (uint)4, (short)2, (long)8 };
            yield return new object[] { (uint)5, (uint)2, (uint)10 };
            yield return new object[] { (uint)6, 2, (long)12 };
            yield return new object[] { (uint)7, (ulong)2, (ulong)14 };
            yield return new object[] { (uint)8, (long)2, (long)16 };
            yield return new object[] { (uint)9, (float)2, (float)18 };
            yield return new object[] { (uint)10, (double)2, (double)20 };
            yield return new object[] { (uint)11, (decimal)2, (decimal)22 };
            yield return new object[] { (uint)12, "2", (double)24 };
            yield return new object[] { (uint)13, true, (long)(-13) };
            yield return new object[] { (uint)14, null, (uint)0 };
            yield return new object[] { (uint)15, uint.MaxValue, 64424509425 };
            yield return new object[] { uint.MaxValue, uint.MaxValue, (decimal)18446744065119617025 };

            // int.
            yield return new object[] { 1, (byte)2, 2 };
            yield return new object[] { 2, (sbyte)2, 4 };
            yield return new object[] { 3, (ushort)2, 6 };
            yield return new object[] { 4, (short)2, 8 };
            yield return new object[] { 5, (uint)2, (long)10 };
            yield return new object[] { 6, 2, 12 };
            yield return new object[] { 7, (ulong)2, (decimal)14 };
            yield return new object[] { 8, (long)2, (long)16 };
            yield return new object[] { 9, (float)2, (float)18 };
            yield return new object[] { 10, (double)2, (double)20 };
            yield return new object[] { 11, (decimal)2, (decimal)22 };
            yield return new object[] { 12, "2", (double)24 };
            yield return new object[] { 13, true, -13 };
            yield return new object[] { 14, null, 0 };
            yield return new object[] { 15, int.MaxValue, (long)32212254705 };
            yield return new object[] { int.MaxValue, int.MaxValue, (long)4611686014132420609 };

            // ulong.
            yield return new object[] { (ulong)1, (byte)2, (ulong)2 };
            yield return new object[] { (ulong)2, (sbyte)2, (decimal)4 };
            yield return new object[] { (ulong)3, (ushort)2, (ulong)6 };
            yield return new object[] { (ulong)4, (short)2, (decimal)8 };
            yield return new object[] { (ulong)5, (uint)2, (ulong)10 };
            yield return new object[] { (ulong)6, 2, (decimal)12 };
            yield return new object[] { (ulong)7, (ulong)2, (ulong)14 };
            yield return new object[] { (ulong)8, (long)2, (decimal)16 };
            yield return new object[] { (ulong)9, (float)2, (float)18 };
            yield return new object[] { (ulong)10, (double)2, (double)20 };
            yield return new object[] { (ulong)11, (decimal)2, (decimal)22 };
            yield return new object[] { (ulong)12, "2", (double)24 };
            yield return new object[] { (ulong)13, true, (decimal)(-13) };
            yield return new object[] { (ulong)14, null, (ulong)0 };
            yield return new object[] { (ulong)15, ulong.MaxValue, decimal.Parse("276701161105643274225", CultureInfo.InvariantCulture) };
            yield return new object[] { ulong.MaxValue, ulong.MaxValue, double.Parse("3.4028236692093846E+38", NumberStyles.Any, CultureInfo.InvariantCulture) };

            // long + primitives.
            yield return new object[] { (long)1, (byte)2, (long)2 };
            yield return new object[] { (long)2, (sbyte)2, (long)4 };
            yield return new object[] { (long)3, (ushort)2, (long)6 };
            yield return new object[] { (long)4, (short)2, (long)8 };
            yield return new object[] { (long)5, (uint)2, (long)10 };
            yield return new object[] { (long)6, 2, (long)12 };
            yield return new object[] { (long)7, (ulong)2, (decimal)14 };
            yield return new object[] { (long)8, (long)2, (long)16 };
            yield return new object[] { (long)9, (float)2, (float)18 };
            yield return new object[] { (long)10, (double)2, (double)20 };
            yield return new object[] { (long)11, (decimal)2, (decimal)22 };
            yield return new object[] { (long)12, "2", (double)24 };
            yield return new object[] { (long)13, true, (long)(-13) };
            yield return new object[] { (long)14, null, (long)0 };
            yield return new object[] { (long)15, long.MaxValue, decimal.Parse("138350580552821637105", CultureInfo.InvariantCulture) };
            yield return new object[] { long.MaxValue, long.MaxValue, double.Parse("8.5070591730234616E+37", NumberStyles.Any, CultureInfo.InvariantCulture) };

            // float + primitives
            yield return new object[] { (float)1, (byte)2, (float)2 };
            yield return new object[] { (float)2, (sbyte)2, (float)4 };
            yield return new object[] { (float)3, (ushort)2, (float)6 };
            yield return new object[] { (float)4, (short)2, (float)8 };
            yield return new object[] { (float)5, (uint)2, (float)10 };
            yield return new object[] { (float)6, 2, (float)12 };
            yield return new object[] { (float)7, (ulong)2, (float)14 };
            yield return new object[] { (float)8, (long)2, (float)16 };
            yield return new object[] { (float)9, (float)2, (float)18 };
            yield return new object[] { (float)10, (double)2, (double)20 };
            yield return new object[] { (float)11, (decimal)2, (float)22 };
            yield return new object[] { (float)12, "2", (double)24 };
            yield return new object[] { (float)13, true, (float)(-13) };
            yield return new object[] { (float)14, null, (float)0 };
            yield return new object[] { (float)15, float.MaxValue, double.Parse("5.1042351995779329E+39", NumberStyles.Any, CultureInfo.InvariantCulture) };
            yield return new object[] { float.MaxValue, float.MaxValue, double.Parse("1.1579207543382391E+77", NumberStyles.Any, CultureInfo.InvariantCulture) };
            yield return new object[] { (float)15, float.PositiveInfinity, float.PositiveInfinity };
            yield return new object[] { (float)15, float.NegativeInfinity, float.NegativeInfinity };
            yield return new object[] { (float)15, float.NaN, double.NaN };
            yield return new object[] { float.PositiveInfinity, (float)2, float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, (float)2, float.NegativeInfinity };
            yield return new object[] { float.NaN, (float)2, double.NaN };

            // double.
            yield return new object[] { (double)1, (byte)2, (double)2 };
            yield return new object[] { (double)2, (sbyte)2, (double)4 };
            yield return new object[] { (double)3, (ushort)2, (double)6 };
            yield return new object[] { (double)4, (short)2, (double)8 };
            yield return new object[] { (double)5, (uint)2, (double)10 };
            yield return new object[] { (double)6, 2, (double)12 };
            yield return new object[] { (double)7, (ulong)2, (double)14 };
            yield return new object[] { (double)8, (long)2, (double)16 };
            yield return new object[] { (double)9, (float)2, (double)18 };
            yield return new object[] { (double)10, (double)2, (double)20 };
            yield return new object[] { (double)11, (decimal)2, (double)22 };
            yield return new object[] { (double)12, "2", (double)24 };
            yield return new object[] { (double)13, true, (double)(-13) };
            yield return new object[] { (double)14, null, (double)0 };
            yield return new object[] { (double)15, double.MaxValue, double.PositiveInfinity};
            yield return new object[] { double.MaxValue, double.MaxValue, double.PositiveInfinity };
            yield return new object[] { (double)15, double.PositiveInfinity, double.PositiveInfinity };
            yield return new object[] { (double)15, double.NegativeInfinity, double.NegativeInfinity };
            yield return new object[] { (double)15, double.NaN, double.NaN };
            yield return new object[] { double.PositiveInfinity, (double)2, double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, (double)2, double.NegativeInfinity };
            yield return new object[] { double.NaN, (double)2, double.NaN };

            // decimal.
            yield return new object[] { (decimal)1, (byte)2, (decimal)2 };
            yield return new object[] { (decimal)2, (sbyte)2, (decimal)4 };
            yield return new object[] { (decimal)3, (ushort)2, (decimal)6 };
            yield return new object[] { (decimal)4, (short)2, (decimal)8 };
            yield return new object[] { (decimal)5, (uint)2, (decimal)10 };
            yield return new object[] { (decimal)6, 2, (decimal)12 };
            yield return new object[] { (decimal)7, (ulong)2, (decimal)14 };
            yield return new object[] { (decimal)8, (long)2, (decimal)16 };
            yield return new object[] { (decimal)9, (float)2, (float)18 };
            yield return new object[] { (decimal)10, (double)2, (double)20 };
            yield return new object[] { (decimal)11, (decimal)2, (decimal)22 };
            yield return new object[] { (decimal)12, "2", (double)24 };
            yield return new object[] { (decimal)13, true, (decimal)(-13) };
            yield return new object[] { (decimal)14, null, (decimal)0 };
            yield return new object[] { (decimal)15, decimal.MaxValue, double.Parse("1.1884224377139651E+30", NumberStyles.Any, CultureInfo.InvariantCulture) };

            // string + primitives
            yield return new object[] { "1", (byte)2, (double)2 };
            yield return new object[] { "2", (sbyte)2, (double)4 };
            yield return new object[] { "3", (ushort)2, (double)6 };
            yield return new object[] { "4", (short)2, (double)8 };
            yield return new object[] { "5", (uint)2, (double)10 };
            yield return new object[] { "6", 2, (double)12 };
            yield return new object[] { "7", (long)2, (double)14 };
            yield return new object[] { "8", (ulong)2, (double)16 };
            yield return new object[] { "9", (float)2, (double)18 };
            yield return new object[] { "10", (double)2, (double)20 };
            yield return new object[] { "11", (decimal)2, (double)22 };
            yield return new object[] { "12", "2", (double)24 };
            yield return new object[] { "13", true, (double)(-13) };
            yield return new object[] { "14", null, (double)0 };

            // bool.
            yield return new object[] { true, (byte)2, (short)(-2) };
            yield return new object[] { true, (sbyte)2, (sbyte)(-2) };
            yield return new object[] { true, (ushort)2, -2 };
            yield return new object[] { true, (short)2, (short)(-2) };
            yield return new object[] { true, (uint)2, (long)(-2) };
            yield return new object[] { true, 2, -2 };
            yield return new object[] { true, (long)2, (long)(-2) };
            yield return new object[] { true, (ulong)2, (decimal)(-2) };
            yield return new object[] { true, (float)2, (float)(-2) };
            yield return new object[] { true, (double)2, (double)(-2) };
            yield return new object[] { true, (decimal)2, (decimal)(-2) };
            yield return new object[] { true, "2", (double)(-2) };
            yield return new object[] { true, false, (short)0 };
            yield return new object[] { true, null, (short)0 };

            // null.
            yield return new object[] { null, (byte)2, (byte)0 };
            yield return new object[] { null, (sbyte)2, (sbyte)0 };
            yield return new object[] { null, (ushort)2, (ushort)0 };
            yield return new object[] { null, (short)2, (short)0 };
            yield return new object[] { null, (uint)2, (uint)0 };
            yield return new object[] { null, 2, 0 };
            yield return new object[] { null, (long)2, (long)0 };
            yield return new object[] { null, (ulong)2, (ulong)0 };
            yield return new object[] { null, (float)2, (float)0 };
            yield return new object[] { null, (double)2, (double)0 };
            yield return new object[] { null, (decimal)2, (decimal)0 };
            yield return new object[] { null, "2", (double)0 };
            yield return new object[] { null, false, (short)0 };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new MultiplyObject(), 2, "custom" };
            yield return new object[] { 2, new MultiplyObject(), "motsuc" };
            yield return new object[] { new MultiplyObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new MultiplyObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(MultiplyObject_Idempotent_TestData))]
        public void MultiplyObject_Convertible_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.MultiplyObject(left, right));

            if (expected is string expectedString)
            {
                string reversed = new string(expectedString.Reverse().ToArray());
                Assert.Equal(reversed, Operators.MultiplyObject(right, left));
            }
            else
            {
                Assert.Equal(expected, Operators.MultiplyObject(right, left));
            }
        }

        public static IEnumerable<object[]> MultiplyObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(MultiplyObject_InvalidObjects_TestData))]
        public void MultiplyObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.MultiplyObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.MultiplyObject(right, left));
        }

        public static IEnumerable<object[]> MultiplyObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new MultiplyObject(), new object() };
            yield return new object[] { new object(), new MultiplyObject() };

            yield return new object[] { new MultiplyObject(), new MultiplyObject() };
        }

        [Theory]
        [MemberData(nameof(MultiplyObject_MismatchingObjects_TestData))]
        public void MultiplyObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.MultiplyObject(left, right));
        }

        public class MultiplyObject
        {
            public static string operator *(MultiplyObject left, int right) => "custom";
            public static string operator *(int left, MultiplyObject right) => "motsuc";

            public static string operator *(MultiplyObject left, OperatorsTests right) => "customobject";
            public static string operator *(OperatorsTests left, MultiplyObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> ModObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)20, (byte)3, (byte)2 };
            yield return new object[] { (byte)20, (ByteEnum)3, (byte)2 };
            yield return new object[] { (byte)20, (sbyte)3, (short)2 };
            yield return new object[] { (byte)20, (SByteEnum)3, (short)2 };
            yield return new object[] { (byte)20, (ushort)3, (ushort)2 };
            yield return new object[] { (byte)20, (UShortEnum)3, (ushort)2 };
            yield return new object[] { (byte)20, (short)3, (short)2 };
            yield return new object[] { (byte)20, (ShortEnum)3, (short)2 };
            yield return new object[] { (byte)20, (uint)3, (uint)2 };
            yield return new object[] { (byte)20, (UIntEnum)3, (uint)2 };
            yield return new object[] { (byte)20, 3, 2 };
            yield return new object[] { (byte)20, (IntEnum)3, 2 };
            yield return new object[] { (byte)20, (ulong)3, (ulong)2 };
            yield return new object[] { (byte)20, (ULongEnum)3, (ulong)2 };
            yield return new object[] { (byte)20, (long)3, (long)2 };
            yield return new object[] { (byte)20, (LongEnum)3, (long)2 };
            yield return new object[] { (byte)20, (float)3, (float)2 };
            yield return new object[] { (byte)20, (double)3, (double)2 };
            yield return new object[] { (byte)20, (decimal)3, (decimal)2 };
            yield return new object[] { (byte)20, "3", (double)2 };
            yield return new object[] { (byte)20, true, (short)0 };

            // sbyte.
            yield return new object[] { (sbyte)20, (byte)3, (short)2 };
            yield return new object[] { (sbyte)20, (ByteEnum)3, (short)2 };
            yield return new object[] { (sbyte)20, (sbyte)3, (sbyte)2 };
            yield return new object[] { (sbyte)20, (SByteEnum)3, (sbyte)2 };
            yield return new object[] { (sbyte)20, (ushort)3, 2 };
            yield return new object[] { (sbyte)20, (UShortEnum)3, 2 };
            yield return new object[] { (sbyte)20, (short)3, (short)2 };
            yield return new object[] { (sbyte)20, (ShortEnum)3, (short)2 };
            yield return new object[] { (sbyte)20, (uint)3, (long)2 };
            yield return new object[] { (sbyte)20, (UIntEnum)3, (long)2 };
            yield return new object[] { (sbyte)20, 3, 2 };
            yield return new object[] { (sbyte)20, (IntEnum)3, 2 };
            yield return new object[] { (sbyte)20, (ulong)3, (decimal)2 };
            yield return new object[] { (sbyte)20, (ULongEnum)3, (decimal)2 };
            yield return new object[] { (sbyte)20, (long)3, (long)2 };
            yield return new object[] { (sbyte)20, (LongEnum)3, (long)2 };
            yield return new object[] { (sbyte)20, (float)3, (float)2 };
            yield return new object[] { (sbyte)20, (double)3, (double)2 };
            yield return new object[] { (sbyte)20, (decimal)3, (decimal)2 };
            yield return new object[] { (sbyte)20, "3", (double)2 };
            yield return new object[] { (sbyte)20, true, (sbyte)0 };
            yield return new object[] { (sbyte)20, (sbyte)(-1), (sbyte)0 };
            yield return new object[] { (sbyte)0, (sbyte)1, (sbyte)0 };
            yield return new object[] { (sbyte)0, (sbyte)(-1), (sbyte)0 };
            yield return new object[] { (sbyte)(-20), (sbyte)1, (sbyte)0 };
            yield return new object[] { (sbyte)(-20), (sbyte)(-1), (sbyte)0 };
            yield return new object[] { sbyte.MaxValue, sbyte.MinValue, sbyte.MaxValue };
            yield return new object[] { sbyte.MaxValue, sbyte.MinValue, sbyte.MaxValue };
            yield return new object[] { sbyte.MaxValue, (sbyte)(-1), (sbyte)0 };
            yield return new object[] { sbyte.MinValue, sbyte.MinValue, (sbyte)0 };
            yield return new object[] { sbyte.MinValue, sbyte.MinValue, (sbyte)0 };
            yield return new object[] { sbyte.MinValue, (sbyte)(-1), (sbyte)0 };

            // ushort.
            yield return new object[] { (ushort)20, (byte)3, (ushort)2 };
            yield return new object[] { (ushort)20, (ByteEnum)3, (ushort)2 };
            yield return new object[] { (ushort)20, (sbyte)3, 2 };
            yield return new object[] { (ushort)20, (SByteEnum)3, 2 };
            yield return new object[] { (ushort)20, (ushort)3, (ushort)2 };
            yield return new object[] { (ushort)20, (UShortEnum)3, (ushort)2 };
            yield return new object[] { (ushort)20, (short)3, 2 };
            yield return new object[] { (ushort)20, (ShortEnum)3, 2 };
            yield return new object[] { (ushort)20, (uint)3, (uint)2 };
            yield return new object[] { (ushort)20, (UIntEnum)3, (uint)2 };
            yield return new object[] { (ushort)20, 3, 2 };
            yield return new object[] { (ushort)20, (IntEnum)3, 2 };
            yield return new object[] { (ushort)20, (ulong)3, (ulong)2 };
            yield return new object[] { (ushort)20, (ULongEnum)3, (ulong)2 };
            yield return new object[] { (ushort)20, (long)3, (long)2 };
            yield return new object[] { (ushort)20, (LongEnum)3, (long)2 };
            yield return new object[] { (ushort)20, (float)3, (float)2 };
            yield return new object[] { (ushort)20, (double)3, (double)2 };
            yield return new object[] { (ushort)20, (decimal)3, (decimal)2 };
            yield return new object[] { (ushort)20, "3", (double)2 };
            yield return new object[] { (ushort)20, true, 0 };

            // short.
            yield return new object[] { (short)20, (byte)3, (short)2 };
            yield return new object[] { (short)20, (ByteEnum)3, (short)2 };
            yield return new object[] { (short)20, (sbyte)3, (short)2 };
            yield return new object[] { (short)20, (SByteEnum)3, (short)2 };
            yield return new object[] { (short)20, (ushort)3, 2 };
            yield return new object[] { (short)20, (UShortEnum)3, 2 };
            yield return new object[] { (short)20, (short)3, (short)2 };
            yield return new object[] { (short)20, (ShortEnum)3, (short)2 };
            yield return new object[] { (short)20, (uint)3, (long)2 };
            yield return new object[] { (short)20, (UIntEnum)3, (long)2 };
            yield return new object[] { (short)20, 3, 2 };
            yield return new object[] { (short)20, (IntEnum)3, 2 };
            yield return new object[] { (short)20, (ulong)3, (decimal)2 };
            yield return new object[] { (short)20, (ULongEnum)3, (decimal)2 };
            yield return new object[] { (short)20, (long)3, (long)2 };
            yield return new object[] { (short)20, (LongEnum)3, (long)2 };
            yield return new object[] { (short)20, (float)3, (float)2 };
            yield return new object[] { (short)20, (double)3, (double)2 };
            yield return new object[] { (short)20, (decimal)3, (decimal)2 };
            yield return new object[] { (short)20, "3", (double)2 };
            yield return new object[] { (short)20, true, (short)0 };
            yield return new object[] { (short)20, (short)(-1), (short)0 };
            yield return new object[] { (short)0, (short)1, (short)0 };
            yield return new object[] { (short)0, (short)(-1), (short)0 };
            yield return new object[] { (short)(-20), (short)1, (short)0 };
            yield return new object[] { (short)(-20), (short)(-1), (short)0 };
            yield return new object[] { short.MaxValue, short.MinValue, short.MaxValue };
            yield return new object[] { short.MaxValue, short.MinValue, short.MaxValue };
            yield return new object[] { short.MaxValue, (short)(-1), (short)0 };
            yield return new object[] { short.MinValue, short.MinValue, (short)0 };
            yield return new object[] { short.MinValue, short.MinValue, (short)0 };
            yield return new object[] { short.MinValue, (short)(-1), (short)0 };

            // uint.
            yield return new object[] { (uint)20, (byte)3, (uint)2 };
            yield return new object[] { (uint)20, (ByteEnum)3, (uint)2 };
            yield return new object[] { (uint)20, (sbyte)3, (long)2 };
            yield return new object[] { (uint)20, (SByteEnum)3, (long)2 };
            yield return new object[] { (uint)20, (ushort)3, (uint)2 };
            yield return new object[] { (uint)20, (UShortEnum)3, (uint)2 };
            yield return new object[] { (uint)20, (short)3, (long)2 };
            yield return new object[] { (uint)20, (ShortEnum)3, (long)2 };
            yield return new object[] { (uint)20, (uint)3, (uint)2 };
            yield return new object[] { (uint)20, (UIntEnum)3, (uint)2 };
            yield return new object[] { (uint)20, 3, (long)2 };
            yield return new object[] { (uint)20, (IntEnum)3, (long)2 };
            yield return new object[] { (uint)20, (ulong)3, (ulong)2 };
            yield return new object[] { (uint)20, (ULongEnum)3, (ulong)2 };
            yield return new object[] { (uint)20, (long)3, (long)2 };
            yield return new object[] { (uint)20, (LongEnum)3, (long)2 };
            yield return new object[] { (uint)20, (float)3, (float)2 };
            yield return new object[] { (uint)20, (double)3, (double)2 };
            yield return new object[] { (uint)20, (decimal)3, (decimal)2 };
            yield return new object[] { (uint)20, "3", (double)2 };
            yield return new object[] { (uint)20, true, (long)0 };

            // int.
            yield return new object[] { 20, (byte)3, 2 };
            yield return new object[] { 20, (ByteEnum)3, 2 };
            yield return new object[] { 20, (sbyte)3, 2 };
            yield return new object[] { 20, (SByteEnum)3, 2 };
            yield return new object[] { 20, (ushort)3, 2 };
            yield return new object[] { 20, (UShortEnum)3, 2 };
            yield return new object[] { 20, (short)3, 2 };
            yield return new object[] { 20, (ShortEnum)3, 2 };
            yield return new object[] { 20, (uint)3, (long)2 };
            yield return new object[] { 20, (UIntEnum)3, (long)2 };
            yield return new object[] { 20, 3, 2 };
            yield return new object[] { 20, (IntEnum)3, 2 };
            yield return new object[] { 20, (ulong)3, (decimal)2 };
            yield return new object[] { 20, (ULongEnum)3, (decimal)2 };
            yield return new object[] { 20, (long)3, (long)2 };
            yield return new object[] { 20, (LongEnum)3, (long)2 };
            yield return new object[] { 20, (float)3, (float)2 };
            yield return new object[] { 20, (double)3, (double)2 };
            yield return new object[] { 20, (decimal)3, (decimal)2 };
            yield return new object[] { 20, "3", (double)2 };
            yield return new object[] { 20, true, 0 };
            yield return new object[] { 20, -1, 0 };
            yield return new object[] { 0, 1, 0 };
            yield return new object[] { 0, -1, 0 };
            yield return new object[] { -20, 1, 0 };
            yield return new object[] { -20, -1, 0 };
            yield return new object[] { int.MaxValue, int.MinValue, int.MaxValue };
            yield return new object[] { int.MaxValue, int.MinValue, int.MaxValue };
            yield return new object[] { int.MaxValue, -1, 0 };
            yield return new object[] { int.MinValue, int.MinValue, 0 };
            yield return new object[] { int.MinValue, int.MinValue, 0 };
            yield return new object[] { int.MinValue, -1, 0 };

            // ulong.
            yield return new object[] { (ulong)20, (byte)3, (ulong)2 };
            yield return new object[] { (ulong)20, (ByteEnum)3, (ulong)2 };
            yield return new object[] { (ulong)20, (sbyte)3, (decimal)2 };
            yield return new object[] { (ulong)20, (SByteEnum)3, (decimal)2 };
            yield return new object[] { (ulong)20, (ushort)3, (ulong)2 };
            yield return new object[] { (ulong)20, (UShortEnum)3, (ulong)2 };
            yield return new object[] { (ulong)20, (short)3, (decimal)2 };
            yield return new object[] { (ulong)20, (ShortEnum)3, (decimal)2 };
            yield return new object[] { (ulong)20, (uint)3, (ulong)2 };
            yield return new object[] { (ulong)20, (UIntEnum)3, (ulong)2 };
            yield return new object[] { (ulong)20, 3, (decimal)2 };
            yield return new object[] { (ulong)20, (IntEnum)3, (decimal)2 };
            yield return new object[] { (ulong)20, (ulong)3, (ulong)2 };
            yield return new object[] { (ulong)20, (ULongEnum)3, (ulong)2 };
            yield return new object[] { (ulong)20, (long)3, (decimal)2 };
            yield return new object[] { (ulong)20, (LongEnum)3, (decimal)2 };
            yield return new object[] { (ulong)20, (float)3, (float)2 };
            yield return new object[] { (ulong)20, (double)3, (double)2 };
            yield return new object[] { (ulong)20, (decimal)3, (decimal)2 };
            yield return new object[] { (ulong)20, "3", (double)2 };
            yield return new object[] { (ulong)20, true, (decimal)0 };

            // long.
            yield return new object[] { (long)20, (byte)3, (long)2 };
            yield return new object[] { (long)20, (ByteEnum)3, (long)2 };
            yield return new object[] { (long)20, (sbyte)3, (long)2 };
            yield return new object[] { (long)20, (SByteEnum)3, (long)2 };
            yield return new object[] { (long)20, (ushort)3, (long)2 };
            yield return new object[] { (long)20, (UShortEnum)3, (long)2 };
            yield return new object[] { (long)20, (short)3, (long)2 };
            yield return new object[] { (long)20, (ShortEnum)3, (long)2 };
            yield return new object[] { (long)20, (uint)3, (long)2 };
            yield return new object[] { (long)20, (UIntEnum)3, (long)2 };
            yield return new object[] { (long)20, 3, (long)2 };
            yield return new object[] { (long)20, (IntEnum)3, (long)2 };
            yield return new object[] { (long)20, (ulong)3, (decimal)2 };
            yield return new object[] { (long)20, (ULongEnum)3, (decimal)2 };
            yield return new object[] { (long)20, (long)3, (long)2 };
            yield return new object[] { (long)20, (LongEnum)3, (long)2 };
            yield return new object[] { (long)20, (float)3, (float)2 };
            yield return new object[] { (long)20, (double)3, (double)2 };
            yield return new object[] { (long)20, (decimal)3, (decimal)2 };
            yield return new object[] { (long)20, "3", (double)2 };
            yield return new object[] { (long)20, true, (long)0 };
            yield return new object[] { (long)20, (long)(-1), (long)0 };
            yield return new object[] { (long)0, (long)1, (long)0 };
            yield return new object[] { (long)0, (long)(-1), (long)0 };
            yield return new object[] { (long)(-20), (long)1, (long)0 };
            yield return new object[] { (long)(-20), (long)(-1), (long)0 };
            yield return new object[] { long.MaxValue, long.MinValue, long.MaxValue };
            yield return new object[] { long.MaxValue, long.MinValue, long.MaxValue };
            yield return new object[] { long.MaxValue, (long)(-1), (long)0 };
            yield return new object[] { long.MinValue, long.MinValue, (long)0 };
            yield return new object[] { long.MinValue, long.MinValue, (long)0 };
            yield return new object[] { long.MinValue, (long)(-1), (long)0 };

            // float.
            yield return new object[] { (float)20, (byte)3, (float)2 };
            yield return new object[] { (float)20, (ByteEnum)3, (float)2 };
            yield return new object[] { (float)20, (sbyte)3, (float)2 };
            yield return new object[] { (float)20, (SByteEnum)3, (float)2 };
            yield return new object[] { (float)20, (ushort)3, (float)2 };
            yield return new object[] { (float)20, (UShortEnum)3, (float)2 };
            yield return new object[] { (float)20, (short)3, (float)2 };
            yield return new object[] { (float)20, (ShortEnum)3, (float)2 };
            yield return new object[] { (float)20, (uint)3, (float)2 };
            yield return new object[] { (float)20, (UIntEnum)3, (float)2 };
            yield return new object[] { (float)20, 3, (float)2 };
            yield return new object[] { (float)20, (IntEnum)3, (float)2 };
            yield return new object[] { (float)20, (ulong)3, (float)2 };
            yield return new object[] { (float)20, (ULongEnum)3, (float)2 };
            yield return new object[] { (float)20, (long)3, (float)2 };
            yield return new object[] { (float)20, (LongEnum)3, (float)2 };
            yield return new object[] { (float)20, (float)3, (float)2 };
            yield return new object[] { (float)20, (double)3, (double)2 };
            yield return new object[] { (float)20, (decimal)3, (float)2 };
            yield return new object[] { (float)20, "3", (double)2 };
            yield return new object[] { (float)20, true, (float)0 };
            yield return new object[] { (float)20, false, float.NaN };
            yield return new object[] { (float)20, null, float.NaN };
            yield return new object[] { (float)20, (float)0, float.NaN };
            yield return new object[] { (float)20, (float)(-1), (float)0 };
            yield return new object[] { (float)0, (float)1, (float)0 };
            yield return new object[] { (float)0, (float)0, float.NaN };
            yield return new object[] { (float)0, (float)(-1), (float)0 };
            yield return new object[] { (float)(-20), (float)1, (float)(-0.0f) };
            yield return new object[] { (float)(-20), (float)0, float.NaN };
            yield return new object[] { (float)(-20), (float)(-1), (float)(-0.0f) };
            yield return new object[] { float.MaxValue, float.MaxValue, (float)0 };
            yield return new object[] { float.MaxValue, float.MinValue, (float)0 };
            yield return new object[] { float.MaxValue, (float)0, float.NaN };
            yield return new object[] { float.MaxValue, (float)(-1), (float)0 };
            yield return new object[] { float.MinValue, float.MaxValue, (float)(-0.0f) };
            yield return new object[] { float.MinValue, float.MinValue, (float)(-0.0f) };
            yield return new object[] { float.MinValue, (float)0, float.NaN };
            yield return new object[] { float.MinValue, (float)(-1), (float)0 };

            // double.
            yield return new object[] { (double)20, (byte)3, (double)2 };
            yield return new object[] { (double)20, (ByteEnum)3, (double)2 };
            yield return new object[] { (double)20, (sbyte)3, (double)2 };
            yield return new object[] { (double)20, (SByteEnum)3, (double)2 };
            yield return new object[] { (double)20, (ushort)3, (double)2 };
            yield return new object[] { (double)20, (UShortEnum)3, (double)2 };
            yield return new object[] { (double)20, (short)3, (double)2 };
            yield return new object[] { (double)20, (ShortEnum)3, (double)2 };
            yield return new object[] { (double)20, (uint)3, (double)2 };
            yield return new object[] { (double)20, (UIntEnum)3, (double)2 };
            yield return new object[] { (double)20, 3, (double)2 };
            yield return new object[] { (double)20, (IntEnum)3, (double)2 };
            yield return new object[] { (double)20, (ulong)3, (double)2 };
            yield return new object[] { (double)20, (ULongEnum)3, (double)2 };
            yield return new object[] { (double)20, (long)3, (double)2 };
            yield return new object[] { (double)20, (LongEnum)3, (double)2 };
            yield return new object[] { (double)20, (float)3, (double)2 };
            yield return new object[] { (double)20, (double)3, (double)2 };
            yield return new object[] { (double)20, (decimal)3, (double)2 };
            yield return new object[] { (double)20, "3", (double)2 };
            yield return new object[] { (double)20, true, (double)0 };
            yield return new object[] { (double)20, false, double.NaN };
            yield return new object[] { (double)20, null, double.NaN };
            yield return new object[] { (double)20, (double)0, double.NaN };
            yield return new object[] { (double)20, (double)(-1), (double)0 };
            yield return new object[] { (double)0, (double)1, (double)0 };
            yield return new object[] { (double)0, (double)0, double.NaN };
            yield return new object[] { (double)0, (double)(-1), (double)0 };
            yield return new object[] { (double)(-20), (double)1, (double)(-0.0) };
            yield return new object[] { (double)(-20), (double)0, double.NaN };
            yield return new object[] { (double)(-20), (double)(-1), (double)(-0.0) };
            yield return new object[] { double.MaxValue, double.MaxValue, (double)0 };
            yield return new object[] { double.MaxValue, double.MinValue, (double)0 };
            yield return new object[] { double.MaxValue, (double)0, double.NaN };
            yield return new object[] { double.MaxValue, (double)(-1), (double)0 };
            yield return new object[] { double.MinValue, double.MaxValue, (double)(-0.0) };
            yield return new object[] { double.MinValue, double.MinValue, (double)(-0.0) };
            yield return new object[] { double.MinValue, (double)0, double.NaN };
            yield return new object[] { double.MinValue, (double)(-1), (double)(-0.0) };

            // decimal.
            yield return new object[] { (decimal)20, (byte)3, (decimal)2 };
            yield return new object[] { (decimal)20, (ByteEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (sbyte)3, (decimal)2 };
            yield return new object[] { (decimal)20, (SByteEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (ushort)3, (decimal)2 };
            yield return new object[] { (decimal)20, (UShortEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (short)3, (decimal)2 };
            yield return new object[] { (decimal)20, (ShortEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (uint)3, (decimal)2 };
            yield return new object[] { (decimal)20, (UIntEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, 3, (decimal)2 };
            yield return new object[] { (decimal)20, (IntEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (ulong)3, (decimal)2 };
            yield return new object[] { (decimal)20, (ULongEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (long)3, (decimal)2 };
            yield return new object[] { (decimal)20, (LongEnum)3, (decimal)2 };
            yield return new object[] { (decimal)20, (float)3, (float)2 };
            yield return new object[] { (decimal)20, (double)3, (double)2 };
            yield return new object[] { (decimal)20, (decimal)3, (decimal)2 };
            yield return new object[] { (decimal)20, "3", (double)2 };
            yield return new object[] { (decimal)20, true, (decimal)0 };
            yield return new object[] { (decimal)20, (decimal)(-1), (decimal)0 };
            yield return new object[] { (decimal)0, (decimal)1, (decimal)0 };
            yield return new object[] { (decimal)0, (decimal)(-1), (decimal)0 };
            yield return new object[] { (decimal)(-20), (decimal)1, (decimal)0 };
            yield return new object[] { (decimal)(-20), (decimal)(-1), (decimal)0 };
            yield return new object[] { decimal.MaxValue, decimal.MinValue, (decimal)0 };
            yield return new object[] { decimal.MaxValue, decimal.MinValue, (decimal)0 };
            yield return new object[] { decimal.MaxValue, (decimal)(-1), (decimal)0 };
            yield return new object[] { decimal.MinValue, decimal.MinValue, (decimal)0 };
            yield return new object[] { decimal.MinValue, decimal.MinValue, (decimal)0 };
            yield return new object[] { decimal.MinValue, (decimal)(-1), (decimal)0 };

            // string.
            yield return new object[] { "20", (byte)3, (double)2 };
            yield return new object[] { "20", (ByteEnum)3, (double)2 };
            yield return new object[] { "20", (sbyte)3, (double)2 };
            yield return new object[] { "20", (SByteEnum)3, (double)2 };
            yield return new object[] { "20", (ushort)3, (double)2 };
            yield return new object[] { "20", (UShortEnum)3, (double)2 };
            yield return new object[] { "20", (short)3, (double)2 };
            yield return new object[] { "20", (ShortEnum)3, (double)2 };
            yield return new object[] { "20", (uint)3, (double)2 };
            yield return new object[] { "20", (UIntEnum)3, (double)2 };
            yield return new object[] { "20", 3, (double)2 };
            yield return new object[] { "20", (IntEnum)3, (double)2 };
            yield return new object[] { "20", (ulong)3, (double)2 };
            yield return new object[] { "20", (ULongEnum)3, (double)2 };
            yield return new object[] { "20", (long)3, (double)2 };
            yield return new object[] { "20", (LongEnum)3, (double)2 };
            yield return new object[] { "20", (float)3, (double)2 };
            yield return new object[] { "20", (double)3, (double)2 };
            yield return new object[] { "20", (decimal)3, (double)2 };
            yield return new object[] { "20", "3", (double)2 };
            yield return new object[] { "20", true, (double)0 };
            yield return new object[] { "20", false, double.NaN };
            yield return new object[] { "20", null, double.NaN };

            // bool.
            yield return new object[] { true, (byte)3, (short)(-1) };
            yield return new object[] { true, (ByteEnum)3, (short)(-1) };
            yield return new object[] { true, (sbyte)3, (sbyte)(-1) };
            yield return new object[] { true, (SByteEnum)3, (sbyte)(-1) };
            yield return new object[] { true, (ushort)3, -1 };
            yield return new object[] { true, (UShortEnum)3, -1 };
            yield return new object[] { true, (short)3, (short)(-1) };
            yield return new object[] { true, (ShortEnum)3, (short)(-1) };
            yield return new object[] { true, (uint)3, (long)(-1) };
            yield return new object[] { true, (UIntEnum)3, (long)(-1) };
            yield return new object[] { true, 3, -1 };
            yield return new object[] { true, (IntEnum)3, -1 };
            yield return new object[] { true, (ulong)3, (decimal)(-1) };
            yield return new object[] { true, (ULongEnum)3, (decimal)(-1) };
            yield return new object[] { true, (long)3, (long)(-1) };
            yield return new object[] { true, (LongEnum)3, (long)(-1) };
            yield return new object[] { true, (float)3, (float)(-1) };
            yield return new object[] { true, (double)3, (double)(-1) };
            yield return new object[] { true, (decimal)3, (decimal)(-1) };
            yield return new object[] { true, "3", (double)(-1) };
            yield return new object[] { true, true, (short)0 };

            // null.
            yield return new object[] { null, (byte)3, byte.MinValue };
            yield return new object[] { null, (ByteEnum)3, byte.MinValue };
            yield return new object[] { null, (sbyte)3, (sbyte)0 };
            yield return new object[] { null, (SByteEnum)3, (sbyte)0 };
            yield return new object[] { null, (ushort)3, ushort.MinValue };
            yield return new object[] { null, (UShortEnum)3, ushort.MinValue };
            yield return new object[] { null, (short)3, (short)0 };
            yield return new object[] { null, (ShortEnum)3, (short)0 };
            yield return new object[] { null, (uint)3, uint.MinValue };
            yield return new object[] { null, (UIntEnum)3, uint.MinValue };
            yield return new object[] { null, 3, 0 };
            yield return new object[] { null, (IntEnum)3, 0 };
            yield return new object[] { null, (ulong)3, ulong.MinValue };
            yield return new object[] { null, (ULongEnum)3, ulong.MinValue };
            yield return new object[] { null, (long)3, (long)0 };
            yield return new object[] { null, (LongEnum)3, (long)0 };
            yield return new object[] { null, (float)3, (float)0 };
            yield return new object[] { null, (double)3, (double)0 };
            yield return new object[] { null, (decimal)3, (decimal)0 };
            yield return new object[] { null, "3", (double)0 };
            yield return new object[] { null, true, (short)0 };

            // object.
            yield return new object[] { new ModObject(), 2, "custom" };
            yield return new object[] { 2, new ModObject(), "motsuc" };
            yield return new object[] { new ModObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new ModObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(ModObject_TestData))]
        public void ModObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
             Assert.Equal(expected, Operators.ModObject(left, right));
        }

        public static IEnumerable<object[]> ModObject_InvalidObject_TestData()
        {
            yield return new object[] { (byte)20, new char[] { '3' } };
            yield return new object[] { (byte)20, '3' };
            yield return new object[] { (byte)20, new DateTime(3) };
            yield return new object[] { (sbyte)20, new char[] { '3' } };
            yield return new object[] { (sbyte)20, '3' };
            yield return new object[] { (sbyte)20, new DateTime(3) };
            yield return new object[] { (ushort)20, new char[] { '3' } };
            yield return new object[] { (ushort)20, '3' };
            yield return new object[] { (ushort)20, new DateTime(3) };
            yield return new object[] { (short)20, new char[] { '3' } };
            yield return new object[] { (short)20, '3' };
            yield return new object[] { (short)20, new DateTime(3) };
            yield return new object[] { (uint)20, new char[] { '3' } };
            yield return new object[] { (uint)20, '3' };
            yield return new object[] { (uint)20, new DateTime(3) };
            yield return new object[] { 20, new char[] { '3' } };
            yield return new object[] { 20, '3' };
            yield return new object[] { 20, new DateTime(3) };
            yield return new object[] { (ulong)20, new char[] { '3' } };
            yield return new object[] { (ulong)20, '3' };
            yield return new object[] { (ulong)20, new DateTime(3) };
            yield return new object[] { (long)20, new char[] { '3' } };
            yield return new object[] { (long)20, '3' };
            yield return new object[] { (long)20, new DateTime(3) };
            yield return new object[] { (float)20, new char[] { '3' } };
            yield return new object[] { (float)20, '3' };
            yield return new object[] { (float)20, new DateTime(3) };
            yield return new object[] { (double)20, new char[] { '3' } };
            yield return new object[] { (double)20, '3' };
            yield return new object[] { (double)20, new DateTime(3) };
            yield return new object[] { (decimal)20, new char[] { '3' } };
            yield return new object[] { (decimal)20, '3' };
            yield return new object[] { (decimal)20, new DateTime(3) };
            yield return new object[] { "20", new char[] { '3' } };
            yield return new object[] { "20", '3' };
            yield return new object[] { "20", new DateTime(3) };
            yield return new object[] { new char[] { '2', '0' }, (byte)3 };
            yield return new object[] { new char[] { '2', '0' }, (ByteEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (sbyte)3 };
            yield return new object[] { new char[] { '2', '0' }, (SByteEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (ushort)3 };
            yield return new object[] { new char[] { '2', '0' }, (UShortEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (short)3 };
            yield return new object[] { new char[] { '2', '0' }, (ShortEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (uint)3 };
            yield return new object[] { new char[] { '2', '0' }, (UIntEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, 3 };
            yield return new object[] { new char[] { '2', '0' }, (IntEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (ulong)3 };
            yield return new object[] { new char[] { '2', '0' }, (ULongEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (long)3 };
            yield return new object[] { new char[] { '2', '0' }, (LongEnum)3 };
            yield return new object[] { new char[] { '2', '0' }, (float)3 };
            yield return new object[] { new char[] { '2', '0' }, (double)3 };
            yield return new object[] { new char[] { '2', '0' }, (decimal)3 };
            yield return new object[] { new char[] { '2', '0' }, "3" };
            yield return new object[] { new char[] { '2', '0' }, new char[] { '3' } };
            yield return new object[] { new char[] { '2', '0' }, true };
            yield return new object[] { new char[] { '2', '0' }, false };
            yield return new object[] { new char[] { '2', '0' }, '3' };
            yield return new object[] { new char[] { '2', '0' }, new DateTime(3) };
            yield return new object[] { new char[] { '2', '0' }, null };
            yield return new object[] { true, new char[] { '3' } };
            yield return new object[] { true, '3' };
            yield return new object[] { true, new DateTime(3) };
            yield return new object[] { new DateTime(20), (byte)3 };
            yield return new object[] { new DateTime(20), (ByteEnum)3 };
            yield return new object[] { new DateTime(20), (sbyte)3 };
            yield return new object[] { new DateTime(20), (SByteEnum)3 };
            yield return new object[] { new DateTime(20), (ushort)3 };
            yield return new object[] { new DateTime(20), (UShortEnum)3 };
            yield return new object[] { new DateTime(20), (short)3 };
            yield return new object[] { new DateTime(20), (ShortEnum)3 };
            yield return new object[] { new DateTime(20), (uint)3 };
            yield return new object[] { new DateTime(20), (UIntEnum)3 };
            yield return new object[] { new DateTime(20), 3 };
            yield return new object[] { new DateTime(20), (IntEnum)3 };
            yield return new object[] { new DateTime(20), (ulong)3 };
            yield return new object[] { new DateTime(20), (ULongEnum)3 };
            yield return new object[] { new DateTime(20), (long)3 };
            yield return new object[] { new DateTime(20), (LongEnum)3 };
            yield return new object[] { new DateTime(20), (float)3 };
            yield return new object[] { new DateTime(20), (double)3 };
            yield return new object[] { new DateTime(20), (decimal)3 };
            yield return new object[] { new DateTime(20), "3" };
            yield return new object[] { new DateTime(20), new char[] { '3' } };
            yield return new object[] { new DateTime(20), true };
            yield return new object[] { new DateTime(20), false };
            yield return new object[] { new DateTime(20), '3' };
            yield return new object[] { new DateTime(20), new DateTime(3) };
            yield return new object[] { new DateTime(20), null };
            yield return new object[] { null, new char[] { '3' } };
            yield return new object[] { null, '3' };
            yield return new object[] { null, new DateTime(3) };
        }

        [Theory]
        [MemberData(nameof(ModObject_InvalidObject_TestData))]
        public void ModObject_InvalidObject_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ModObject(left, right));
        }

        public static IEnumerable<object[]> ModObject_DivideByZeroObject_TestData()
        {
            yield return new object[] { (byte)20, false };
            yield return new object[] { (byte)20, null };
            yield return new object[] { (byte)20, byte.MinValue };
            yield return new object[] { byte.MinValue, byte.MinValue };
            yield return new object[] { byte.MaxValue, byte.MinValue };
            yield return new object[] { byte.MaxValue, byte.MinValue };
            yield return new object[] { byte.MaxValue, byte.MinValue };
            yield return new object[] { byte.MinValue, byte.MinValue };
            yield return new object[] { byte.MinValue, byte.MinValue };
            yield return new object[] { byte.MinValue, byte.MinValue };
            yield return new object[] { (sbyte)20, false };
            yield return new object[] { (sbyte)20, null };
            yield return new object[] { (sbyte)20, (sbyte)0 };
            yield return new object[] { (sbyte)0, (sbyte)0 };
            yield return new object[] { (sbyte)(-20), (sbyte)0 };
            yield return new object[] { sbyte.MaxValue, (sbyte)0 };
            yield return new object[] { sbyte.MinValue, (sbyte)0 };
            yield return new object[] { (ushort)20, false };
            yield return new object[] { (ushort)20, null };
            yield return new object[] { (ushort)20, ushort.MinValue };
            yield return new object[] { ushort.MinValue, ushort.MinValue };
            yield return new object[] { ushort.MaxValue, ushort.MinValue };
            yield return new object[] { ushort.MaxValue, ushort.MinValue };
            yield return new object[] { ushort.MaxValue, ushort.MinValue };
            yield return new object[] { ushort.MinValue, ushort.MinValue };
            yield return new object[] { ushort.MinValue, ushort.MinValue };
            yield return new object[] { ushort.MinValue, ushort.MinValue };
            yield return new object[] { (short)20, false };
            yield return new object[] { (short)20, null };
            yield return new object[] { (short)20, (short)0 };
            yield return new object[] { (short)0, (short)0 };
            yield return new object[] { (short)(-20), (short)0 };
            yield return new object[] { short.MaxValue, (short)0 };
            yield return new object[] { short.MinValue, (short)0 };
            yield return new object[] { (uint)20, false };
            yield return new object[] { (uint)20, null };
            yield return new object[] { (uint)20, uint.MinValue };
            yield return new object[] { uint.MinValue, uint.MinValue };
            yield return new object[] { uint.MaxValue, uint.MinValue };
            yield return new object[] { uint.MaxValue, uint.MinValue };
            yield return new object[] { uint.MaxValue, uint.MinValue };
            yield return new object[] { uint.MinValue, uint.MinValue };
            yield return new object[] { uint.MinValue, uint.MinValue };
            yield return new object[] { uint.MinValue, uint.MinValue };
            yield return new object[] { 20, false };
            yield return new object[] { 20, null };
            yield return new object[] { 20, 0 };
            yield return new object[] { 0, 0 };
            yield return new object[] { -20, 0 };
            yield return new object[] { int.MaxValue, 0 };
            yield return new object[] { int.MinValue, 0 };
            yield return new object[] { (ulong)20, false };
            yield return new object[] { (ulong)20, null };
            yield return new object[] { (ulong)20, ulong.MinValue };
            yield return new object[] { ulong.MinValue, ulong.MinValue };
            yield return new object[] { ulong.MaxValue, ulong.MinValue };
            yield return new object[] { ulong.MaxValue, ulong.MinValue };
            yield return new object[] { ulong.MaxValue, ulong.MinValue };
            yield return new object[] { ulong.MinValue, ulong.MinValue };
            yield return new object[] { ulong.MinValue, ulong.MinValue };
            yield return new object[] { ulong.MinValue, ulong.MinValue };
            yield return new object[] { (long)20, false };
            yield return new object[] { (long)20, null };
            yield return new object[] { (long)20, (long)0 };
            yield return new object[] { (long)0, (long)0 };
            yield return new object[] { (long)(-20), (long)0 };
            yield return new object[] { long.MaxValue, (long)0 };
            yield return new object[] { long.MinValue, (long)0 };
            yield return new object[] { (decimal)20, false };
            yield return new object[] { (decimal)20, null };
            yield return new object[] { (decimal)20, (decimal)0 };
            yield return new object[] { (decimal)0, (decimal)0 };
            yield return new object[] { (decimal)(-20), (decimal)0 };
            yield return new object[] { decimal.MaxValue, (decimal)0 };
            yield return new object[] { decimal.MinValue, (decimal)0 };
            yield return new object[] { true, false };
            yield return new object[] { true, null };
            yield return new object[] { null, false };
            yield return new object[] { null, null };
        }

        [Theory]
        [MemberData(nameof(ModObject_DivideByZeroObject_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Unfixed JIT bug in the .NET Framework")]
        public void ModObject_DivideByZeroObject_ThrowsDivideByZeroException(object left, object right)
        {
            Assert.Throws<DivideByZeroException>(() => Operators.ModObject(left, right));
        }

        public static IEnumerable<object[]> ModObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { '2', 1 };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { DBNull.Value, 2 };
            yield return new object[] { '3', new object() };
            yield return new object[] { new object(), '3' };

            yield return new object[] { new char[] { '8' }, 10 };
            yield return new object[] { 10, new char[] { '8' } };
            yield return new object[] { new char[] { '8' }, new object() };
            yield return new object[] { new object(), new char[] { '8' } };
        }

        [Theory]
        [MemberData(nameof(ModObject_InvalidObjects_TestData))]
        public void ModObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.ModObject(left, right));
        }

        public static IEnumerable<object[]> ModObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new ModObject(), new object() };
            yield return new object[] { new object(), new ModObject() };

            yield return new object[] { new ModObject(), new ModObject() };
        }

        [Theory]
        [MemberData(nameof(ModObject_MismatchingObjects_TestData))]
        public void ModObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.ModObject(left, right));
        }

        public class ModObject
        {
             [SpecialName]
             public static string op_Modulus(ModObject left, int right) => "custom";

             [SpecialName]
             public static string op_Modulus(int left, ModObject right) => "motsuc";

             [SpecialName]
             public static string op_Modulus(ModObject left, OperatorsTests right) => "customobject";

             [SpecialName]
             public static string op_Modulus(OperatorsTests left, ModObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> NegateObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)9, (short)(-9) };
            yield return new object[] { (ByteEnum)9, (short)(-9) };
            yield return new object[] { byte.MinValue, (short)0 };
            yield return new object[] { byte.MaxValue, (short)(-255) };

            // sbyte.
            yield return new object[] { (sbyte)9, (sbyte)(-9) };
            yield return new object[] { (SByteEnum)9, (sbyte)(-9) };
            yield return new object[] { sbyte.MinValue, (short)128 };
            yield return new object[] { sbyte.MaxValue, (sbyte)(-127) };

            // ushort.
            yield return new object[] { (ushort)9, -9 };
            yield return new object[] { (UShortEnum)9, -9 };
            yield return new object[] { ushort.MinValue, 0 };
            yield return new object[] { ushort.MaxValue, -65535 };

            // short.
            yield return new object[] { (short)9, (short)(-9) };
            yield return new object[] { (ShortEnum)9, (short)(-9) };
            yield return new object[] { short.MinValue, 32768 };
            yield return new object[] { short.MaxValue, (short)(-32767) };

            // uint.
            yield return new object[] { (uint)9, (long)(-9) };
            yield return new object[] { (UIntEnum)9, (long)(-9) };
            yield return new object[] { uint.MinValue, (long)0 };
            yield return new object[] { uint.MaxValue, (long)(-4294967295) };

            // int.
            yield return new object[] { 9, -9 };
            yield return new object[] { (IntEnum)9, -9 };
            yield return new object[] { int.MinValue, (long)2147483648 };
            yield return new object[] { int.MaxValue, -2147483647 };

            // ulong.
            yield return new object[] { (ulong)9, (decimal)(-9) };
            yield return new object[] { (ULongEnum)9, (decimal)(-9) };
            yield return new object[] { ulong.MinValue, (decimal)0 };
            yield return new object[] { ulong.MaxValue, decimal.Parse("-18446744073709551615", CultureInfo.InvariantCulture) };

            // long.
            yield return new object[] { (long)9, (long)(-9) };
            yield return new object[] { (LongEnum)9, (long)(-9) };
            yield return new object[] { long.MinValue, decimal.Parse("9223372036854775808", CultureInfo.InvariantCulture) };
            yield return new object[] { long.MaxValue, (long)(-9223372036854775807) };

            // float.
            yield return new object[] { (float)9, (float)(-9) };
            yield return new object[] { float.MinValue, float.MaxValue };
            yield return new object[] { float.MaxValue, float.MinValue };

            // double.
            yield return new object[] { (double)9, (double)(-9) };
            yield return new object[] { double.MinValue, double.MaxValue };
            yield return new object[] { double.MaxValue, double.MinValue };

            // decimal.
            yield return new object[] { (decimal)9, (decimal)(-9) };
            yield return new object[] { decimal.MinValue, decimal.MaxValue };
            yield return new object[] { decimal.MaxValue, decimal.MinValue };

            // string.
            yield return new object[] { "9", (double)(-9) };

            // bool.
            yield return new object[] { true, (short)1 };
            yield return new object[] { false, (short)0 };

            // null.
            yield return new object[] { null, 0 };

            // object.
            yield return new object[] { new NegateObject(), "custom" };
        }

        [Theory]
        [MemberData(nameof(NegateObject_TestData))]
        public void NegateObject_Invoke_ReturnsExpected(object value, object expected)
        {
             Assert.Equal(expected, Operators.NegateObject(value));
        }

        public static IEnumerable<object[]> NegateObject_InvalidObject_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { new char[] { '9' } };
            yield return new object[] { '9' };
            yield return new object[] { new DateTime(2018, 7, 20) };
        }

        [Theory]
        [MemberData(nameof(NegateObject_InvalidObject_TestData))]
        public void NegateObject_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Operators.NegateObject(value));
        }

        public class NegateObject
        {
             [SpecialName]
             public static string op_UnaryNegation(NegateObject value) => "custom";
        }

        public static IEnumerable<object[]> NotObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)9, (byte)246 };
            yield return new object[] { (ByteEnum)9, (ByteEnum)246 };
            yield return new object[] { byte.MinValue, byte.MaxValue };
            yield return new object[] { byte.MaxValue, byte.MinValue };

            // sbyte.
            yield return new object[] { (sbyte)9, (sbyte)(-10) };
            yield return new object[] { (SByteEnum)9, (SByteEnum)(-10) };
            yield return new object[] { sbyte.MinValue, sbyte.MaxValue };
            yield return new object[] { sbyte.MaxValue, sbyte.MinValue };

            // ushort.
            yield return new object[] { (ushort)9, (ushort)65526 };
            yield return new object[] { (UShortEnum)9, (UShortEnum)65526 };
            yield return new object[] { ushort.MinValue, ushort.MaxValue };
            yield return new object[] { ushort.MaxValue, ushort.MinValue };

            // short.
            yield return new object[] { (short)9, (short)(-10) };
            yield return new object[] { (ShortEnum)9, (ShortEnum)(-10) };
            yield return new object[] { short.MinValue, short.MaxValue };
            yield return new object[] { short.MaxValue, short.MinValue };

            // uint.
            yield return new object[] { (uint)9, (uint)4294967286 };
            yield return new object[] { (UIntEnum)9, (UIntEnum)4294967286 };
            yield return new object[] { uint.MinValue, uint.MaxValue };
            yield return new object[] { uint.MaxValue, uint.MinValue };

            // int.
            yield return new object[] { 9, -10 };
            yield return new object[] { (IntEnum)9, (IntEnum)(-10) };
            yield return new object[] { int.MinValue, int.MaxValue };
            yield return new object[] { int.MaxValue, int.MinValue };

            // ulong.
            yield return new object[] { (ulong)9, (ulong)18446744073709551606 };
            yield return new object[] { (ULongEnum)9, (ULongEnum)18446744073709551606 };
            yield return new object[] { ulong.MinValue, ulong.MaxValue };
            yield return new object[] { ulong.MaxValue, ulong.MinValue };

            // long.
            yield return new object[] { (long)9, (long)(-10) };
            yield return new object[] { (LongEnum)9, (LongEnum)(-10) };
            yield return new object[] { long.MinValue, long.MaxValue };
            yield return new object[] { long.MaxValue, long.MinValue };

            // float.
            yield return new object[] { (float)9, (long)(-10) };

            // double.
            yield return new object[] { (double)9, (long)(-10) };

            // decimal.
            yield return new object[] { (decimal)9, (long)(-10) };

            // string.
            yield return new object[] { "9", (long)(-10) };

            // bool.
            yield return new object[] { true, false };
            yield return new object[] { false, true };

            // null.
            yield return new object[] { null, -1 };

            // object.
            yield return new object[] { new NotObject(), "custom" };
        }

        [Theory]
        [MemberData(nameof(NotObject_TestData))]
        public void NotObject_Invoke_ReturnsExpected(object value, object expected)
        {
             Assert.Equal(expected, Operators.NotObject(value));
        }

        public static IEnumerable<object[]> NotObject_InvalidObject_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { new char[] { '9' } };
            yield return new object[] { '9' };
            yield return new object[] { new DateTime(2018, 7, 20) };
        }

        [Theory]
        [MemberData(nameof(NotObject_InvalidObject_TestData))]
        public void NotObject_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Operators.NotObject(value));
        }

        public static IEnumerable<object[]> NotObject_OverflowObject_TestData()
        {
            yield return new object[] { float.MinValue };
            yield return new object[] { float.MaxValue };
            yield return new object[] { double.MinValue };
            yield return new object[] { double.MaxValue };
            yield return new object[] { decimal.MinValue };
            yield return new object[] { decimal.MaxValue };
        }

        [Theory]
        [MemberData(nameof(NotObject_OverflowObject_TestData))]
        public void NotObject_OverflowObject_ThrowsOverflowException(object value)
        {
            Assert.Throws<OverflowException>(() => Operators.NotObject(value));
        }

        public class NotObject
        {
             [SpecialName]
             public static string op_OnesComplement(NotObject value) => "custom";
        }

        public static IEnumerable<object[]> OrObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)10, (byte)14, (byte)14 };
            yield return new object[] { (byte)10, (ByteEnum)14, (byte)14 };
            yield return new object[] { (byte)10, (sbyte)14, (short)14 };
            yield return new object[] { (byte)10, (SByteEnum)14, (short)14 };
            yield return new object[] { (byte)10, (ushort)14, (ushort)14 };
            yield return new object[] { (byte)10, (UShortEnum)14, (ushort)14 };
            yield return new object[] { (byte)10, (short)14, (short)14 };
            yield return new object[] { (byte)10, (ShortEnum)14, (short)14 };
            yield return new object[] { (byte)10, (uint)14, (uint)14 };
            yield return new object[] { (byte)10, (UIntEnum)14, (uint)14 };
            yield return new object[] { (byte)10, 14, 14 };
            yield return new object[] { (byte)10, (IntEnum)14, 14 };
            yield return new object[] { (byte)10, (ulong)14, (ulong)14 };
            yield return new object[] { (byte)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (byte)10, (long)14, (long)14 };
            yield return new object[] { (byte)10, (LongEnum)14, (long)14 };
            yield return new object[] { (byte)10, (float)14, (long)14 };
            yield return new object[] { (byte)10, (double)14, (long)14 };
            yield return new object[] { (byte)10, (decimal)14, (long)14 };
            yield return new object[] { (byte)10, "14", (long)14 };
            yield return new object[] { (byte)10, true, (short)-1 };
            yield return new object[] { (byte)10, null, (byte)10 };

            yield return new object[] { (ByteEnum)10, (byte)14, (byte)14 };
            yield return new object[] { (ByteEnum)10, (ByteEnum)14, (ByteEnum)14 };
            yield return new object[] { (ByteEnum)10, (ByteEnum2)14, (byte)14 };
            yield return new object[] { (ByteEnum)10, (sbyte)14, (short)14 };
            yield return new object[] { (ByteEnum)10, (SByteEnum)14, (short)14 };
            yield return new object[] { (ByteEnum)10, (ushort)14, (ushort)14 };
            yield return new object[] { (ByteEnum)10, (UShortEnum)14, (ushort)14 };
            yield return new object[] { (ByteEnum)10, (short)14, (short)14 };
            yield return new object[] { (ByteEnum)10, (ShortEnum)14, (short)14 };
            yield return new object[] { (ByteEnum)10, (uint)14, (uint)14 };
            yield return new object[] { (ByteEnum)10, (UIntEnum)14, (uint)14 };
            yield return new object[] { (ByteEnum)10, 14, 14 };
            yield return new object[] { (ByteEnum)10, (IntEnum)14, 14 };
            yield return new object[] { (ByteEnum)10, (ulong)14, (ulong)14 };
            yield return new object[] { (ByteEnum)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (ByteEnum)10, (long)14, (long)14 };
            yield return new object[] { (ByteEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (ByteEnum)10, (float)14, (long)14 };
            yield return new object[] { (ByteEnum)10, (double)14, (long)14 };
            yield return new object[] { (ByteEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (ByteEnum)10, "14", (long)14 };
            yield return new object[] { (ByteEnum)10, true, (short)-1 };
            yield return new object[] { (ByteEnum)10, null, (ByteEnum)10 };

            // sbyte.
            yield return new object[] { (sbyte)10, (byte)14, (short)14 };
            yield return new object[] { (sbyte)10, (ByteEnum)14, (short)14 };
            yield return new object[] { (sbyte)10, (sbyte)14, (sbyte)14 };
            yield return new object[] { (sbyte)10, (SByteEnum)14, (sbyte)14 };
            yield return new object[] { (sbyte)10, (ushort)14, 14 };
            yield return new object[] { (sbyte)10, (UShortEnum)14, 14 };
            yield return new object[] { (sbyte)10, (short)14, (short)14 };
            yield return new object[] { (sbyte)10, (ShortEnum)14, (short)14 };
            yield return new object[] { (sbyte)10, (uint)14, (long)14 };
            yield return new object[] { (sbyte)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (sbyte)10, 14, 14 };
            yield return new object[] { (sbyte)10, (IntEnum)14, 14 };
            yield return new object[] { (sbyte)10, (ulong)14, (long)14 };
            yield return new object[] { (sbyte)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (sbyte)10, (long)14, (long)14 };
            yield return new object[] { (sbyte)10, (LongEnum)14, (long)14 };
            yield return new object[] { (sbyte)10, (float)14, (long)14 };
            yield return new object[] { (sbyte)10, (double)14, (long)14 };
            yield return new object[] { (sbyte)10, (decimal)14, (long)14 };
            yield return new object[] { (sbyte)10, "14", (long)14 };
            yield return new object[] { (sbyte)10, true, (sbyte)-1 };
            yield return new object[] { (sbyte)10, null, (sbyte)10 };

            yield return new object[] { (SByteEnum)10, (byte)14, (short)14 };
            yield return new object[] { (SByteEnum)10, (ByteEnum)14, (short)14 };
            yield return new object[] { (SByteEnum)10, (sbyte)14, (sbyte)14 };
            yield return new object[] { (SByteEnum)10, (SByteEnum)14, (SByteEnum)14 };
            yield return new object[] { (SByteEnum)10, (SByteEnum2)14, (sbyte)14 };
            yield return new object[] { (SByteEnum)10, (ushort)14, 14 };
            yield return new object[] { (SByteEnum)10, (UShortEnum)14, 14 };
            yield return new object[] { (SByteEnum)10, (short)14, (short)14 };
            yield return new object[] { (SByteEnum)10, (ShortEnum)14, (short)14 };
            yield return new object[] { (SByteEnum)10, (uint)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (SByteEnum)10, 14, 14 };
            yield return new object[] { (SByteEnum)10, (IntEnum)14, 14 };
            yield return new object[] { (SByteEnum)10, (ulong)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (long)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (float)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (double)14, (long)14 };
            yield return new object[] { (SByteEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (SByteEnum)10, "14", (long)14 };
            yield return new object[] { (SByteEnum)10, true, (sbyte)-1 };
            yield return new object[] { (SByteEnum)10, null, (SByteEnum)10 };

            // ushort.
            yield return new object[] { (ushort)10, (byte)14, (ushort)14 };
            yield return new object[] { (ushort)10, (ByteEnum)14, (ushort)14 };
            yield return new object[] { (ushort)10, (sbyte)14, 14 };
            yield return new object[] { (ushort)10, (SByteEnum)14, 14 };
            yield return new object[] { (ushort)10, (ushort)14, (ushort)14 };
            yield return new object[] { (ushort)10, (UShortEnum)14, (ushort)14 };
            yield return new object[] { (ushort)10, (short)14, 14 };
            yield return new object[] { (ushort)10, (ShortEnum)14, 14 };
            yield return new object[] { (ushort)10, (uint)14, (uint)14 };
            yield return new object[] { (ushort)10, (UIntEnum)14, (uint)14 };
            yield return new object[] { (ushort)10, 14, 14 };
            yield return new object[] { (ushort)10, (IntEnum)14, 14 };
            yield return new object[] { (ushort)10, (ulong)14, (ulong)14 };
            yield return new object[] { (ushort)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (ushort)10, (long)14, (long)14 };
            yield return new object[] { (ushort)10, (LongEnum)14, (long)14 };
            yield return new object[] { (ushort)10, (float)14, (long)14 };
            yield return new object[] { (ushort)10, (double)14, (long)14 };
            yield return new object[] { (ushort)10, (decimal)14, (long)14 };
            yield return new object[] { (ushort)10, "14", (long)14 };
            yield return new object[] { (ushort)10, true, -1 };
            yield return new object[] { (ushort)10, null, (ushort)10 };

            yield return new object[] { (UShortEnum)10, (byte)14, (ushort)14 };
            yield return new object[] { (UShortEnum)10, (ByteEnum)14, (ushort)14 };
            yield return new object[] { (UShortEnum)10, (sbyte)14, 14 };
            yield return new object[] { (UShortEnum)10, (SByteEnum)14, 14 };
            yield return new object[] { (UShortEnum)10, (ushort)14, (ushort)14 };
            yield return new object[] { (UShortEnum)10, (UShortEnum)14, (UShortEnum)14 };
            yield return new object[] { (UShortEnum)10, (UShortEnum2)14, (ushort)14 };
            yield return new object[] { (UShortEnum)10, (short)14, 14 };
            yield return new object[] { (UShortEnum)10, (ShortEnum)14, 14 };
            yield return new object[] { (UShortEnum)10, (uint)14, (uint)14 };
            yield return new object[] { (UShortEnum)10, (UIntEnum)14, (uint)14 };
            yield return new object[] { (UShortEnum)10, 14, 14 };
            yield return new object[] { (UShortEnum)10, (IntEnum)14, 14 };
            yield return new object[] { (UShortEnum)10, (ulong)14, (ulong)14 };
            yield return new object[] { (UShortEnum)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (UShortEnum)10, (long)14, (long)14 };
            yield return new object[] { (UShortEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (UShortEnum)10, (float)14, (long)14 };
            yield return new object[] { (UShortEnum)10, (double)14, (long)14 };
            yield return new object[] { (UShortEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (UShortEnum)10, "14", (long)14 };
            yield return new object[] { (UShortEnum)10, true, -1 };
            yield return new object[] { (UShortEnum)10, null, (UShortEnum)10 };

            // short.
            yield return new object[] { (short)10, (byte)14, (short)14 };
            yield return new object[] { (short)10, (ByteEnum)14, (short)14 };
            yield return new object[] { (short)10, (sbyte)14, (short)14 };
            yield return new object[] { (short)10, (SByteEnum)14, (short)14 };
            yield return new object[] { (short)10, (ushort)14, 14 };
            yield return new object[] { (short)10, (UShortEnum)14, 14 };
            yield return new object[] { (short)10, (short)14, (short)14 };
            yield return new object[] { (short)10, (ShortEnum)14, (short)14 };
            yield return new object[] { (short)10, (uint)14, (long)14 };
            yield return new object[] { (short)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (short)10, 14, 14 };
            yield return new object[] { (short)10, (IntEnum)14, 14 };
            yield return new object[] { (short)10, (ulong)14, (long)14 };
            yield return new object[] { (short)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (short)10, (long)14, (long)14 };
            yield return new object[] { (short)10, (LongEnum)14, (long)14 };
            yield return new object[] { (short)10, (float)14, (long)14 };
            yield return new object[] { (short)10, (double)14, (long)14 };
            yield return new object[] { (short)10, (decimal)14, (long)14 };
            yield return new object[] { (short)10, "14", (long)14 };
            yield return new object[] { (short)10, true, (short)-1 };
            yield return new object[] { (short)10, null, (short)10 };

            yield return new object[] { (ShortEnum)10, (byte)14, (short)14 };
            yield return new object[] { (ShortEnum)10, (ByteEnum)14, (short)14 };
            yield return new object[] { (ShortEnum)10, (sbyte)14, (short)14 };
            yield return new object[] { (ShortEnum)10, (SByteEnum)14, (short)14 };
            yield return new object[] { (ShortEnum)10, (ushort)14, 14 };
            yield return new object[] { (ShortEnum)10, (UShortEnum)14, 14 };
            yield return new object[] { (ShortEnum)10, (short)14, (short)14 };
            yield return new object[] { (ShortEnum)10, (ShortEnum)14, (ShortEnum)14 };
            yield return new object[] { (ShortEnum)10, (ShortEnum2)14, (short)14 };
            yield return new object[] { (ShortEnum)10, (uint)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (ShortEnum)10, 14, 14 };
            yield return new object[] { (ShortEnum)10, (IntEnum)14, 14 };
            yield return new object[] { (ShortEnum)10, (ulong)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (long)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (float)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (double)14, (long)14 };
            yield return new object[] { (ShortEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (ShortEnum)10, "14", (long)14 };
            yield return new object[] { (ShortEnum)10, true, (short)-1 };
            yield return new object[] { (ShortEnum)10, null, (ShortEnum)10 };

            // uint.
            yield return new object[] { (uint)10, (byte)14, (uint)14 };
            yield return new object[] { (uint)10, (ByteEnum)14, (uint)14 };
            yield return new object[] { (uint)10, (sbyte)14, (long)14 };
            yield return new object[] { (uint)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (uint)10, (ushort)14, (uint)14 };
            yield return new object[] { (uint)10, (UShortEnum)14, (uint)14 };
            yield return new object[] { (uint)10, (short)14, (long)14 };
            yield return new object[] { (uint)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (uint)10, (uint)14, (uint)14 };
            yield return new object[] { (uint)10, (UIntEnum)14, (uint)14 };
            yield return new object[] { (uint)10, 14, (long)14 };
            yield return new object[] { (uint)10, (IntEnum)14, (long)14 };
            yield return new object[] { (uint)10, (ulong)14, (ulong)14 };
            yield return new object[] { (uint)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (uint)10, (long)14, (long)14 };
            yield return new object[] { (uint)10, (LongEnum)14, (long)14 };
            yield return new object[] { (uint)10, (float)14, (long)14 };
            yield return new object[] { (uint)10, (double)14, (long)14 };
            yield return new object[] { (uint)10, (decimal)14, (long)14 };
            yield return new object[] { (uint)10, "14", (long)14 };
            yield return new object[] { (uint)10, true, (long)-1 };
            yield return new object[] { (uint)10, null, (uint)10 };

            yield return new object[] { (UIntEnum)10, (byte)14, (uint)14 };
            yield return new object[] { (UIntEnum)10, (ByteEnum)14, (uint)14 };
            yield return new object[] { (UIntEnum)10, (sbyte)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (ushort)14, (uint)14 };
            yield return new object[] { (UIntEnum)10, (UShortEnum)14, (uint)14 };
            yield return new object[] { (UIntEnum)10, (short)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (uint)14, (uint)14 };
            yield return new object[] { (UIntEnum)10, (UIntEnum)14, (UIntEnum)14 };
            yield return new object[] { (UIntEnum)10, (UIntEnum2)14, (uint)14 };
            yield return new object[] { (UIntEnum)10, 14, (long)14 };
            yield return new object[] { (UIntEnum)10, (IntEnum)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (ulong)14, (ulong)14 };
            yield return new object[] { (UIntEnum)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (UIntEnum)10, (long)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (float)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (double)14, (long)14 };
            yield return new object[] { (UIntEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (UIntEnum)10, "14", (long)14 };
            yield return new object[] { (UIntEnum)10, true, (long)-1 };
            yield return new object[] { (UIntEnum)10, null, (UIntEnum)10 };

            // int.
            yield return new object[] { 10, (byte)14, 14 };
            yield return new object[] { 10, (ByteEnum)14, 14 };
            yield return new object[] { 10, (sbyte)14, 14 };
            yield return new object[] { 10, (SByteEnum)14, 14 };
            yield return new object[] { 10, (ushort)14, 14 };
            yield return new object[] { 10, (UShortEnum)14, 14 };
            yield return new object[] { 10, (short)14, 14 };
            yield return new object[] { 10, (ShortEnum)14, 14 };
            yield return new object[] { 10, (uint)14, (long)14 };
            yield return new object[] { 10, (UIntEnum)14, (long)14 };
            yield return new object[] { 10, 14, 14 };
            yield return new object[] { 10, (IntEnum)14, 14 };
            yield return new object[] { 10, (ulong)14, (long)14 };
            yield return new object[] { 10, (ULongEnum)14, (long)14 };
            yield return new object[] { 10, (long)14, (long)14 };
            yield return new object[] { 10, (LongEnum)14, (long)14 };
            yield return new object[] { 10, (float)14, (long)14 };
            yield return new object[] { 10, (double)14, (long)14 };
            yield return new object[] { 10, (decimal)14, (long)14 };
            yield return new object[] { 10, "14", (long)14 };
            yield return new object[] { 10, true, -1 };
            yield return new object[] { 10, null, 10 };

            yield return new object[] { (IntEnum)10, (byte)14, 14 };
            yield return new object[] { (IntEnum)10, (ByteEnum)14, 14 };
            yield return new object[] { (IntEnum)10, (sbyte)14, 14 };
            yield return new object[] { (IntEnum)10, (SByteEnum)14, 14 };
            yield return new object[] { (IntEnum)10, (ushort)14, 14 };
            yield return new object[] { (IntEnum)10, (UShortEnum)14, 14 };
            yield return new object[] { (IntEnum)10, (short)14, 14 };
            yield return new object[] { (IntEnum)10, (ShortEnum)14, 14 };
            yield return new object[] { (IntEnum)10, (uint)14, (long)14 };
            yield return new object[] { (IntEnum)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (IntEnum)10, 14, 14 };
            yield return new object[] { (IntEnum)10, (IntEnum)14, (IntEnum)14 };
            yield return new object[] { (IntEnum)10, (IntEnum2)14, 14 };
            yield return new object[] { (IntEnum)10, (ulong)14, (long)14 };
            yield return new object[] { (IntEnum)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (IntEnum)10, (long)14, (long)14 };
            yield return new object[] { (IntEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (IntEnum)10, (float)14, (long)14 };
            yield return new object[] { (IntEnum)10, (double)14, (long)14 };
            yield return new object[] { (IntEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (IntEnum)10, "14", (long)14 };
            yield return new object[] { (IntEnum)10, true, -1 };
            yield return new object[] { (IntEnum)10, null, (IntEnum)10 };

            // ulong.
            yield return new object[] { (ulong)10, (byte)14, (ulong)14 };
            yield return new object[] { (ulong)10, (ByteEnum)14, (ulong)14 };
            yield return new object[] { (ulong)10, (sbyte)14, (long)14 };
            yield return new object[] { (ulong)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (ulong)10, (ushort)14, (ulong)14 };
            yield return new object[] { (ulong)10, (UShortEnum)14, (ulong)14 };
            yield return new object[] { (ulong)10, (short)14, (long)14 };
            yield return new object[] { (ulong)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (ulong)10, (uint)14, (ulong)14 };
            yield return new object[] { (ulong)10, (UIntEnum)14, (ulong)14 };
            yield return new object[] { (ulong)10, 14, (long)14 };
            yield return new object[] { (ulong)10, (IntEnum)14, (long)14 };
            yield return new object[] { (ulong)10, (ulong)14, (ulong)14 };
            yield return new object[] { (ulong)10, (ULongEnum)14, (ulong)14 };
            yield return new object[] { (ulong)10, (long)14, (long)14 };
            yield return new object[] { (ulong)10, (LongEnum)14, (long)14 };
            yield return new object[] { (ulong)10, (float)14, (long)14 };
            yield return new object[] { (ulong)10, (double)14, (long)14 };
            yield return new object[] { (ulong)10, (decimal)14, (long)14 };
            yield return new object[] { (ulong)10, "14", (long)14 };
            yield return new object[] { (ulong)10, true, (long)-1 };
            yield return new object[] { (ulong)10, null, (ulong)10 };

            yield return new object[] { (ULongEnum)10, (byte)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (ByteEnum)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (sbyte)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (ushort)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (UShortEnum)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (short)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (uint)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (UIntEnum)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, 14, (long)14 };
            yield return new object[] { (ULongEnum)10, (IntEnum)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (ulong)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (ULongEnum)14, (ULongEnum)14 };
            yield return new object[] { (ULongEnum)10, (ULongEnum2)14, (ulong)14 };
            yield return new object[] { (ULongEnum)10, (long)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (LongEnum)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (float)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (double)14, (long)14 };
            yield return new object[] { (ULongEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (ULongEnum)10, "14", (long)14 };
            yield return new object[] { (ULongEnum)10, true, (long)-1 };
            yield return new object[] { (ULongEnum)10, null, (ULongEnum)10 };

            // long.
            yield return new object[] { (long)10, (byte)14, (long)14 };
            yield return new object[] { (long)10, (ByteEnum)14, (long)14 };
            yield return new object[] { (long)10, (sbyte)14, (long)14 };
            yield return new object[] { (long)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (long)10, (ushort)14, (long)14 };
            yield return new object[] { (long)10, (UShortEnum)14, (long)14 };
            yield return new object[] { (long)10, (short)14, (long)14 };
            yield return new object[] { (long)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (long)10, (uint)14, (long)14 };
            yield return new object[] { (long)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (long)10, 14, (long)14 };
            yield return new object[] { (long)10, (IntEnum)14, (long)14 };
            yield return new object[] { (long)10, (ulong)14, (long)14 };
            yield return new object[] { (long)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (long)10, (long)14, (long)14 };
            yield return new object[] { (long)10, (LongEnum)14, (long)14 };
            yield return new object[] { (long)10, (float)14, (long)14 };
            yield return new object[] { (long)10, (double)14, (long)14 };
            yield return new object[] { (long)10, (decimal)14, (long)14 };
            yield return new object[] { (long)10, "14", (long)14 };
            yield return new object[] { (long)10, true, (long)-1 };
            yield return new object[] { (long)10, null, (long)10 };

            yield return new object[] { (LongEnum)10, (byte)14, (long)14 };
            yield return new object[] { (LongEnum)10, (ByteEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, (sbyte)14, (long)14 };
            yield return new object[] { (LongEnum)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, (ushort)14, (long)14 };
            yield return new object[] { (LongEnum)10, (UShortEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, (short)14, (long)14 };
            yield return new object[] { (LongEnum)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, (uint)14, (long)14 };
            yield return new object[] { (LongEnum)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, 14, (long)14 };
            yield return new object[] { (LongEnum)10, (IntEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, (ulong)14, (long)14 };
            yield return new object[] { (LongEnum)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (LongEnum)10, (long)14, (long)14 };
            yield return new object[] { (LongEnum)10, (LongEnum)14, (LongEnum)14 };
            yield return new object[] { (LongEnum)10, (LongEnum2)14, (long)14 };
            yield return new object[] { (LongEnum)10, (float)14, (long)14 };
            yield return new object[] { (LongEnum)10, (double)14, (long)14 };
            yield return new object[] { (LongEnum)10, (decimal)14, (long)14 };
            yield return new object[] { (LongEnum)10, "14", (long)14 };
            yield return new object[] { (LongEnum)10, true, (long)-1 };
            yield return new object[] { (LongEnum)10, null, (LongEnum)10 };

            // float.
            yield return new object[] { (float)10, (byte)14, (long)14 };
            yield return new object[] { (float)10, (ByteEnum)14, (long)14 };
            yield return new object[] { (float)10, (sbyte)14, (long)14 };
            yield return new object[] { (float)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (float)10, (ushort)14, (long)14 };
            yield return new object[] { (float)10, (UShortEnum)14, (long)14 };
            yield return new object[] { (float)10, (short)14, (long)14 };
            yield return new object[] { (float)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (float)10, (uint)14, (long)14 };
            yield return new object[] { (float)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (float)10, 14, (long)14 };
            yield return new object[] { (float)10, (IntEnum)14, (long)14 };
            yield return new object[] { (float)10, (ulong)14, (long)14 };
            yield return new object[] { (float)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (float)10, (long)14, (long)14 };
            yield return new object[] { (float)10, (LongEnum)14, (long)14 };
            yield return new object[] { (float)10, (float)14, (long)14 };
            yield return new object[] { (float)10, (double)14, (long)14 };
            yield return new object[] { (float)10, (decimal)14, (long)14 };
            yield return new object[] { (float)10, "14", (long)14 };
            yield return new object[] { (float)10, true, (long)-1 };
            yield return new object[] { (float)10, null, (long)10 };

            // double.
            yield return new object[] { (double)10, (byte)14, (long)14 };
            yield return new object[] { (double)10, (ByteEnum)14, (long)14 };
            yield return new object[] { (double)10, (sbyte)14, (long)14 };
            yield return new object[] { (double)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (double)10, (ushort)14, (long)14 };
            yield return new object[] { (double)10, (UShortEnum)14, (long)14 };
            yield return new object[] { (double)10, (short)14, (long)14 };
            yield return new object[] { (double)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (double)10, (uint)14, (long)14 };
            yield return new object[] { (double)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (double)10, 14, (long)14 };
            yield return new object[] { (double)10, (IntEnum)14, (long)14 };
            yield return new object[] { (double)10, (ulong)14, (long)14 };
            yield return new object[] { (double)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (double)10, (long)14, (long)14 };
            yield return new object[] { (double)10, (LongEnum)14, (long)14 };
            yield return new object[] { (double)10, (float)14, (long)14 };
            yield return new object[] { (double)10, (double)14, (long)14 };
            yield return new object[] { (double)10, (decimal)14, (long)14 };
            yield return new object[] { (double)10, "14", (long)14 };
            yield return new object[] { (double)10, true, (long)-1 };
            yield return new object[] { (double)10, null, (long)10 };

            // decimal.
            yield return new object[] { (decimal)10, (byte)14, (long)14 };
            yield return new object[] { (decimal)10, (ByteEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (sbyte)14, (long)14 };
            yield return new object[] { (decimal)10, (SByteEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (ushort)14, (long)14 };
            yield return new object[] { (decimal)10, (UShortEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (short)14, (long)14 };
            yield return new object[] { (decimal)10, (ShortEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (uint)14, (long)14 };
            yield return new object[] { (decimal)10, (UIntEnum)14, (long)14 };
            yield return new object[] { (decimal)10, 14, (long)14 };
            yield return new object[] { (decimal)10, (IntEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (ulong)14, (long)14 };
            yield return new object[] { (decimal)10, (ULongEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (long)14, (long)14 };
            yield return new object[] { (decimal)10, (LongEnum)14, (long)14 };
            yield return new object[] { (decimal)10, (float)14, (long)14 };
            yield return new object[] { (decimal)10, (double)14, (long)14 };
            yield return new object[] { (decimal)10, (decimal)14, (long)14 };
            yield return new object[] { (decimal)10, "14", (long)14 };
            yield return new object[] { (decimal)10, true, (long)-1 };
            yield return new object[] { (decimal)10, null, (long)10 };

            // string.
            yield return new object[] { "10", (byte)14, (long)14 };
            yield return new object[] { "10", (ByteEnum)14, (long)14 };
            yield return new object[] { "10", (sbyte)14, (long)14 };
            yield return new object[] { "10", (SByteEnum)14, (long)14 };
            yield return new object[] { "10", (ushort)14, (long)14 };
            yield return new object[] { "10", (UShortEnum)14, (long)14 };
            yield return new object[] { "10", (short)14, (long)14 };
            yield return new object[] { "10", (ShortEnum)14, (long)14 };
            yield return new object[] { "10", (uint)14, (long)14 };
            yield return new object[] { "10", (UIntEnum)14, (long)14 };
            yield return new object[] { "10", 14, (long)14 };
            yield return new object[] { "10", (IntEnum)14, (long)14 };
            yield return new object[] { "10", (ulong)14, (long)14 };
            yield return new object[] { "10", (ULongEnum)14, (long)14 };
            yield return new object[] { "10", (long)14, (long)14 };
            yield return new object[] { "10", (LongEnum)14, (long)14 };
            yield return new object[] { "10", (float)14, (long)14 };
            yield return new object[] { "10", (double)14, (long)14 };
            yield return new object[] { "10", (decimal)14, (long)14 };
            yield return new object[] { "10", "14", (long)14 };
            yield return new object[] { "10", true, true };
            yield return new object[] { "10", null, (long)10 };

            // bool.
            yield return new object[] { true, (byte)14, (short)-1 };
            yield return new object[] { true, (ByteEnum)14, (short)-1 };
            yield return new object[] { true, (sbyte)14, (sbyte)-1 };
            yield return new object[] { true, (SByteEnum)14, (sbyte)-1 };
            yield return new object[] { true, (ushort)14, -1 };
            yield return new object[] { true, (UShortEnum)14, -1 };
            yield return new object[] { true, (short)14, (short)-1 };
            yield return new object[] { true, (ShortEnum)14, (short)-1 };
            yield return new object[] { true, (uint)14, (long)-1 };
            yield return new object[] { true, (UIntEnum)14, (long)-1 };
            yield return new object[] { true, 14, -1 };
            yield return new object[] { true, (IntEnum)14, -1 };
            yield return new object[] { true, (ulong)14, (long)-1 };
            yield return new object[] { true, (ULongEnum)14, (long)-1 };
            yield return new object[] { true, (long)14, (long)-1 };
            yield return new object[] { true, (LongEnum)14, (long)-1 };
            yield return new object[] { true, (float)14, (long)-1 };
            yield return new object[] { true, (double)14, (long)-1 };
            yield return new object[] { true, (decimal)14, (long)-1 };
            yield return new object[] { true, "14", true };
            yield return new object[] { true, true, true };
            yield return new object[] { true, null, true };

            // null.
            yield return new object[] { null, (byte)14, (byte)14 };
            yield return new object[] { null, (ByteEnum)14, (ByteEnum)14 };
            yield return new object[] { null, (sbyte)14, (sbyte)14 };
            yield return new object[] { null, (SByteEnum)14, (SByteEnum)14 };
            yield return new object[] { null, (ushort)14, (ushort)14 };
            yield return new object[] { null, (UShortEnum)14, (UShortEnum)14 };
            yield return new object[] { null, (short)14, (short)14 };
            yield return new object[] { null, (ShortEnum)14, (ShortEnum)14 };
            yield return new object[] { null, (uint)14, (uint)14 };
            yield return new object[] { null, (UIntEnum)14, (UIntEnum)14 };
            yield return new object[] { null, 14, 14 };
            yield return new object[] { null, (IntEnum)14, (IntEnum)14 };
            yield return new object[] { null, (ulong)14, (ulong)14 };
            yield return new object[] { null, (ULongEnum)14, (ULongEnum)14 };
            yield return new object[] { null, (long)14, (long)14 };
            yield return new object[] { null, (LongEnum)14, (LongEnum)14 };
            yield return new object[] { null, (float)14, (long)14 };
            yield return new object[] { null, (double)14, (long)14 };
            yield return new object[] { null, (decimal)14, (long)14 };
            yield return new object[] { null, "14", (long)14 };
            yield return new object[] { null, true, true };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new OrObject(), 2, "custom" };
            yield return new object[] { 2, new OrObject(), "motsuc" };
            yield return new object[] { new OrObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new OrObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(OrObject_TestData))]
        public void OrObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.OrObject(left, right));
        }

        public static IEnumerable<object[]> OrObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(OrObject_InvalidObjects_TestData))]
        public void OrObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.OrObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.OrObject(right, left));
        }

        public static IEnumerable<object[]> OrObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new OrObject(), new object() };
            yield return new object[] { new object(), new OrObject() };

            yield return new object[] { new OrObject(), new OrObject() };
        }

        [Theory]
        [MemberData(nameof(OrObject_MismatchingObjects_TestData))]
        public void OrObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.OrObject(left, right));
        }

        public class OrObject
        {
            public static string operator |(OrObject left, int right) => "custom";
            public static string operator |(int left, OrObject right) => "motsuc";

            public static string operator |(OrObject left, OperatorsTests right) => "customobject";
            public static string operator |(OperatorsTests left, OrObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> PlusObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)1, (byte)1 };

            // sbyte.
            yield return new object[] { (sbyte)1, (sbyte)1 };
            yield return new object[] { (sbyte)(-1), (sbyte)(-1) };

            // ushort.
            yield return new object[] { (ushort)3, (ushort)3 };

            // short.
            yield return new object[] { (short)4, (short)4 };
            yield return new object[] { (short)(-4), (short)(-4) };

            // uint.
            yield return new object[] { (uint)4, (uint)4 };

            // int.
            yield return new object[] { 6, 6 };
            yield return new object[] { -6, -6 };

            // ulong.
            yield return new object[] { (ulong)7, (ulong)7 };

            // long.
            yield return new object[] { (long)8, (long)8 };
            yield return new object[] { (long)(-8), (long)(-8) };

            // float.
            yield return new object[] { (float)9, (float)9 };
            yield return new object[] { (float)(-9), (float)(-9) };

            // double.
            yield return new object[] { (double)10, (double)10 };
            yield return new object[] { (double)(-10), (double)(-10) };

            // decimal.
            yield return new object[] { (decimal)11, (decimal)11 };
            yield return new object[] { (decimal)(-11), (decimal)(-11) };

            // bool.
            yield return new object[] { true, (short)(-1) };
            yield return new object[] { false, (short)0 };

            // string.
            yield return new object[] { "1", (double)1 };
            yield return new object[] { "-1", (double)(-1) };

            // null.
            yield return new object[] { null, 0 };

            // object.
            yield return new object[] { new PlusObject(), "custom" };
        }

        [Theory]
        [MemberData(nameof(PlusObject_TestData))]
        public void PlusObject_Invoke_ReturnsExpected(object value, object expected)
        {
            Assert.Equal(expected, Operators.PlusObject(value));
        }

        public static IEnumerable<object[]> PlusObject_InvalidObject_TestData()
        {
            yield return new object[] { "a" };
            yield return new object[] { 'a' };
            yield return new object[] { '1' };
            yield return new object[] { DBNull.Value };
            yield return new object[] { new DateTime(10) };
            yield return new object[] { new object() };
        }

        [Theory]
        [MemberData(nameof(PlusObject_InvalidObject_TestData))]
        public void PlusObject_InvalidObject_ThrowsInvalidCastException(object value)
        {
            Assert.Throws<InvalidCastException>(() => Operators.PlusObject(value));
        }

        public class PlusObject
        {
            public static string operator +(PlusObject o) => "custom";
        }

        public static IEnumerable<object[]> RightShiftObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)10, (byte)2, (byte)2 };
            yield return new object[] { (byte)10, (sbyte)2, (byte)2 };
            yield return new object[] { (byte)10, (ushort)2, (byte)2 };
            yield return new object[] { (byte)10, (short)2, (byte)2 };
            yield return new object[] { (byte)10, (uint)2, (byte)2 };
            yield return new object[] { (byte)10, 2, (byte)2 };
            yield return new object[] { (byte)10, (ulong)2, (byte)2 };
            yield return new object[] { (byte)10, (long)2, (byte)2 };
            yield return new object[] { (byte)10, (float)2, (byte)2 };
            yield return new object[] { (byte)10, (double)2, (byte)2 };
            yield return new object[] { (byte)10, (decimal)2, (byte)2 };
            yield return new object[] { (byte)10, "2", (byte)2 };
            yield return new object[] { (byte)10, true, (byte)0 };
            yield return new object[] { (byte)10, null, (byte)10 };

            // sbyte.
            yield return new object[] { (sbyte)10, (byte)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (sbyte)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (ushort)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (short)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (uint)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, 2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (ulong)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (long)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (float)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (double)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, (decimal)2, (sbyte)2 };
            yield return new object[] { (sbyte)10, "2", (sbyte)2 };
            yield return new object[] { (sbyte)10, true, (sbyte)0 };
            yield return new object[] { (sbyte)10, null, (sbyte)10 };

            // ushort.
            yield return new object[] { (ushort)10, (byte)2, (ushort)2 };
            yield return new object[] { (ushort)10, (sbyte)2, (ushort)2 };
            yield return new object[] { (ushort)10, (ushort)2, (ushort)2 };
            yield return new object[] { (ushort)10, (short)2, (ushort)2 };
            yield return new object[] { (ushort)10, (uint)2, (ushort)2 };
            yield return new object[] { (ushort)10, 2, (ushort)2 };
            yield return new object[] { (ushort)10, (ulong)2, (ushort)2 };
            yield return new object[] { (ushort)10, (long)2, (ushort)2 };
            yield return new object[] { (ushort)10, (float)2, (ushort)2 };
            yield return new object[] { (ushort)10, (double)2, (ushort)2 };
            yield return new object[] { (ushort)10, (decimal)2, (ushort)2 };
            yield return new object[] { (ushort)10, "2", (ushort)2 };
            yield return new object[] { (ushort)10, true, (ushort)0 };
            yield return new object[] { (ushort)10, null, (ushort)10 };

            // short.
            yield return new object[] { (short)10, (byte)2, (short)2 };
            yield return new object[] { (short)10, (sbyte)2, (short)2 };
            yield return new object[] { (short)10, (ushort)2, (short)2 };
            yield return new object[] { (short)10, (short)2, (short)2 };
            yield return new object[] { (short)10, (uint)2, (short)2 };
            yield return new object[] { (short)10, 2, (short)2 };
            yield return new object[] { (short)10, (ulong)2, (short)2 };
            yield return new object[] { (short)10, (long)2, (short)2 };
            yield return new object[] { (short)10, (float)2, (short)2 };
            yield return new object[] { (short)10, (double)2, (short)2 };
            yield return new object[] { (short)10, (decimal)2, (short)2 };
            yield return new object[] { (short)10, "2", (short)2 };
            yield return new object[] { (short)10, true, (short)0 };
            yield return new object[] { (short)10, null, (short)10 };

            // uint.
            yield return new object[] { (uint)10, (byte)2, (uint)2 };
            yield return new object[] { (uint)10, (sbyte)2, (uint)2 };
            yield return new object[] { (uint)10, (ushort)2, (uint)2 };
            yield return new object[] { (uint)10, (short)2, (uint)2 };
            yield return new object[] { (uint)10, (uint)2, (uint)2 };
            yield return new object[] { (uint)10, 2, (uint)2 };
            yield return new object[] { (uint)10, (ulong)2, (uint)2 };
            yield return new object[] { (uint)10, (long)2, (uint)2 };
            yield return new object[] { (uint)10, (float)2, (uint)2 };
            yield return new object[] { (uint)10, (double)2, (uint)2 };
            yield return new object[] { (uint)10, (decimal)2, (uint)2 };
            yield return new object[] { (uint)10, "2", (uint)2 };
            yield return new object[] { (uint)10, true, (uint)0 };
            yield return new object[] { (uint)10, null, (uint)10 };

            // int.
            yield return new object[] { 10, (byte)2, 2 };
            yield return new object[] { 10, (sbyte)2, 2 };
            yield return new object[] { 10, (ushort)2, 2 };
            yield return new object[] { 10, (short)2, 2 };
            yield return new object[] { 10, (uint)2, 2 };
            yield return new object[] { 10, 2, 2 };
            yield return new object[] { 10, (ulong)2, 2 };
            yield return new object[] { 10, (long)2, 2 };
            yield return new object[] { 10, (float)2, 2 };
            yield return new object[] { 10, (double)2, 2 };
            yield return new object[] { 10, (decimal)2, 2 };
            yield return new object[] { 10, "2", 2 };
            yield return new object[] { 10, true, 0 };
            yield return new object[] { 10, null, 10 };

            // ulong.
            yield return new object[] { (ulong)10, (byte)2, (ulong)2 };
            yield return new object[] { (ulong)10, (sbyte)2, (ulong)2 };
            yield return new object[] { (ulong)10, (ushort)2, (ulong)2 };
            yield return new object[] { (ulong)10, (short)2, (ulong)2 };
            yield return new object[] { (ulong)10, (uint)2, (ulong)2 };
            yield return new object[] { (ulong)10, 2, (ulong)2 };
            yield return new object[] { (ulong)10, (ulong)2, (ulong)2 };
            yield return new object[] { (ulong)10, (long)2, (ulong)2 };
            yield return new object[] { (ulong)10, (float)2, (ulong)2 };
            yield return new object[] { (ulong)10, (double)2, (ulong)2 };
            yield return new object[] { (ulong)10, (decimal)2, (ulong)2 };
            yield return new object[] { (ulong)10, "2", (ulong)2 };
            yield return new object[] { (ulong)10, true, (ulong)0 };
            yield return new object[] { (ulong)10, null, (ulong)10 };

            // long.
            yield return new object[] { (long)10, (byte)2, (long)2 };
            yield return new object[] { (long)10, (sbyte)2, (long)2 };
            yield return new object[] { (long)10, (ushort)2, (long)2 };
            yield return new object[] { (long)10, (short)2, (long)2 };
            yield return new object[] { (long)10, (uint)2, (long)2 };
            yield return new object[] { (long)10, 2, (long)2 };
            yield return new object[] { (long)10, (ulong)2, (long)2 };
            yield return new object[] { (long)10, (long)2, (long)2 };
            yield return new object[] { (long)10, (float)2, (long)2 };
            yield return new object[] { (long)10, (double)2, (long)2 };
            yield return new object[] { (long)10, (decimal)2, (long)2 };
            yield return new object[] { (long)10, "2", (long)2 };
            yield return new object[] { (long)10, true, (long)0 };
            yield return new object[] { (long)10, null, (long)10 };

            // float.
            yield return new object[] { (float)10, (byte)2, (long)2 };
            yield return new object[] { (float)10, (sbyte)2, (long)2 };
            yield return new object[] { (float)10, (ushort)2, (long)2 };
            yield return new object[] { (float)10, (short)2, (long)2 };
            yield return new object[] { (float)10, (uint)2, (long)2 };
            yield return new object[] { (float)10, 2, (long)2 };
            yield return new object[] { (float)10, (ulong)2, (long)2 };
            yield return new object[] { (float)10, (long)2, (long)2 };
            yield return new object[] { (float)10, (float)2, (long)2 };
            yield return new object[] { (float)10, (double)2, (long)2 };
            yield return new object[] { (float)10, (decimal)2, (long)2 };
            yield return new object[] { (float)10, "2", (long)2 };
            yield return new object[] { (float)10, true, (long)0 };
            yield return new object[] { (float)10, null, (long)10 };

            // double.
            yield return new object[] { (double)10, (byte)2, (long)2 };
            yield return new object[] { (double)10, (sbyte)2, (long)2 };
            yield return new object[] { (double)10, (ushort)2, (long)2 };
            yield return new object[] { (double)10, (short)2, (long)2 };
            yield return new object[] { (double)10, (uint)2, (long)2 };
            yield return new object[] { (double)10, 2, (long)2 };
            yield return new object[] { (double)10, (ulong)2, (long)2 };
            yield return new object[] { (double)10, (long)2, (long)2 };
            yield return new object[] { (double)10, (float)2, (long)2 };
            yield return new object[] { (double)10, (double)2, (long)2 };
            yield return new object[] { (double)10, (decimal)2, (long)2 };
            yield return new object[] { (double)10, "2", (long)2 };
            yield return new object[] { (double)10, true, (long)0 };
            yield return new object[] { (double)10, null, (long)10 };

            // decimal.
            yield return new object[] { (decimal)10, (byte)2, (long)2 };
            yield return new object[] { (decimal)10, (sbyte)2, (long)2 };
            yield return new object[] { (decimal)10, (ushort)2, (long)2 };
            yield return new object[] { (decimal)10, (short)2, (long)2 };
            yield return new object[] { (decimal)10, (uint)2, (long)2 };
            yield return new object[] { (decimal)10, 2, (long)2 };
            yield return new object[] { (decimal)10, (ulong)2, (long)2 };
            yield return new object[] { (decimal)10, (long)2, (long)2 };
            yield return new object[] { (decimal)10, (float)2, (long)2 };
            yield return new object[] { (decimal)10, (double)2, (long)2 };
            yield return new object[] { (decimal)10, (decimal)2, (long)2 };
            yield return new object[] { (decimal)10, "2", (long)2 };
            yield return new object[] { (decimal)10, true, (long)0 };
            yield return new object[] { (decimal)10, null, (long)10 };

            // string.
            yield return new object[] { "10", (byte)2, (long)2 };
            yield return new object[] { "10", (sbyte)2, (long)2 };
            yield return new object[] { "10", (ushort)2, (long)2 };
            yield return new object[] { "10", (short)2, (long)2 };
            yield return new object[] { "10", (uint)2, (long)2 };
            yield return new object[] { "10", 2, (long)2 };
            yield return new object[] { "10", (ulong)2, (long)2 };
            yield return new object[] { "10", (long)2, (long)2 };
            yield return new object[] { "10", (float)2, (long)2 };
            yield return new object[] { "10", (double)2, (long)2 };
            yield return new object[] { "10", (decimal)2, (long)2 };
            yield return new object[] { "10", "2", (long)2 };
            yield return new object[] { "10", true, (long)0 };
            yield return new object[] { "10", null, (long)10 };

            // bool.
            yield return new object[] { true, (byte)2, (short)-1 };
            yield return new object[] { true, (sbyte)2, (short)-1 };
            yield return new object[] { true, (ushort)2, (short)-1 };
            yield return new object[] { true, (short)2, (short)-1 };
            yield return new object[] { true, (uint)2, (short)-1 };
            yield return new object[] { true, 2, (short)-1 };
            yield return new object[] { true, (ulong)2, (short)-1 };
            yield return new object[] { true, (long)2, (short)-1 };
            yield return new object[] { true, (float)2, (short)-1 };
            yield return new object[] { true, (double)2, (short)-1 };
            yield return new object[] { true, (decimal)2, (short)-1 };
            yield return new object[] { true, "2", (short)-1 };
            yield return new object[] { true, true, (short)-1 };
            yield return new object[] { true, null, (short)-1 };

            // null.
            yield return new object[] { null, (byte)2, 0 };
            yield return new object[] { null, (sbyte)2, 0 };
            yield return new object[] { null, (ushort)2, 0 };
            yield return new object[] { null, (short)2, 0 };
            yield return new object[] { null, (uint)2, 0 };
            yield return new object[] { null, 2, 0 };
            yield return new object[] { null, (ulong)2, 0 };
            yield return new object[] { null, (long)2, 0 };
            yield return new object[] { null, (float)2, 0 };
            yield return new object[] { null, (double)2, 0 };
            yield return new object[] { null, (decimal)2, 0 };
            yield return new object[] { null, "2", 0 };
            yield return new object[] { null, true, 0 };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new RightShiftObject(), 2, "custom" };
            yield return new object[] { 2, new RightShiftObject(), "motsuc" };
            yield return new object[] { new RightShiftObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new RightShiftObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(RightShiftObject_TestData))]
        public void RightShiftObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.RightShiftObject(left, right));
        }

        public static IEnumerable<object[]> RightShiftObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(RightShiftObject_InvalidObjects_TestData))]
        public void RightShiftObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.RightShiftObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.RightShiftObject(right, left));
        }

        public static IEnumerable<object[]> RightShiftObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new RightShiftObject(), new object() };
            yield return new object[] { new object(), new RightShiftObject() };

            yield return new object[] { new RightShiftObject(), new RightShiftObject() };
        }

        [Theory]
        [MemberData(nameof(RightShiftObject_MismatchingObjects_TestData))]
        public void RightShiftObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.RightShiftObject(left, right));
        }

        public class RightShiftObject
        {
            [SpecialName]
            public static string op_RightShift(RightShiftObject left, int right) => "custom";

            [SpecialName]
            public static string op_RightShift(int left, RightShiftObject right) => "motsuc";

            [SpecialName]
            public static string op_RightShift(RightShiftObject left, OperatorsTests right) => "customobject";

            [SpecialName]
            public static string op_RightShift(OperatorsTests left, RightShiftObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> SubtractObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)2, (byte)2, (byte)0 };
            yield return new object[] { (byte)3, (sbyte)2, (short)1 };
            yield return new object[] { (byte)4, (ushort)2, (ushort)2 };
            yield return new object[] { (byte)5, (short)2, (short)3 };
            yield return new object[] { (byte)6, (uint)2, (uint)4 };
            yield return new object[] { (byte)7, 2, 5 };
            yield return new object[] { (byte)8, (long)2, (long)6 };
            yield return new object[] { (byte)9, (ulong)2, (ulong)7 };
            yield return new object[] { (byte)10, (float)2, (float)8 };
            yield return new object[] { (byte)11, (double)2, (double)9 };
            yield return new object[] { (byte)12, (decimal)2, (decimal)10 };
            yield return new object[] { (byte)13, "2", (double)11 };
            yield return new object[] { (byte)14, true, (short)15 };
            yield return new object[] { (byte)15, null, (byte)15 };
            yield return new object[] { (byte)16, byte.MaxValue, (short)(-239) };

            // sbyte.
            yield return new object[] { (sbyte)2, (byte)2, (short)0 };
            yield return new object[] { (sbyte)3, (sbyte)2, (sbyte)1 };
            yield return new object[] { (sbyte)4, (ushort)2, 2 };
            yield return new object[] { (sbyte)5, (short)2, (short)3 };
            yield return new object[] { (sbyte)6, (uint)2, (long)4 };
            yield return new object[] { (sbyte)7, 2, 5 };
            yield return new object[] { (sbyte)8, (long)2, (long)6 };
            yield return new object[] { (sbyte)9, (ulong)2, (decimal)7 };
            yield return new object[] { (sbyte)10, (float)2, (float)8 };
            yield return new object[] { (sbyte)11, (double)2, (double)9 };
            yield return new object[] { (sbyte)12, (decimal)2, (decimal)10 };
            yield return new object[] { (sbyte)13, "2", (double)11 };
            yield return new object[] { (sbyte)14, true, (sbyte)15 };
            yield return new object[] { (sbyte)15, null, (sbyte)15 };
            yield return new object[] { (sbyte)(-2), sbyte.MaxValue, (short)(-129) };

            // ushort.
            yield return new object[] { (ushort)2, (byte)2, (ushort)0 };
            yield return new object[] { (ushort)3, (sbyte)2, 1 };
            yield return new object[] { (ushort)4, (ushort)2, (ushort)2 };
            yield return new object[] { (ushort)5, (short)2, 3 };
            yield return new object[] { (ushort)6, (uint)2, (uint)4 };
            yield return new object[] { (ushort)7, 2, 5 };
            yield return new object[] { (ushort)8, (long)2, (long)6 };
            yield return new object[] { (ushort)9, (ulong)2, (ulong)7 };
            yield return new object[] { (ushort)10, (float)2, (float)8 };
            yield return new object[] { (ushort)11, (double)2, (double)9 };
            yield return new object[] { (ushort)12, (decimal)2, (decimal)10 };
            yield return new object[] { (ushort)13, "2", (double)11 };
            yield return new object[] { (ushort)14, true, 15 };
            yield return new object[] { (ushort)15, null, (ushort)15 };
            yield return new object[] { (ushort)16, ushort.MaxValue, -65519 };

            // short.
            yield return new object[] { (short)2, (byte)2, (short)0 };
            yield return new object[] { (short)3, (sbyte)2, (short)1 };
            yield return new object[] { (short)4, (ushort)2, 2 };
            yield return new object[] { (short)5, (short)2, (short)3 };
            yield return new object[] { (short)6, (uint)2, (long)4 };
            yield return new object[] { (short)7, 2, 5 };
            yield return new object[] { (short)8, (long)2, (long)6 };
            yield return new object[] { (short)9, (ulong)2, (decimal)7 };
            yield return new object[] { (short)10, (float)2, (float)8 };
            yield return new object[] { (short)11, (double)2, (double)9 };
            yield return new object[] { (short)12, (decimal)2, (decimal)10 };
            yield return new object[] { (short)13, "2", (double)11 };
            yield return new object[] { (short)14, true, (short)15 };
            yield return new object[] { (short)15, null, (short)15 };
            yield return new object[] { (short)(-2), short.MaxValue, -32769 };

            // uint.
            yield return new object[] { (uint)2, (byte)2, (uint)0 };
            yield return new object[] { (uint)3, (sbyte)2, (long)1 };
            yield return new object[] { (uint)4, (ushort)2, (uint)2 };
            yield return new object[] { (uint)5, (short)2, (long)3 };
            yield return new object[] { (uint)6, (uint)2, (uint)4 };
            yield return new object[] { (uint)7, 2, (long)5 };
            yield return new object[] { (uint)8, (long)2, (long)6 };
            yield return new object[] { (uint)9, (ulong)2, (ulong)7 };
            yield return new object[] { (uint)10, (float)2, (float)8 };
            yield return new object[] { (uint)11, (double)2, (double)9 };
            yield return new object[] { (uint)12, (decimal)2, (decimal)10 };
            yield return new object[] { (uint)13, "2", (double)11 };
            yield return new object[] { (uint)14, true, (long)15 };
            yield return new object[] { (uint)15, null, (uint)15 };
            yield return new object[] { (uint)16, uint.MaxValue, (long)(-4294967279) };

            // int.
            yield return new object[] { 2, (byte)2, 0 };
            yield return new object[] { 3, (sbyte)2, 1 };
            yield return new object[] { 4, (ushort)2, 2 };
            yield return new object[] { 5, (short)2, 3 };
            yield return new object[] { 6, (uint)2, (long)4 };
            yield return new object[] { 7, 2, 5 };
            yield return new object[] { 8, (long)2, (long)6 };
            yield return new object[] { 9, (ulong)2, (decimal)7 };
            yield return new object[] { 10, (float)2, (float)8 };
            yield return new object[] { 11, (double)2, (double)9 };
            yield return new object[] { 12, (decimal)2, (decimal)10 };
            yield return new object[] { 13, "2", (double)11 };
            yield return new object[] { 14, true, 15 };
            yield return new object[] { 15, null, 15 };
            yield return new object[] { -2, int.MaxValue, (long)(-2147483649) };

            // ulong.
            yield return new object[] { (ulong)2, (byte)2, (ulong)0 };
            yield return new object[] { (ulong)3, (sbyte)2, (decimal)1 };
            yield return new object[] { (ulong)4, (ushort)2, (ulong)2 };
            yield return new object[] { (ulong)5, (short)2, (decimal)3 };
            yield return new object[] { (ulong)6, (uint)2, (ulong)4 };
            yield return new object[] { (ulong)7, 2, (decimal)5 };
            yield return new object[] { (ulong)8, (long)2, (decimal)6 };
            yield return new object[] { (ulong)9, (ulong)2, (ulong)7 };
            yield return new object[] { (ulong)10, (float)2, (float)8 };
            yield return new object[] { (ulong)11, (double)2, (double)9 };
            yield return new object[] { (ulong)12, (decimal)2, (decimal)10 };
            yield return new object[] { (ulong)13, "2", (double)11 };
            yield return new object[] { (ulong)14, true, (decimal)15 };
            yield return new object[] { (ulong)15, null, (ulong)15 };
            yield return new object[] { (ulong)16, ulong.MaxValue, decimal.Parse("-18446744073709551599", CultureInfo.InvariantCulture) };

            // long.
            yield return new object[] { (long)2, (byte)2, (long)0 };
            yield return new object[] { (long)3, (sbyte)2, (long)1 };
            yield return new object[] { (long)4, (ushort)2, (long)2 };
            yield return new object[] { (long)5, (short)2, (long)3 };
            yield return new object[] { (long)6, (uint)2, (long)4 };
            yield return new object[] { (long)7, 2, (long)5 };
            yield return new object[] { (long)8, (long)2, (long)6 };
            yield return new object[] { (long)9, (ulong)2, (decimal)7 };
            yield return new object[] { (long)10, (float)2, (float)8 };
            yield return new object[] { (long)11, (double)2, (double)9 };
            yield return new object[] { (long)12, (decimal)2, (decimal)10 };
            yield return new object[] { (long)13, "2", (double)11 };
            yield return new object[] { (long)14, true, (long)15 };
            yield return new object[] { (long)15, null, (long)15 };
            yield return new object[] { (long)(-2), long.MaxValue, decimal.Parse("-9223372036854775809", CultureInfo.InvariantCulture) };

            // float.
            yield return new object[] { (float)2, (byte)2, (float)0 };
            yield return new object[] { (float)3, (sbyte)2, (float)1 };
            yield return new object[] { (float)4, (ushort)2, (float)2 };
            yield return new object[] { (float)5, (short)2, (float)3 };
            yield return new object[] { (float)6, (uint)2, (float)4 };
            yield return new object[] { (float)7, 2, (float)5 };
            yield return new object[] { (float)8, (long)2, (float)6 };
            yield return new object[] { (float)9, (ulong)2, (float)7 };
            yield return new object[] { (float)10, (float)2, (float)8 };
            yield return new object[] { (float)11, (double)2, (double)9 };
            yield return new object[] { (float)12, (decimal)2, (float)10 };
            yield return new object[] { (float)13, "2", (double)11 };
            yield return new object[] { (float)14, true, (float)15 };
            yield return new object[] { (float)15, null, (float)15 };
            yield return new object[] { float.MinValue, float.MaxValue, (double)float.MinValue - (double)float.MaxValue };
            yield return new object[] { (float)16, float.PositiveInfinity, float.NegativeInfinity };
            yield return new object[] { (float)17, float.NegativeInfinity, float.PositiveInfinity };
            yield return new object[] { (float)18, float.NaN, double.NaN };
            yield return new object[] { float.PositiveInfinity, (float)2, float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity, (float)2, float.NegativeInfinity };
            yield return new object[] { float.NaN, (float)2, double.NaN };

            // double.
            yield return new object[] { (double)2, (byte)2, (double)0 };
            yield return new object[] { (double)3, (sbyte)2, (double)1 };
            yield return new object[] { (double)4, (ushort)2, (double)2 };
            yield return new object[] { (double)5, (short)2, (double)3 };
            yield return new object[] { (double)6, (uint)2, (double)4 };
            yield return new object[] { (double)7, 2, (double)5 };
            yield return new object[] { (double)8, (long)2, (double)6 };
            yield return new object[] { (double)9, (ulong)2, (double)7 };
            yield return new object[] { (double)10, (float)2, (double)8 };
            yield return new object[] { (double)11, (double)2, (double)9 };
            yield return new object[] { (double)12, (decimal)2, (double)10 };
            yield return new object[] { (double)13, "2", (double)11 };
            yield return new object[] { (double)14, true, (double)15 };
            yield return new object[] { (double)15, null, (double)15 };
            yield return new object[] { double.MinValue, double.MaxValue, double.NegativeInfinity };
            yield return new object[] { (double)16, double.PositiveInfinity, double.NegativeInfinity };
            yield return new object[] { (double)17, double.NegativeInfinity, double.PositiveInfinity };
            yield return new object[] { (double)18, double.NaN, double.NaN };
            yield return new object[] { double.PositiveInfinity, (double)2, double.PositiveInfinity };
            yield return new object[] { double.NegativeInfinity, (double)2, double.NegativeInfinity };
            yield return new object[] { double.NaN, (double)2, double.NaN };

            // decimal.
            yield return new object[] { (decimal)2, (byte)2, (decimal)0 };
            yield return new object[] { (decimal)3, (sbyte)2, (decimal)1 };
            yield return new object[] { (decimal)4, (ushort)2, (decimal)2 };
            yield return new object[] { (decimal)5, (short)2, (decimal)3 };
            yield return new object[] { (decimal)6, (uint)2, (decimal)4 };
            yield return new object[] { (decimal)7, 2, (decimal)5 };
            yield return new object[] { (decimal)8, (long)2, (decimal)6 };
            yield return new object[] { (decimal)9, (ulong)2, (decimal)7 };
            yield return new object[] { (decimal)10, (float)2, (float)8 };
            yield return new object[] { (decimal)11, (double)2, (double)9 };
            yield return new object[] { (decimal)12, (decimal)2, (decimal)10 };
            yield return new object[] { (decimal)13, "2", (double)11 };
            yield return new object[] { (decimal)14, true, (decimal)15 };
            yield return new object[] { (decimal)15, null, (decimal)15 };
            yield return new object[] { decimal.MinValue, decimal.MaxValue, double.Parse("-1.5845632502852868E+29", NumberStyles.Any, CultureInfo.InvariantCulture) };
        
            // string.
            yield return new object[] { "2", (byte)2, (double)0 };
            yield return new object[] { "3", (sbyte)2, (double)1 };
            yield return new object[] { "4", (ushort)2, (double)2 };
            yield return new object[] { "5", (short)2, (double)3 };
            yield return new object[] { "6", (uint)2, (double)4 };
            yield return new object[] { "7", 2, (double)5 };
            yield return new object[] { "8", (long)2, (double)6 };
            yield return new object[] { "9", (ulong)2, (double)7 };
            yield return new object[] { "10", (float)2, (double)8 };
            yield return new object[] { "11", (double)2, (double)9 };
            yield return new object[] { "12", (decimal)2, (double)10 };
            yield return new object[] { "13", "2", (double)11 };
            yield return new object[] { "14", true, (double)15 };
            yield return new object[] { "15", null, (double)15 };

            // bool.
            yield return new object[] { true, (byte)2, (short)(-3) };
            yield return new object[] { true, (sbyte)2, (sbyte)(-3) };
            yield return new object[] { true, (ushort)2, -3 };
            yield return new object[] { true, (short)2, (short)(-3) };
            yield return new object[] { true, (uint)2, (long)(-3) };
            yield return new object[] { true, 2, -3 };
            yield return new object[] { true, (long)2, (long)(-3) };
            yield return new object[] { true, (ulong)2, (decimal)(-3) };
            yield return new object[] { true, (float)2, (float)(-3) };
            yield return new object[] { true, (double)2, (double)(-3) };
            yield return new object[] { true, (decimal)2, (decimal)(-3) };
            yield return new object[] { true, "2", (double)(-3) };
            yield return new object[] { true, false, (short)(-1) };
            yield return new object[] { true, null, (short)(-1) };

            // null.
            yield return new object[] { null, (byte)2, (short)(-2) };
            yield return new object[] { null, (sbyte)2, (sbyte)(-2) };
            yield return new object[] { null, (ushort)2, -2 };
            yield return new object[] { null, (short)2, (short)(-2) };
            yield return new object[] { null, (uint)2, (long)(-2) };
            yield return new object[] { null, 2, -2 };
            yield return new object[] { null, (long)2, (long)(-2) };
            yield return new object[] { null, (ulong)2, (decimal)(-2) };
            yield return new object[] { null, (float)2, (float)(-2) };
            yield return new object[] { null, (double)2, (double)(-2) };
            yield return new object[] { null, (decimal)2, (decimal)(-2) };
            yield return new object[] { null, "2", (double)(-2) };
            yield return new object[] { null, false, (short)0 };
            yield return new object[] { null, new DateTime(10), new TimeSpan(-10) };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new SubtractObject(), 2, "custom" };
            yield return new object[] { 2, new SubtractObject(), "motsuc" };
            yield return new object[] { new SubtractObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new SubtractObject(), "tcejbomotsuc" };

            // DateTime.
            yield return new object[] { new DateTime(10), new TimeSpan(5), new DateTime(5) };
            yield return new object[] { new DateTime(10), new DateTime(5), new TimeSpan(5) };
            yield return new object[] { new DateTime(10), null, new TimeSpan(10) };
        }

        [Theory]
        [MemberData(nameof(SubtractObject_TestData))]
        public void SubtractObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.SubtractObject(left, right));
        }

        public static IEnumerable<object[]> SubtractObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(SubtractObject_InvalidObjects_TestData))]
        public void SubtractObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.SubtractObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.SubtractObject(right, left));
        }

        public static IEnumerable<object[]> SubtractObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new SubtractObject(), new object() };
            yield return new object[] { new object(), new SubtractObject() };

            yield return new object[] { new SubtractObject(), new SubtractObject() };
        }

        [Theory]
        [MemberData(nameof(SubtractObject_MismatchingObjects_TestData))]
        public void SubtractObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.SubtractObject(left, right));
        }

        public class SubtractObject
        {
            public static string operator -(SubtractObject left, int right) => "custom";
            public static string operator -(int left, SubtractObject right) => "motsuc";

            public static string operator -(SubtractObject left, OperatorsTests right) => "customobject";
            public static string operator -(OperatorsTests left, SubtractObject right) => "tcejbomotsuc";
        }

        public static IEnumerable<object[]> XorObject_TestData()
        {
            // byte.
            yield return new object[] { (byte)10, (byte)14, (byte)4 };
            yield return new object[] { (byte)10, (ByteEnum)14, (byte)4 };
            yield return new object[] { (byte)10, (sbyte)14, (short)4 };
            yield return new object[] { (byte)10, (SByteEnum)14, (short)4 };
            yield return new object[] { (byte)10, (ushort)14, (ushort)4 };
            yield return new object[] { (byte)10, (UShortEnum)14, (ushort)4 };
            yield return new object[] { (byte)10, (short)14, (short)4 };
            yield return new object[] { (byte)10, (ShortEnum)14, (short)4 };
            yield return new object[] { (byte)10, (uint)14, (uint)4 };
            yield return new object[] { (byte)10, (UIntEnum)14, (uint)4 };
            yield return new object[] { (byte)10, 14, 4 };
            yield return new object[] { (byte)10, (IntEnum)14, 4 };
            yield return new object[] { (byte)10, (ulong)14, (ulong)4 };
            yield return new object[] { (byte)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (byte)10, (long)14, (long)4 };
            yield return new object[] { (byte)10, (LongEnum)14, (long)4 };
            yield return new object[] { (byte)10, (float)14, (long)4 };
            yield return new object[] { (byte)10, (double)14, (long)4 };
            yield return new object[] { (byte)10, (decimal)14, (long)4 };
            yield return new object[] { (byte)10, "14", (long)4 };
            yield return new object[] { (byte)10, true, (short)-11 };
            yield return new object[] { (byte)10, null, (byte)10 };

            yield return new object[] { (ByteEnum)10, (byte)14, (byte)4 };
            yield return new object[] { (ByteEnum)10, (ByteEnum)14, (ByteEnum)4 };
            yield return new object[] { (ByteEnum)10, (sbyte)14, (short)4 };
            yield return new object[] { (ByteEnum)10, (SByteEnum)14, (short)4 };
            yield return new object[] { (ByteEnum)10, (ushort)14, (ushort)4 };
            yield return new object[] { (ByteEnum)10, (UShortEnum)14, (ushort)4 };
            yield return new object[] { (ByteEnum)10, (short)14, (short)4 };
            yield return new object[] { (ByteEnum)10, (ShortEnum)14, (short)4 };
            yield return new object[] { (ByteEnum)10, (uint)14, (uint)4 };
            yield return new object[] { (ByteEnum)10, (UIntEnum)14, (uint)4 };
            yield return new object[] { (ByteEnum)10, 14, 4 };
            yield return new object[] { (ByteEnum)10, (IntEnum)14, 4 };
            yield return new object[] { (ByteEnum)10, (ulong)14, (ulong)4 };
            yield return new object[] { (ByteEnum)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (ByteEnum)10, (long)14, (long)4 };
            yield return new object[] { (ByteEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (ByteEnum)10, (float)14, (long)4 };
            yield return new object[] { (ByteEnum)10, (double)14, (long)4 };
            yield return new object[] { (ByteEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (ByteEnum)10, "14", (long)4 };
            yield return new object[] { (ByteEnum)10, true, (short)-11 };
            yield return new object[] { (ByteEnum)10, null, (ByteEnum)10 };

            // sbyte.
            yield return new object[] { (sbyte)10, (byte)14, (short)4 };
            yield return new object[] { (sbyte)10, (ByteEnum)14, (short)4 };
            yield return new object[] { (sbyte)10, (sbyte)14, (sbyte)4 };
            yield return new object[] { (sbyte)10, (SByteEnum)14, (sbyte)4 };
            yield return new object[] { (sbyte)10, (ushort)14, 4 };
            yield return new object[] { (sbyte)10, (UShortEnum)14, 4 };
            yield return new object[] { (sbyte)10, (short)14, (short)4 };
            yield return new object[] { (sbyte)10, (ShortEnum)14, (short)4 };
            yield return new object[] { (sbyte)10, (uint)14, (long)4 };
            yield return new object[] { (sbyte)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (sbyte)10, 14, 4 };
            yield return new object[] { (sbyte)10, (IntEnum)14, 4 };
            yield return new object[] { (sbyte)10, (ulong)14, (long)4 };
            yield return new object[] { (sbyte)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (sbyte)10, (long)14, (long)4 };
            yield return new object[] { (sbyte)10, (LongEnum)14, (long)4 };
            yield return new object[] { (sbyte)10, (float)14, (long)4 };
            yield return new object[] { (sbyte)10, (double)14, (long)4 };
            yield return new object[] { (sbyte)10, (decimal)14, (long)4 };
            yield return new object[] { (sbyte)10, "14", (long)4 };
            yield return new object[] { (sbyte)10, true, (sbyte)-11 };
            yield return new object[] { (sbyte)10, null, (sbyte)10 };

            yield return new object[] { (SByteEnum)10, (byte)14, (short)4 };
            yield return new object[] { (SByteEnum)10, (ByteEnum)14, (short)4 };
            yield return new object[] { (SByteEnum)10, (sbyte)14, (sbyte)4 };
            yield return new object[] { (SByteEnum)10, (SByteEnum)14, (SByteEnum)4 };
            yield return new object[] { (SByteEnum)10, (ushort)14, 4 };
            yield return new object[] { (SByteEnum)10, (UShortEnum)14, 4 };
            yield return new object[] { (SByteEnum)10, (short)14, (short)4 };
            yield return new object[] { (SByteEnum)10, (ShortEnum)14, (short)4 };
            yield return new object[] { (SByteEnum)10, (uint)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (SByteEnum)10, 14, 4 };
            yield return new object[] { (SByteEnum)10, (IntEnum)14, 4 };
            yield return new object[] { (SByteEnum)10, (ulong)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (long)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (float)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (double)14, (long)4 };
            yield return new object[] { (SByteEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (SByteEnum)10, "14", (long)4 };
            yield return new object[] { (SByteEnum)10, true, (sbyte)-11 };
            yield return new object[] { (SByteEnum)10, null, (SByteEnum)10 };

            // ushort.
            yield return new object[] { (ushort)10, (byte)14, (ushort)4 };
            yield return new object[] { (ushort)10, (ByteEnum)14, (ushort)4 };
            yield return new object[] { (ushort)10, (sbyte)14, 4 };
            yield return new object[] { (ushort)10, (SByteEnum)14, 4 };
            yield return new object[] { (ushort)10, (ushort)14, (ushort)4 };
            yield return new object[] { (ushort)10, (UShortEnum)14, (ushort)4 };
            yield return new object[] { (ushort)10, (short)14, 4 };
            yield return new object[] { (ushort)10, (ShortEnum)14, 4 };
            yield return new object[] { (ushort)10, (uint)14, (uint)4 };
            yield return new object[] { (ushort)10, (UIntEnum)14, (uint)4 };
            yield return new object[] { (ushort)10, 14, 4 };
            yield return new object[] { (ushort)10, (IntEnum)14, 4 };
            yield return new object[] { (ushort)10, (ulong)14, (ulong)4 };
            yield return new object[] { (ushort)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (ushort)10, (long)14, (long)4 };
            yield return new object[] { (ushort)10, (LongEnum)14, (long)4 };
            yield return new object[] { (ushort)10, (float)14, (long)4 };
            yield return new object[] { (ushort)10, (double)14, (long)4 };
            yield return new object[] { (ushort)10, (decimal)14, (long)4 };
            yield return new object[] { (ushort)10, "14", (long)4 };
            yield return new object[] { (ushort)10, true, -11 };
            yield return new object[] { (ushort)10, null, (ushort)10 };

            yield return new object[] { (UShortEnum)10, (byte)14, (ushort)4 };
            yield return new object[] { (UShortEnum)10, (ByteEnum)14, (ushort)4 };
            yield return new object[] { (UShortEnum)10, (sbyte)14, 4 };
            yield return new object[] { (UShortEnum)10, (SByteEnum)14, 4 };
            yield return new object[] { (UShortEnum)10, (ushort)14, (ushort)4 };
            yield return new object[] { (UShortEnum)10, (UShortEnum)14, (UShortEnum)4 };
            yield return new object[] { (UShortEnum)10, (short)14, 4 };
            yield return new object[] { (UShortEnum)10, (ShortEnum)14, 4 };
            yield return new object[] { (UShortEnum)10, (uint)14, (uint)4 };
            yield return new object[] { (UShortEnum)10, (UIntEnum)14, (uint)4 };
            yield return new object[] { (UShortEnum)10, 14, 4 };
            yield return new object[] { (UShortEnum)10, (IntEnum)14, 4 };
            yield return new object[] { (UShortEnum)10, (ulong)14, (ulong)4 };
            yield return new object[] { (UShortEnum)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (UShortEnum)10, (long)14, (long)4 };
            yield return new object[] { (UShortEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (UShortEnum)10, (float)14, (long)4 };
            yield return new object[] { (UShortEnum)10, (double)14, (long)4 };
            yield return new object[] { (UShortEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (UShortEnum)10, "14", (long)4 };
            yield return new object[] { (UShortEnum)10, true, -11 };
            yield return new object[] { (UShortEnum)10, null, (UShortEnum)10 };

            // short.
            yield return new object[] { (short)10, (byte)14, (short)4 };
            yield return new object[] { (short)10, (ByteEnum)14, (short)4 };
            yield return new object[] { (short)10, (sbyte)14, (short)4 };
            yield return new object[] { (short)10, (SByteEnum)14, (short)4 };
            yield return new object[] { (short)10, (ushort)14, 4 };
            yield return new object[] { (short)10, (UShortEnum)14, 4 };
            yield return new object[] { (short)10, (short)14, (short)4 };
            yield return new object[] { (short)10, (ShortEnum)14, (short)4 };
            yield return new object[] { (short)10, (uint)14, (long)4 };
            yield return new object[] { (short)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (short)10, 14, 4 };
            yield return new object[] { (short)10, (IntEnum)14, 4 };
            yield return new object[] { (short)10, (ulong)14, (long)4 };
            yield return new object[] { (short)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (short)10, (long)14, (long)4 };
            yield return new object[] { (short)10, (LongEnum)14, (long)4 };
            yield return new object[] { (short)10, (float)14, (long)4 };
            yield return new object[] { (short)10, (double)14, (long)4 };
            yield return new object[] { (short)10, (decimal)14, (long)4 };
            yield return new object[] { (short)10, "14", (long)4 };
            yield return new object[] { (short)10, true, (short)-11 };
            yield return new object[] { (short)10, null, (short)10 };

            yield return new object[] { (ShortEnum)10, (byte)14, (short)4 };
            yield return new object[] { (ShortEnum)10, (ByteEnum)14, (short)4 };
            yield return new object[] { (ShortEnum)10, (sbyte)14, (short)4 };
            yield return new object[] { (ShortEnum)10, (SByteEnum)14, (short)4 };
            yield return new object[] { (ShortEnum)10, (ushort)14, 4 };
            yield return new object[] { (ShortEnum)10, (UShortEnum)14, 4 };
            yield return new object[] { (ShortEnum)10, (short)14, (short)4 };
            yield return new object[] { (ShortEnum)10, (ShortEnum)14, (ShortEnum)4 };
            yield return new object[] { (ShortEnum)10, (uint)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (ShortEnum)10, 14, 4 };
            yield return new object[] { (ShortEnum)10, (IntEnum)14, 4 };
            yield return new object[] { (ShortEnum)10, (ulong)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (long)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (float)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (double)14, (long)4 };
            yield return new object[] { (ShortEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (ShortEnum)10, "14", (long)4 };
            yield return new object[] { (ShortEnum)10, true, (short)-11 };
            yield return new object[] { (ShortEnum)10, null, (ShortEnum)10 };

            // uint.
            yield return new object[] { (uint)10, (byte)14, (uint)4 };
            yield return new object[] { (uint)10, (ByteEnum)14, (uint)4 };
            yield return new object[] { (uint)10, (sbyte)14, (long)4 };
            yield return new object[] { (uint)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (uint)10, (ushort)14, (uint)4 };
            yield return new object[] { (uint)10, (UShortEnum)14, (uint)4 };
            yield return new object[] { (uint)10, (short)14, (long)4 };
            yield return new object[] { (uint)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (uint)10, (uint)14, (uint)4 };
            yield return new object[] { (uint)10, (UIntEnum)14, (uint)4 };
            yield return new object[] { (uint)10, 14, (long)4 };
            yield return new object[] { (uint)10, (IntEnum)14, (long)4 };
            yield return new object[] { (uint)10, (ulong)14, (ulong)4 };
            yield return new object[] { (uint)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (uint)10, (long)14, (long)4 };
            yield return new object[] { (uint)10, (LongEnum)14, (long)4 };
            yield return new object[] { (uint)10, (float)14, (long)4 };
            yield return new object[] { (uint)10, (double)14, (long)4 };
            yield return new object[] { (uint)10, (decimal)14, (long)4 };
            yield return new object[] { (uint)10, "14", (long)4 };
            yield return new object[] { (uint)10, true, (long)-11 };
            yield return new object[] { (uint)10, null, (uint)10 };

            yield return new object[] { (UIntEnum)10, (byte)14, (uint)4 };
            yield return new object[] { (UIntEnum)10, (ByteEnum)14, (uint)4 };
            yield return new object[] { (UIntEnum)10, (sbyte)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (ushort)14, (uint)4 };
            yield return new object[] { (UIntEnum)10, (UShortEnum)14, (uint)4 };
            yield return new object[] { (UIntEnum)10, (short)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (uint)14, (uint)4 };
            yield return new object[] { (UIntEnum)10, (UIntEnum)14, (UIntEnum)4 };
            yield return new object[] { (UIntEnum)10, 14, (long)4 };
            yield return new object[] { (UIntEnum)10, (IntEnum)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (ulong)14, (ulong)4 };
            yield return new object[] { (UIntEnum)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (UIntEnum)10, (long)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (float)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (double)14, (long)4 };
            yield return new object[] { (UIntEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (UIntEnum)10, "14", (long)4 };
            yield return new object[] { (UIntEnum)10, true, (long)-11 };
            yield return new object[] { (UIntEnum)10, null, (UIntEnum)10 };

            // int.
            yield return new object[] { 10, (byte)14, 4 };
            yield return new object[] { 10, (ByteEnum)14, 4 };
            yield return new object[] { 10, (sbyte)14, 4 };
            yield return new object[] { 10, (SByteEnum)14, 4 };
            yield return new object[] { 10, (ushort)14, 4 };
            yield return new object[] { 10, (UShortEnum)14, 4 };
            yield return new object[] { 10, (short)14, 4 };
            yield return new object[] { 10, (ShortEnum)14, 4 };
            yield return new object[] { 10, (uint)14, (long)4 };
            yield return new object[] { 10, (UIntEnum)14, (long)4 };
            yield return new object[] { 10, 14, 4 };
            yield return new object[] { 10, (IntEnum)14, 4 };
            yield return new object[] { 10, (ulong)14, (long)4 };
            yield return new object[] { 10, (ULongEnum)14, (long)4 };
            yield return new object[] { 10, (long)14, (long)4 };
            yield return new object[] { 10, (LongEnum)14, (long)4 };
            yield return new object[] { 10, (float)14, (long)4 };
            yield return new object[] { 10, (double)14, (long)4 };
            yield return new object[] { 10, (decimal)14, (long)4 };
            yield return new object[] { 10, "14", (long)4 };
            yield return new object[] { 10, true, -11 };
            yield return new object[] { 10, null, 10 };

            yield return new object[] { (IntEnum)10, (byte)14, 4 };
            yield return new object[] { (IntEnum)10, (ByteEnum)14, 4 };
            yield return new object[] { (IntEnum)10, (sbyte)14, 4 };
            yield return new object[] { (IntEnum)10, (SByteEnum)14, 4 };
            yield return new object[] { (IntEnum)10, (ushort)14, 4 };
            yield return new object[] { (IntEnum)10, (UShortEnum)14, 4 };
            yield return new object[] { (IntEnum)10, (short)14, 4 };
            yield return new object[] { (IntEnum)10, (ShortEnum)14, 4 };
            yield return new object[] { (IntEnum)10, (uint)14, (long)4 };
            yield return new object[] { (IntEnum)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (IntEnum)10, 14, 4 };
            yield return new object[] { (IntEnum)10, (IntEnum)14, (IntEnum)4 };
            yield return new object[] { (IntEnum)10, (ulong)14, (long)4 };
            yield return new object[] { (IntEnum)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (IntEnum)10, (long)14, (long)4 };
            yield return new object[] { (IntEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (IntEnum)10, (float)14, (long)4 };
            yield return new object[] { (IntEnum)10, (double)14, (long)4 };
            yield return new object[] { (IntEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (IntEnum)10, "14", (long)4 };
            yield return new object[] { (IntEnum)10, true, -11 };
            yield return new object[] { (IntEnum)10, null, (IntEnum)10 };

            // ulong.
            yield return new object[] { (ulong)10, (byte)14, (ulong)4 };
            yield return new object[] { (ulong)10, (ByteEnum)14, (ulong)4 };
            yield return new object[] { (ulong)10, (sbyte)14, (long)4 };
            yield return new object[] { (ulong)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (ulong)10, (ushort)14, (ulong)4 };
            yield return new object[] { (ulong)10, (UShortEnum)14, (ulong)4 };
            yield return new object[] { (ulong)10, (short)14, (long)4 };
            yield return new object[] { (ulong)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (ulong)10, (uint)14, (ulong)4 };
            yield return new object[] { (ulong)10, (UIntEnum)14, (ulong)4 };
            yield return new object[] { (ulong)10, 14, (long)4 };
            yield return new object[] { (ulong)10, (IntEnum)14, (long)4 };
            yield return new object[] { (ulong)10, (ulong)14, (ulong)4 };
            yield return new object[] { (ulong)10, (ULongEnum)14, (ulong)4 };
            yield return new object[] { (ulong)10, (long)14, (long)4 };
            yield return new object[] { (ulong)10, (LongEnum)14, (long)4 };
            yield return new object[] { (ulong)10, (float)14, (long)4 };
            yield return new object[] { (ulong)10, (double)14, (long)4 };
            yield return new object[] { (ulong)10, (decimal)14, (long)4 };
            yield return new object[] { (ulong)10, "14", (long)4 };
            yield return new object[] { (ulong)10, true, (long)-11 };
            yield return new object[] { (ulong)10, null, (ulong)10 };

            yield return new object[] { (ULongEnum)10, (byte)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, (ByteEnum)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, (sbyte)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (ushort)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, (UShortEnum)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, (short)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (uint)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, (UIntEnum)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, 14, (long)4 };
            yield return new object[] { (ULongEnum)10, (IntEnum)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (ulong)14, (ulong)4 };
            yield return new object[] { (ULongEnum)10, (ULongEnum)14, (ULongEnum)4 };
            yield return new object[] { (ULongEnum)10, (long)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (LongEnum)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (float)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (double)14, (long)4 };
            yield return new object[] { (ULongEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (ULongEnum)10, "14", (long)4 };
            yield return new object[] { (ULongEnum)10, true, (long)-11 };
            yield return new object[] { (ULongEnum)10, null, (ULongEnum)10 };

            // long.
            yield return new object[] { (long)10, (byte)14, (long)4 };
            yield return new object[] { (long)10, (ByteEnum)14, (long)4 };
            yield return new object[] { (long)10, (sbyte)14, (long)4 };
            yield return new object[] { (long)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (long)10, (ushort)14, (long)4 };
            yield return new object[] { (long)10, (UShortEnum)14, (long)4 };
            yield return new object[] { (long)10, (short)14, (long)4 };
            yield return new object[] { (long)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (long)10, (uint)14, (long)4 };
            yield return new object[] { (long)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (long)10, 14, (long)4 };
            yield return new object[] { (long)10, (IntEnum)14, (long)4 };
            yield return new object[] { (long)10, (ulong)14, (long)4 };
            yield return new object[] { (long)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (long)10, (long)14, (long)4 };
            yield return new object[] { (long)10, (LongEnum)14, (long)4 };
            yield return new object[] { (long)10, (float)14, (long)4 };
            yield return new object[] { (long)10, (double)14, (long)4 };
            yield return new object[] { (long)10, (decimal)14, (long)4 };
            yield return new object[] { (long)10, "14", (long)4 };
            yield return new object[] { (long)10, true, (long)-11 };
            yield return new object[] { (long)10, null, (long)10 };

            yield return new object[] { (LongEnum)10, (byte)14, (long)4 };
            yield return new object[] { (LongEnum)10, (ByteEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, (sbyte)14, (long)4 };
            yield return new object[] { (LongEnum)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, (ushort)14, (long)4 };
            yield return new object[] { (LongEnum)10, (UShortEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, (short)14, (long)4 };
            yield return new object[] { (LongEnum)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, (uint)14, (long)4 };
            yield return new object[] { (LongEnum)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, 14, (long)4 };
            yield return new object[] { (LongEnum)10, (IntEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, (ulong)14, (long)4 };
            yield return new object[] { (LongEnum)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (LongEnum)10, (long)14, (long)4 };
            yield return new object[] { (LongEnum)10, (LongEnum)14, (LongEnum)4 };
            yield return new object[] { (LongEnum)10, (float)14, (long)4 };
            yield return new object[] { (LongEnum)10, (double)14, (long)4 };
            yield return new object[] { (LongEnum)10, (decimal)14, (long)4 };
            yield return new object[] { (LongEnum)10, "14", (long)4 };
            yield return new object[] { (LongEnum)10, true, (long)-11 };
            yield return new object[] { (LongEnum)10, null, (LongEnum)10 };

            // float.
            yield return new object[] { (float)10, (byte)14, (long)4 };
            yield return new object[] { (float)10, (ByteEnum)14, (long)4 };
            yield return new object[] { (float)10, (sbyte)14, (long)4 };
            yield return new object[] { (float)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (float)10, (ushort)14, (long)4 };
            yield return new object[] { (float)10, (UShortEnum)14, (long)4 };
            yield return new object[] { (float)10, (short)14, (long)4 };
            yield return new object[] { (float)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (float)10, (uint)14, (long)4 };
            yield return new object[] { (float)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (float)10, 14, (long)4 };
            yield return new object[] { (float)10, (IntEnum)14, (long)4 };
            yield return new object[] { (float)10, (ulong)14, (long)4 };
            yield return new object[] { (float)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (float)10, (long)14, (long)4 };
            yield return new object[] { (float)10, (LongEnum)14, (long)4 };
            yield return new object[] { (float)10, (float)14, (long)4 };
            yield return new object[] { (float)10, (double)14, (long)4 };
            yield return new object[] { (float)10, (decimal)14, (long)4 };
            yield return new object[] { (float)10, "14", (long)4 };
            yield return new object[] { (float)10, true, (long)-11 };
            yield return new object[] { (float)10, null, (long)10 };

            // double.
            yield return new object[] { (double)10, (byte)14, (long)4 };
            yield return new object[] { (double)10, (ByteEnum)14, (long)4 };
            yield return new object[] { (double)10, (sbyte)14, (long)4 };
            yield return new object[] { (double)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (double)10, (ushort)14, (long)4 };
            yield return new object[] { (double)10, (UShortEnum)14, (long)4 };
            yield return new object[] { (double)10, (short)14, (long)4 };
            yield return new object[] { (double)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (double)10, (uint)14, (long)4 };
            yield return new object[] { (double)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (double)10, 14, (long)4 };
            yield return new object[] { (double)10, (IntEnum)14, (long)4 };
            yield return new object[] { (double)10, (ulong)14, (long)4 };
            yield return new object[] { (double)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (double)10, (long)14, (long)4 };
            yield return new object[] { (double)10, (LongEnum)14, (long)4 };
            yield return new object[] { (double)10, (float)14, (long)4 };
            yield return new object[] { (double)10, (double)14, (long)4 };
            yield return new object[] { (double)10, (decimal)14, (long)4 };
            yield return new object[] { (double)10, "14", (long)4 };
            yield return new object[] { (double)10, true, (long)-11 };
            yield return new object[] { (double)10, null, (long)10 };

            // decimal.
            yield return new object[] { (decimal)10, (byte)14, (long)4 };
            yield return new object[] { (decimal)10, (ByteEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (sbyte)14, (long)4 };
            yield return new object[] { (decimal)10, (SByteEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (ushort)14, (long)4 };
            yield return new object[] { (decimal)10, (UShortEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (short)14, (long)4 };
            yield return new object[] { (decimal)10, (ShortEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (uint)14, (long)4 };
            yield return new object[] { (decimal)10, (UIntEnum)14, (long)4 };
            yield return new object[] { (decimal)10, 14, (long)4 };
            yield return new object[] { (decimal)10, (IntEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (ulong)14, (long)4 };
            yield return new object[] { (decimal)10, (ULongEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (long)14, (long)4 };
            yield return new object[] { (decimal)10, (LongEnum)14, (long)4 };
            yield return new object[] { (decimal)10, (float)14, (long)4 };
            yield return new object[] { (decimal)10, (double)14, (long)4 };
            yield return new object[] { (decimal)10, (decimal)14, (long)4 };
            yield return new object[] { (decimal)10, "14", (long)4 };
            yield return new object[] { (decimal)10, true, (long)-11 };
            yield return new object[] { (decimal)10, null, (long)10 };

            // string.
            yield return new object[] { "10", (byte)14, (long)4 };
            yield return new object[] { "10", (ByteEnum)14, (long)4 };
            yield return new object[] { "10", (sbyte)14, (long)4 };
            yield return new object[] { "10", (SByteEnum)14, (long)4 };
            yield return new object[] { "10", (ushort)14, (long)4 };
            yield return new object[] { "10", (UShortEnum)14, (long)4 };
            yield return new object[] { "10", (short)14, (long)4 };
            yield return new object[] { "10", (ShortEnum)14, (long)4 };
            yield return new object[] { "10", (uint)14, (long)4 };
            yield return new object[] { "10", (UIntEnum)14, (long)4 };
            yield return new object[] { "10", 14, (long)4 };
            yield return new object[] { "10", (IntEnum)14, (long)4 };
            yield return new object[] { "10", (ulong)14, (long)4 };
            yield return new object[] { "10", (ULongEnum)14, (long)4 };
            yield return new object[] { "10", (long)14, (long)4 };
            yield return new object[] { "10", (LongEnum)14, (long)4 };
            yield return new object[] { "10", (float)14, (long)4 };
            yield return new object[] { "10", (double)14, (long)4 };
            yield return new object[] { "10", (decimal)14, (long)4 };
            yield return new object[] { "10", "14", (long)4 };
            yield return new object[] { "10", true, false };
            yield return new object[] { "10", null, (long)10 };

            // bool.
            yield return new object[] { true, (byte)14, (short)-15 };
            yield return new object[] { true, (ByteEnum)14, (short)-15 };
            yield return new object[] { true, (sbyte)14, (sbyte)-15 };
            yield return new object[] { true, (SByteEnum)14, (sbyte)-15 };
            yield return new object[] { true, (ushort)14, -15 };
            yield return new object[] { true, (UShortEnum)14, -15 };
            yield return new object[] { true, (short)14, (short)-15 };
            yield return new object[] { true, (ShortEnum)14, (short)-15 };
            yield return new object[] { true, (uint)14, (long)-15 };
            yield return new object[] { true, (UIntEnum)14, (long)-15 };
            yield return new object[] { true, 14, -15 };
            yield return new object[] { true, (IntEnum)14, -15 };
            yield return new object[] { true, (ulong)14, (long)-15 };
            yield return new object[] { true, (ULongEnum)14, (long)-15 };
            yield return new object[] { true, (long)14, (long)-15 };
            yield return new object[] { true, (LongEnum)14, (long)-15 };
            yield return new object[] { true, (float)14, (long)-15 };
            yield return new object[] { true, (double)14, (long)-15 };
            yield return new object[] { true, (decimal)14, (long)-15 };
            yield return new object[] { true, "14", false };
            yield return new object[] { true, true, false };
            yield return new object[] { true, null, true };

            // null.
            yield return new object[] { null, (byte)14, (byte)14 };
            yield return new object[] { null, (ByteEnum)14, (ByteEnum)14 };
            yield return new object[] { null, (sbyte)14, (sbyte)14 };
            yield return new object[] { null, (SByteEnum)14, (SByteEnum)14 };
            yield return new object[] { null, (ushort)14, (ushort)14 };
            yield return new object[] { null, (UShortEnum)14, (UShortEnum)14 };
            yield return new object[] { null, (short)14, (short)14 };
            yield return new object[] { null, (ShortEnum)14, (ShortEnum)14 };
            yield return new object[] { null, (uint)14, (uint)14 };
            yield return new object[] { null, (UIntEnum)14, (UIntEnum)14 };
            yield return new object[] { null, 14, 14 };
            yield return new object[] { null, (IntEnum)14, (IntEnum)14 };
            yield return new object[] { null, (ulong)14, (ulong)14 };
            yield return new object[] { null, (ULongEnum)14, (ULongEnum)14 };
            yield return new object[] { null, (long)14, (long)14 };
            yield return new object[] { null, (LongEnum)14, (LongEnum)14 };
            yield return new object[] { null, (float)14, (long)14 };
            yield return new object[] { null, (double)14, (long)14 };
            yield return new object[] { null, (decimal)14, (long)14 };
            yield return new object[] { null, "14", (long)14 };
            yield return new object[] { null, true, true };
            yield return new object[] { null, null, 0 };

            // object.
            yield return new object[] { new XorObject(), 2, "custom" };
            yield return new object[] { 2, new XorObject(), "motsuc" };
            yield return new object[] { new XorObject(), new OperatorsTests(), "customobject" };
            yield return new object[] { new OperatorsTests(), new XorObject(), "tcejbomotsuc" };
        }

        [Theory]
        [MemberData(nameof(XorObject_TestData))]
        public void XorObject_Invoke_ReturnsExpected(object left, object right, object expected)
        {
            Assert.Equal(expected, Operators.XorObject(left, right));
        }

        public static IEnumerable<object[]> XorObject_InvalidObjects_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(XorObject_InvalidObjects_TestData))]
        public void XorObject_InvalidObjects_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.XorObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.XorObject(right, left));
        }

        public static IEnumerable<object[]> XorObject_MismatchingObjects_TestData()
        {
            yield return new object[] { new XorObject(), new object() };
            yield return new object[] { new object(), new XorObject() };

            yield return new object[] { new XorObject(), new XorObject() };
        }

        [Theory]
        [MemberData(nameof(XorObject_MismatchingObjects_TestData))]
        public void XorObject_MismatchingObjects_ThrowsAmibguousMatchException(object left, object right)
        {
            Assert.Throws<AmbiguousMatchException>(() => Operators.XorObject(left, right));
        }

        public class XorObject
        {
            public static string operator ^(XorObject left, int right) => "custom";
            public static string operator ^(int left, XorObject right) => "motsuc";

            public static string operator ^(XorObject left, OperatorsTests right) => "customobject";
            public static string operator ^(OperatorsTests left, XorObject right) => "tcejbomotsuc";
        }
    
        public enum ByteEnum : byte { Value = 1 }
        public enum ByteEnum2 : byte { Value = 1 }

        public enum SByteEnum : sbyte { Value = 1 }
        public enum SByteEnum2 : sbyte { Value = 1 }

        public enum UShortEnum : ushort { Value = 1 }
        public enum UShortEnum2 : ushort { Value = 1 }

        public enum ShortEnum : short { Value = 1 }
        public enum ShortEnum2 : short { Value = 1 }

        public enum UIntEnum : uint { Value = 1 }
        public enum UIntEnum2 : uint { Value = 1 }

        public enum IntEnum : int { Value = 1 }
        public enum IntEnum2 : int { Value = 1 }

        public enum ULongEnum : ulong { Value = 1 }
        public enum ULongEnum2 : ulong { Value = 1 }

        public enum LongEnum : long { Value = 1 }
        public enum LongEnum2 : long { Value = 1 }
    }
}
