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
    public class SqlSingleTest
    {
        // Test constructor
        [Fact]
        public void Create()
        {
            SqlSingle test = new SqlSingle((float)34.87);
            SqlSingle test2 = 45.2f;

            Assert.Equal(34.87f, test.Value);
            Assert.Equal(45.2f, test2.Value);

            test = new SqlSingle(-9000.6543);
            Assert.Equal(-9000.6543f, test.Value);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.Equal(3.40282346638528859E+38f, SqlSingle.MaxValue.Value);
            Assert.Equal(-3.40282346638528859E+38f, SqlSingle.MinValue.Value);
            Assert.True(SqlSingle.Null.IsNull);
            Assert.Equal(0f, SqlSingle.Zero.Value);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            SqlSingle test = new SqlSingle(5443e12f);
            SqlSingle test1 = new SqlSingle(1);

            Assert.True(SqlSingle.Null.IsNull);
            Assert.Equal(5443e12f, test.Value);
            Assert.Equal(1, test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlSingle test0 = new SqlSingle(0);
            SqlSingle test1 = new SqlSingle(15E+18);
            SqlSingle test2 = new SqlSingle(-65E+6);
            SqlSingle test3 = new SqlSingle(5E+30);
            SqlSingle test4 = new SqlSingle(5E+18);
            SqlSingle testMax = new SqlSingle(SqlSingle.MaxValue.Value);

            // Add()
            Assert.Equal(15E+18f, SqlSingle.Add(test1, test0).Value);
            Assert.Equal(1.5E+19f, SqlSingle.Add(test1, test2).Value);

            Assert.Throws<OverflowException>(() => SqlSingle.Add(SqlSingle.MaxValue, SqlSingle.MaxValue));

            // Divide()
            Assert.Equal(3, SqlSingle.Divide(test1, test4));
            Assert.Equal(-1.3E-23f, SqlSingle.Divide(test2, test3).Value);


            Assert.Throws<DivideByZeroException>(() => SqlSingle.Divide(test1, test0));

            // Multiply()
            Assert.Equal((float)(7.5E+37), SqlSingle.Multiply(test1, test4).Value);
            Assert.Equal(0, SqlSingle.Multiply(test1, test0).Value);

            Assert.Throws<OverflowException>(() => SqlSingle.Multiply(testMax, test1));

            // Subtract()
            Assert.Equal((float)(-5E+30), SqlSingle.Subtract(test1, test3).Value);

            Assert.Throws<OverflowException>(() => SqlSingle.Subtract(SqlSingle.MinValue, SqlSingle.MaxValue));
        }

        [Fact]
        public void CompareTo()
        {
            SqlSingle test1 = new SqlSingle(4E+30);
            SqlSingle test11 = new SqlSingle(4E+30);
            SqlSingle test2 = new SqlSingle(-9E+30);
            SqlSingle test3 = new SqlSingle(10000);
            SqlString testString = new SqlString("This is a test");

            Assert.True(test1.CompareTo(test3) > 0);
            Assert.True(test2.CompareTo(test3) < 0);
            Assert.Equal(0, test1.CompareTo(test11));
            Assert.True(test11.CompareTo(SqlSingle.Null) > 0);

            Assert.Throws<ArgumentException>(() => test1.CompareTo(testString));
        }

        [Fact]
        public void EqualsMethods()
        {
            SqlSingle test0 = new SqlSingle(0);
            SqlSingle test1 = new SqlSingle(1.58e30);
            SqlSingle test2 = new SqlSingle(1.8e32);
            SqlSingle test22 = new SqlSingle(1.8e32);

            Assert.False(test0.Equals(test1));
            Assert.False(test0.Equals((object)test1));
            Assert.False(test1.Equals(test2));
            Assert.False(test1.Equals((object)test2));
            Assert.False(test2.Equals(new SqlString("TEST")));
            Assert.True(test2.Equals(test22));
            Assert.True(test2.Equals((object)test22));

            // Static Equals()-method
            Assert.True(SqlSingle.Equals(test2, test22).Value);
            Assert.False(SqlSingle.Equals(test1, test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlSingle test15 = new SqlSingle(15);

            // FIXME: Better way to test HashCode
            Assert.Equal(test15.GetHashCode(), test15.GetHashCode());
        }

        [Fact]
        public void Greaters()
        {
            SqlSingle test1 = new SqlSingle(1e10);
            SqlSingle test11 = new SqlSingle(1e10);
            SqlSingle test2 = new SqlSingle(64e14);

            // GreateThan ()
            Assert.False(SqlSingle.GreaterThan(test1, test2).Value);
            Assert.True(SqlSingle.GreaterThan(test2, test1).Value);
            Assert.False(SqlSingle.GreaterThan(test1, test11).Value);

            // GreaterTharOrEqual ()
            Assert.False(SqlSingle.GreaterThanOrEqual(test1, test2).Value);
            Assert.True(SqlSingle.GreaterThanOrEqual(test2, test1).Value);
            Assert.True(SqlSingle.GreaterThanOrEqual(test1, test11).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlSingle test1 = new SqlSingle(1.8e10);
            SqlSingle test11 = new SqlSingle(1.8e10);
            SqlSingle test2 = new SqlSingle(64e14);

            // LessThan()
            Assert.False(SqlSingle.LessThan(test1, test11).Value);
            Assert.False(SqlSingle.LessThan(test2, test1).Value);
            Assert.True(SqlSingle.LessThan(test11, test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlSingle.LessThanOrEqual(test1, test2).Value);
            Assert.False(SqlSingle.LessThanOrEqual(test2, test1).Value);
            Assert.True(SqlSingle.LessThanOrEqual(test11, test1).Value);
            Assert.True(SqlSingle.LessThanOrEqual(test11, SqlSingle.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlSingle test1 = new SqlSingle(12800000000001);
            SqlSingle test2 = new SqlSingle(128e10);
            SqlSingle test22 = new SqlSingle(128e10);

            Assert.True(SqlSingle.NotEquals(test1, test2).Value);
            Assert.True(SqlSingle.NotEquals(test2, test1).Value);
            Assert.True(SqlSingle.NotEquals(test22, test1).Value);
            Assert.False(SqlSingle.NotEquals(test22, test2).Value);
            Assert.False(SqlSingle.NotEquals(test2, test22).Value);
            Assert.True(SqlSingle.NotEquals(SqlSingle.Null, test22).IsNull);
            Assert.True(SqlSingle.NotEquals(SqlSingle.Null, test22).IsNull);
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlSingle.Parse(null));

            Assert.Throws<FormatException>(() => SqlSingle.Parse("not-a-number"));

            Assert.Throws<OverflowException>(() => SqlSingle.Parse("9e44"));

            Assert.Equal(150, SqlSingle.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlSingle test0 = new SqlSingle(0);
            SqlSingle test1 = new SqlSingle(250);
            SqlSingle test2 = new SqlSingle(64E+16);
            SqlSingle test3 = new SqlSingle(64E+30);
            SqlSingle TestNull = SqlSingle.Null;

            // ToSqlBoolean ()
            Assert.True(test1.ToSqlBoolean().Value);
            Assert.False(test0.ToSqlBoolean().Value);
            Assert.True(TestNull.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)250, test1.ToSqlByte().Value);
            Assert.Equal((byte)0, test0.ToSqlByte().Value);

            Assert.Throws<OverflowException>(() => test2.ToSqlByte());

            // ToSqlDecimal ()
            Assert.Equal(250M, test1.ToSqlDecimal().Value);
            Assert.Equal(0, test0.ToSqlDecimal().Value);

            Assert.Throws<OverflowException>(() => test3.ToSqlDecimal().Value);

            // ToSqlInt16 ()
            Assert.Equal((short)250, test1.ToSqlInt16().Value);
            Assert.Equal((short)0, test0.ToSqlInt16().Value);

            Assert.Throws<OverflowException>(() => test2.ToSqlInt16());

            // ToSqlInt32 ()
            Assert.Equal(250, test1.ToSqlInt32().Value);
            Assert.Equal(0, test0.ToSqlInt32().Value);

            Assert.Throws<OverflowException>(() => test2.ToSqlInt32());

            // ToSqlInt64 ()
            Assert.Equal(250, test1.ToSqlInt64().Value);
            Assert.Equal(0, test0.ToSqlInt64().Value);

            Assert.Throws<OverflowException>(() => test3.ToSqlInt64());

            // ToSqlMoney ()
            Assert.Equal(250.0000M, test1.ToSqlMoney().Value);
            Assert.Equal(0, test0.ToSqlMoney().Value);

            Assert.Throws<OverflowException>(() => test2.ToSqlMoney());


            // ToSqlString ()
            Assert.Equal(250.ToString(), test1.ToSqlString().Value);
            Assert.Equal(0.ToString(), test0.ToSqlString().Value);
            Assert.Equal(6.4E+17.ToString(), test2.ToSqlString().Value);

            // ToString ()
            Assert.Equal(250.ToString(), test1.ToString());
            Assert.Equal(0.ToString(), test0.ToString());
            Assert.Equal(6.4E+17.ToString(), test2.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlSingle test0 = new SqlSingle(0);
            SqlSingle test1 = new SqlSingle(24E+11);
            SqlSingle test2 = new SqlSingle(12E+11);
            SqlSingle test3 = new SqlSingle(1E+10);
            SqlSingle test4 = new SqlSingle(2E+10);

            // "+"-operator
            Assert.Equal((SqlSingle)3E+10, test3 + test4);

            Assert.Throws<OverflowException>(() => SqlSingle.MaxValue + SqlSingle.MaxValue);

            // "/"-operator
            Assert.Equal(2, test1 / test2);

            Assert.Throws<DivideByZeroException>(() => test2 / test0);

            // "*"-operator
            Assert.Equal((SqlSingle)2E+20, test3 * test4);

            Assert.Throws<OverflowException>(() => SqlSingle.MaxValue * test1);

            // "-"-operator
            Assert.Equal((SqlSingle)12e11, test1 - test2);

            Assert.Throws<OverflowException>(() => SqlSingle.MinValue - SqlSingle.MaxValue);
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlSingle test1 = new SqlSingle(1.0E+14f);
            SqlSingle test2 = new SqlSingle(9.7E+11);
            SqlSingle test22 = new SqlSingle(9.7E+11);
            SqlSingle test3 = new SqlSingle(2.0E+22f);

            // == -operator
            Assert.True((test2 == test22).Value);
            Assert.False((test1 == test2).Value);
            Assert.True((test1 == SqlSingle.Null).IsNull);

            // != -operator
            Assert.False((test2 != test22).Value);
            Assert.True((test2 != test3).Value);
            Assert.True((test1 != test3).Value);
            Assert.True((test1 != SqlSingle.Null).IsNull);

            // > -operator
            Assert.True((test1 > test2).Value);
            Assert.False((test1 > test3).Value);
            Assert.False((test2 > test22).Value);
            Assert.True((test1 > SqlSingle.Null).IsNull);

            // >=  -operator
            Assert.False((test1 >= test3).Value);
            Assert.True((test3 >= test1).Value);
            Assert.True((test2 >= test22).Value);
            Assert.True((test1 >= SqlSingle.Null).IsNull);

            // < -operator
            Assert.False((test1 < test2).Value);
            Assert.True((test1 < test3).Value);
            Assert.False((test2 < test22).Value);
            Assert.True((test1 < SqlSingle.Null).IsNull);

            // <= -operator
            Assert.True((test1 <= test3).Value);
            Assert.False((test3 <= test1).Value);
            Assert.True((test2 <= test22).Value);
            Assert.True((test1 <= SqlSingle.Null).IsNull);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlSingle test = new SqlSingle(2000000001);
            SqlSingle testNeg = new SqlSingle(-3000);

            SqlSingle result = -test;
            Assert.Equal(-2000000001, result.Value);

            result = -testNeg;
            Assert.Equal(3000, result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlSingle()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlSingle result;

            result = (SqlSingle)testBoolean;

            Assert.Equal(1, result.Value);

            result = (SqlSingle)SqlBoolean.Null;
            Assert.True(result.IsNull);
        }

        [Fact]
        public void SqlDoubleToSqlSingle()
        {
            SqlDouble test = new SqlDouble(12e12);
            SqlSingle testSqlSingle = (SqlSingle)test;
            Assert.Equal(12e12f, testSqlSingle.Value);
        }

        [Fact]
        public void SqlSingleToSingle()
        {
            SqlSingle test = new SqlSingle(12e12);
            float result = (float)test;
            Assert.Equal(12e12f, result);
        }

        [Fact]
        public void SqlStringToSqlSingle()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("100");

            Assert.Equal(100, ((SqlSingle)testString100).Value);

            Assert.Throws<FormatException>(() => (SqlSingle)testString);
        }

        [Fact]
        public void ByteToSqlSingle()
        {
            short testShort = 14;
            Assert.Equal(14, ((SqlSingle)testShort).Value);
        }

        [Fact]
        public void SqlDecimalToSqlSingle()
        {
            SqlDecimal testDecimal64 = new SqlDecimal(64);

            Assert.Equal(64, ((SqlSingle)testDecimal64).Value);
            Assert.Equal(SqlSingle.Null, SqlDecimal.Null);
        }

        [Fact]
        public void SqlIntToSqlSingle()
        {
            SqlInt16 test64 = new SqlInt16(64);
            SqlInt32 test640 = new SqlInt32(640);
            SqlInt64 test64000 = new SqlInt64(64000);
            Assert.Equal(64, ((SqlSingle)test64).Value);
            Assert.Equal(640, ((SqlSingle)test640).Value);
            Assert.Equal(64000, ((SqlSingle)test64000).Value);
        }

        [Fact]
        public void SqlMoneyToSqlSingle()
        {
            SqlMoney testMoney64 = new SqlMoney(64);
            Assert.Equal(64, ((SqlSingle)testMoney64).Value);
        }

        [Fact]
        public void SingleToSqlSingle()
        {
            float testSingle64 = 64;
            Assert.Equal(64, ((SqlSingle)testSingle64).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlSingle.GetXsdType(null);
            Assert.Equal("float", qualifiedName.Name);
        }
    }
}
