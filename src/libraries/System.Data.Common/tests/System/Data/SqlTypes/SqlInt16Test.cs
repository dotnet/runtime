// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            SqlInt16 testShort = new SqlInt16(29);
            Assert.Equal((short)29, testShort.Value);

            testShort = new SqlInt16(-9000);
            Assert.Equal((short)-9000, testShort.Value);
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
            SqlInt16 test5443 = new SqlInt16(5443);
            SqlInt16 test1 = new SqlInt16(1);
            Assert.True(SqlInt16.Null.IsNull);
            Assert.Equal((short)5443, test5443.Value);
            Assert.Equal((short)1, test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlInt16 test64 = new SqlInt16(64);
            SqlInt16 test0 = new SqlInt16(0);
            SqlInt16 test164 = new SqlInt16(164);
            SqlInt16 testMax = new SqlInt16(SqlInt16.MaxValue.Value);

            // Add()
            Assert.Equal((short)64, SqlInt16.Add(test64, test0).Value);
            Assert.Equal((short)228, SqlInt16.Add(test64, test164).Value);
            Assert.Equal((short)164, SqlInt16.Add(test0, test164).Value);
            Assert.Equal((short)SqlInt16.MaxValue, SqlInt16.Add(testMax, test0).Value);

            Assert.Throws<OverflowException>(() => SqlInt16.Add(testMax, test64));

            // Divide()
            Assert.Equal((short)2, SqlInt16.Divide(test164, test64).Value);
            Assert.Equal((short)0, SqlInt16.Divide(test64, test164).Value);

            Assert.Throws<DivideByZeroException>(() => SqlInt16.Divide(test64, test0));

            // Mod()
            Assert.Equal((SqlInt16)36, SqlInt16.Mod(test164, test64));
            Assert.Equal((SqlInt16)64, SqlInt16.Mod(test64, test164));

            // Multiply()
            Assert.Equal((short)10496, SqlInt16.Multiply(test64, test164).Value);
            Assert.Equal((short)0, SqlInt16.Multiply(test64, test0).Value);

            Assert.Throws<OverflowException>(() => SqlInt16.Multiply(testMax, test64));

            // Subtract()
            Assert.Equal((short)100, SqlInt16.Subtract(test164, test64).Value);

            Assert.Throws<OverflowException>(() => SqlInt16.Subtract(SqlInt16.MinValue, test164));

            // Modulus ()
            Assert.Equal((SqlInt16)36, SqlInt16.Modulus(test164, test64));
            Assert.Equal((SqlInt16)64, SqlInt16.Modulus(test64, test164));
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
            SqlInt16 testInt4000 = new SqlInt16(4000);
            SqlInt16 testInt4000II = new SqlInt16(4000);
            SqlInt16 testInt10 = new SqlInt16(10);
            SqlInt16 testInt10000 = new SqlInt16(10000);
            SqlString testString = new SqlString("This is a test");

            Assert.True(testInt4000.CompareTo(testInt10) > 0);
            Assert.True(testInt10.CompareTo(testInt4000) < 0);
            Assert.Equal(0, testInt4000II.CompareTo(testInt4000));
            Assert.True(testInt4000II.CompareTo(SqlInt16.Null) > 0);

            Assert.Throws<ArgumentException>(() => testInt10.CompareTo(testString));
        }

        [Fact]
        public void EqualsMethod()
        {
            SqlInt16 test0 = new SqlInt16(0);
            SqlInt16 test158 = new SqlInt16(158);
            SqlInt16 test180 = new SqlInt16(180);
            SqlInt16 test180II = new SqlInt16(180);

            Assert.False(test0.Equals(test158));
            Assert.False(test0.Equals((object)test158));
            Assert.False(test158.Equals(test180));
            Assert.False(test158.Equals((object)test180));
            Assert.False(test180.Equals(new SqlString("TEST")));
            Assert.True(test180.Equals(test180II));
            Assert.True(test180.Equals((object)test180II));
        }

        [Fact]
        public void StaticEqualsMethod()
        {
            SqlInt16 test34 = new SqlInt16(34);
            SqlInt16 test34II = new SqlInt16(34);
            SqlInt16 test15 = new SqlInt16(15);

            Assert.True(SqlInt16.Equals(test34, test34II).Value);
            Assert.False(SqlInt16.Equals(test34, test15).Value);
            Assert.False(SqlInt16.Equals(test15, test34II).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlInt16 test15 = new SqlInt16(15);

            // FIXME: Better way to test GetHashCode()-methods
            Assert.Equal(test15.GetHashCode(), test15.GetHashCode());
        }

        [Fact]
        public void Greaters()
        {
            SqlInt16 test10 = new SqlInt16(10);
            SqlInt16 test10II = new SqlInt16(10);
            SqlInt16 test110 = new SqlInt16(110);

            // GreateThan ()
            Assert.False(SqlInt16.GreaterThan(test10, test110).Value);
            Assert.True(SqlInt16.GreaterThan(test110, test10).Value);
            Assert.False(SqlInt16.GreaterThan(test10II, test10).Value);

            // GreaterTharOrEqual ()
            Assert.False(SqlInt16.GreaterThanOrEqual(test10, test110).Value);
            Assert.True(SqlInt16.GreaterThanOrEqual(test110, test10).Value);
            Assert.True(SqlInt16.GreaterThanOrEqual(test10II, test10).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlInt16 test10 = new SqlInt16(10);
            SqlInt16 test10II = new SqlInt16(10);
            SqlInt16 test110 = new SqlInt16(110);

            // LessThan()
            Assert.True(SqlInt16.LessThan(test10, test110).Value);
            Assert.False(SqlInt16.LessThan(test110, test10).Value);
            Assert.False(SqlInt16.LessThan(test10II, test10).Value);

            // LessThanOrEqual ()
            Assert.True(SqlInt16.LessThanOrEqual(test10, test110).Value);
            Assert.False(SqlInt16.LessThanOrEqual(test110, test10).Value);
            Assert.True(SqlInt16.LessThanOrEqual(test10II, test10).Value);
            Assert.True(SqlInt16.LessThanOrEqual(test10II, SqlInt16.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlInt16 test12 = new SqlInt16(12);
            SqlInt16 test128 = new SqlInt16(128);
            SqlInt16 test128II = new SqlInt16(128);

            Assert.True(SqlInt16.NotEquals(test12, test128).Value);
            Assert.True(SqlInt16.NotEquals(test128, test12).Value);
            Assert.True(SqlInt16.NotEquals(test128II, test12).Value);
            Assert.False(SqlInt16.NotEquals(test128II, test128).Value);
            Assert.False(SqlInt16.NotEquals(test128, test128II).Value);
            Assert.True(SqlInt16.NotEquals(SqlInt16.Null, test128II).IsNull);
            Assert.True(SqlInt16.NotEquals(SqlInt16.Null, test128II).IsNull);
        }

        [Fact]
        public void OnesComplement()
        {
            SqlInt16 test12 = new SqlInt16(12);
            SqlInt16 test128 = new SqlInt16(128);

            Assert.Equal((SqlInt16)(-13), SqlInt16.OnesComplement(test12));
            Assert.Equal((SqlInt16)(-129), SqlInt16.OnesComplement(test128));
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlInt16.Parse(null));

            Assert.Throws<FormatException>(() => SqlInt16.Parse("not-a-number"));

            Assert.Throws<OverflowException>(() => SqlInt16.Parse(((int)SqlInt16.MaxValue + 1).ToString()));

            Assert.Equal((short)150, SqlInt16.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlInt16 test12 = new SqlInt16(12);
            SqlInt16 test0 = new SqlInt16(0);
            SqlInt16 testNull = SqlInt16.Null;
            SqlInt16 test1000 = new SqlInt16(1000);
            SqlInt16 test288 = new SqlInt16(288);

            // ToSqlBoolean ()
            Assert.True(test12.ToSqlBoolean().Value);
            Assert.False(test0.ToSqlBoolean().Value);
            Assert.True(testNull.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)12, test12.ToSqlByte().Value);
            Assert.Equal((byte)0, test0.ToSqlByte().Value);

            Assert.Throws<OverflowException>(() => test1000.ToSqlByte());

            // ToSqlDecimal ()
            Assert.Equal(12, test12.ToSqlDecimal().Value);
            Assert.Equal(0, test0.ToSqlDecimal().Value);
            Assert.Equal(288, test288.ToSqlDecimal().Value);

            // ToSqlDouble ()
            Assert.Equal(12, test12.ToSqlDouble().Value);
            Assert.Equal(0, test0.ToSqlDouble().Value);
            Assert.Equal(1000, test1000.ToSqlDouble().Value);

            // ToSqlInt32 ()
            Assert.Equal(12, test12.ToSqlInt32().Value);
            Assert.Equal(0, test0.ToSqlInt32().Value);
            Assert.Equal(288, test288.ToSqlInt32().Value);

            // ToSqlInt64 ()
            Assert.Equal(12, test12.ToSqlInt64().Value);
            Assert.Equal(0, test0.ToSqlInt64().Value);
            Assert.Equal(288, test288.ToSqlInt64().Value);

            // ToSqlMoney ()
            Assert.Equal(12.0000M, test12.ToSqlMoney().Value);
            Assert.Equal(0, test0.ToSqlMoney().Value);
            Assert.Equal(288.0000M, test288.ToSqlMoney().Value);

            // ToSqlSingle ()
            Assert.Equal(12, test12.ToSqlSingle().Value);
            Assert.Equal(0, test0.ToSqlSingle().Value);
            Assert.Equal(288, test288.ToSqlSingle().Value);

            // ToSqlString ()
            Assert.Equal("12", test12.ToSqlString().Value);
            Assert.Equal("0", test0.ToSqlString().Value);
            Assert.Equal("288", test288.ToSqlString().Value);

            // ToString ()
            Assert.Equal("12", test12.ToString());
            Assert.Equal("0", test0.ToString());
            Assert.Equal("288", test288.ToString());
        }

        [Fact]
        public void Xor()
        {
            SqlInt16 test14 = new SqlInt16(14);
            SqlInt16 test58 = new SqlInt16(58);
            SqlInt16 test130 = new SqlInt16(130);
            SqlInt16 testMax = new SqlInt16(SqlInt16.MaxValue.Value);
            SqlInt16 test0 = new SqlInt16(0);

            Assert.Equal((short)52, SqlInt16.Xor(test14, test58).Value);
            Assert.Equal((short)140, SqlInt16.Xor(test14, test130).Value);
            Assert.Equal((short)184, SqlInt16.Xor(test58, test130).Value);
            Assert.Equal((short)0, SqlInt16.Xor(testMax, testMax).Value);
            Assert.Equal(testMax.Value, SqlInt16.Xor(testMax, test0).Value);
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlInt16 test24 = new SqlInt16(24);
            SqlInt16 test64 = new SqlInt16(64);
            SqlInt16 test2550 = new SqlInt16(2550);
            SqlInt16 test0 = new SqlInt16(0);

            // "+"-operator
            Assert.Equal((SqlInt16)2614, test2550 + test64);
            Assert.Throws<OverflowException>(() => test64 + SqlInt16.MaxValue);

            // "/"-operator
            Assert.Equal((SqlInt16)39, test2550 / test64);
            Assert.Equal((SqlInt16)0, test24 / test64);

            Assert.Throws<DivideByZeroException>(() => test2550 / test0);

            // "*"-operator
            Assert.Equal((SqlInt16)1536, test64 * test24);

            Assert.Throws<OverflowException>(() => SqlInt16.MaxValue * test64);
            // "-"-operator
            Assert.Equal((SqlInt16)2526, test2550 - test24);

            Assert.Throws<OverflowException>(() => SqlInt16.MinValue - test64);
            // "%"-operator
            Assert.Equal((SqlInt16)54, test2550 % test64);
            Assert.Equal((SqlInt16)24, test24 % test64);
            Assert.Equal((SqlInt16)0, new SqlInt16(100) % new SqlInt16(10));
        }

        [Fact]
        public void BitwiseOperators()
        {
            SqlInt16 test2 = new SqlInt16(2);
            SqlInt16 test4 = new SqlInt16(4);
            SqlInt16 test2550 = new SqlInt16(2550);

            // & -operator
            Assert.Equal((SqlInt16)0, test2 & test4);
            Assert.Equal((SqlInt16)2, test2 & test2550);
            Assert.Equal((SqlInt16)0, SqlInt16.MaxValue & SqlInt16.MinValue);

            // | -operator
            Assert.Equal((SqlInt16)6, test2 | test4);
            Assert.Equal((SqlInt16)2550, test2 | test2550);
            Assert.Equal((SqlInt16)(-1), SqlInt16.MinValue | SqlInt16.MaxValue);

            //  ^ -operator
            Assert.Equal((SqlInt16)2546, (test2550 ^ test4));
            Assert.Equal((SqlInt16)6, (test2 ^ test4));
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlInt16 test165 = new SqlInt16(165);
            SqlInt16 test100 = new SqlInt16(100);
            SqlInt16 test100II = new SqlInt16(100);
            SqlInt16 test255 = new SqlInt16(2550);

            // == -operator
            Assert.True((test100 == test100II).Value);
            Assert.False((test165 == test100).Value);
            Assert.True((test165 == SqlInt16.Null).IsNull);

            // != -operator
            Assert.False((test100 != test100II).Value);
            Assert.True((test100 != test255).Value);
            Assert.True((test165 != test255).Value);
            Assert.True((test165 != SqlInt16.Null).IsNull);

            // > -operator
            Assert.True((test165 > test100).Value);
            Assert.False((test165 > test255).Value);
            Assert.False((test100 > test100II).Value);
            Assert.True((test165 > SqlInt16.Null).IsNull);

            // >=  -operator
            Assert.False((test165 >= test255).Value);
            Assert.True((test255 >= test165).Value);
            Assert.True((test100 >= test100II).Value);
            Assert.True((test165 >= SqlInt16.Null).IsNull);

            // < -operator
            Assert.False((test165 < test100).Value);
            Assert.True((test165 < test255).Value);
            Assert.False((test100 < test100II).Value);
            Assert.True((test165 < SqlInt16.Null).IsNull);

            // <= -operator
            Assert.True((test165 <= test255).Value);
            Assert.False((test255 <= test165).Value);
            Assert.True((test100 <= test100II).Value);
            Assert.True((test165 <= SqlInt16.Null).IsNull);
        }

        [Fact]
        public void OnesComplementOperator()
        {
            SqlInt16 test12 = new SqlInt16(12);
            SqlInt16 test128 = new SqlInt16(128);

            Assert.Equal((SqlInt16)(-13), ~test12);
            Assert.Equal((SqlInt16)(-129), ~test128);
            Assert.Equal(SqlInt16.Null, ~SqlInt16.Null);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlInt16 test = new SqlInt16(2000);
            SqlInt16 testNeg = new SqlInt16(-3000);

            SqlInt16 result = -test;
            Assert.Equal((short)(-2000), result.Value);

            result = -testNeg;
            Assert.Equal((short)3000, result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlInt16()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlInt16 result;

            result = (SqlInt16)testBoolean;

            Assert.Equal((short)1, result.Value);

            result = (SqlInt16)SqlBoolean.Null;
            Assert.True(result.IsNull);
        }

        [Fact]
        public void SqlDecimalToSqlInt16()
        {
            SqlDecimal testDecimal64 = new SqlDecimal(64);
            SqlDecimal testDecimal900 = new SqlDecimal(90000);

            Assert.Equal((short)64, ((SqlInt16)testDecimal64).Value);
            Assert.Equal(SqlInt16.Null, ((SqlInt16)SqlDecimal.Null));

            Assert.Throws<OverflowException>(() => (SqlInt16)testDecimal900);
        }

        [Fact]
        public void SqlDoubleToSqlInt16()
        {
            SqlDouble testDouble64 = new SqlDouble(64);
            SqlDouble testDouble900 = new SqlDouble(90000);

            Assert.Equal((short)64, ((SqlInt16)testDouble64).Value);
            Assert.Equal(SqlInt16.Null, ((SqlInt16)SqlDouble.Null));

            Assert.Throws<OverflowException>(() => (SqlInt16)testDouble900);
        }

        [Fact]
        public void SqlIntToInt16()
        {
            SqlInt16 test = new SqlInt16(12);
            short result = (short)test;
            Assert.Equal((short)12, result);
        }

        [Fact]
        public void SqlInt32ToSqlInt16()
        {
            SqlInt32 test64 = new SqlInt32(64);
            SqlInt32 test900 = new SqlInt32(90000);

            Assert.Equal((short)64, ((SqlInt16)test64).Value);

            Assert.Throws<OverflowException>(() =>(SqlInt16)test900);
        }

        [Fact]
        public void SqlInt64ToSqlInt16()
        {
            SqlInt64 test64 = new SqlInt64(64);
            SqlInt64 test900 = new SqlInt64(90000);

            Assert.Equal((short)64, ((SqlInt16)test64).Value);

            Assert.Throws<OverflowException>(() => (SqlInt16)test900);
        }

        [Fact]
        public void SqlMoneyToSqlInt16()
        {
            SqlMoney testMoney64 = new SqlMoney(64);
            SqlMoney testMoney900 = new SqlMoney(90000);

            Assert.Equal((short)64, ((SqlInt16)testMoney64).Value);

            Assert.Throws<OverflowException>(() => (SqlInt16)testMoney900);
        }

        [Fact]
        public void SqlSingleToSqlInt16()
        {
            SqlSingle testSingle64 = new SqlSingle(64);
            SqlSingle testSingle900 = new SqlSingle(90000);

            Assert.Equal((short)64, ((SqlInt16)testSingle64).Value);

            Assert.Throws<OverflowException>(() => (SqlInt16)testSingle900);
        }

        [Fact]
        public void SqlStringToSqlInt16()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("100");
            SqlString testString1000 = new SqlString("100000");

            Assert.Equal((short)100, ((SqlInt16)testString100).Value);

            Assert.Throws<OverflowException>(() => (SqlInt16)testString1000);

            Assert.Throws<FormatException>(() => (SqlInt16)testString);
        }

        [Fact]
        public void ByteToSqlInt16()
        {
            short testShort = 14;
            Assert.Equal((short)14, ((SqlInt16)testShort).Value);
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

            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => ReadWriteXmlTestInternal(xml3, test3, "BA03"));
            Assert.Equal(typeof(FormatException), ex.InnerException.GetType());
        }
    }
}
