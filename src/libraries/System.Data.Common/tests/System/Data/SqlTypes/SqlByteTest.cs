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
    public class SqlByteTest
    {
        private const string Error = " does not work correctly";

        // Test constructor
        [Fact]
        public void Create()
        {
            byte b = 29;
            SqlByte TestByte = new SqlByte(b);
            Assert.Equal((byte)29, TestByte.Value);
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
            SqlByte TestByte = new SqlByte(54);
            SqlByte TestByte2 = new SqlByte(1);

            Assert.True(SqlByte.Null.IsNull);
            Assert.Equal((byte)54, TestByte.Value);
            Assert.Equal((byte)1, TestByte2.Value);
        }

        // PUBLIC STATIC METHODS
        [Fact]
        public void AddMethod()
        {
            SqlByte TestByte64 = new SqlByte(64);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte164 = new SqlByte(164);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((byte)64, SqlByte.Add(TestByte64, TestByte0).Value);
            Assert.Equal((byte)228, SqlByte.Add(TestByte64, TestByte164).Value);
            Assert.Equal((byte)164, SqlByte.Add(TestByte0, TestByte164).Value);
            Assert.Equal((byte)255, SqlByte.Add(TestByte255, TestByte0).Value);

            try
            {
                SqlByte.Add(TestByte255, TestByte64);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void BitwiseAndMethod()
        {
            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte1 = new SqlByte(1);
            SqlByte TestByte62 = new SqlByte(62);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((byte)0, SqlByte.BitwiseAnd(TestByte2, TestByte1).Value);
            Assert.Equal((byte)0, SqlByte.BitwiseAnd(TestByte1, TestByte62).Value);
            Assert.Equal((byte)2, SqlByte.BitwiseAnd(TestByte62, TestByte2).Value);
            Assert.Equal((byte)1, SqlByte.BitwiseAnd(TestByte1, TestByte255).Value);
            Assert.Equal((byte)62, SqlByte.BitwiseAnd(TestByte62, TestByte255).Value);
        }

        [Fact]
        public void BitwiseOrMethod()
        {
            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte1 = new SqlByte(1);
            SqlByte TestByte62 = new SqlByte(62);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((byte)3, SqlByte.BitwiseOr(TestByte2, TestByte1).Value);
            Assert.Equal((byte)63, SqlByte.BitwiseOr(TestByte1, TestByte62).Value);
            Assert.Equal((byte)62, SqlByte.BitwiseOr(TestByte62, TestByte2).Value);
            Assert.Equal((byte)255, SqlByte.BitwiseOr(TestByte1, TestByte255).Value);
            Assert.Equal((byte)255, SqlByte.BitwiseOr(TestByte62, TestByte255).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlByte TestByte13 = new SqlByte(13);
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte10II = new SqlByte(10);

            SqlString TestString = new SqlString("This is a test");

            Assert.True(TestByte13.CompareTo(TestByte10) > 0);
            Assert.True(TestByte10.CompareTo(TestByte13) < 0);
            Assert.True(TestByte10.CompareTo(TestByte10II) == 0);

            try
            {
                TestByte13.CompareTo(TestString);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
        }

        [Fact]
        public void DivideMethod()
        {
            SqlByte TestByte13 = new SqlByte(13);
            SqlByte TestByte0 = new SqlByte(0);

            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte180 = new SqlByte(180);
            SqlByte TestByte3 = new SqlByte(3);

            Assert.Equal((byte)6, SqlByte.Divide(TestByte13, TestByte2).Value);
            Assert.Equal((byte)90, SqlByte.Divide(TestByte180, TestByte2).Value);
            Assert.Equal((byte)60, SqlByte.Divide(TestByte180, TestByte3).Value);
            Assert.Equal((byte)0, SqlByte.Divide(TestByte13, TestByte180).Value);
            Assert.Equal((byte)0, SqlByte.Divide(TestByte13, TestByte180).Value);

            try
            {
                SqlByte.Divide(TestByte13, TestByte0);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }
        }

        [Fact]
        public void EqualsMethod()
        {
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte158 = new SqlByte(158);
            SqlByte TestByte180 = new SqlByte(180);
            SqlByte TestByte180II = new SqlByte(180);

            Assert.True(!TestByte0.Equals(TestByte158));
            Assert.True(!TestByte158.Equals(TestByte180));
            Assert.True(!TestByte180.Equals(new SqlString("TEST")));
            Assert.True(TestByte180.Equals(TestByte180II));
        }

        [Fact]
        public void StaticEqualsMethod()
        {
            SqlByte TestByte34 = new SqlByte(34);
            SqlByte TestByte34II = new SqlByte(34);
            SqlByte TestByte15 = new SqlByte(15);

            Assert.True(SqlByte.Equals(TestByte34, TestByte34II).Value);
            Assert.True(!SqlByte.Equals(TestByte34, TestByte15).Value);
            Assert.True(!SqlByte.Equals(TestByte15, TestByte34II).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlByte TestByte15 = new SqlByte(15);
            SqlByte TestByte216 = new SqlByte(216);

            Assert.Equal(15, TestByte15.GetHashCode());
            Assert.Equal(216, TestByte216.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            SqlByte TestByte = new SqlByte(84);

            Assert.Equal("System.Data.SqlTypes.SqlByte", TestByte.GetType().ToString());
        }

        [Fact]
        public void GreaterThan()
        {
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte10II = new SqlByte(10);
            SqlByte TestByte110 = new SqlByte(110);

            Assert.True(!SqlByte.GreaterThan(TestByte10, TestByte110).Value);
            Assert.True(SqlByte.GreaterThan(TestByte110, TestByte10).Value);
            Assert.True(!SqlByte.GreaterThan(TestByte10II, TestByte10).Value);
        }

        [Fact]
        public void GreaterThanOrEqual()
        {
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte10II = new SqlByte(10);
            SqlByte TestByte110 = new SqlByte(110);

            Assert.True(!SqlByte.GreaterThanOrEqual(TestByte10, TestByte110).Value);

            Assert.True(SqlByte.GreaterThanOrEqual(TestByte110, TestByte10).Value);

            Assert.True(SqlByte.GreaterThanOrEqual(TestByte10II, TestByte10).Value);
        }

        [Fact]
        public void LessThan()
        {
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte10II = new SqlByte(10);
            SqlByte TestByte110 = new SqlByte(110);

            Assert.True(SqlByte.LessThan(TestByte10, TestByte110).Value);

            Assert.True(!SqlByte.LessThan(TestByte110, TestByte10).Value);

            Assert.True(!SqlByte.LessThan(TestByte10II, TestByte10).Value);
        }

        [Fact]
        public void LessThanOrEqual()
        {
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte10II = new SqlByte(10);
            SqlByte TestByte110 = new SqlByte(110);

            Assert.True(SqlByte.LessThanOrEqual(TestByte10, TestByte110).Value);

            Assert.True(!SqlByte.LessThanOrEqual(TestByte110, TestByte10).Value);

            Assert.True(SqlByte.LessThanOrEqual(TestByte10II, TestByte10).Value);

            Assert.True(SqlByte.LessThanOrEqual(TestByte10II, SqlByte.Null).IsNull);
        }

        [Fact]
        public void Mod()
        {
            SqlByte TestByte132 = new SqlByte(132);
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte200 = new SqlByte(200);

            Assert.Equal((SqlByte)2, SqlByte.Mod(TestByte132, TestByte10));
            Assert.Equal((SqlByte)10, SqlByte.Mod(TestByte10, TestByte200));
            Assert.Equal((SqlByte)0, SqlByte.Mod(TestByte200, TestByte10));
            Assert.Equal((SqlByte)68, SqlByte.Mod(TestByte200, TestByte132));
        }

        [Fact]
        public void Multiply()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte128 = new SqlByte(128);

            Assert.Equal((byte)24, SqlByte.Multiply(TestByte12, TestByte2).Value);
            Assert.Equal((byte)24, SqlByte.Multiply(TestByte2, TestByte12).Value);

            try
            {
                SqlByte.Multiply(TestByte128, TestByte2);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void NotEquals()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte128 = new SqlByte(128);
            SqlByte TestByte128II = new SqlByte(128);

            Assert.True(SqlByte.NotEquals(TestByte12, TestByte128).Value);
            Assert.True(SqlByte.NotEquals(TestByte128, TestByte12).Value);
            Assert.True(SqlByte.NotEquals(TestByte128II, TestByte12).Value);
            Assert.True(!SqlByte.NotEquals(TestByte128II, TestByte128).Value);
            Assert.True(!SqlByte.NotEquals(TestByte128, TestByte128II).Value);
        }

        [Fact]
        public void OnesComplement()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)243, SqlByte.OnesComplement(TestByte12));
            Assert.Equal((SqlByte)127, SqlByte.OnesComplement(TestByte128));
        }

        [Fact]
        public void Parse()
        {
            try
            {
                SqlByte.Parse(null);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            try
            {
                SqlByte.Parse("not-a-number");
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }

            try
            {
                int OverInt = (int)SqlByte.MaxValue + 1;
                SqlByte.Parse(OverInt.ToString());
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            Assert.Equal((byte)150, SqlByte.Parse("150").Value);
        }

        [Fact]
        public void Subtract()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte128 = new SqlByte(128);
            Assert.Equal((byte)116, SqlByte.Subtract(TestByte128, TestByte12).Value);

            try
            {
                SqlByte.Subtract(TestByte12, TestByte128);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void ToSqlBoolean()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByteNull = SqlByte.Null;

            Assert.True(TestByte12.ToSqlBoolean().Value);
            Assert.True(!TestByte0.ToSqlBoolean().Value);
            Assert.True(TestByteNull.ToSqlBoolean().IsNull);
        }

        [Fact]
        public void ToSqlDecimal()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal(12, TestByte12.ToSqlDecimal().Value);
            Assert.Equal(0, TestByte0.ToSqlDecimal().Value);
            Assert.Equal(228, TestByte228.ToSqlDecimal().Value);
        }

        [Fact]
        public void ToSqlDouble()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal(12, TestByte12.ToSqlDouble().Value);
            Assert.Equal(0, TestByte0.ToSqlDouble().Value);
            Assert.Equal(228, TestByte228.ToSqlDouble().Value);
        }

        [Fact]
        public void ToSqlInt16()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal((short)12, TestByte12.ToSqlInt16().Value);
            Assert.Equal((short)0, TestByte0.ToSqlInt16().Value);
            Assert.Equal((short)228, TestByte228.ToSqlInt16().Value);
        }

        [Fact]
        public void ToSqlInt32()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal(12, TestByte12.ToSqlInt32().Value);
            Assert.Equal(0, TestByte0.ToSqlInt32().Value);
            Assert.Equal(228, TestByte228.ToSqlInt32().Value);
        }

        [Fact]
        public void ToSqlInt64()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal(12, TestByte12.ToSqlInt64().Value);
            Assert.Equal(0, TestByte0.ToSqlInt64().Value);
            Assert.Equal(228, TestByte228.ToSqlInt64().Value);
        }

        [Fact]
        public void ToSqlMoney()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal(12.0000M, TestByte12.ToSqlMoney().Value);
            Assert.Equal(0, TestByte0.ToSqlMoney().Value);
            Assert.Equal(228.0000M, TestByte228.ToSqlMoney().Value);
        }

        [Fact]
        public void ToSqlSingle()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal(12, TestByte12.ToSqlSingle().Value);
            Assert.Equal(0, TestByte0.ToSqlSingle().Value);
            Assert.Equal(228, TestByte228.ToSqlSingle().Value);
        }

        [Fact]
        public void ToSqlString()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal("12", TestByte12.ToSqlString().Value);
            Assert.Equal("0", TestByte0.ToSqlString().Value);
            Assert.Equal("228", TestByte228.ToSqlString().Value);
        }

        [Fact]
        public void ToStringTest()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte0 = new SqlByte(0);
            SqlByte TestByte228 = new SqlByte(228);

            Assert.Equal("12", TestByte12.ToString());
            Assert.Equal("0", TestByte0.ToString());
            Assert.Equal("228", TestByte228.ToString());
        }

        [Fact]
        public void TestXor()
        {
            SqlByte TestByte14 = new SqlByte(14);
            SqlByte TestByte58 = new SqlByte(58);
            SqlByte TestByte130 = new SqlByte(130);

            Assert.Equal((byte)52, SqlByte.Xor(TestByte14, TestByte58).Value);
            Assert.Equal((byte)140, SqlByte.Xor(TestByte14, TestByte130).Value);
            Assert.Equal((byte)184, SqlByte.Xor(TestByte58, TestByte130).Value);
        }

        // OPERATORS

        [Fact]
        public void AdditionOperator()
        {
            SqlByte TestByte24 = new SqlByte(24);
            SqlByte TestByte64 = new SqlByte(64);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)88, TestByte24 + TestByte64);

            try
            {
                SqlByte result = TestByte64 + TestByte255;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void BitwiseAndOperator()
        {
            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte4 = new SqlByte(4);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)0, TestByte2 & TestByte4);
            Assert.Equal((SqlByte)2, TestByte2 & TestByte255);
        }

        [Fact]
        public void BitwiseOrOperator()
        {
            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte4 = new SqlByte(4);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)6, TestByte2 | TestByte4);
            Assert.Equal((SqlByte)255, TestByte2 | TestByte255);
        }

        [Fact]
        public void DivisionOperator()
        {
            SqlByte TestByte2 = new SqlByte(2);
            SqlByte TestByte4 = new SqlByte(4);
            SqlByte TestByte255 = new SqlByte(255);
            SqlByte TestByte0 = new SqlByte(0);

            Assert.Equal((SqlByte)2, TestByte4 / TestByte2);
            Assert.Equal((SqlByte)127, TestByte255 / TestByte2);

            try
            {
                TestByte2 = TestByte255 / TestByte0;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }
        }

        [Fact]
        public void EqualityOperator()
        {
            SqlByte TestByte15 = new SqlByte(15);
            SqlByte TestByte15II = new SqlByte(15);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.True((TestByte15 == TestByte15II).Value);
            Assert.True(!(TestByte15 == TestByte255).Value);
            Assert.True(!(TestByte15 != TestByte15II).Value);
            Assert.True((TestByte15 != TestByte255).Value);
        }

        [Fact]
        public void ExclusiveOrOperator()
        {
            SqlByte TestByte15 = new SqlByte(15);
            SqlByte TestByte10 = new SqlByte(10);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.Equal((SqlByte)5, (TestByte15 ^ TestByte10));
            Assert.Equal((SqlByte)240, (TestByte15 ^ TestByte255));
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlByte TestByte165 = new SqlByte(165);
            SqlByte TestByte100 = new SqlByte(100);
            SqlByte TestByte100II = new SqlByte(100);
            SqlByte TestByte255 = new SqlByte(255);

            Assert.True((TestByte165 > TestByte100).Value);
            Assert.True(!(TestByte165 > TestByte255).Value);
            Assert.True(!(TestByte100 > TestByte100II).Value);
            Assert.True(!(TestByte165 >= TestByte255).Value);
            Assert.True((TestByte255 >= TestByte165).Value);
            Assert.True((TestByte100 >= TestByte100II).Value);

            Assert.True(!(TestByte165 < TestByte100).Value);
            Assert.True((TestByte165 < TestByte255).Value);
            Assert.True(!(TestByte100 < TestByte100II).Value);
            Assert.True((TestByte165 <= TestByte255).Value);
            Assert.True(!(TestByte255 <= TestByte165).Value);
            Assert.True((TestByte100 <= TestByte100II).Value);
        }

        [Fact]
        public void MultiplicationOperator()
        {
            SqlByte TestByte4 = new SqlByte(4);
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)48, TestByte4 * TestByte12);
            try
            {
                SqlByte test = (TestByte128 * TestByte4);
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void OnesComplementOperator()
        {
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)243, ~TestByte12);
            Assert.Equal((SqlByte)127, ~TestByte128);
        }

        [Fact]
        public void SubtractionOperator()
        {
            SqlByte TestByte4 = new SqlByte(4);
            SqlByte TestByte12 = new SqlByte(12);
            SqlByte TestByte128 = new SqlByte(128);

            Assert.Equal((SqlByte)8, TestByte12 - TestByte4);
            try
            {
                SqlByte test = TestByte4 - TestByte128;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlBooleanToSqlByte()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);
            SqlByte TestByte;

            TestByte = (SqlByte)TestBoolean;

            Assert.Equal((byte)1, TestByte.Value);
        }

        [Fact]
        public void SqlByteToByte()
        {
            SqlByte TestByte = new SqlByte(12);
            byte test = (byte)TestByte;
            Assert.Equal((byte)12, test);
        }

        [Fact]
        public void SqlDecimalToSqlByte()
        {
            SqlDecimal TestDecimal64 = new SqlDecimal(64);
            SqlDecimal TestDecimal900 = new SqlDecimal(900);

            Assert.Equal((byte)64, ((SqlByte)TestDecimal64).Value);

            try
            {
                SqlByte test = (SqlByte)TestDecimal900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlDoubleToSqlByte()
        {
            SqlDouble TestDouble64 = new SqlDouble(64);
            SqlDouble TestDouble900 = new SqlDouble(900);

            Assert.Equal((byte)64, ((SqlByte)TestDouble64).Value);

            try
            {
                SqlByte test = (SqlByte)TestDouble900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt16ToSqlByte()
        {
            SqlInt16 TestInt1664 = new SqlInt16(64);
            SqlInt16 TestInt16900 = new SqlInt16(900);

            Assert.Equal((byte)64, ((SqlByte)TestInt1664).Value);

            try
            {
                SqlByte test = (SqlByte)TestInt16900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt32ToSqlByte()
        {
            SqlInt32 TestInt3264 = new SqlInt32(64);
            SqlInt32 TestInt32900 = new SqlInt32(900);

            Assert.Equal((byte)64, ((SqlByte)TestInt3264).Value);

            try
            {
                SqlByte test = (SqlByte)TestInt32900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt64ToSqlByte()
        {
            SqlInt64 TestInt6464 = new SqlInt64(64);
            SqlInt64 TestInt64900 = new SqlInt64(900);

            Assert.Equal((byte)64, ((SqlByte)TestInt6464).Value);

            try
            {
                SqlByte test = (SqlByte)TestInt64900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlMoneyToSqlByte()
        {
            SqlMoney TestMoney64 = new SqlMoney(64);
            SqlMoney TestMoney900 = new SqlMoney(900);

            Assert.Equal((byte)64, ((SqlByte)TestMoney64).Value);

            try
            {
                SqlByte test = (SqlByte)TestMoney900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlSingleToSqlByte()
        {
            SqlSingle TestSingle64 = new SqlSingle(64);
            SqlSingle TestSingle900 = new SqlSingle(900);

            Assert.Equal((byte)64, ((SqlByte)TestSingle64).Value);

            try
            {
                SqlByte test = (SqlByte)TestSingle900;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlStringToSqlByte()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");
            SqlString TestString1000 = new SqlString("1000");

            Assert.Equal((byte)100, ((SqlByte)TestString100).Value);

            try
            {
                SqlByte test = (SqlByte)TestString1000;
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            try
            {
                SqlByte test = (SqlByte)TestString;
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }
        }

        [Fact]
        public void ByteToSqlByte()
        {
            byte TestByte = 14;
            Assert.Equal((byte)14, ((SqlByte)TestByte).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlByte.GetXsdType(null);
            Assert.Equal("unsignedByte", qualifiedName.Name);
        }
    }
}

