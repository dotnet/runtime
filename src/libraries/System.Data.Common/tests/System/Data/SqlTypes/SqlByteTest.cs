// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) 2002 Ville Palo
// (C) 2003 Martin Willemoes Hansen

// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xunit;
using System.Xml;
using System.Data.SqlTypes;

namespace System.Data.Tests.SqlTypes
{
    public class SqlByteTest
    {
        // Test constructor
        [Fact]
        public void Create()
        {
            byte b = 29;
            SqlByte testByte = new SqlByte(b);
            Assert.Equal((byte)29, testByte.Value);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.Equal((SqlByte)255, SqlByte.MaxValue);
            Assert.Equal((SqlByte)0, SqlByte.MinValue);
            Assert.True(SqlByte.Null.IsNull);
            Assert.Equal((byte)0, SqlByte.Zero.Value);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            SqlByte testByte = new SqlByte(54);
            SqlByte testByte2 = new SqlByte(1);

            Assert.True(SqlByte.Null.IsNull);
            Assert.Equal((byte)54, testByte.Value);
            Assert.Equal((byte)1, testByte2.Value);
        }

        // PUBLIC STATIC METHODS
        [Fact]
        public void AddMethod()
        {
            SqlByte testByte64 = new SqlByte(64);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte164 = new SqlByte(164);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((byte)64, SqlByte.Add(testByte64, testByte0).Value);
            Assert.Equal((byte)228, SqlByte.Add(testByte64, testByte164).Value);
            Assert.Equal((byte)164, SqlByte.Add(testByte0, testByte164).Value);
            Assert.Equal((byte)255, SqlByte.Add(testByte255, testByte0).Value);

            Assert.Throws<OverflowException>(() => SqlByte.Add(testByte255, testByte64));
        }

        [Fact]
        public void BitwiseAndMethod()
        {
            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte1 = new SqlByte(1);
            SqlByte testByte62 = new SqlByte(62);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((byte)0, SqlByte.BitwiseAnd(testByte2, testByte1).Value);
            Assert.Equal((byte)0, SqlByte.BitwiseAnd(testByte1, testByte62).Value);
            Assert.Equal((byte)2, SqlByte.BitwiseAnd(testByte62, testByte2).Value);
            Assert.Equal((byte)1, SqlByte.BitwiseAnd(testByte1, testByte255).Value);
            Assert.Equal((byte)62, SqlByte.BitwiseAnd(testByte62, testByte255).Value);
        }

