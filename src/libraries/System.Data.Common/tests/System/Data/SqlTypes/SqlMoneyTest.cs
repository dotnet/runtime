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
    public class SqlMoneyTest
    {
        private SqlMoney _test1;
        private SqlMoney _test2;
        private SqlMoney _test3;
        private SqlMoney _test4;

        public SqlMoneyTest()
        {
            _test1 = new SqlMoney(6464.6464d);
            _test2 = new SqlMoney(90000m);
            _test3 = new SqlMoney(90000m);
            _test4 = new SqlMoney(-45000m);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            Assert.Throws<OverflowException>(() => new SqlMoney(1000000000000000m));

            SqlMoney CreationTest = new SqlMoney((decimal)913.3);
            Assert.Equal(913.3m, CreationTest.Value);

            Assert.Throws<OverflowException>(() => new SqlMoney(1e200));

            SqlMoney CreationTest2 = new SqlMoney(913.3);
            Assert.Equal(913.3m, CreationTest2.Value);

            SqlMoney CreationTest3 = new SqlMoney(913);
            Assert.Equal(913m, CreationTest3.Value);

            SqlMoney CreationTest4 = new SqlMoney((long)913.3);
            Assert.Equal(913m, CreationTest4.Value);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            // FIXME: There is an error in msdn docs, it says thath MaxValue
            // is 922,337,203,685,475.5807 when the actual value is
            //    922,337,203,685,477.5807
            Assert.Equal(922337203685477.5807m, SqlMoney.MaxValue.Value);
            Assert.Equal(-922337203685477.5808m, SqlMoney.MinValue.Value);
            Assert.True(SqlMoney.Null.IsNull);
            Assert.Equal(0m, SqlMoney.Zero.Value);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            Assert.Equal(90000m, _test2.Value);
            Assert.Equal(-45000m, _test4.Value);
            Assert.True(SqlMoney.Null.IsNull);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlMoney testMoney2 = new SqlMoney(2);

            // Add
            Assert.Equal(96464.6464m, SqlMoney.Add(_test1, _test2));
            Assert.Equal(180000m, SqlMoney.Add(_test2, _test2));
            Assert.Equal(45000m, SqlMoney.Add(_test2, _test4));

            Assert.Throws<OverflowException>(() => SqlMoney.Add(SqlMoney.MaxValue, _test2));

            Assert.Throws<DivideByZeroException>(() => SqlMoney.Divide(_test2, SqlMoney.Zero));

            // Multiply
            Assert.Equal(581818176m, SqlMoney.Multiply(_test1, _test2));
            Assert.Equal(-4050000000m, SqlMoney.Multiply(_test3, _test4));

            Assert.Throws<OverflowException>(() => SqlMoney.Multiply(SqlMoney.MaxValue, _test2));

            // Subtract
            Assert.Equal(0m, SqlMoney.Subtract(_test2, _test3));
            Assert.Equal(83535.3536m, SqlMoney.Subtract(_test2, _test1));

            Assert.Throws<OverflowException>(() => SqlMoney.Subtract(SqlMoney.MinValue, _test2));
        }

        [Fact]
        public void CompareTo()
        {
            Assert.True(_test1.CompareTo(_test2) < 0);
            Assert.True(_test3.CompareTo(_test1) > 0);
            Assert.Equal(0, _test3.CompareTo(_test2));
            Assert.True(_test3.CompareTo(SqlMoney.Null) > 0);
        }

        [Fact]
        public void EqualsMethods()
        {
            Assert.False(_test1.Equals(_test2));
            Assert.False(_test1.Equals((object)_test2));
            Assert.True(_test2.Equals(_test3));
            Assert.True(_test2.Equals((object)_test3));
            Assert.False(SqlMoney.Equals(_test1, _test2).Value);
            Assert.True(SqlMoney.Equals(_test3, _test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            // FIXME: Better way to test HashCode
            Assert.Equal(_test3.GetHashCode(), _test2.GetHashCode());
        }

        [Fact]
        public void Greaters()
        {
            // GreateThan ()
            Assert.False(SqlMoney.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlMoney.GreaterThan(_test2, _test1).Value);
            Assert.False(SqlMoney.GreaterThan(_test2, _test3).Value);
            Assert.True(SqlMoney.GreaterThan(_test2, SqlMoney.Null).IsNull);

            // GreaterTharOrEqual ()
            Assert.False(SqlMoney.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlMoney.GreaterThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlMoney.GreaterThanOrEqual(_test3, _test2).Value);
            Assert.True(SqlMoney.GreaterThanOrEqual(_test3, SqlMoney.Null).IsNull);
        }

        [Fact]
        public void Lessers()
        {
            // LessThan()
            Assert.False(SqlMoney.LessThan(_test2, _test3).Value);
            Assert.False(SqlMoney.LessThan(_test2, _test1).Value);
            Assert.True(SqlMoney.LessThan(_test1, _test2).Value);
            Assert.True(SqlMoney.LessThan(SqlMoney.Null, _test2).IsNull);

            // LessThanOrEqual ()
            Assert.True(SqlMoney.LessThanOrEqual(_test1, _test2).Value);
            Assert.False(SqlMoney.LessThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlMoney.LessThanOrEqual(_test2, _test2).Value);
            Assert.True(SqlMoney.LessThanOrEqual(_test2, SqlMoney.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            Assert.True(SqlMoney.NotEquals(_test1, _test2).Value);
            Assert.True(SqlMoney.NotEquals(_test2, _test1).Value);
            Assert.False(SqlMoney.NotEquals(_test2, _test3).Value);
            Assert.False(SqlMoney.NotEquals(_test3, _test2).Value);
            Assert.True(SqlMoney.NotEquals(SqlMoney.Null, _test2).IsNull);
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlMoney.Parse(null));

            Assert.Throws<FormatException>(() => SqlMoney.Parse("not-a-number"));

            Assert.Throws<OverflowException>(() => SqlMoney.Parse("1000000000000000"));

            Assert.Equal(150M, SqlMoney.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlMoney testMoney100 = new SqlMoney(100);

            // ToDecimal
            Assert.Equal((decimal)6464.6464, _test1.ToDecimal());

            // ToDouble
            Assert.Equal(6464.6464, _test1.ToDouble());

            // ToInt32
            Assert.Equal(90000, _test2.ToInt32());
            Assert.Equal(6465, _test1.ToInt32());

            // ToInt64
            Assert.Equal(90000, _test2.ToInt64());
            Assert.Equal(6465, _test1.ToInt64());

            // ToSqlBoolean ()
            Assert.True(_test1.ToSqlBoolean().Value);
            Assert.False(SqlMoney.Zero.ToSqlBoolean().Value);
            Assert.True(SqlMoney.Null.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)100, testMoney100.ToSqlByte().Value);

            Assert.Throws<OverflowException>(() => _test2.ToSqlByte());

            // ToSqlDecimal ()
            Assert.Equal((decimal)6464.6464, _test1.ToSqlDecimal().Value);
            Assert.Equal(-45000m, _test4.ToSqlDecimal().Value);

            // ToSqlInt16 ()
            Assert.Equal((short)6465, _test1.ToSqlInt16().Value);

            Assert.Throws<OverflowException>(() => SqlMoney.MaxValue.ToSqlInt16());

            // ToSqlInt32 ()
            Assert.Equal(6465, _test1.ToSqlInt32().Value);
            Assert.Equal(-45000, _test4.ToSqlInt32().Value);

            Assert.Throws<OverflowException>(() => SqlMoney.MaxValue.ToSqlInt32());

            // ToSqlInt64 ()
            Assert.Equal(6465, _test1.ToSqlInt64().Value);
            Assert.Equal(-45000, _test4.ToSqlInt64().Value);

            // ToSqlSingle ()
            Assert.Equal((float)6464.6464, _test1.ToSqlSingle().Value);

            // ToSqlString ()
            Assert.Equal(6464.6464.ToString(), _test1.ToSqlString().Value);
            Assert.Equal(90000.00m.ToString(), _test2.ToSqlString().Value);

            // ToString ()
            Assert.Equal(6464.6464.ToString(), _test1.ToString());
            Assert.Equal(90000.00m.ToString(), _test2.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            // "+"-operator
            Assert.Equal(96464.6464m, _test1 + _test2);

            Assert.Throws<OverflowException>(() => SqlMoney.MaxValue + SqlMoney.MaxValue);

            // "/"-operator
            Assert.Equal(13.9219m, _test2 / _test1);

            Assert.Throws<DivideByZeroException>(() => _test3 / SqlMoney.Zero);

            // "*"-operator
            Assert.Equal(581818176m, _test1 * _test2);

            Assert.Throws<OverflowException>(() => SqlMoney.MaxValue * _test1);

            // "-"-operator
            Assert.Equal(83535.3536m, _test2 - _test1);

            Assert.Throws<OverflowException>(() => SqlMoney.MinValue - SqlMoney.MaxValue);
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            // == -operator
            Assert.False((_test1 == _test2).Value);
            Assert.True((_test1 == SqlMoney.Null).IsNull);

            // != -operator
            Assert.False((_test2 != _test3).Value);
            Assert.True((_test1 != _test3).Value);
            Assert.True((_test1 != _test4).Value);
            Assert.True((_test1 != SqlMoney.Null).IsNull);

            // > -operator
            Assert.True((_test1 > _test4).Value);
            Assert.True((_test2 > _test1).Value);
            Assert.False((_test2 > _test3).Value);
            Assert.True((_test1 > SqlMoney.Null).IsNull);

            // >=  -operator
            Assert.False((_test1 >= _test3).Value);
            Assert.True((_test3 >= _test1).Value);
            Assert.True((_test2 >= _test3).Value);
            Assert.True((_test1 >= SqlMoney.Null).IsNull);

            // < -operator
            Assert.False((_test2 < _test1).Value);
            Assert.True((_test1 < _test3).Value);
            Assert.False((_test2 < _test3).Value);
            Assert.True((_test1 < SqlMoney.Null).IsNull);

            // <= -operator
            Assert.True((_test1 <= _test3).Value);
            Assert.False((_test3 <= _test1).Value);
            Assert.True((_test2 <= _test3).Value);
            Assert.True((_test1 <= SqlMoney.Null).IsNull);
        }

        [Fact]
        public void UnaryNegation()
        {
            Assert.Equal((decimal)(-6464.6464), -(_test1).Value);
            Assert.Equal(45000M, -(_test4).Value);
        }

        [Fact]
        public void SqlBooleanToSqlMoney()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);

            Assert.Equal(1M, ((SqlMoney)TestBoolean).Value);
            Assert.True(((SqlDecimal)SqlBoolean.Null).IsNull);
        }

        [Fact]
        public void SqlDecimalToSqlMoney()
        {
            SqlDecimal testDecimal = new SqlDecimal(4000);
            SqlDecimal testDecimal2 = new SqlDecimal(1E+20);

            SqlMoney testMoney = (SqlMoney)testDecimal;
            Assert.Equal(4000M, testMoney.Value);

            Assert.Throws<OverflowException>(() => (SqlMoney)testDecimal2);
        }

        [Fact]
        public void SqlDoubleToSqlMoney()
        {
            SqlDouble testDouble = new SqlDouble(1E+9);
            SqlDouble testDouble2 = new SqlDouble(1E+20);

            SqlMoney testMoney = (SqlMoney)testDouble;
            Assert.Equal(1000000000m, testMoney.Value);

            Assert.Throws<OverflowException>(() => (SqlMoney)testDouble2);
        }

        [Fact]
        public void SqlMoneyToDecimal()
        {
            Assert.Equal((decimal)6464.6464, (decimal)_test1);
            Assert.Equal(-45000M, (decimal)_test4);
        }

        [Fact]
        public void SqlSingleToSqlMoney()
        {
            SqlSingle testSingle = new SqlSingle(1e10);
            SqlSingle testSingle2 = new SqlSingle(1e20);

            Assert.Equal(10000000000m, ((SqlMoney)testSingle).Value);

            Assert.Throws<OverflowException>(() => (SqlMoney)testSingle2);
        }

        [Fact]
        public void SqlStringToSqlMoney()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("100");

            Assert.Equal(100M, ((SqlMoney)testString100).Value);

            Assert.Throws<FormatException>(() => (SqlMoney)testString);
        }

        [Fact]
        public void DecimalToSqlMoney()
        {
            decimal testDecimal = 1e10m;
            decimal testDecimal2 = 1e20m;
            Assert.Equal(10000000000M, ((SqlMoney)testDecimal).Value);

            Assert.Throws<OverflowException>(() => (SqlMoney)testDecimal2);
        }

        [Fact]
        public void SqlByteToSqlMoney()
        {
            SqlByte TestByte = new SqlByte(200);
            Assert.Equal(200m, ((SqlMoney)TestByte).Value);
        }

        [Fact]
        public void IntsToSqlMoney()
        {
            SqlInt16 testInt16 = new SqlInt16(5000);
            SqlInt32 testInt32 = new SqlInt32(5000);
            SqlInt64 testInt64 = new SqlInt64(5000);

            Assert.Equal(5000m, ((SqlMoney)testInt16).Value);
            Assert.Equal(5000m, ((SqlMoney)testInt32).Value);
            Assert.Equal(5000m, ((SqlMoney)testInt64).Value);

            Assert.Throws<OverflowException>(() => (SqlMoney)SqlInt64.MaxValue);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlMoney.GetXsdType(null);
            Assert.Equal("decimal", qualifiedName.Name);
        }

        [Fact]
        public void GetTdsValue()
        {
            Assert.Equal(long.MaxValue,SqlMoney.MaxValue.GetTdsValue());
            Assert.Equal(long.MinValue, SqlMoney.MinValue.GetTdsValue());
            Assert.Equal((long)0, new SqlMoney(0).GetTdsValue());
            Assert.Throws<SqlNullValueException>(() => SqlMoney.Null.GetTdsValue());
        }

        [Fact]
        public void FromTdsValue()
        {
            Assert.Equal(SqlMoney.FromTdsValue(long.MaxValue), SqlMoney.MaxValue);
            Assert.Equal(SqlMoney.FromTdsValue(long.MinValue), SqlMoney.MinValue);
            Assert.Equal(SqlMoney.FromTdsValue(0), new SqlMoney(0));
        }
    }
}
