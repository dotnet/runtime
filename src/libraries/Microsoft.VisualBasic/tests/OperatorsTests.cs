// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.CompilerServices.Tests
{
    public class OperatorsTests
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
            yield return new object[] { (ulong)15, ulong.MaxValue, decimal.Parse("18446744073709551630") };

            // long + primitives.
            yield return new object[] { (long)8, (long)2, (long)10 };
            yield return new object[] { (long)9, (float)2, (float)11 };
            yield return new object[] { (long)10, (double)2, (double)12 };
            yield return new object[] { (long)11, (decimal)2, (decimal)13 };
            yield return new object[] { (long)12, "2", (double)14 };
            yield return new object[] { (long)13, true, (long)12 };
            yield return new object[] { (long)14, null, (long)14 };
            yield return new object[] { (long)15, long.MaxValue, decimal.Parse("9223372036854775822") };

            // float + primitives
            yield return new object[] { (float)9, (float)2, (float)11 };
            yield return new object[] { (float)10, (double)2, (double)12 };
            yield return new object[] { (float)11, (decimal)2, (float)13 };
            yield return new object[] { (float)12, "2", (double)14 };
            yield return new object[] { (float)13, true, (float)12 };
            yield return new object[] { (float)14, null, (float)14 };
            yield return new object[] { (float)15, float.PositiveInfinity, float.PositiveInfinity };

            // double + primitives
            yield return new object[] { (double)10, (double)2, (double)12 };
            yield return new object[] { (double)11, (decimal)2, (double)13 };
            yield return new object[] { (double)12, "2", (double)14 };
            yield return new object[] { (double)13, true, (double)12 };
            yield return new object[] { (double)14, null, (double)14 };
            yield return new object[] { (double)15, double.PositiveInfinity, double.PositiveInfinity };

            // decimal + primitives
            yield return new object[] { (decimal)11, (decimal)2, (decimal)13 };
            yield return new object[] { (decimal)12, "2", (double)14 };
            yield return new object[] { (decimal)13, true, (decimal)12 };
            yield return new object[] { (decimal)14, null, (decimal)14 };

            // string + primitives
            yield return new object[] { "1", "2", "12" };
            yield return new object[] { "2", '2', "22" };
            yield return new object[] { "3", true, (double)2 };
            yield return new object[] { "5", null, "5" };

            // bool + primitives
            yield return new object[] { true, "2", (double)1 };
            yield return new object[] { true, true, (short)-2 };
            yield return new object[] { true, false, (short)-1 };
            yield return new object[] { true, null, (short)-1 };

            // char + primitives
            yield return new object[] { 'a', null, "a\0" };

            // null + null
            yield return new object[] { null, null, 0 };
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

        public static IEnumerable<object[]> IncompatibleAddObject_TestData()
        {
            yield return new object[] { 1, '2' };
            yield return new object[] { 2, DBNull.Value };
            yield return new object[] { '3', new object() };
        }

        [Theory]
        [MemberData(nameof(IncompatibleAddObject_TestData))]
        public void AddObject_Incompatible_ThrowsInvalidCastException(object left, object right)
        {
            Assert.Throws<InvalidCastException>(() => Operators.AddObject(left, right));
            Assert.Throws<InvalidCastException>(() => Operators.AddObject(right, left));
        }
    }
}
