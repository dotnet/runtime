// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) 2002 Ville Palo
// (C) 2003 Martin Willemoes Hansen
//
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
using System.Xml.Serialization;
using System.IO;

namespace System.Data.Tests.SqlTypes
{
    public class SqlInt16Test
    {
        // Test constructor
        [Fact]
        public void Create()
        {
            SqlInt16 TestShort = new SqlInt16(29);
            Assert.Equal((short)29, TestShort.Value);

            TestShort = new SqlInt16(-9000);
            Assert.Equal((short)-9000, TestShort.Value);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.Equal((SqlInt16)32767, SqlInt16.MaxValue);
            Assert.Equal((SqlInt16)(-32768), SqlInt16.MinValue);
            Assert.True(SqlInt16.Null.IsNull);
            Assert.Equal((short)0, SqlInt16.Zero.Value);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            SqlInt16 Test5443 = new SqlInt16(5443);
            SqlInt16 Test1 = new SqlInt16(1);
            Assert.True(SqlInt16.Null.IsNull);
            Assert.Equal((short)5443, Test5443.Value);
            Assert.Equal((short)1, Test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlInt16 Test64 = new SqlInt16(64);
            SqlInt16 Test0 = new SqlInt16(0);
            SqlInt16 Test164 = new SqlInt16(164);
            SqlInt16 TestMax = new SqlInt16(SqlInt16.MaxValue.Value);

            // Add()
            Assert.Equal((short)64, SqlInt16.Add(Test64, Test0).Value);
            Assert.Equal((short)228, SqlInt16.Add(Test64, Test164).Value);
            Assert.Equal((short)164, SqlInt16.Add(Test0, Test164).Value);
            Assert.Equal((short)SqlInt16.MaxValue, SqlInt16.Add(TestMax, Test0).Value);

            try
            {
                SqlInt16.Add(TestMax, Test64);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Divide()
            Assert.Equal((short)2, SqlInt16.Divide(Test164, Test64).Value);
            Assert.Equal((short)0, SqlInt16.Divide(Test64, Test164).Value);
            try
            {
                SqlInt16.Divide(Test64, Test0);
                Assert.False(true);
            }
            catch (DivideByZeroException e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // Mod()
            Assert.Equal((SqlInt16)36, SqlInt16.Mod(Test164, Test64));
            Assert.Equal((SqlInt16)64, SqlInt16.Mod(Test64, Test164));

            // Multiply()
            Assert.Equal((short)10496, SqlInt16.Multiply(Test64, Test164).Value);
            Assert.Equal((short)0, SqlInt16.Multiply(Test64, Test0).Value);

            try
            {
                SqlInt16.Multiply(TestMax, Test64);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Subtract()
            Assert.Equal((short)100, SqlInt16.Subtract(Test164, Test64).Value);

            try
            {
                SqlInt16.Subtract(SqlInt16.MinValue, Test164);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Modulus ()
            Assert.Equal((SqlInt16)36, SqlInt16.Modulus(Test164, Test64));
            Assert.Equal((SqlInt16)64, SqlInt16.Modulus(Test64, Test164));
        }

        [Fact]
        public void BitwiseMethods()
        {
            short MaxValue = SqlInt16.MaxValue.Value;
            SqlInt16 TestInt = new SqlInt16(0);
            SqlInt16 TestIntMax = new SqlInt16(MaxValue);
            SqlInt16 TestInt2 = new SqlInt16(10922);
            SqlInt16 TestInt3 = new SqlInt16(21845);

            // BitwiseAnd
            Assert.Equal((short)21845, SqlInt16.BitwiseAnd(TestInt3, TestIntMax).Value);
            Assert.Equal((short)0, SqlInt16.BitwiseAnd(TestInt2, TestInt3).Value);
            Assert.Equal((short)10922, SqlInt16.BitwiseAnd(TestInt2, TestIntMax).Value);

            //BitwiseOr
            Assert.Equal(MaxValue, SqlInt16.BitwiseOr(TestInt2, TestInt3).Value);
            Assert.Equal((short)21845, SqlInt16.BitwiseOr(TestInt, TestInt3).Value);
            Assert.Equal(MaxValue, SqlInt16.BitwiseOr(TestIntMax, TestInt2).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlInt16 TestInt4000 = new SqlInt16(4000);
            SqlInt16 TestInt4000II = new SqlInt16(4000);
            SqlInt16 TestInt10 = new SqlInt16(10);
            SqlInt16 TestInt10000 = new SqlInt16(10000);
            SqlString TestString = new SqlString("This is a test");

            Assert.True(TestInt4000.CompareTo(TestInt10) > 0);
            Assert.True(TestInt10.CompareTo(TestInt4000) < 0);
            Assert.True(TestInt4000II.CompareTo(TestInt4000) == 0);
            Assert.True(TestInt4000II.CompareTo(SqlInt16.Null) > 0);

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
            SqlInt16 Test0 = new SqlInt16(0);
            SqlInt16 Test158 = new SqlInt16(158);
            SqlInt16 Test180 = new SqlInt16(180);
            SqlInt16 Test180II = new SqlInt16(180);

            Assert.True(!Test0.Equals(Test158));
            Assert.True(!Test158.Equals(Test180));
            Assert.True(!Test180.Equals(new SqlString("TEST")));
            Assert.True(Test180.Equals(Test180II));
        }

        [Fact]
        public void StaticEqualsMethod()
        {
            SqlInt16 Test34 = new SqlInt16(34);
            SqlInt16 Test34II = new SqlInt16(34);
            SqlInt16 Test15 = new SqlInt16(15);

            Assert.True(SqlInt16.Equals(Test34, Test34II).Value);
            Assert.True(!SqlInt16.Equals(Test34, Test15).Value);
            Assert.True(!SqlInt16.Equals(Test15, Test34II).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlInt16 Test15 = new SqlInt16(15);

            // FIXME: Better way to test GetHashCode()-methods
            Assert.Equal(Test15.GetHashCode(), Test15.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            SqlInt16 Test = new SqlInt16(84);
            Assert.Equal("System.Data.SqlTypes.SqlInt16", Test.GetType().ToString());
        }

        [Fact]
        public void Greaters()
        {
            SqlInt16 Test10 = new SqlInt16(10);
            SqlInt16 Test10II = new SqlInt16(10);
            SqlInt16 Test110 = new SqlInt16(110);

            // GreateThan ()
            Assert.True(!SqlInt16.GreaterThan(Test10, Test110).Value);
            Assert.True(SqlInt16.GreaterThan(Test110, Test10).Value);
            Assert.True(!SqlInt16.GreaterThan(Test10II, Test10).Value);

            // GreaterTharOrEqual ()
            Assert.True(!SqlInt16.GreaterThanOrEqual(Test10, Test110).Value);
            Assert.True(SqlInt16.GreaterThanOrEqual(Test110, Test10).Value);
            Assert.True(SqlInt16.GreaterThanOrEqual(Test10II, Test10).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlInt16 Test10 = new SqlInt16(10);
            SqlInt16 Test10II = new SqlInt16(10);
            SqlInt16 Test110 = new SqlInt16(110);

            // LessThan()
            Assert.True(SqlInt16.LessThan(Test10, Test110).Value);
            Assert.True(!SqlInt16.LessThan(Test110, Test10).Value);
            Assert.True(!SqlInt16.LessThan(Test10II, Test10).Value);

            // LessThanOrEqual ()
            Assert.True(SqlInt16.LessThanOrEqual(Test10, Test110).Value);
            Assert.True(!SqlInt16.LessThanOrEqual(Test110, Test10).Value);
            Assert.True(SqlInt16.LessThanOrEqual(Test10II, Test10).Value);
            Assert.True(SqlInt16.LessThanOrEqual(Test10II, SqlInt16.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlInt16 Test12 = new SqlInt16(12);
            SqlInt16 Test128 = new SqlInt16(128);
            SqlInt16 Test128II = new SqlInt16(128);

            Assert.True(SqlInt16.NotEquals(Test12, Test128).Value);
            Assert.True(SqlInt16.NotEquals(Test128, Test12).Value);
            Assert.True(SqlInt16.NotEquals(Test128II, Test12).Value);
            Assert.True(!SqlInt16.NotEquals(Test128II, Test128).Value);
            Assert.True(!SqlInt16.NotEquals(Test128, Test128II).Value);
            Assert.True(SqlInt16.NotEquals(SqlInt16.Null, Test128II).IsNull);
            Assert.True(SqlInt16.NotEquals(SqlInt16.Null, Test128II).IsNull);
        }

        [Fact]
        public void OnesComplement()
        {
            SqlInt16 Test12 = new SqlInt16(12);
            SqlInt16 Test128 = new SqlInt16(128);

            Assert.Equal((SqlInt16)(-13), SqlInt16.OnesComplement(Test12));
            Assert.Equal((SqlInt16)(-129), SqlInt16.OnesComplement(Test128));
        }

        [Fact]
        public void Parse()
        {
            try
            {
                SqlInt16.Parse(null);
                Assert.False(true);
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            try
            {
                SqlInt16.Parse("not-a-number");
                Assert.False(true);
            }
            catch (FormatException e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }

            try
            {
                int OverInt = (int)SqlInt16.MaxValue + 1;
                SqlInt16.Parse(OverInt.ToString());
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            Assert.Equal((short)150, SqlInt16.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlInt16 Test12 = new SqlInt16(12);
            SqlInt16 Test0 = new SqlInt16(0);
            SqlInt16 TestNull = SqlInt16.Null;
            SqlInt16 Test1000 = new SqlInt16(1000);
            SqlInt16 Test288 = new SqlInt16(288);

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

            // ToSqlInt64 ()
            Assert.Equal(12, Test12.ToSqlInt64().Value);
            Assert.Equal(0, Test0.ToSqlInt64().Value);
            Assert.Equal(288, Test288.ToSqlInt64().Value);

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
            SqlInt16 Test14 = new SqlInt16(14);
            SqlInt16 Test58 = new SqlInt16(58);
            SqlInt16 Test130 = new SqlInt16(130);
            SqlInt16 TestMax = new SqlInt16(SqlInt16.MaxValue.Value);
            SqlInt16 Test0 = new SqlInt16(0);

            Assert.Equal((short)52, SqlInt16.Xor(Test14, Test58).Value);
            Assert.Equal((short)140, SqlInt16.Xor(Test14, Test130).Value);
            Assert.Equal((short)184, SqlInt16.Xor(Test58, Test130).Value);
            Assert.Equal((short)0, SqlInt16.Xor(TestMax, TestMax).Value);
            Assert.Equal(TestMax.Value, SqlInt16.Xor(TestMax, Test0).Value);
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlInt16 Test24 = new SqlInt16(24);
            SqlInt16 Test64 = new SqlInt16(64);
            SqlInt16 Test2550 = new SqlInt16(2550);
            SqlInt16 Test0 = new SqlInt16(0);

            // "+"-operator
            Assert.Equal((SqlInt16)2614, Test2550 + Test64);
            try
            {
                SqlInt16 result = Test64 + SqlInt16.MaxValue;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "/"-operator
            Assert.Equal((SqlInt16)39, Test2550 / Test64);
            Assert.Equal((SqlInt16)0, Test24 / Test64);

            try
            {
                SqlInt16 result = Test2550 / Test0;
                Assert.False(true);
            }
            catch (DivideByZeroException e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // "*"-operator
            Assert.Equal((SqlInt16)1536, Test64 * Test24);

            try
            {
                SqlInt16 test = (SqlInt16.MaxValue * Test64);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "-"-operator
            Assert.Equal((SqlInt16)2526, Test2550 - Test24);

            try
            {
                SqlInt16 test = SqlInt16.MinValue - Test64;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "%"-operator
            Assert.Equal((SqlInt16)54, Test2550 % Test64);
            Assert.Equal((SqlInt16)24, Test24 % Test64);
            Assert.Equal((SqlInt16)0, new SqlInt16(100) % new SqlInt16(10));
        }

        [Fact]
        public void BitwiseOperators()
        {
            SqlInt16 Test2 = new SqlInt16(2);
            SqlInt16 Test4 = new SqlInt16(4);
            SqlInt16 Test2550 = new SqlInt16(2550);

            // & -operator
            Assert.Equal((SqlInt16)0, Test2 & Test4);
            Assert.Equal((SqlInt16)2, Test2 & Test2550);
            Assert.Equal((SqlInt16)0, SqlInt16.MaxValue & SqlInt16.MinValue);

            // | -operator
            Assert.Equal((SqlInt16)6, Test2 | Test4);
            Assert.Equal((SqlInt16)2550, Test2 | Test2550);
            Assert.Equal((SqlInt16)(-1), SqlInt16.MinValue | SqlInt16.MaxValue);

            //  ^ -operator
            Assert.Equal((SqlInt16)2546, (Test2550 ^ Test4));
            Assert.Equal((SqlInt16)6, (Test2 ^ Test4));
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlInt16 Test165 = new SqlInt16(165);
            SqlInt16 Test100 = new SqlInt16(100);
            SqlInt16 Test100II = new SqlInt16(100);
            SqlInt16 Test255 = new SqlInt16(2550);

            // == -operator
            Assert.True((Test100 == Test100II).Value);
            Assert.True(!(Test165 == Test100).Value);
            Assert.True((Test165 == SqlInt16.Null).IsNull);

            // != -operator
            Assert.True(!(Test100 != Test100II).Value);
            Assert.True((Test100 != Test255).Value);
            Assert.True((Test165 != Test255).Value);
            Assert.True((Test165 != SqlInt16.Null).IsNull);

            // > -operator
            Assert.True((Test165 > Test100).Value);
            Assert.True(!(Test165 > Test255).Value);
            Assert.True(!(Test100 > Test100II).Value);
            Assert.True((Test165 > SqlInt16.Null).IsNull);

            // >=  -operator
            Assert.True(!(Test165 >= Test255).Value);
            Assert.True((Test255 >= Test165).Value);
            Assert.True((Test100 >= Test100II).Value);
            Assert.True((Test165 >= SqlInt16.Null).IsNull);

            // < -operator
            Assert.True(!(Test165 < Test100).Value);
            Assert.True((Test165 < Test255).Value);
            Assert.True(!(Test100 < Test100II).Value);
            Assert.True((Test165 < SqlInt16.Null).IsNull);

            // <= -operator
            Assert.True((Test165 <= Test255).Value);
            Assert.True(!(Test255 <= Test165).Value);
            Assert.True((Test100 <= Test100II).Value);
            Assert.True((Test165 <= SqlInt16.Null).IsNull);
        }

        [Fact]
        public void OnesComplementOperator()
        {
            SqlInt16 Test12 = new SqlInt16(12);
            SqlInt16 Test128 = new SqlInt16(128);

            Assert.Equal((SqlInt16)(-13), ~Test12);
            Assert.Equal((SqlInt16)(-129), ~Test128);
            Assert.Equal(SqlInt16.Null, ~SqlInt16.Null);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlInt16 Test = new SqlInt16(2000);
            SqlInt16 TestNeg = new SqlInt16(-3000);

            SqlInt16 Result = -Test;
            Assert.Equal((short)(-2000), Result.Value);

            Result = -TestNeg;
            Assert.Equal((short)3000, Result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlInt16()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);
            SqlInt16 Result;

            Result = (SqlInt16)TestBoolean;

            Assert.Equal((short)1, Result.Value);

            Result = (SqlInt16)SqlBoolean.Null;
            Assert.True(Result.IsNull);
        }

        [Fact]
        public void SqlDecimalToSqlInt16()
        {
            SqlDecimal TestDecimal64 = new SqlDecimal(64);
            SqlDecimal TestDecimal900 = new SqlDecimal(90000);

            Assert.Equal((short)64, ((SqlInt16)TestDecimal64).Value);
            Assert.Equal(SqlInt16.Null, ((SqlInt16)SqlDecimal.Null));

            try
            {
                SqlInt16 test = (SqlInt16)TestDecimal900;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlDoubleToSqlInt16()
        {
            SqlDouble TestDouble64 = new SqlDouble(64);
            SqlDouble TestDouble900 = new SqlDouble(90000);

            Assert.Equal((short)64, ((SqlInt16)TestDouble64).Value);
            Assert.Equal(SqlInt16.Null, ((SqlInt16)SqlDouble.Null));

            try
            {
                SqlInt16 test = (SqlInt16)TestDouble900;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlIntToInt16()
        {
            SqlInt16 Test = new SqlInt16(12);
            short Result = (short)Test;
            Assert.Equal((short)12, Result);
        }

        [Fact]
        public void SqlInt32ToSqlInt16()
        {
            SqlInt32 Test64 = new SqlInt32(64);
            SqlInt32 Test900 = new SqlInt32(90000);

            Assert.Equal((short)64, ((SqlInt16)Test64).Value);

            try
            {
                SqlInt16 test = (SqlInt16)Test900;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt64ToSqlInt16()
        {
            SqlInt64 Test64 = new SqlInt64(64);
            SqlInt64 Test900 = new SqlInt64(90000);

            Assert.Equal((short)64, ((SqlInt16)Test64).Value);

            try
            {
                SqlInt16 test = (SqlInt16)Test900;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlMoneyToSqlInt16()
        {
            SqlMoney TestMoney64 = new SqlMoney(64);
            SqlMoney TestMoney900 = new SqlMoney(90000);

            Assert.Equal((short)64, ((SqlInt16)TestMoney64).Value);

            try
            {
                SqlInt16 test = (SqlInt16)TestMoney900;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlSingleToSqlInt16()
        {
            SqlSingle TestSingle64 = new SqlSingle(64);
            SqlSingle TestSingle900 = new SqlSingle(90000);

            Assert.Equal((short)64, ((SqlInt16)TestSingle64).Value);

            try
            {
                SqlInt16 test = (SqlInt16)TestSingle900;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void SqlStringToSqlInt16()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");
            SqlString TestString1000 = new SqlString("100000");

            Assert.Equal((short)100, ((SqlInt16)TestString100).Value);

            try
            {
                SqlInt16 test = (SqlInt16)TestString1000;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            try
            {
                SqlInt16 test = (SqlInt16)TestString;
                Assert.False(true);
            }
            catch (FormatException e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }
        }

        [Fact]
        public void ByteToSqlInt16()
        {
            short TestShort = 14;
            Assert.Equal((short)14, ((SqlInt16)TestShort).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlInt16.GetXsdType(null);
            Assert.Equal("short", qualifiedName.Name);
        }

        internal void ReadWriteXmlTestInternal(string xml,
                               short testval,
                               string unit_test_id)
        {
            SqlInt16 test;
            SqlInt16 test1;
            XmlSerializer ser;
            StringWriter sw;
            XmlTextWriter xw;
            StringReader sr;
            XmlTextReader xr;

            test = new SqlInt16(testval);
            ser = new XmlSerializer(typeof(SqlInt16));
            sw = new StringWriter();
            xw = new XmlTextWriter(sw);

            ser.Serialize(xw, test);

            // Assert.Equal (xml, sw.ToString ());

            sr = new StringReader(xml);
            xr = new XmlTextReader(sr);
            test1 = (SqlInt16)ser.Deserialize(xr);

            Assert.Equal(testval, test1.Value);
        }

        [Fact]
        //[Category ("MobileNotWorking")]
        public void ReadWriteXmlTest()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><short>4556</short>";
            string xml2 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><short>-6445</short>";
            string xml3 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><short>0x455687AB3E4D56F</short>";
            short test1 = 4556;
            short test2 = -6445;
            short test3 = 0x4F56;

            ReadWriteXmlTestInternal(xml1, test1, "BA01");
            ReadWriteXmlTestInternal(xml2, test2, "BA02");

            try
            {
                ReadWriteXmlTestInternal(xml3, test3, "BA03");
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
                Assert.Equal(typeof(FormatException), e.InnerException.GetType());
            }
        }
    }
}