        [Fact]
        public void BitwiseOrMethod()
        {
            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte1 = new SqlByte(1);
            SqlByte testByte62 = new SqlByte(62);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((byte)3, SqlByte.BitwiseOr(testByte2, testByte1).Value);
            Assert.Equal((byte)63, SqlByte.BitwiseOr(testByte1, testByte62).Value);
            Assert.Equal((byte)62, SqlByte.BitwiseOr(testByte62, testByte2).Value);
            Assert.Equal((byte)255, SqlByte.BitwiseOr(testByte1, testByte255).Value);
            Assert.Equal((byte)255, SqlByte.BitwiseOr(testByte62, testByte255).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlByte testByte13 = new SqlByte(13);
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte10II = new SqlByte(10);

            SqlString testString = new SqlString("This is a test");

            Assert.True(testByte13.CompareTo(testByte10) > 0);
            Assert.True(testByte10.CompareTo(testByte13) < 0);
            Assert.Equal(0, testByte10.CompareTo(testByte10II));

            Assert.Throws<ArgumentException>(() => testByte13.CompareTo(testString));
        }

        [Fact]
        public void DivideMethod()
        {
            SqlByte testByte13 = new SqlByte(13);
            SqlByte testByte0 = new SqlByte(0);

            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte180 = new SqlByte(180);
            SqlByte testByte3 = new SqlByte(3);

            Assert.Equal((byte)6, SqlByte.Divide(testByte13, testByte2).Value);
            Assert.Equal((byte)90, SqlByte.Divide(testByte180, testByte2).Value);
            Assert.Equal((byte)60, SqlByte.Divide(testByte180, testByte3).Value);
            Assert.Equal((byte)0, SqlByte.Divide(testByte13, testByte180).Value);
            Assert.Equal((byte)0, SqlByte.Divide(testByte13, testByte180).Value);

            Assert.Throws<DivideByZeroException>(() => SqlByte.Divide(testByte13, testByte0));
        }

        [Fact]
        public void EqualsMethod()
        {
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte158 = new SqlByte(158);
            SqlByte testByte180 = new SqlByte(180);
            SqlByte testByte180II = new SqlByte(180);

            Assert.False(testByte0.Equals(testByte158));
            Assert.False(testByte158.Equals(testByte180));
            Assert.False(testByte180.Equals(new SqlString("TEST")));
            Assert.True(testByte180.Equals(testByte180II));
        }

        [Fact]
        public void StaticEqualsMethod()
        {
            SqlByte testByte34 = new SqlByte(34);
            SqlByte testByte34II = new SqlByte(34);
            SqlByte testByte15 = new SqlByte(15);

            Assert.True(SqlByte.Equals(testByte34, testByte34II).Value);
            Assert.False(SqlByte.Equals(testByte34, testByte15).Value);
            Assert.False(SqlByte.Equals(testByte15, testByte34II).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlByte testByte15 = new SqlByte(15);
            SqlByte testByte216 = new SqlByte(216);

            Assert.Equal(15, testByte15.GetHashCode());
            Assert.Equal(216, testByte216.GetHashCode());
        }

        [Fact]
        public void GreaterThan()
        {
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte10II = new SqlByte(10);
            SqlByte testByte110 = new SqlByte(110);

            Assert.False(SqlByte.GreaterThan(testByte10, testByte110).Value);
            Assert.True(SqlByte.GreaterThan(testByte110, testByte10).Value);
            Assert.False(SqlByte.GreaterThan(testByte10II, testByte10).Value);
        }

        [Fact]
        public void GreaterThanOrEqual()
        {
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte10II = new SqlByte(10);
            SqlByte testByte110 = new SqlByte(110);

            Assert.False(SqlByte.GreaterThanOrEqual(testByte10, testByte110).Value);

            Assert.True(SqlByte.GreaterThanOrEqual(testByte110, testByte10).Value);

            Assert.True(SqlByte.GreaterThanOrEqual(testByte10II, testByte10).Value);
        }

        [Fact]
        public void LessThan()
        {
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte10II = new SqlByte(10);
            SqlByte testByte110 = new SqlByte(110);

            Assert.True(SqlByte.LessThan(testByte10, testByte110).Value);

            Assert.False(SqlByte.LessThan(testByte110, testByte10).Value);

            Assert.False(SqlByte.LessThan(testByte10II, testByte10).Value);
        }

        [Fact]
        public void LessThanOrEqual()
        {
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte10II = new SqlByte(10);
            SqlByte testByte110 = new SqlByte(110);

            Assert.True(SqlByte.LessThanOrEqual(testByte10, testByte110).Value);

            Assert.False(SqlByte.LessThanOrEqual(testByte110, testByte10).Value);

            Assert.True(SqlByte.LessThanOrEqual(testByte10II, testByte10).Value);

            Assert.True(SqlByte.LessThanOrEqual(testByte10II, SqlByte.Null).IsNull);
        }

        [Fact]
        public void Mod()
        {
            SqlByte testByte132 = new SqlByte(132);
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte200 = new SqlByte(200);

            Assert.Equal((SqlByte)2, SqlByte.Mod(testByte132, testByte10));
            Assert.Equal((SqlByte)10, SqlByte.Mod(testByte10, testByte200));
            Assert.Equal((SqlByte)0, SqlByte.Mod(testByte200, testByte10));
            Assert.Equal((SqlByte)68, SqlByte.Mod(testByte200, testByte132));
        }

        [Fact]
        public void Multiply()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte128 = new SqlByte(128);

            Assert.Equal((byte)24, SqlByte.Multiply(testByte12, testByte2).Value);
            Assert.Equal((byte)24, SqlByte.Multiply(testByte2, testByte12).Value);

            Assert.Throws<OverflowException>(() => SqlByte.Multiply(testByte128, testByte2));
        }

        [Fact]
        public void NotEquals()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte128 = new SqlByte(128);
            SqlByte testByte128II = new SqlByte(128);

            Assert.True(SqlByte.NotEquals(testByte12, testByte128).Value);
            Assert.True(SqlByte.NotEquals(testByte128, testByte12).Value);
            Assert.True(SqlByte.NotEquals(testByte128II, testByte12).Value);
            Assert.False(SqlByte.NotEquals(testByte128II, testByte128).Value);
            Assert.False(SqlByte.NotEquals(testByte128, testByte128II).Value);
        }

