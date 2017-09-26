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
    public class SqlSingleTest
    {
        // Test constructor
        [Fact]
        public void Create()
        {
            SqlSingle Test = new SqlSingle((float)34.87);
            SqlSingle Test2 = 45.2f;

            Assert.Equal(34.87f, Test.Value);
            Assert.Equal(45.2f, Test2.Value);

            Test = new SqlSingle(-9000.6543);
            Assert.Equal(-9000.6543f, Test.Value);
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
            SqlSingle Test = new SqlSingle(5443e12f);
            SqlSingle Test1 = new SqlSingle(1);

            Assert.True(SqlSingle.Null.IsNull);
            Assert.Equal(5443e12f, Test.Value);
            Assert.Equal(1, Test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlSingle Test0 = new SqlSingle(0);
            SqlSingle Test1 = new SqlSingle(15E+18);
            SqlSingle Test2 = new SqlSingle(-65E+6);
            SqlSingle Test3 = new SqlSingle(5E+30);
            SqlSingle Test4 = new SqlSingle(5E+18);
            SqlSingle TestMax = new SqlSingle(SqlSingle.MaxValue.Value);

            // Add()
            Assert.Equal(15E+18f, SqlSingle.Add(Test1, Test0).Value);
            Assert.Equal(1.5E+19f, SqlSingle.Add(Test1, Test2).Value);

            try
            {
                SqlSingle test = SqlSingle.Add(SqlSingle.MaxValue,
             SqlSingle.MaxValue);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Divide()
            Assert.Equal(3, SqlSingle.Divide(Test1, Test4));
            Assert.Equal(-1.3E-23f, SqlSingle.Divide(Test2, Test3).Value);

            try
            {
                SqlSingle test = SqlSingle.Divide(Test1, Test0).Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // Multiply()
            Assert.Equal((float)(7.5E+37), SqlSingle.Multiply(Test1, Test4).Value);
            Assert.Equal(0, SqlSingle.Multiply(Test1, Test0).Value);

            try
            {
                SqlSingle test = SqlSingle.Multiply(TestMax, Test1);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }


            // Subtract()
            Assert.Equal((float)(-5E+30), SqlSingle.Subtract(Test1, Test3).Value);

            try
            {
                SqlSingle test = SqlSingle.Subtract(
    SqlSingle.MinValue, SqlSingle.MaxValue);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void CompareTo()
        {
            SqlSingle Test1 = new SqlSingle(4E+30);
            SqlSingle Test11 = new SqlSingle(4E+30);
            SqlSingle Test2 = new SqlSingle(-9E+30);
            SqlSingle Test3 = new SqlSingle(10000);
            SqlString TestString = new SqlString("This is a test");

            Assert.True(Test1.CompareTo(Test3) > 0);
            Assert.True(Test2.CompareTo(Test3) < 0);
            Assert.True(Test1.CompareTo(Test11) == 0);
            Assert.True(Test11.CompareTo(SqlSingle.Null) > 0);

            try
            {
                Test1.CompareTo(TestString);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
        }

        [Fact]
        public void EqualsMethods()
        {
            SqlSingle Test0 = new SqlSingle(0);
            SqlSingle Test1 = new SqlSingle(1.58e30);
            SqlSingle Test2 = new SqlSingle(1.8e32);
            SqlSingle Test22 = new SqlSingle(1.8e32);

            Assert.True(!Test0.Equals(Test1));
            Assert.True(!Test1.Equals(Test2));
            Assert.True(!Test2.Equals(new SqlString("TEST")));
            Assert.True(Test2.Equals(Test22));

            // Static Equals()-method
            Assert.True(SqlSingle.Equals(Test2, Test22).Value);
            Assert.True(!SqlSingle.Equals(Test1, Test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlSingle Test15 = new SqlSingle(15);

            // FIXME: Better way to test HashCode
            Assert.Equal(Test15.GetHashCode(), Test15.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            SqlSingle Test = new SqlSingle(84);
            Assert.Equal("System.Data.SqlTypes.SqlSingle", Test.GetType().ToString());
            Assert.Equal("System.Single", Test.Value.GetType().ToString());
        }

        [Fact]
        public void Greaters()
        {
            SqlSingle Test1 = new SqlSingle(1e10);
            SqlSingle Test11 = new SqlSingle(1e10);
            SqlSingle Test2 = new SqlSingle(64e14);

            // GreateThan ()
            Assert.True(!SqlSingle.GreaterThan(Test1, Test2).Value);
            Assert.True(SqlSingle.GreaterThan(Test2, Test1).Value);
            Assert.True(!SqlSingle.GreaterThan(Test1, Test11).Value);

            // GreaterTharOrEqual ()
            Assert.True(!SqlSingle.GreaterThanOrEqual(Test1, Test2).Value);
            Assert.True(SqlSingle.GreaterThanOrEqual(Test2, Test1).Value);
            Assert.True(SqlSingle.GreaterThanOrEqual(Test1, Test11).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlSingle Test1 = new SqlSingle(1.8e10);
            SqlSingle Test11 = new SqlSingle(1.8e10);
            SqlSingle Test2 = new SqlSingle(64e14);

            // LessThan()
            Assert.True(!SqlSingle.LessThan(Test1, Test11).Value);
            Assert.True(!SqlSingle.LessThan(Test2, Test1).Value);
            Assert.True(SqlSingle.LessThan(Test11, Test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlSingle.LessThanOrEqual(Test1, Test2).Value);
            Assert.True(!SqlSingle.LessThanOrEqual(Test2, Test1).Value);
            Assert.True(SqlSingle.LessThanOrEqual(Test11, Test1).Value);
            Assert.True(SqlSingle.LessThanOrEqual(Test11, SqlSingle.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlSingle Test1 = new SqlSingle(12800000000001);
            SqlSingle Test2 = new SqlSingle(128e10);
            SqlSingle Test22 = new SqlSingle(128e10);

            Assert.True(SqlSingle.NotEquals(Test1, Test2).Value);
            Assert.True(SqlSingle.NotEquals(Test2, Test1).Value);
            Assert.True(SqlSingle.NotEquals(Test22, Test1).Value);
            Assert.True(!SqlSingle.NotEquals(Test22, Test2).Value);
            Assert.True(!SqlSingle.NotEquals(Test2, Test22).Value);
            Assert.True(SqlSingle.NotEquals(SqlSingle.Null, Test22).IsNull);
            Assert.True(SqlSingle.NotEquals(SqlSingle.Null, Test22).IsNull);
        }

        [Fact]
        public void Parse()
        {
            try
            {
                SqlSingle.Parse(null);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            try
            {
                SqlSingle.Parse("not-a-number");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }

            try
            {
                SqlSingle.Parse("9e44");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            Assert.Equal(150, SqlSingle.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlSingle Test0 = new SqlSingle(0);
            SqlSingle Test1 = new SqlSingle(250);
            SqlSingle Test2 = new SqlSingle(64E+16);
            SqlSingle Test3 = new SqlSingle(64E+30);
            SqlSingle TestNull = SqlSingle.Null;

            // ToSqlBoolean ()
            Assert.True(Test1.ToSqlBoolean().Value);
            Assert.True(!Test0.ToSqlBoolean().Value);
            Assert.True(TestNull.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)250, Test1.ToSqlByte().Value);
            Assert.Equal((byte)0, Test0.ToSqlByte().Value);

            try
            {
                SqlByte b = (byte)Test2.ToSqlByte();
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlDecimal ()
            Assert.Equal(250.00000000000000M, Test1.ToSqlDecimal().Value);
            Assert.Equal(0, Test0.ToSqlDecimal().Value);

            try
            {
                SqlDecimal test = Test3.ToSqlDecimal().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlInt16 ()
            Assert.Equal((short)250, Test1.ToSqlInt16().Value);
            Assert.Equal((short)0, Test0.ToSqlInt16().Value);

            try
            {
                SqlInt16 test = Test2.ToSqlInt16().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlInt32 ()
            Assert.Equal(250, Test1.ToSqlInt32().Value);
            Assert.Equal(0, Test0.ToSqlInt32().Value);

            try
            {
                SqlInt32 test = Test2.ToSqlInt32().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlInt64 ()
            Assert.Equal(250, Test1.ToSqlInt64().Value);
            Assert.Equal(0, Test0.ToSqlInt64().Value);

            try
            {
                SqlInt64 test = Test3.ToSqlInt64().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlMoney ()
            Assert.Equal(250.0000M, Test1.ToSqlMoney().Value);
            Assert.Equal(0, Test0.ToSqlMoney().Value);

            try
            {
                SqlMoney test = Test3.ToSqlMoney().Value;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }


            // ToSqlString ()
            Assert.Equal("250", Test1.ToSqlString().Value);
            Assert.Equal("0", Test0.ToSqlString().Value);
            Assert.Equal("6.4E+17", Test2.ToSqlString().Value);

            // ToString ()
            Assert.Equal("250", Test1.ToString());
            Assert.Equal("0", Test0.ToString());
            Assert.Equal("6.4E+17", Test2.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlSingle Test0 = new SqlSingle(0);
            SqlSingle Test1 = new SqlSingle(24E+11);
            SqlSingle Test2 = new SqlSingle(64E+32);
            SqlSingle Test3 = new SqlSingle(12E+11);
            SqlSingle Test4 = new SqlSingle(1E+10);
            SqlSingle Test5 = new SqlSingle(2E+10);

            // "+"-operator
            Assert.Equal((SqlSingle)3E+10, Test4 + Test5);

            try
            {
                SqlSingle test = SqlSingle.MaxValue + SqlSingle.MaxValue;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            try
            {
                SqlSingle test = SqlSingle.MaxValue + SqlSingle.MaxValue;
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "/"-operator
            Assert.Equal(2, Test1 / Test3);

            try
            {
                SqlSingle test = Test3 / Test0;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // "*"-operator
            Assert.Equal((SqlSingle)2E+20, Test4 * Test5);

            try
            {
                SqlSingle test = SqlSingle.MaxValue * Test1;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "-"-operator
            Assert.Equal((SqlSingle)12e11, Test1 - Test3);

            try
            {
                SqlSingle test = SqlSingle.MinValue - SqlSingle.MaxValue;
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
            SqlSingle Test1 = new SqlSingle(1.0E+14f);
            SqlSingle Test2 = new SqlSingle(9.7E+11);
            SqlSingle Test22 = new SqlSingle(9.7E+11);
            SqlSingle Test3 = new SqlSingle(2.0E+22f);

            // == -operator
            Assert.True((Test2 == Test22).Value);
            Assert.True(!(Test1 == Test2).Value);
            Assert.True((Test1 == SqlSingle.Null).IsNull);

            // != -operator
            Assert.True(!(Test2 != Test22).Value);
            Assert.True((Test2 != Test3).Value);
            Assert.True((Test1 != Test3).Value);
            Assert.True((Test1 != SqlSingle.Null).IsNull);

            // > -operator
            Assert.True((Test1 > Test2).Value);
            Assert.True(!(Test1 > Test3).Value);
            Assert.True(!(Test2 > Test22).Value);
            Assert.True((Test1 > SqlSingle.Null).IsNull);

            // >=  -operator
            Assert.True(!(Test1 >= Test3).Value);
            Assert.True((Test3 >= Test1).Value);
            Assert.True((Test2 >= Test22).Value);
            Assert.True((Test1 >= SqlSingle.Null).IsNull);

            // < -operator
            Assert.True(!(Test1 < Test2).Value);
            Assert.True((Test1 < Test3).Value);
            Assert.True(!(Test2 < Test22).Value);
            Assert.True((Test1 < SqlSingle.Null).IsNull);

            // <= -operator
            Assert.True((Test1 <= Test3).Value);
            Assert.True(!(Test3 <= Test1).Value);
            Assert.True((Test2 <= Test22).Value);
            Assert.True((Test1 <= SqlSingle.Null).IsNull);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlSingle Test = new SqlSingle(2000000001);
            SqlSingle TestNeg = new SqlSingle(-3000);

            SqlSingle Result = -Test;
            Assert.Equal(-2000000001, Result.Value);

            Result = -TestNeg;
            Assert.Equal(3000, Result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlSingle()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);
            SqlSingle Result;

            Result = (SqlSingle)TestBoolean;

            Assert.Equal(1, Result.Value);

            Result = (SqlSingle)SqlBoolean.Null;
            Assert.True(Result.IsNull);
        }

        [Fact]
        public void SqlDoubleToSqlSingle()
        {
            SqlDouble Test = new SqlDouble(12e12);
            SqlSingle TestSqlSingle = (SqlSingle)Test;
            Assert.Equal(12e12f, TestSqlSingle.Value);
        }

        [Fact]
        public void SqlSingleToSingle()
        {
            SqlSingle Test = new SqlSingle(12e12);
            float Result = (float)Test;
            Assert.Equal(12e12f, Result);
        }

        [Fact]
        public void SqlStringToSqlSingle()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");

            Assert.Equal(100, ((SqlSingle)TestString100).Value);

            try
            {
                SqlSingle test = (SqlSingle)TestString;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }
        }

        [Fact]
        public void ByteToSqlSingle()
        {
            short TestShort = 14;
            Assert.Equal(14, ((SqlSingle)TestShort).Value);
        }

        [Fact]
        public void SqlDecimalToSqlSingle()
        {
            SqlDecimal TestDecimal64 = new SqlDecimal(64);

            Assert.Equal(64, ((SqlSingle)TestDecimal64).Value);
            Assert.Equal(SqlSingle.Null, SqlDecimal.Null);
        }

        [Fact]
        public void SqlIntToSqlSingle()
        {
            SqlInt16 Test64 = new SqlInt16(64);
            SqlInt32 Test640 = new SqlInt32(640);
            SqlInt64 Test64000 = new SqlInt64(64000);
            Assert.Equal(64, ((SqlSingle)Test64).Value);
            Assert.Equal(640, ((SqlSingle)Test640).Value);
            Assert.Equal(64000, ((SqlSingle)Test64000).Value);
        }

        [Fact]
        public void SqlMoneyToSqlSingle()
        {
            SqlMoney TestMoney64 = new SqlMoney(64);
            Assert.Equal(64, ((SqlSingle)TestMoney64).Value);
        }

        [Fact]
        public void SingleToSqlSingle()
        {
            float TestSingle64 = 64;
            Assert.Equal(64, ((SqlSingle)TestSingle64).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlSingle.GetXsdType(null);
            Assert.Equal("float", qualifiedName.Name);
        }
    }
}

