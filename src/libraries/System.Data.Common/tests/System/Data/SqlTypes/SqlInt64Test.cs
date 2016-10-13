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

using System.Xml;
using System.Data.SqlTypes;

using System.Xml.Serialization;
using System.IO;

using Xunit;

namespace System.Data.Tests.SqlTypes
{
    public class SqlInt64Test
    {
        // Test constructor
        [Fact]
        public void Create()
        {
            SqlInt64 TestLong = new SqlInt64(29);
            Assert.Equal(29, TestLong.Value);

            TestLong = new SqlInt64(-9000);
            Assert.Equal(-9000, TestLong.Value);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.Equal(9223372036854775807, SqlInt64.MaxValue.Value);
            Assert.Equal(-9223372036854775808, SqlInt64.MinValue.Value);
            Assert.True(SqlInt64.Null.IsNull);
            Assert.Equal(0, SqlInt64.Zero.Value);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            SqlInt64 Test5443 = new SqlInt64(5443);
            SqlInt64 Test1 = new SqlInt64(1);

            Assert.True(SqlInt64.Null.IsNull);
            Assert.Equal(5443, Test5443.Value);
            Assert.Equal(1, Test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlInt64 Test64 = new SqlInt64(64);
            SqlInt64 Test0 = new SqlInt64(0);
            SqlInt64 Test164 = new SqlInt64(164);
            SqlInt64 TestMax = new SqlInt64(SqlInt64.MaxValue.Value);

            // Add()
            Assert.Equal(64, SqlInt64.Add(Test64, Test0).Value);
            Assert.Equal(228, SqlInt64.Add(Test64, Test164).Value);
            Assert.Equal(164, SqlInt64.Add(Test0, Test164).Value);
            Assert.Equal((long)SqlInt64.MaxValue, SqlInt64.Add(TestMax, Test0).Value);

            try
            {
                SqlInt64.Add(TestMax, Test64);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Divide()
            Assert.Equal(2, SqlInt64.Divide(Test164, Test64).Value);
            Assert.Equal(0, SqlInt64.Divide(Test64, Test164).Value);

            try
            {
                SqlInt64.Divide(Test64, Test0);
                Assert.False(true);
            }
            catch (DivideByZeroException e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // Mod()
            Assert.Equal(36, SqlInt64.Mod(Test164, Test64));
            Assert.Equal(64, SqlInt64.Mod(Test64, Test164));

            // Multiply()
            Assert.Equal(10496, SqlInt64.Multiply(Test64, Test164).Value);
            Assert.Equal(0, SqlInt64.Multiply(Test64, Test0).Value);

            try
            {
                SqlInt64.Multiply(TestMax, Test64);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Subtract()
            Assert.Equal(100, SqlInt64.Subtract(Test164, Test64).Value);

            try
            {
                SqlInt64.Subtract(SqlInt64.MinValue, Test164);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Modulus ()
            Assert.Equal(36, SqlInt64.Modulus(Test164, Test64));
            Assert.Equal(64, SqlInt64.Modulus(Test64, Test164));
        }

        [Fact]
        public void BitwiseMethods()
        {
            long MaxValue = SqlInt64.MaxValue.Value;
            SqlInt64 TestInt = new SqlInt64(0);
            SqlInt64 TestIntMax = new SqlInt64(MaxValue);
            SqlInt64 TestInt2 = new SqlInt64(10922);
            SqlInt64 TestInt3 = new SqlInt64(21845);

            // BitwiseAnd
            Assert.Equal(21845, SqlInt64.BitwiseAnd(TestInt3, TestIntMax).Value);
            Assert.Equal(0, SqlInt64.BitwiseAnd(TestInt2, TestInt3).Value);
            Assert.Equal(10922, SqlInt64.BitwiseAnd(TestInt2, TestIntMax).Value);

            //BitwiseOr
            Assert.Equal(21845, SqlInt64.BitwiseOr(TestInt, TestInt3).Value);
            Assert.Equal(MaxValue, SqlInt64.BitwiseOr(TestIntMax, TestInt2).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlInt64 TestInt4000 = new SqlInt64(4000);
            SqlInt64 TestInt4000II = new SqlInt64(4000);
            SqlInt64 TestInt10 = new SqlInt64(10);
            SqlInt64 TestInt10000 = new SqlInt64(10000);
            SqlString TestString = new SqlString("This is a test");

            Assert.True(TestInt4000.CompareTo(TestInt10) > 0);
            Assert.True(TestInt10.CompareTo(TestInt4000) < 0);
            Assert.True(TestInt4000II.CompareTo(TestInt4000) == 0);
            Assert.True(TestInt4000II.CompareTo(SqlInt64.Null) > 0);

            try
            {
                TestInt10.CompareTo(TestString);
                Assert.False(true);
            }
            catch (ArgumentException e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
        }

        [Fact]
        public void EqualsMethod()
        {
            SqlInt64 Test0 = new SqlInt64(0);
            SqlInt64 Test158 = new SqlInt64(158);
            SqlInt64 Test180 = new SqlInt64(180);
            SqlInt64 Test180II = new SqlInt64(180);

            Assert.True(!Test0.Equals(Test158));
            Assert.True(!Test158.Equals(Test180));
            Assert.True(!Test180.Equals(new SqlString("TEST")));
            Assert.True(Test180.Equals(Test180II));
        }

        [Fact]
        public void StaticEqualsMethod()
        {
            SqlInt64 Test34 = new SqlInt64(34);
            SqlInt64 Test34II = new SqlInt64(34);
            SqlInt64 Test15 = new SqlInt64(15);

            Assert.True(SqlInt64.Equals(Test34, Test34II).Value);
            Assert.True(!SqlInt64.Equals(Test34, Test15).Value);
            Assert.True(!SqlInt64.Equals(Test15, Test34II).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlInt64 Test15 = new SqlInt64(15);

            // FIXME: Better way to test HashCode
            Assert.Equal(15, Test15.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            SqlInt64 Test = new SqlInt64(84);
            Assert.Equal("System.Data.SqlTypes.SqlInt64", Test.GetType().ToString());
        }

        [Fact]
        public void Greaters()
        {
            SqlInt64 Test10 = new SqlInt64(10);
            SqlInt64 Test10II = new SqlInt64(10);
            SqlInt64 Test110 = new SqlInt64(110);

            // GreateThan ()
            Assert.True(!SqlInt64.GreaterThan(Test10, Test110).Value);
            Assert.True(SqlInt64.GreaterThan(Test110, Test10).Value);
            Assert.True(!SqlInt64.GreaterThan(Test10II, Test10).Value);

            // GreaterTharOrEqual ()
            Assert.True(!SqlInt64.GreaterThanOrEqual(Test10, Test110).Value);
            Assert.True(SqlInt64.GreaterThanOrEqual(Test110, Test10).Value);
            Assert.True(SqlInt64.GreaterThanOrEqual(Test10II, Test10).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlInt64 Test10 = new SqlInt64(10);
            SqlInt64 Test10II = new SqlInt64(10);
            SqlInt64 Test110 = new SqlInt64(110);

            // LessThan()
            Assert.True(SqlInt64.LessThan(Test10, Test110).Value);
            Assert.True(!SqlInt64.LessThan(Test110, Test10).Value);
            Assert.True(!SqlInt64.LessThan(Test10II, Test10).Value);

            // LessThanOrEqual ()
            Assert.True(SqlInt64.LessThanOrEqual(Test10, Test110).Value);
            Assert.True(!SqlInt64.LessThanOrEqual(Test110, Test10).Value);
            Assert.True(SqlInt64.LessThanOrEqual(Test10II, Test10).Value);
            Assert.True(SqlInt64.LessThanOrEqual(Test10II, SqlInt64.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlInt64 Test12 = new SqlInt64(12);
            SqlInt64 Test128 = new SqlInt64(128);
            SqlInt64 Test128II = new SqlInt64(128);

            Assert.True(SqlInt64.NotEquals(Test12, Test128).Value);
            Assert.True(SqlInt64.NotEquals(Test128, Test12).Value);
            Assert.True(SqlInt64.NotEquals(Test128II, Test12).Value);
            Assert.True(!SqlInt64.NotEquals(Test128II, Test128).Value);
            Assert.True(!SqlInt64.NotEquals(Test128, Test128II).Value);
            Assert.True(SqlInt64.NotEquals(SqlInt64.Null, Test128II).IsNull);
            Assert.True(SqlInt64.NotEquals(SqlInt64.Null, Test128II).IsNull);
        }

        [Fact]
        public void OnesComplement()
        {
            SqlInt64 Test12 = new SqlInt64(12);
            SqlInt64 Test128 = new SqlInt64(128);

            Assert.Equal(-13, SqlInt64.OnesComplement(Test12));
            Assert.Equal(-129, SqlInt64.OnesComplement(Test128));
        }

        [Fact]
        public void Parse()
        {
            try
            {
                SqlInt64.Parse(null);
                Assert.False(true);
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            try
            {
                SqlInt64.Parse("not-a-number");
                Assert.False(true);
            }
            catch (FormatException e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }

            try
            {
                SqlInt64.Parse("1000000000000000000000000000");
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            Assert.Equal(150, SqlInt64.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlInt64 Test12 = new SqlInt64(12);
            SqlInt64 Test0 = new SqlInt64(0);
            SqlInt64 TestNull = SqlInt64.Null;
            SqlInt64 Test1000 = new SqlInt64(1000);
            SqlInt64 Test288 = new SqlInt64(288);

            // ToSqlBoolean ()
            Assert.True(Test12.ToSqlBoolean().Value);
            Assert.True(!Test0.ToSqlBoolean().Value);
            Assert.True(TestNull.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)12, Test12.ToSqlByte().Value);
            Assert.Equal((byte)0, Test0.ToSqlByte().Value);

            try
            {
                SqlByte b = (byte)Test1000.ToSqlByte();
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlDecimal ()
            Assert.Equal(12, Test12.ToSqlDecimal().Value);
            Assert.Equal(0, Test0.ToSqlDecimal().Value);
            Assert.Equal(288, Test288.ToSqlDecimal().Value);

            // ToSqlDouble ()
            Assert.Equal(12, Test12.ToSqlDouble().Value);
            Assert.Equal(0, Test0.ToSqlDouble().Value);
            Assert.Equal(1000, Test1000.ToSqlDouble().Value);

            // ToSqlInt32 ()
            Assert.Equal(12, Test12.ToSqlInt32().Value);
            Assert.Equal(0, Test0.ToSqlInt32().Value);
            Assert.Equal(288, Test288.ToSqlInt32().Value);

            // ToSqlInt16 ()
            Assert.Equal((short)12, Test12.ToSqlInt16().Value);
            Assert.Equal((short)0, Test0.ToSqlInt16().Value);
            Assert.Equal((short)288, Test288.ToSqlInt16().Value);

            // ToSqlMoney ()
            Assert.Equal(12.0000M, Test12.ToSqlMoney().Value);
            Assert.Equal(0, Test0.ToSqlMoney().Value);
            Assert.Equal(288.0000M, Test288.ToSqlMoney().Value);

            // ToSqlSingle ()
            Assert.Equal(12, Test12.ToSqlSingle().Value);
            Assert.Equal(0, Test0.ToSqlSingle().Value);
            Assert.Equal(288, Test288.ToSqlSingle().Value);

            // ToSqlString ()
            Assert.Equal("12", Test12.ToSqlString().Value);
            Assert.Equal("0", Test0.ToSqlString().Value);
            Assert.Equal("288", Test288.ToSqlString().Value);

            // ToString ()
            Assert.Equal("12", Test12.ToString());
            Assert.Equal("0", Test0.ToString());
            Assert.Equal("288", Test288.ToString());
        }

        [Fact]
        public void Xor()
        {
            SqlInt64 Test14 = new SqlInt64(14);
            SqlInt64 Test58 = new SqlInt64(58);
            SqlInt64 Test130 = new SqlInt64(130);
            SqlInt64 TestMax = new SqlInt64(SqlInt64.MaxValue.Value);
            SqlInt64 Test0 = new SqlInt64(0);

            Assert.Equal(52, SqlInt64.Xor(Test14, Test58).Value);
            Assert.Equal(140, SqlInt64.Xor(Test14, Test130).Value);
            Assert.Equal(184, SqlInt64.Xor(Test58, Test130).Value);
            Assert.Equal(0, SqlInt64.Xor(TestMax, TestMax).Value);
            Assert.Equal(TestMax.Value, SqlInt64.Xor(TestMax, Test0).Value);
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlInt64 Test24 = new SqlInt64(24);
            SqlInt64 Test64 = new SqlInt64(64);
            SqlInt64 Test2550 = new SqlInt64(2550);
            SqlInt64 Test0 = new SqlInt64(0);

            // "+"-operator
            Assert.Equal(2614, Test2550 + Test64);
            try
            {
                SqlInt64 result = Test64 + SqlInt64.MaxValue;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "/"-operator
            Assert.Equal(39, Test2550 / Test64);
            Assert.Equal(0, Test24 / Test64);

            try
            {
                SqlInt64 result = Test2550 / Test0;
                Assert.False(true);
            }
            catch (DivideByZeroException e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // "*"-operator
            Assert.Equal(1536, Test64 * Test24);

            try
            {
                SqlInt64 test = (SqlInt64.MaxValue * Test64);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "-"-operator
            Assert.Equal(2526, Test2550 - Test24);

            try
            {
                SqlInt64 test = SqlInt64.MinValue - Test64;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "%"-operator
            Assert.Equal(54, Test2550 % Test64);
            Assert.Equal(24, Test24 % Test64);
            Assert.Equal(0, new SqlInt64(100) % new SqlInt64(10));
        }

        [Fact]
        public void BitwiseOperators()
        {
            SqlInt64 Test2 = new SqlInt64(2);
            SqlInt64 Test4 = new SqlInt64(4);

            SqlInt64 Test2550 = new SqlInt64(2550);

            // & -operator
            Assert.Equal(0, Test2 & Test4);
            Assert.Equal(2, Test2 & Test2550);
            Assert.Equal(0, SqlInt64.MaxValue & SqlInt64.MinValue);

            // | -operator
            Assert.Equal(6, Test2 | Test4);
            Assert.Equal(2550, Test2 | Test2550);
            Assert.Equal(-1, SqlInt64.MinValue | SqlInt64.MaxValue);

            //  ^ -operator
            Assert.Equal(2546, (Test2550 ^ Test4));
            Assert.Equal(6, (Test2 ^ Test4));
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlInt64 Test165 = new SqlInt64(165);
            SqlInt64 Test100 = new SqlInt64(100);
            SqlInt64 Test100II = new SqlInt64(100);
            SqlInt64 Test255 = new SqlInt64(2550);

            // == -operator
            Assert.True((Test100 == Test100II).Value);
            Assert.True(!(Test165 == Test100).Value);
            Assert.True((Test165 == SqlInt64.Null).IsNull);

            // != -operator
            Assert.True(!(Test100 != Test100II).Value);
            Assert.True((Test100 != Test255).Value);
            Assert.True((Test165 != Test255).Value);
            Assert.True((Test165 != SqlInt64.Null).IsNull);

            // > -operator
            Assert.True((Test165 > Test100).Value);
            Assert.True(!(Test165 > Test255).Value);
            Assert.True(!(Test100 > Test100II).Value);
            Assert.True((Test165 > SqlInt64.Null).IsNull);

            // >=  -operator
            Assert.True(!(Test165 >= Test255).Value);
            Assert.True((Test255 >= Test165).Value);
            Assert.True((Test100 >= Test100II).Value);
            Assert.True((Test165 >= SqlInt64.Null).IsNull);

            // < -operator
            Assert.True(!(Test165 < Test100).Value);
            Assert.True((Test165 < Test255).Value);
            Assert.True(!(Test100 < Test100II).Value);
            Assert.True((Test165 < SqlInt64.Null).IsNull);

            // <= -operator
            Assert.True((Test165 <= Test255).Value);
            Assert.True(!(Test255 <= Test165).Value);
            Assert.True((Test100 <= Test100II).Value);
            Assert.True((Test165 <= SqlInt64.Null).IsNull);
        }

        [Fact]
        public void OnesComplementOperator()
        {
            SqlInt64 Test12 = new SqlInt64(12);
            SqlInt64 Test128 = new SqlInt64(128);

            Assert.Equal(-13, ~Test12);
            Assert.Equal(-129, ~Test128);
            Assert.Equal(SqlInt64.Null, ~SqlInt64.Null);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlInt64 Test = new SqlInt64(2000);
            SqlInt64 TestNeg = new SqlInt64(-3000);

            SqlInt64 Result = -Test;
            Assert.Equal(-2000, Result.Value);

            Result = -TestNeg;
            Assert.Equal(3000, Result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlInt64()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);
            SqlInt64 Result;

            Result = (SqlInt64)TestBoolean;

            Assert.Equal(1, Result.Value);

            Result = (SqlInt64)SqlBoolean.Null;
            Assert.True(Result.IsNull);
        }

        [Fact]
        public void SqlDecimalToSqlInt64()
        {
            SqlDecimal TestDecimal64 = new SqlDecimal(64);
            SqlDecimal TestDecimal900 = new SqlDecimal(90000);

            Assert.Equal(64, ((SqlInt64)TestDecimal64).Value);
            Assert.Equal(SqlInt64.Null, ((SqlInt64)SqlDecimal.Null));

            try
            {
                SqlInt64 test = (SqlInt64)SqlDecimal.MaxValue;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlDoubleToSqlInt64()
        {
            SqlDouble TestDouble64 = new SqlDouble(64);
            SqlDouble TestDouble900 = new SqlDouble(90000);

            Assert.Equal(64, ((SqlInt64)TestDouble64).Value);
            Assert.Equal(SqlInt64.Null, ((SqlInt64)SqlDouble.Null));

            try
            {
                SqlInt64 test = (SqlInt64)SqlDouble.MaxValue;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void Sql64IntToInt64()
        {
            SqlInt64 Test = new SqlInt64(12);
            long Result = (long)Test;
            Assert.Equal(12, Result);
        }

        [Fact]
        public void SqlInt32ToSqlInt64()
        {
            SqlInt32 Test64 = new SqlInt32(64);
            Assert.Equal(64, ((SqlInt64)Test64).Value);
        }

        [Fact]
        public void SqlInt16ToSqlInt64()
        {
            SqlInt16 Test64 = new SqlInt16(64);
            Assert.Equal(64, ((SqlInt64)Test64).Value);
        }

        [Fact]
        public void SqlMoneyToSqlInt64()
        {
            SqlMoney TestMoney64 = new SqlMoney(64);
            Assert.Equal(64, ((SqlInt64)TestMoney64).Value);
        }

        [Fact]
        public void SqlSingleToSqlInt64()
        {
            SqlSingle TestSingle64 = new SqlSingle(64);
            Assert.Equal(64, ((SqlInt64)TestSingle64).Value);
        }

        [Fact]
        public void SqlStringToSqlInt64()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");
            SqlString TestString1000 = new SqlString("1000000000000000000000");

            Assert.Equal(100, ((SqlInt64)TestString100).Value);

            try
            {
                SqlInt64 test = (SqlInt64)TestString1000;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            try
            {
                SqlInt64 test = (SqlInt64)TestString;
                Assert.False(true);
            }
            catch (FormatException e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }
        }

        [Fact]
        public void ByteToSqlInt64()
        {
            short TestShort = 14;
            Assert.Equal(14, ((SqlInt64)TestShort).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlInt64.GetXsdType(null);
            Assert.Equal("long", qualifiedName.Name);
        }

        internal void ReadWriteXmlTestInternal(string xml,
                               long testval,
                               string unit_test_id)
        {
            SqlInt64 test;
            SqlInt64 test1;
            XmlSerializer ser;
            StringWriter sw;
            XmlTextWriter xw;
            StringReader sr;
            XmlTextReader xr;

            test = new SqlInt64(testval);
            ser = new XmlSerializer(typeof(SqlInt64));
            sw = new StringWriter();
            xw = new XmlTextWriter(sw);

            ser.Serialize(xw, test);

            // Assert.Equal (xml, sw.ToString ());

            sr = new StringReader(xml);
            xr = new XmlTextReader(sr);
            test1 = (SqlInt64)ser.Deserialize(xr);

            Assert.Equal(testval, test1.Value);
        }

        [Fact]
        //[Category ("MobileNotWorking")]
        public void ReadWriteXmlTest()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><long>4556</long>";
            string xml2 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><long>-6445</long>";
            string xml3 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><long>0x455687AB3E4D56F</long>";
            long lngtest1 = 4556;
            long lngtest2 = -6445;
            long lngtest3 = 0x455687AB3E4D56F;

            ReadWriteXmlTestInternal(xml1, lngtest1, "BA01");
            ReadWriteXmlTestInternal(xml2, lngtest2, "BA02");

            try
            {
                ReadWriteXmlTestInternal(xml3, lngtest3, "BA03");
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
                Assert.Equal(typeof(FormatException), e.InnerException.GetType());
            }
        }
    }
}
