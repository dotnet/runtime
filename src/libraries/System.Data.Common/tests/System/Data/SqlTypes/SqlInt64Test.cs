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
            SqlInt64 test5443 = new SqlInt64(5443);
            SqlInt64 test1 = new SqlInt64(1);

            Assert.True(SqlInt64.Null.IsNull);
            Assert.Equal(5443, test5443.Value);
            Assert.Equal(1, test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlInt64 test64 = new SqlInt64(64);
            SqlInt64 test0 = new SqlInt64(0);
            SqlInt64 test164 = new SqlInt64(164);
            SqlInt64 testMax = new SqlInt64(SqlInt64.MaxValue.Value);

            // Add()
            Assert.Equal(64, SqlInt64.Add(test64, test0).Value);
            Assert.Equal(228, SqlInt64.Add(test64, test164).Value);
            Assert.Equal(164, SqlInt64.Add(test0, test164).Value);
            Assert.Equal((long)SqlInt64.MaxValue, SqlInt64.Add(testMax, test0).Value);

            Assert.Throws<OverflowException>(() => SqlInt64.Add(testMax, test64));

            // Divide()
            Assert.Equal(2, SqlInt64.Divide(test164, test64).Value);
            Assert.Equal(0, SqlInt64.Divide(test64, test164).Value);

            Assert.Throws<DivideByZeroException>(() => SqlInt64.Divide(test64, test0));

            // Mod()
            Assert.Equal(36, SqlInt64.Mod(test164, test64));
            Assert.Equal(64, SqlInt64.Mod(test64, test164));

            // Multiply()
            Assert.Equal(10496, SqlInt64.Multiply(test64, test164).Value);
            Assert.Equal(0, SqlInt64.Multiply(test64, test0).Value);

            Assert.Throws<OverflowException>(() => SqlInt64.Multiply(testMax, test64));

            // Subtract()
            Assert.Equal(100, SqlInt64.Subtract(test164, test64).Value);

            Assert.Throws<OverflowException>(() => SqlInt64.Subtract(SqlInt64.MinValue, test164));

            // Modulus ()
            Assert.Equal(36, SqlInt64.Modulus(test164, test64));
            Assert.Equal(64, SqlInt64.Modulus(test64, test164));
        }

        [Fact]
        public void BitwiseMethods()
        {
            long MaxValue = SqlInt64.MaxValue.Value;
            SqlInt64 testInt = new SqlInt64(0);
            SqlInt64 testIntMax = new SqlInt64(MaxValue);
            SqlInt64 testInt2 = new SqlInt64(10922);
            SqlInt64 testInt3 = new SqlInt64(21845);

            // BitwiseAnd
            Assert.Equal(21845, SqlInt64.BitwiseAnd(testInt3, testIntMax).Value);
            Assert.Equal(0, SqlInt64.BitwiseAnd(testInt2, testInt3).Value);
            Assert.Equal(10922, SqlInt64.BitwiseAnd(testInt2, testIntMax).Value);

            //BitwiseOr
            Assert.Equal(21845, SqlInt64.BitwiseOr(testInt, testInt3).Value);
            Assert.Equal(MaxValue, SqlInt64.BitwiseOr(testIntMax, testInt2).Value);
        }

        [Fact]
        public void CompareTo()
        {
            SqlInt64 testInt4000 = new SqlInt64(4000);
            SqlInt64 testInt4000II = new SqlInt64(4000);
            SqlInt64 testInt10 = new SqlInt64(10);
            SqlInt64 testInt10000 = new SqlInt64(10000);
            SqlString testString = new SqlString("This is a test");

            Assert.True(testInt4000.CompareTo(testInt10) > 0);
            Assert.True(testInt10.CompareTo(testInt4000) < 0);
            Assert.Equal(0, testInt4000II.CompareTo(testInt4000));
            Assert.True(testInt4000II.CompareTo(SqlInt64.Null) > 0);

            Assert.Throws<ArgumentException>(() => testInt10.CompareTo(testString));
        }

        [Fact]
        public void EqualsMethod()
        {
            SqlInt64 test0 = new SqlInt64(0);
            SqlInt64 test158 = new SqlInt64(158);
            SqlInt64 test180 = new SqlInt64(180);
            SqlInt64 test180II = new SqlInt64(180);

            Assert.False(test0.Equals(test158));
            Assert.False(test158.Equals(test180));
            Assert.False(test180.Equals(new SqlString("TEST")));
            Assert.True(test180.Equals(test180II));
        }

        [Fact]
        public void StaticEqualsMethod()
        {
            SqlInt64 test34 = new SqlInt64(34);
            SqlInt64 test34II = new SqlInt64(34);
            SqlInt64 test15 = new SqlInt64(15);

            Assert.True(SqlInt64.Equals(test34, test34II).Value);
            Assert.False(SqlInt64.Equals(test34, test15).Value);
            Assert.False(SqlInt64.Equals(test15, test34II).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlInt64 test15 = new SqlInt64(15);

            // FIXME: Better way to test HashCode
            Assert.Equal(15, test15.GetHashCode());
        }

        [Fact]
        public void Greaters()
        {
            SqlInt64 test10 = new SqlInt64(10);
            SqlInt64 test10II = new SqlInt64(10);
            SqlInt64 test110 = new SqlInt64(110);

            // GreateThan ()
            Assert.False(SqlInt64.GreaterThan(test10, test110).Value);
            Assert.True(SqlInt64.GreaterThan(test110, test10).Value);
            Assert.False(SqlInt64.GreaterThan(test10II, test10).Value);

            // GreaterTharOrEqual ()
            Assert.False(SqlInt64.GreaterThanOrEqual(test10, test110).Value);
            Assert.True(SqlInt64.GreaterThanOrEqual(test110, test10).Value);
            Assert.True(SqlInt64.GreaterThanOrEqual(test10II, test10).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlInt64 test10 = new SqlInt64(10);
            SqlInt64 test10II = new SqlInt64(10);
            SqlInt64 test110 = new SqlInt64(110);

            // LessThan()
            Assert.True(SqlInt64.LessThan(test10, test110).Value);
            Assert.False(SqlInt64.LessThan(test110, test10).Value);
            Assert.False(SqlInt64.LessThan(test10II, test10).Value);

            // LessThanOrEqual ()
            Assert.True(SqlInt64.LessThanOrEqual(test10, test110).Value);
            Assert.False(SqlInt64.LessThanOrEqual(test110, test10).Value);
            Assert.True(SqlInt64.LessThanOrEqual(test10II, test10).Value);
            Assert.True(SqlInt64.LessThanOrEqual(test10II, SqlInt64.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlInt64 test12 = new SqlInt64(12);
            SqlInt64 test128 = new SqlInt64(128);
            SqlInt64 test128II = new SqlInt64(128);

            Assert.True(SqlInt64.NotEquals(test12, test128).Value);
            Assert.True(SqlInt64.NotEquals(test128, test12).Value);
            Assert.True(SqlInt64.NotEquals(test128II, test12).Value);
            Assert.False(SqlInt64.NotEquals(test128II, test128).Value);
            Assert.False(SqlInt64.NotEquals(test128, test128II).Value);
            Assert.True(SqlInt64.NotEquals(SqlInt64.Null, test128II).IsNull);
            Assert.True(SqlInt64.NotEquals(SqlInt64.Null, test128II).IsNull);
        }

        [Fact]
        public void OnesComplement()
        {
            SqlInt64 test12 = new SqlInt64(12);
            SqlInt64 test128 = new SqlInt64(128);

            Assert.Equal(-13, SqlInt64.OnesComplement(test12));
            Assert.Equal(-129, SqlInt64.OnesComplement(test128));
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlInt64.Parse(null));

            Assert.Throws<FormatException>(() => SqlInt64.Parse("not-a-number"));

            Assert.Throws<OverflowException>(() => SqlInt64.Parse("1000000000000000000000000000"));

            Assert.Equal(150, SqlInt64.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlInt64 test12 = new SqlInt64(12);
            SqlInt64 test0 = new SqlInt64(0);
            SqlInt64 TestNull = SqlInt64.Null;
            SqlInt64 test1000 = new SqlInt64(1000);
            SqlInt64 test288 = new SqlInt64(288);

            // ToSqlBoolean ()
            Assert.True(test12.ToSqlBoolean().Value);
            Assert.False(test0.ToSqlBoolean().Value);
            Assert.True(TestNull.ToSqlBoolean().IsNull);

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

            // ToSqlInt16 ()
            Assert.Equal((short)12, test12.ToSqlInt16().Value);
            Assert.Equal((short)0, test0.ToSqlInt16().Value);
            Assert.Equal((short)288, test288.ToSqlInt16().Value);

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
            SqlInt64 test14 = new SqlInt64(14);
            SqlInt64 test58 = new SqlInt64(58);
            SqlInt64 test130 = new SqlInt64(130);
            SqlInt64 testMax = new SqlInt64(SqlInt64.MaxValue.Value);
            SqlInt64 test0 = new SqlInt64(0);

            Assert.Equal(52, SqlInt64.Xor(test14, test58).Value);
            Assert.Equal(140, SqlInt64.Xor(test14, test130).Value);
            Assert.Equal(184, SqlInt64.Xor(test58, test130).Value);
            Assert.Equal(0, SqlInt64.Xor(testMax, testMax).Value);
            Assert.Equal(testMax.Value, SqlInt64.Xor(testMax, test0).Value);
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlInt64 test24 = new SqlInt64(24);
            SqlInt64 test64 = new SqlInt64(64);
            SqlInt64 test2550 = new SqlInt64(2550);
            SqlInt64 test0 = new SqlInt64(0);

            // "+"-operator
            Assert.Equal(2614, test2550 + test64);
            Assert.Throws<OverflowException>(() => test64 + SqlInt64.MaxValue);


            // "/"-operator
            Assert.Equal(39, test2550 / test64);
            Assert.Equal(0, test24 / test64);

            Assert.Throws<DivideByZeroException>(() => test2550 / test0);

            // "*"-operator
            Assert.Equal(1536, test64 * test24);

            Assert.Throws<OverflowException>(() => SqlInt64.MaxValue * test64);

            // "-"-operator
            Assert.Equal(2526, test2550 - test24);

            Assert.Throws<OverflowException>(() => SqlInt64.MinValue - test64);

            // "%"-operator
            Assert.Equal(54, test2550 % test64);
            Assert.Equal(24, test24 % test64);
            Assert.Equal(0, new SqlInt64(100) % new SqlInt64(10));
        }

        [Fact]
        public void BitwiseOperators()
        {
            SqlInt64 test2 = new SqlInt64(2);
            SqlInt64 test4 = new SqlInt64(4);

            SqlInt64 test2550 = new SqlInt64(2550);

            // & -operator
            Assert.Equal(0, test2 & test4);
            Assert.Equal(2, test2 & test2550);
            Assert.Equal(0, SqlInt64.MaxValue & SqlInt64.MinValue);

            // | -operator
            Assert.Equal(6, test2 | test4);
            Assert.Equal(2550, test2 | test2550);
            Assert.Equal(-1, SqlInt64.MinValue | SqlInt64.MaxValue);

            //  ^ -operator
            Assert.Equal(2546, (test2550 ^ test4));
            Assert.Equal(6, (test2 ^ test4));
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlInt64 test165 = new SqlInt64(165);
            SqlInt64 test100 = new SqlInt64(100);
            SqlInt64 test100II = new SqlInt64(100);
            SqlInt64 test255 = new SqlInt64(2550);

            // == -operator
            Assert.True((test100 == test100II).Value);
            Assert.False((test165 == test100).Value);
            Assert.True((test165 == SqlInt64.Null).IsNull);

            // != -operator
            Assert.False((test100 != test100II).Value);
            Assert.True((test100 != test255).Value);
            Assert.True((test165 != test255).Value);
            Assert.True((test165 != SqlInt64.Null).IsNull);

            // > -operator
            Assert.True((test165 > test100).Value);
            Assert.False((test165 > test255).Value);
            Assert.False((test100 > test100II).Value);
            Assert.True((test165 > SqlInt64.Null).IsNull);

            // >=  -operator
            Assert.False((test165 >= test255).Value);
            Assert.True((test255 >= test165).Value);
            Assert.True((test100 >= test100II).Value);
            Assert.True((test165 >= SqlInt64.Null).IsNull);

            // < -operator
            Assert.False((test165 < test100).Value);
            Assert.True((test165 < test255).Value);
            Assert.False((test100 < test100II).Value);
            Assert.True((test165 < SqlInt64.Null).IsNull);

            // <= -operator
            Assert.True((test165 <= test255).Value);
            Assert.False((test255 <= test165).Value);
            Assert.True((test100 <= test100II).Value);
            Assert.True((test165 <= SqlInt64.Null).IsNull);
        }

        [Fact]
        public void OnesComplementOperator()
        {
            SqlInt64 test12 = new SqlInt64(12);
            SqlInt64 test128 = new SqlInt64(128);

            Assert.Equal(-13, ~test12);
            Assert.Equal(-129, ~test128);
            Assert.Equal(SqlInt64.Null, ~SqlInt64.Null);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlInt64 test = new SqlInt64(2000);
            SqlInt64 testNeg = new SqlInt64(-3000);

            SqlInt64 result = -test;
            Assert.Equal(-2000, result.Value);

            result = -testNeg;
            Assert.Equal(3000, result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlInt64()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlInt64 tesult;

            tesult = (SqlInt64)testBoolean;

            Assert.Equal(1, tesult.Value);

            tesult = (SqlInt64)SqlBoolean.Null;
            Assert.True(tesult.IsNull);
        }

        [Fact]
        public void SqlDecimalToSqlInt64()
        {
            SqlDecimal testDecimal64 = new SqlDecimal(64);

            Assert.Equal(64, ((SqlInt64)testDecimal64).Value);
            Assert.Equal(SqlInt64.Null, ((SqlInt64)SqlDecimal.Null));

            Assert.Throws<OverflowException>(() => (SqlInt64)SqlDecimal.MaxValue);
        }

        [Fact]
        public void SqlDoubleToSqlInt64()
        {
            SqlDouble testDouble64 = new SqlDouble(64);
            SqlDouble testDouble900 = new SqlDouble(90000);

            Assert.Equal(64, ((SqlInt64)testDouble64).Value);
            Assert.Equal(SqlInt64.Null, ((SqlInt64)SqlDouble.Null));

            Assert.Throws<OverflowException>(() => (SqlInt64)SqlDouble.MaxValue);
        }

        [Fact]
        public void Sql64IntToInt64()
        {
            SqlInt64 test = new SqlInt64(12);
            long result = (long)test;
            Assert.Equal(12, result);
        }

        [Fact]
        public void SqlInt32ToSqlInt64()
        {
            SqlInt32 test64 = new SqlInt32(64);
            Assert.Equal(64, ((SqlInt64)test64).Value);
        }

        [Fact]
        public void SqlInt16ToSqlInt64()
        {
            SqlInt16 test64 = new SqlInt16(64);
            Assert.Equal(64, ((SqlInt64)test64).Value);
        }

        [Fact]
        public void SqlMoneyToSqlInt64()
        {
            SqlMoney testMoney64 = new SqlMoney(64);
            Assert.Equal(64, ((SqlInt64)testMoney64).Value);
        }

        [Fact]
        public void SqlSingleToSqlInt64()
        {
            SqlSingle testSingle64 = new SqlSingle(64);
            Assert.Equal(64, ((SqlInt64)testSingle64).Value);
        }

        [Fact]
        public void SqlStringToSqlInt64()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("100");
            SqlString testString1000 = new SqlString("1000000000000000000000");

            Assert.Equal(100, ((SqlInt64)testString100).Value);

            Assert.Throws<OverflowException>(() => (SqlInt64)testString1000);

            Assert.Throws<FormatException>(() => (SqlInt64)testString);
        }

        [Fact]
        public void ByteToSqlInt64()
        {
            short testShort = 14;
            Assert.Equal(14, ((SqlInt64)testShort).Value);
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

            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => ReadWriteXmlTestInternal(xml3, lngtest3, "#BA03"));
            Assert.Equal(typeof(FormatException), ex.InnerException.GetType());
        }
    }
}