        [Fact]
        public void OnesComplement()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)243, SqlByte.OnesComplement(testByte12));
            Assert.Equal((SqlByte)127, SqlByte.OnesComplement(testByte128));
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlByte.Parse(null));

            Assert.Throws<FormatException>(() => SqlByte.Parse("not-a-number"));

            Assert.Throws<OverflowException>(() => SqlByte.Parse(((int)SqlByte.MaxValue + 1).ToString()));

            Assert.Equal((byte)150, SqlByte.Parse("150").Value);
        }

        [Fact]
        public void Subtract()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte128 = new SqlByte(128);
            Assert.Equal((byte)116, SqlByte.Subtract(testByte128, testByte12).Value);

            Assert.Throws<OverflowException>(() => SqlByte.Subtract(testByte12, testByte128));
        }

        [Fact]
        public void ToSqlBoolean()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByteNull = SqlByte.Null;

            Assert.True(testByte12.ToSqlBoolean().Value);
            Assert.False(testByte0.ToSqlBoolean().Value);
            Assert.True(testByteNull.ToSqlBoolean().IsNull);
        }

        [Fact]
        public void ToSqlDecimal()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal(12, testByte12.ToSqlDecimal().Value);
            Assert.Equal(0, testByte0.ToSqlDecimal().Value);
            Assert.Equal(228, testByte228.ToSqlDecimal().Value);
        }

        [Fact]
        public void ToSqlDouble()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal(12, testByte12.ToSqlDouble().Value);
            Assert.Equal(0, testByte0.ToSqlDouble().Value);
            Assert.Equal(228, testByte228.ToSqlDouble().Value);
        }

        [Fact]
        public void ToSqlInt16()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal((short)12, testByte12.ToSqlInt16().Value);
            Assert.Equal((short)0, testByte0.ToSqlInt16().Value);
            Assert.Equal((short)228, testByte228.ToSqlInt16().Value);
        }

        [Fact]
        public void ToSqlInt32()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal(12, testByte12.ToSqlInt32().Value);
            Assert.Equal(0, testByte0.ToSqlInt32().Value);
            Assert.Equal(228, testByte228.ToSqlInt32().Value);
        }

        [Fact]
        public void ToSqlInt64()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal(12, testByte12.ToSqlInt64().Value);
            Assert.Equal(0, testByte0.ToSqlInt64().Value);
            Assert.Equal(228, testByte228.ToSqlInt64().Value);
        }

        [Fact]
        public void ToSqlMoney()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal(12M, testByte12.ToSqlMoney().Value);
            Assert.Equal(0, testByte0.ToSqlMoney().Value);
            Assert.Equal(228M, testByte228.ToSqlMoney().Value);
        }

        [Fact]
        public void ToSqlSingle()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal(12, testByte12.ToSqlSingle().Value);
            Assert.Equal(0, testByte0.ToSqlSingle().Value);
            Assert.Equal(228, testByte228.ToSqlSingle().Value);
        }

        [Fact]
        public void ToSqlString()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal("12", testByte12.ToSqlString().Value);
            Assert.Equal("0", testByte0.ToSqlString().Value);
            Assert.Equal("228", testByte228.ToSqlString().Value);
        }

        [Fact]
        public void ToStringTest()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte0 = new SqlByte(0);
            SqlByte testByte228 = new SqlByte(228);

            Assert.Equal("12", testByte12.ToString());
            Assert.Equal("0", testByte0.ToString());
            Assert.Equal("228", testByte228.ToString());
        }

        [Fact]
        public void TestXor()
        {
            SqlByte testByte14 = new SqlByte(14);
            SqlByte testByte58 = new SqlByte(58);
            SqlByte testByte130 = new SqlByte(130);

            Assert.Equal((byte)52, SqlByte.Xor(testByte14, testByte58).Value);
            Assert.Equal((byte)140, SqlByte.Xor(testByte14, testByte130).Value);
            Assert.Equal((byte)184, SqlByte.Xor(testByte58, testByte130).Value);
        }

        // OPERATORS

        [Fact]
        public void AdditionOperator()
        {
            SqlByte testByte24 = new SqlByte(24);
            SqlByte testByte64 = new SqlByte(64);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)88, testByte24 + testByte64);

            Assert.Throws<OverflowException>(() => testByte64 + testByte255);
        }

        [Fact]
        public void BitwiseAndOperator()
        {
            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte4 = new SqlByte(4);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)0, testByte2 & testByte4);
            Assert.Equal((SqlByte)2, testByte2 & testByte255);
        }

        [Fact]
        public void BitwiseOrOperator()
        {
            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte4 = new SqlByte(4);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)6, testByte2 | testByte4);
            Assert.Equal((SqlByte)255, testByte2 | testByte255);
        }

        [Fact]
        public void DivisionOperator()
        {
            SqlByte testByte2 = new SqlByte(2);
            SqlByte testByte4 = new SqlByte(4);
            SqlByte testByte255 = new SqlByte(255);
            SqlByte testByte0 = new SqlByte(0);

            Assert.Equal((SqlByte)2, testByte4 / testByte2);
            Assert.Equal((SqlByte)127, testByte255 / testByte2);

            Assert.Throws<DivideByZeroException>(() => testByte255 / testByte0);
        }

        [Fact]
        public void EqualityOperator()
        {
            SqlByte testByte15 = new SqlByte(15);
            SqlByte testByte15II = new SqlByte(15);
            SqlByte testByte255 = new SqlByte(255);

            Assert.True((testByte15 == testByte15II).Value);
            Assert.False((testByte15 == testByte255).Value);
            Assert.False((testByte15 != testByte15II).Value);
            Assert.True((testByte15 != testByte255).Value);
        }

        [Fact]
        public void ExclusiveOrOperator()
        {
            SqlByte testByte15 = new SqlByte(15);
            SqlByte testByte10 = new SqlByte(10);
            SqlByte testByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)5, (testByte15 ^ testByte10));
            Assert.Equal((SqlByte)240, (testByte15 ^ testByte255));
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlByte testByte165 = new SqlByte(165);
            SqlByte testByte100 = new SqlByte(100);
            SqlByte testByte100II = new SqlByte(100);
            SqlByte testByte255 = new SqlByte(255);

            Assert.True((testByte165 > testByte100).Value);
            Assert.False((testByte165 > testByte255).Value);
            Assert.False((testByte100 > testByte100II).Value);
            Assert.False((testByte165 >= testByte255).Value);
            Assert.True((testByte255 >= testByte165).Value);
            Assert.True((testByte100 >= testByte100II).Value);

            Assert.False((testByte165 < testByte100).Value);
            Assert.True((testByte165 < testByte255).Value);
            Assert.False((testByte100 < testByte100II).Value);
            Assert.True((testByte165 <= testByte255).Value);
            Assert.False((testByte255 <= testByte165).Value);
            Assert.True((testByte100 <= testByte100II).Value);
        }

        [Fact]
        public void MultiplicationOperator()
        {
            SqlByte testByte4 = new SqlByte(4);
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)48, testByte4 * testByte12);
            Assert.Throws<OverflowException>(() => testByte128 * testByte4);
        }

        [Fact]
        public void OnesComplementOperator()
        {
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)243, ~testByte12);
            Assert.Equal((SqlByte)127, ~testByte128);
        }

        [Fact]
        public void SubtractionOperator()
        {
            SqlByte testByte4 = new SqlByte(4);
            SqlByte testByte12 = new SqlByte(12);
            SqlByte testByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)8, testByte12 - testByte4);
            Assert.Throws<OverflowException>(() => testByte4 - testByte128);
        }

        [Fact]
        public void SqlBooleanToSqlByte()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlByte testByte;

            testByte = (SqlByte)testBoolean;

            Assert.Equal((byte)1, testByte.Value);
        }

        [Fact]
        public void SqlByteToByte()
        {
            SqlByte testByte = new SqlByte(12);
            byte test = (byte)testByte;
            Assert.Equal((byte)12, test);
        }

        [Fact]
        public void SqlDecimalToSqlByte()
        {
            SqlDecimal testDecimal64 = new SqlDecimal(64);
            SqlDecimal testDecimal900 = new SqlDecimal(900);

            Assert.Equal((byte)64, ((SqlByte)testDecimal64).Value);
            Assert.Throws<OverflowException>(() => (SqlByte)testDecimal900);
        }

        [Fact]
        public void SqlDoubleToSqlByte()
        {
            SqlDouble testDouble64 = new SqlDouble(64);
            SqlDouble testDouble900 = new SqlDouble(900);

            Assert.Equal((byte)64, ((SqlByte)testDouble64).Value);
            Assert.Throws<OverflowException>(() => (SqlByte)testDouble900);
        }

        [Fact]
        public void SqlInt16ToSqlByte()
        {
            SqlInt16 testInt1664 = new SqlInt16(64);
            SqlInt16 testInt16900 = new SqlInt16(900);

            Assert.Equal((byte)64, ((SqlByte)testInt1664).Value);

            Assert.Throws<OverflowException>(() => (SqlByte)testInt16900);
        }

        [Fact]
        public void SqlInt32ToSqlByte()
        {
            SqlInt32 testInt3264 = new SqlInt32(64);
            SqlInt32 testInt32900 = new SqlInt32(900);

            Assert.Equal((byte)64, ((SqlByte)testInt3264).Value);
            Assert.Throws<OverflowException>(() => (SqlByte)testInt32900);
        }

        [Fact]
        public void SqlInt64ToSqlByte()
        {
            SqlInt64 testInt6464 = new SqlInt64(64);
            SqlInt64 testInt64900 = new SqlInt64(900);

            Assert.Equal((byte)64, ((SqlByte)testInt6464).Value);
            Assert.Throws<OverflowException>(() => (SqlByte)testInt64900);
        }

        [Fact]
        public void SqlMoneyToSqlByte()
        {
            SqlMoney testMoney64 = new SqlMoney(64);
            SqlMoney testMoney900 = new SqlMoney(900);

            Assert.Equal((byte)64, ((SqlByte)testMoney64).Value);
            Assert.Throws<OverflowException>(() => (SqlByte)testMoney900);
        }

        [Fact]
        public void SqlSingleToSqlByte()
        {
            SqlSingle testSingle64 = new SqlSingle(64);
            SqlSingle testSingle900 = new SqlSingle(900);

            Assert.Equal((byte)64, ((SqlByte)testSingle64).Value);
            Assert.Throws<OverflowException>(() => (SqlByte)testSingle900);
        }

        [Fact]
        public void SqlStringToSqlByte()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("100");
            SqlString testString1000 = new SqlString("1000");

            Assert.Equal((byte)100, ((SqlByte)testString100).Value);

            Assert.Throws<OverflowException>(() => (SqlByte)testString1000);

            Assert.Throws<FormatException>(() => (SqlByte)testString);
        }

        [Fact]
        public void ByteToSqlByte()
        {
            byte testByte = 14;
            Assert.Equal((byte)14, ((SqlByte)testByte).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlByte.GetXsdType(null);
            Assert.Equal("unsignedByte", qualifiedName.Name);
        }
    }
}
