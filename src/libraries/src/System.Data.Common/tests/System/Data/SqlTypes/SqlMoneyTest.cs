// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
using System.Globalization;

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
            _test2 = new SqlMoney(90000.0m);
            _test3 = new SqlMoney(90000.0m);
            _test4 = new SqlMoney(-45000.0m);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            try
            {
                SqlMoney Test = new SqlMoney(1000000000000000m);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            SqlMoney CreationTest = new SqlMoney((decimal)913.3);
            Assert.Equal(913.3000m, CreationTest.Value);

            try
            {
                SqlMoney Test = new SqlMoney(1e200);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            SqlMoney CreationTest2 = new SqlMoney(913.3);
            Assert.Equal(913.3000m, CreationTest2.Value);

            SqlMoney CreationTest3 = new SqlMoney(913);
            Assert.Equal(913.0000m, CreationTest3.Value);

            SqlMoney CreationTest4 = new SqlMoney((long)913.3);
            Assert.Equal(913.0000m, CreationTest4.Value);
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
            Assert.Equal(90000.0000m, _test2.Value);
            Assert.Equal(-45000.0000m, _test4.Value);
            Assert.True(SqlMoney.Null.IsNull);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlMoney TestMoney2 = new SqlMoney(2);

            // Add
            Assert.Equal(96464.6464m, SqlMoney.Add(_test1, _test2));
            Assert.Equal(180000m, SqlMoney.Add(_test2, _test2));
            Assert.Equal(45000m, SqlMoney.Add(_test2, _test4));

            try
            {
                SqlMoney test = SqlMoney.Add(SqlMoney.MaxValue, _test2);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Divide
            Assert.Equal(45000m, SqlMoney.Divide(_test2, TestMoney2));
            try
            {
                SqlMoney test = SqlMoney.Divide(_test2, SqlMoney.Zero);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // Multiply
            Assert.Equal(581818176m, SqlMoney.Multiply(_test1, _test2));
            Assert.Equal(-4050000000m, SqlMoney.Multiply(_test3, _test4));

            try
            {
                SqlMoney test = SqlMoney.Multiply(SqlMoney.MaxValue, _test2);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Subtract
            Assert.Equal(0m, SqlMoney.Subtract(_test2, _test3));
            Assert.Equal(83535.3536m, SqlMoney.Subtract(_test2, _test1));

            try
            {
                SqlMoney test = SqlMoney.Subtract(SqlMoney.MinValue, _test2);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void CompareTo()
        {
            Assert.True(_test1.CompareTo(_test2) < 0);
            Assert.True(_test3.CompareTo(_test1) > 0);
            Assert.True(_test3.CompareTo(_test2) == 0);
            Assert.True(_test3.CompareTo(SqlMoney.Null) > 0);
        }

        [Fact]
        public void EqualsMethods()
        {
            Assert.True(!_test1.Equals(_test2));
            Assert.True(_test2.Equals(_test3));
            Assert.True(!SqlMoney.Equals(_test1, _test2).Value);
            Assert.True(SqlMoney.Equals(_test3, _test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            // FIXME: Better way to test HashCode
            Assert.Equal(_test3.GetHashCode(), _test2.GetHashCode());
            Assert.True(_test2.GetHashCode() != _test1.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            Assert.Equal("System.Data.SqlTypes.SqlMoney", _test1.GetType().ToString());
        }

        [Fact]
        public void Greaters()
        {
            // GreateThan ()
            Assert.True(!SqlMoney.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlMoney.GreaterThan(_test2, _test1).Value);
            Assert.True(!SqlMoney.GreaterThan(_test2, _test3).Value);
            Assert.True(SqlMoney.GreaterThan(_test2, SqlMoney.Null).IsNull);

            // GreaterTharOrEqual ()
            Assert.True(!SqlMoney.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlMoney.GreaterThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlMoney.GreaterThanOrEqual(_test3, _test2).Value);
            Assert.True(SqlMoney.GreaterThanOrEqual(_test3, SqlMoney.Null).IsNull);
        }

        [Fact]
        public void Lessers()
        {
            // LessThan()
            Assert.True(!SqlMoney.LessThan(_test2, _test3).Value);
            Assert.True(!SqlMoney.LessThan(_test2, _test1).Value);
            Assert.True(SqlMoney.LessThan(_test1, _test2).Value);
            Assert.True(SqlMoney.LessThan(SqlMoney.Null, _test2).IsNull);

            // LessThanOrEqual ()
            Assert.True(SqlMoney.LessThanOrEqual(_test1, _test2).Value);
            Assert.True(!SqlMoney.LessThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlMoney.LessThanOrEqual(_test2, _test2).Value);
            Assert.True(SqlMoney.LessThanOrEqual(_test2, SqlMoney.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            Assert.True(SqlMoney.NotEquals(_test1, _test2).Value);
            Assert.True(SqlMoney.NotEquals(_test2, _test1).Value);
            Assert.True(!SqlMoney.NotEquals(_test2, _test3).Value);
            Assert.True(!SqlMoney.NotEquals(_test3, _test2).Value);
            Assert.True(SqlMoney.NotEquals(SqlMoney.Null, _test2).IsNull);
        }

        [Fact]
        public void Parse()
        {
            try
            {
                SqlMoney.Parse(null);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            try
            {
                SqlMoney.Parse("not-a-number");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }

            try
            {
                SqlMoney.Parse("1000000000000000");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            Assert.Equal(150.0000M, SqlMoney.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlMoney TestMoney100 = new SqlMoney(100);

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
            Assert.True(!SqlMoney.Zero.ToSqlBoolean().Value);
            Assert.True(SqlMoney.Null.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)100, TestMoney100.ToSqlByte().Value);

            try
            {
                SqlByte b = (byte)_test2.ToSqlByte();
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlDecimal ()
            Assert.Equal((decimal)6464.6464, _test1.ToSqlDecimal().Value);
            Assert.Equal(-45000.0000m, _test4.ToSqlDecimal().Value);

            // ToSqlInt16 ()
            Assert.Equal((short)6465, _test1.ToSqlInt16().Value);

            try
            {
                SqlInt16 test = SqlMoney.MaxValue.ToSqlInt16().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlInt32 ()
            Assert.Equal(6465, _test1.ToSqlInt32().Value);
            Assert.Equal(-45000, _test4.ToSqlInt32().Value);

            try
            {
                SqlInt32 test = SqlMoney.MaxValue.ToSqlInt32().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlInt64 ()
            Assert.Equal(6465, _test1.ToSqlInt64().Value);
            Assert.Equal(-45000, _test4.ToSqlInt64().Value);

            // ToSqlSingle ()
            Assert.Equal((float)6464.6464, _test1.ToSqlSingle().Value);

            // ToSqlString ()
            Assert.Equal("6464.6464", _test1.ToSqlString().Value);
            Assert.Equal("90000.00", _test2.ToSqlString().Value);

            // ToString ()
            Assert.Equal("6464.6464", _test1.ToString());
            Assert.Equal("90000.00", _test2.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            // "+"-operator
            Assert.Equal(96464.6464m, _test1 + _test2);

            try
            {
                SqlMoney test = SqlMoney.MaxValue + SqlMoney.MaxValue;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "/"-operator
            Assert.Equal(13.9219m, _test2 / _test1);

            try
            {
                SqlMoney test = _test3 / SqlMoney.Zero;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // "*"-operator
            Assert.Equal(581818176m, _test1 * _test2);

            try
            {
                SqlMoney test = SqlMoney.MaxValue * _test1;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "-"-operator
            Assert.Equal(83535.3536m, _test2 - _test1);

            try
            {
                SqlMoney test = SqlMoney.MinValue - SqlMoney.MaxValue;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            // == -operator
            Assert.True(!(_test1 == _test2).Value);
            Assert.True((_test1 == SqlMoney.Null).IsNull);

            // != -operator
            Assert.True(!(_test2 != _test3).Value);
            Assert.True((_test1 != _test3).Value);
            Assert.True((_test1 != _test4).Value);
            Assert.True((_test1 != SqlMoney.Null).IsNull);

            // > -operator
            Assert.True((_test1 > _test4).Value);
            Assert.True((_test2 > _test1).Value);
            Assert.True(!(_test2 > _test3).Value);
            Assert.True((_test1 > SqlMoney.Null).IsNull);

            // >=  -operator
            Assert.True(!(_test1 >= _test3).Value);
            Assert.True((_test3 >= _test1).Value);
            Assert.True((_test2 >= _test3).Value);
            Assert.True((_test1 >= SqlMoney.Null).IsNull);

            // < -operator
            Assert.True(!(_test2 < _test1).Value);
            Assert.True((_test1 < _test3).Value);
            Assert.True(!(_test2 < _test3).Value);
            Assert.True((_test1 < SqlMoney.Null).IsNull);

            // <= -operator
            Assert.True((_test1 <= _test3).Value);
            Assert.True(!(_test3 <= _test1).Value);
            Assert.True((_test2 <= _test3).Value);
            Assert.True((_test1 <= SqlMoney.Null).IsNull);
        }

        [Fact]
        public void UnaryNegation()
        {
            Assert.Equal((decimal)(-6464.6464), -(_test1).Value);
            Assert.Equal(45000.0000M, -(_test4).Value);
        }

        [Fact]
        public void SqlBooleanToSqlMoney()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);

            Assert.Equal(1.0000M, ((SqlMoney)TestBoolean).Value);
            Assert.True(((SqlDecimal)SqlBoolean.Null).IsNull);
        }

        [Fact]
        public void SqlDecimalToSqlMoney()
        {
            SqlDecimal TestDecimal = new SqlDecimal(4000);
            SqlDecimal TestDecimal2 = new SqlDecimal(1E+20);

            SqlMoney TestMoney = (SqlMoney)TestDecimal;
            Assert.Equal(4000.0000M, TestMoney.Value);

            try
            {
                SqlMoney test = (SqlMoney)TestDecimal2;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlDoubleToSqlMoney()
        {
            SqlDouble TestDouble = new SqlDouble(1E+9);
            SqlDouble TestDouble2 = new SqlDouble(1E+20);

            SqlMoney TestMoney = (SqlMoney)TestDouble;
            Assert.Equal(1000000000.0000m, TestMoney.Value);

            try
            {
                SqlMoney test = (SqlMoney)TestDouble2;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlMoneyToDecimal()
        {
            Assert.Equal((decimal)6464.6464, (decimal)_test1);
            Assert.Equal(-45000.0000M, (decimal)_test4);
        }

        [Fact]
        public void SqlSingleToSqlMoney()
        {
            SqlSingle TestSingle = new SqlSingle(1e10);
            SqlSingle TestSingle2 = new SqlSingle(1e20);

            Assert.Equal(10000000000.0000m, ((SqlMoney)TestSingle).Value);

            try
            {
                SqlMoney test = (SqlMoney)TestSingle2;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlStringToSqlMoney()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");

            Assert.Equal(100.0000M, ((SqlMoney)TestString100).Value);

            try
            {
                SqlMoney test = (SqlMoney)TestString;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }
        }

        [Fact]
        public void DecimalToSqlMoney()
        {
            decimal TestDecimal = 1e10m;
            decimal TestDecimal2 = 1e20m;
            Assert.Equal(10000000000.0000M, ((SqlMoney)TestDecimal).Value);

            try
            {
                SqlMoney test = TestDecimal2;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlByteToSqlMoney()
        {
            SqlByte TestByte = new SqlByte(200);
            Assert.Equal(200.0000m, ((SqlMoney)TestByte).Value);
        }

        [Fact]
        public void IntsToSqlMoney()
        {
            SqlInt16 TestInt16 = new SqlInt16(5000);
            SqlInt32 TestInt32 = new SqlInt32(5000);
            SqlInt64 TestInt64 = new SqlInt64(5000);

            Assert.Equal(5000.0000m, ((SqlMoney)TestInt16).Value);
            Assert.Equal(5000.0000m, ((SqlMoney)TestInt32).Value);
            Assert.Equal(5000.0000m, ((SqlMoney)TestInt64).Value);

            try
            {
                SqlMoney test = SqlInt64.MaxValue;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlMoney.GetXsdType(null);
            Assert.Equal("decimal", qualifiedName.Name);
        }
    }
}

