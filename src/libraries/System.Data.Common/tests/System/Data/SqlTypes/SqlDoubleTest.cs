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

using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Xunit;

namespace System.Data.Tests.SqlTypes
{
    public class SqlDoubleTest
    {
        private CultureInfo _originalCulture;

        // Test constructor
        [Fact]
        public void Create()
        {
            SqlDouble test = new SqlDouble(34.87);
            Assert.Equal(34.87D, test.Value);

            test = new SqlDouble(-9000.6543);
            Assert.Equal(-9000.6543D, test.Value);
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.Equal(1.7976931348623157e+308, SqlDouble.MaxValue.Value);
            Assert.Equal(-1.7976931348623157e+308, SqlDouble.MinValue.Value);
            Assert.True(SqlDouble.Null.IsNull);
            Assert.Equal(0d, SqlDouble.Zero.Value);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            SqlDouble test5443 = new SqlDouble(5443e12);
            SqlDouble test1 = new SqlDouble(1);

            Assert.True(SqlDouble.Null.IsNull);
            Assert.Equal(5443e12, test5443.Value);
            Assert.Equal(1, test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlDouble test0 = new SqlDouble(0);
            SqlDouble test1 = new SqlDouble(15E+108);
            SqlDouble test2 = new SqlDouble(-65E+64);
            SqlDouble test3 = new SqlDouble(5E+64);
            SqlDouble test4 = new SqlDouble(5E+108);
            SqlDouble testMax = new SqlDouble(SqlDouble.MaxValue.Value);

            // Add()
            Assert.Equal(15E+108, SqlDouble.Add(test1, test0).Value);
            Assert.Equal(1.5E+109, SqlDouble.Add(test1, test2).Value);

            Assert.Throws<OverflowException>(() => SqlDouble.Add(SqlDouble.MaxValue, SqlDouble.MaxValue));

            // Divide()
            Assert.Equal(3, SqlDouble.Divide(test1, test4));
            Assert.Equal(-13d, SqlDouble.Divide(test2, test3).Value);

            Assert.Throws<DivideByZeroException>(() => SqlDouble.Divide(test1, test0).Value);

            // Multiply()
            Assert.Equal(75E+216, SqlDouble.Multiply(test1, test4).Value);
            Assert.Equal(0, SqlDouble.Multiply(test1, test0).Value);

            Assert.Throws<OverflowException>(() => SqlDouble.Multiply(testMax, test1));


            // Subtract()
            Assert.Equal(1.5E+109, SqlDouble.Subtract(test1, test3).Value);

            Assert.Throws<OverflowException>(() => SqlDouble.Subtract(SqlDouble.MinValue, SqlDouble.MaxValue));
        }

        [Fact]
        public void CompareTo()
        {
            SqlDouble test1 = new SqlDouble(4e64);
            SqlDouble test11 = new SqlDouble(4e64);
            SqlDouble test2 = new SqlDouble(-9e34);
            SqlDouble test3 = new SqlDouble(10000);
            SqlString testString = new SqlString("This is a test");

            Assert.True(test1.CompareTo(test3) > 0);
            Assert.True(test2.CompareTo(test3) < 0);
            Assert.True(test1.CompareTo(test11) == 0);
            Assert.True(test11.CompareTo(SqlDouble.Null) > 0);

            Assert.Throws<ArgumentException>(() => test1.CompareTo(testString));
        }

        [Fact]
        public void EqualsMethods()
        {
            SqlDouble test0 = new SqlDouble(0);
            SqlDouble test1 = new SqlDouble(1.58e30);
            SqlDouble test2 = new SqlDouble(1.8e180);
            SqlDouble test22 = new SqlDouble(1.8e180);

            Assert.False(test0.Equals(test1));
            Assert.False(test1.Equals(test2));
            Assert.False(test2.Equals(new SqlString("TEST")));
            Assert.True(test2.Equals(test22));

            // Static Equals()-method
            Assert.True(SqlDouble.Equals(test2, test22).Value);
            Assert.False(SqlDouble.Equals(test1, test2).Value);
        }

        [Fact]
        public void Greaters()
        {
            SqlDouble test1 = new SqlDouble(1e100);
            SqlDouble test11 = new SqlDouble(1e100);
            SqlDouble test2 = new SqlDouble(64e164);

            // GreateThan ()
            Assert.False(SqlDouble.GreaterThan(test1, test2).Value);
            Assert.True(SqlDouble.GreaterThan(test2, test1).Value);
            Assert.False(SqlDouble.GreaterThan(test1, test11).Value);

            // GreaterTharOrEqual ()
            Assert.False(SqlDouble.GreaterThanOrEqual(test1, test2).Value);
            Assert.True(SqlDouble.GreaterThanOrEqual(test2, test1).Value);
            Assert.True(SqlDouble.GreaterThanOrEqual(test1, test11).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlDouble test1 = new SqlDouble(1.8e100);
            SqlDouble test11 = new SqlDouble(1.8e100);
            SqlDouble test2 = new SqlDouble(64e164);

            // LessThan()
            Assert.False(SqlDouble.LessThan(test1, test11).Value);
            Assert.False(SqlDouble.LessThan(test2, test1).Value);
            Assert.True(SqlDouble.LessThan(test11, test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlDouble.LessThanOrEqual(test1, test2).Value);
            Assert.False(SqlDouble.LessThanOrEqual(test2, test1).Value);
            Assert.True(SqlDouble.LessThanOrEqual(test11, test1).Value);
            Assert.True(SqlDouble.LessThanOrEqual(test11, SqlDouble.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlDouble test1 = new SqlDouble(1280000000001);
            SqlDouble test2 = new SqlDouble(128e10);
            SqlDouble test22 = new SqlDouble(128e10);

            Assert.True(SqlDouble.NotEquals(test1, test2).Value);
            Assert.True(SqlDouble.NotEquals(test2, test1).Value);
            Assert.True(SqlDouble.NotEquals(test22, test1).Value);
            Assert.False(SqlDouble.NotEquals(test22, test2).Value);
            Assert.False(SqlDouble.NotEquals(test2, test22).Value);
            Assert.True(SqlDouble.NotEquals(SqlDouble.Null, test22).IsNull);
            Assert.True(SqlDouble.NotEquals(SqlDouble.Null, test22).IsNull);
        }

        [Fact]
        public void Parse()
        {
            Assert.Throws<ArgumentNullException>(() => SqlDouble.Parse(null));

            Assert.Throws<FormatException>(() => SqlDouble.Parse("not-a-number"));

            Assert.Throws<OverflowException>(() => SqlDouble.Parse("9e400"));
            Assert.Equal(150, SqlDouble.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlDouble test0 = new SqlDouble(0);
            SqlDouble test1 = new SqlDouble(250);
            SqlDouble test2 = new SqlDouble(64e64);
            SqlDouble test3 = new SqlDouble(64e164);
            SqlDouble TestNull = SqlDouble.Null;

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

            Assert.Throws<OverflowException>(() => test3.ToSqlDecimal());

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

            Assert.Throws<OverflowException>(() => test2.ToSqlInt64());

            // ToSqlMoney ()
            Assert.Equal(250M, test1.ToSqlMoney().Value);
            Assert.Equal(0, test0.ToSqlMoney().Value);

            Assert.Throws<OverflowException>(() => test2.ToSqlMoney());

            // ToSqlSingle ()
            Assert.Equal(250, test1.ToSqlSingle().Value);
            Assert.Equal(0, test0.ToSqlSingle().Value);

            Assert.Throws<OverflowException>(() => test2.ToSqlSingle());

            // ToSqlString ()
            Assert.Equal(250.ToString(), test1.ToSqlString().Value);
            Assert.Equal(0.ToString(), test0.ToSqlString().Value);
            Assert.Equal(6.4E+65.ToString(), test2.ToSqlString().Value);

            // ToString ()
            Assert.Equal(250.ToString(), test1.ToString());
            Assert.Equal(0.ToString(), test0.ToString());
            Assert.Equal(6.4E+65.ToString(), test2.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlDouble test0 = new SqlDouble(0);
            SqlDouble test1 = new SqlDouble(24E+100);
            SqlDouble test3 = new SqlDouble(12E+100);
            SqlDouble test4 = new SqlDouble(1E+10);
            SqlDouble test5 = new SqlDouble(2E+10);

            // "+"-operator
            Assert.Equal(3E+10, test4 + test5);

            Assert.Throws<OverflowException>(() => SqlDouble.MaxValue + SqlDouble.MaxValue);

            // "/"-operator
            Assert.Equal(2, test1 / test3);

            Assert.Throws<DivideByZeroException>(() => test3 / test0);

            // "*"-operator
            Assert.Equal(2e20, test4 * test5);

            Assert.Throws<OverflowException>(() => SqlDouble.MaxValue * test1);

            // "-"-operator
            Assert.Equal(12e100, test1 - test3);

            Assert.Throws<OverflowException>(() => SqlDouble.MinValue - SqlDouble.MaxValue);
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlDouble test1 = new SqlDouble(1E+164);
            SqlDouble test2 = new SqlDouble(9.7E+100);
            SqlDouble test22 = new SqlDouble(9.7E+100);
            SqlDouble test3 = new SqlDouble(2E+200);

            // == -operator
            Assert.True((test2 == test22).Value);
            Assert.False((test1 == test2).Value);
            Assert.True((test1 == SqlDouble.Null).IsNull);

            // != -operator
            Assert.False((test2 != test22).Value);
            Assert.True((test2 != test3).Value);
            Assert.True((test1 != test3).Value);
            Assert.True((test1 != SqlDouble.Null).IsNull);

            // > -operator
            Assert.True((test1 > test2).Value);
            Assert.False((test1 > test3).Value);
            Assert.False((test2 > test22).Value);
            Assert.True((test1 > SqlDouble.Null).IsNull);

            // >=  -operator
            Assert.False((test1 >= test3).Value);
            Assert.True((test3 >= test1).Value);
            Assert.True((test2 >= test22).Value);
            Assert.True((test1 >= SqlDouble.Null).IsNull);

            // < -operator
            Assert.False((test1 < test2).Value);
            Assert.True((test1 < test3).Value);
            Assert.False((test2 < test22).Value);
            Assert.True((test1 < SqlDouble.Null).IsNull);

            // <= -operator
            Assert.True((test1 <= test3).Value);
            Assert.False((test3 <= test1).Value);
            Assert.True((test2 <= test22).Value);
            Assert.True((test1 <= SqlDouble.Null).IsNull);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlDouble test = new SqlDouble(2000000001);
            SqlDouble testNeg = new SqlDouble(-3000);

            SqlDouble result = -test;
            Assert.Equal(-2000000001, result.Value);

            result = -testNeg;
            Assert.Equal(3000, result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlDouble()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlDouble result;

            result = (SqlDouble)testBoolean;

            Assert.Equal(1, result.Value);

            result = (SqlDouble)SqlBoolean.Null;
            Assert.True(result.IsNull);
        }

        [Fact]
        public void SqlDoubleToDouble()
        {
            SqlDouble test = new SqlDouble(12e12);
            double result = (double)test;
            Assert.Equal(12e12, result);
        }

        [Fact]
        public void SqlStringToSqlDouble()
        {
            SqlString testString = new SqlString("Test string");
            SqlString testString100 = new SqlString("100");

            Assert.Equal(100, ((SqlDouble)testString100).Value);

            Assert.Throws<FormatException>(() => (SqlDouble)testString);
        }

        [Fact]
        public void DoubleToSqlDouble()
        {
            double test1 = 5e64;
            SqlDouble result = test1;
            Assert.Equal(5e64, result.Value);
        }

        [Fact]
        public void ByteToSqlDouble()
        {
            short TestShort = 14;
            Assert.Equal(14, ((SqlDouble)TestShort).Value);
        }

        [Fact]
        public void SqlDecimalToSqlDouble()
        {
            SqlDecimal TestDecimal64 = new SqlDecimal(64);

            Assert.Equal(64, ((SqlDouble)TestDecimal64).Value);
            Assert.Equal(SqlDouble.Null, SqlDecimal.Null);
        }

        [Fact]
        public void SqlIntToSqlDouble()
        {
            SqlInt16 Test64 = new SqlInt16(64);
            SqlInt32 Test640 = new SqlInt32(640);
            SqlInt64 Test64000 = new SqlInt64(64000);
            Assert.Equal(64, ((SqlDouble)Test64).Value);
            Assert.Equal(640, ((SqlDouble)Test640).Value);
            Assert.Equal(64000, ((SqlDouble)Test64000).Value);
        }

        [Fact]
        public void SqlMoneyToSqlDouble()
        {
            SqlMoney TestMoney64 = new SqlMoney(64);
            Assert.Equal(64, ((SqlDouble)TestMoney64).Value);
        }

        [Fact]
        public void SqlSingleToSqlDouble()
        {
            SqlSingle TestSingle64 = new SqlSingle(64);
            Assert.Equal(64, ((SqlDouble)TestSingle64).Value);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlDouble.GetXsdType(null);
            Assert.Equal("double", qualifiedName.Name);
        }

        internal void ReadWriteXmlTestInternal(string xml,
                               double testval,
                               string unit_test_id)
        {
            SqlDouble test;
            SqlDouble test1;
            XmlSerializer ser;
            StringWriter sw;
            XmlTextWriter xw;
            StringReader sr;
            XmlTextReader xr;

            test = new SqlDouble(testval);
            ser = new XmlSerializer(typeof(SqlDouble));
            sw = new StringWriter();
            xw = new XmlTextWriter(sw);

            ser.Serialize(xw, test);

            // Assert.Equal (xml, sw.ToString ());

            sr = new StringReader(xml);
            xr = new XmlTextReader(sr);
            test1 = (SqlDouble)ser.Deserialize(xr);

            Assert.Equal(testval, test1.Value);
        }

        [Fact]
        //[Category ("MobileNotWorking")]
        public void ReadWriteXmlTest()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><double>4556.99999999999999999988</double>";
            string xml2 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><double>-6445.8888888888899999999</double>";
            string xml3 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><double>0x455687AB3E4D56F</double>";
            double test1 = 4556.99999999999999999988;
            double test2 = -6445.8888888888899999999;
            double test3 = 0x4F56;

            ReadWriteXmlTestInternal(xml1, test1, "BA01");
            ReadWriteXmlTestInternal(xml2, test2, "BA02");

            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => ReadWriteXmlTestInternal(xml3, test3, "BA03"));
            Assert.Equal(typeof(FormatException), ex.InnerException.GetType());
        }
    }
}
