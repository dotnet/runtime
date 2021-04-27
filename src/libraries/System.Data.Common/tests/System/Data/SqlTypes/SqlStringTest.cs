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
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using System.Tests;

namespace System.Data.Tests.SqlTypes
{
    public class SqlStringTest
    {
        private SqlString _test1;
        private SqlString _test2;
        private SqlString _test3;

        static SqlStringTest()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public SqlStringTest()
        {
            _test1 = new SqlString("First TestString");
            _test2 = new SqlString("This is just a test SqlString");
            _test3 = new SqlString("This is just a test SqlString");
        }

        [Fact]
        public void Constructor_Value_Success()
        {
            const string value = "foo";
            ValidateProperties(value, CultureInfo.CurrentCulture, new SqlString(value));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        [InlineData(1033, "en-US")]
        [InlineData(1036, "fr-FR")]
        public void Constructor_ValueLcid_Success(int lcid, string name)
        {
            const string value = "foo";
            ValidateProperties(value, new CultureInfo(name), new SqlString(value, lcid));
        }

        private static void ValidateProperties(string value, CultureInfo culture, SqlString sqlString)
        {
            Assert.Same(value, sqlString.Value);
            Assert.False(sqlString.IsNull);
            Assert.Equal(SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth, sqlString.SqlCompareOptions);
            Assert.Equal(culture, sqlString.CultureInfo);
            Assert.Equal(culture.CompareInfo, sqlString.CompareInfo);
        }

        [Fact]
        public void CultureInfo_InvalidLcid_Throws()
        {
            const string value = "foo";
            Assert.Throws<ArgumentOutOfRangeException>(() => new SqlString(value, int.MinValue).CultureInfo);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SqlString(value, -1).CultureInfo);
            Assert.Throws<CultureNotFoundException>(() => new SqlString(value, int.MaxValue).CultureInfo);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            // SqlString (String)
            SqlString testString = new SqlString("Test");
            Assert.Equal("Test", testString.Value);

            if (PlatformDetection.IsNotInvariantGlobalization)
            {
                // SqlString (String, int)
                testString = new SqlString("Test", 2057);
                Assert.Equal(2057, testString.LCID);

                // SqlString (int, SqlCompareOptions, byte[])
                testString = new SqlString(2057,
                    SqlCompareOptions.BinarySort | SqlCompareOptions.IgnoreCase,
                    new byte[2] { 123, 221 });
                Assert.Equal(2057, testString.CompareInfo.LCID);

                // SqlString(string, int, SqlCompareOptions)
                testString = new SqlString("Test", 2057, SqlCompareOptions.IgnoreNonSpace);
                Assert.False(testString.IsNull);

                // SqlString (int, SqlCompareOptions, byte[], bool)
                testString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[4] { 100, 100, 200, 45 }, true);
                Assert.Equal((byte)63, testString.GetNonUnicodeBytes()[0]);
                testString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[2] { 113, 100 }, false);
                Assert.Equal("qd", testString.Value);

                // SqlString (int, SqlCompareOptions, byte[], int, int)
                testString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[2] { 113, 100 }, 0, 2);
                Assert.False(testString.IsNull);

                // SqlString (int, SqlCompareOptions, byte[], int, int, bool)
                testString = new SqlString(2057, SqlCompareOptions.IgnoreCase, new byte[3] { 100, 111, 50 }, 1, 2, false);
                Assert.Equal("o2", testString.Value);
                testString = new SqlString(2057, SqlCompareOptions.IgnoreCase, new byte[3] { 123, 111, 222 }, 1, 2, true);
                Assert.False(testString.IsNull);
            }
        }

        [Fact]
        public void CtorArgumentOutOfRangeException1()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SqlString TestString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[2] { 113, 100 }, 2, 1);
            });
        }

        [Fact]
        public void CtorArgumentOutOfRangeException2()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                SqlString TestString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[2] { 113, 100 }, 0, 4);
            });
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            // BinarySort
            Assert.Equal(32768, SqlString.BinarySort);

            // IgnoreCase
            Assert.Equal(1, SqlString.IgnoreCase);

            // IgnoreKanaType
            Assert.Equal(8, SqlString.IgnoreKanaType);

            // IgnoreNonSpace
            Assert.Equal(2, SqlString.IgnoreNonSpace);

            // IgnoreWidth
            Assert.Equal(16, SqlString.IgnoreWidth);

            // Null
            Assert.True(SqlString.Null.IsNull);
        }

        // Test properties
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void Properties()
        {
            using (new ThreadCultureChange("en-AU"))
            {
                var one = new SqlString("First TestString");

                // CompareInfo
                Assert.Equal(3081, one.CompareInfo.LCID);

                // CultureInfo
                Assert.Equal(3081, one.CultureInfo.LCID);

                // LCID
                Assert.Equal(3081, one.LCID);

                // IsNull
                Assert.False(one.IsNull);
                Assert.True(SqlString.Null.IsNull);

                // SqlCompareOptions
                Assert.Equal("IgnoreCase, IgnoreKanaType, IgnoreWidth", one.SqlCompareOptions.ToString());

                // Value
                Assert.Equal("First TestString", one.Value);
            }
        }

        // PUBLIC METHODS

        [Fact]
        public void CompareToArgumentException()
        {
            SqlByte test = new SqlByte(1);
            AssertExtensions.Throws<ArgumentException>(null, () => _test1.CompareTo(test));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void CompareToSqlTypeException()
        {
            SqlString t1 = new SqlString("test", 2057, SqlCompareOptions.IgnoreCase);
            SqlString t2 = new SqlString("TEST", 2057, SqlCompareOptions.None);
            Assert.Throws<SqlTypeException>(() => t1.CompareTo(t2));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotInvariantGlobalization))]
        public void CompareTo()
        {
            Assert.True(_test1.CompareTo(_test3) < 0);
            Assert.True(_test2.CompareTo(_test1) > 0);
            Assert.Equal(0, _test2.CompareTo(_test3));
            Assert.True(_test3.CompareTo(SqlString.Null) > 0);

            // IgnoreCase
            SqlString t1 = new SqlString("test", 2057, SqlCompareOptions.IgnoreCase);
            SqlString t2 = new SqlString("TEST", 2057, SqlCompareOptions.IgnoreCase);
            Assert.Equal(0, t2.CompareTo(t1));

            t1 = new SqlString("test", 2057);
            t2 = new SqlString("TEST", 2057);
            Assert.Equal(0, t2.CompareTo(t1));

            t1 = new SqlString("test", 2057, SqlCompareOptions.None);
            t2 = new SqlString("TEST", 2057, SqlCompareOptions.None);
            Assert.NotEqual(0, t2.CompareTo(t1));

            // IgnoreNonSpace
            t1 = new SqlString("TEST\xF1", 2057, SqlCompareOptions.IgnoreNonSpace);
            t2 = new SqlString("TESTn", 2057, SqlCompareOptions.IgnoreNonSpace);
            Assert.Equal(0, t2.CompareTo(t1));

            t1 = new SqlString("TEST\u00F1", 2057, SqlCompareOptions.None);
            t2 = new SqlString("TESTn", 2057, SqlCompareOptions.None);
            Assert.NotEqual(0, t2.CompareTo(t1));

            // BinarySort
            t1 = new SqlString("01_", 2057, SqlCompareOptions.BinarySort);
            t2 = new SqlString("_01", 2057, SqlCompareOptions.BinarySort);
            Assert.True(t1.CompareTo(t2) < 0);

            t1 = new SqlString("01_", 2057, SqlCompareOptions.None);
            t2 = new SqlString("_01", 2057, SqlCompareOptions.None);
            Assert.True(t1.CompareTo(t2) > 0);
        }

        [Fact]
        public void EqualsMethods()
        {
            Assert.False(_test1.Equals(_test2));
            Assert.False(_test3.Equals(_test1));
            Assert.False(_test2.Equals(new SqlString("TEST")));
            Assert.True(_test2.Equals(_test3));

            // Static Equals()-method
            Assert.True(SqlString.Equals(_test2, _test3).Value);
            Assert.False(SqlString.Equals(_test1, _test2).Value);
        }

        [Fact]
        public void Greaters()
        {
            // GreateThan ()
            Assert.False(SqlString.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlString.GreaterThan(_test2, _test1).Value);
            Assert.False(SqlString.GreaterThan(_test2, _test3).Value);

            // GreaterTharOrEqual ()
            Assert.False(SqlString.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlString.GreaterThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlString.GreaterThanOrEqual(_test2, _test3).Value);
        }

        [Fact]
        public void Lessers()
        {
            // LessThan()
            Assert.False(SqlString.LessThan(_test2, _test3).Value);
            Assert.False(SqlString.LessThan(_test2, _test1).Value);
            Assert.True(SqlString.LessThan(_test1, _test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlString.LessThanOrEqual(_test1, _test2).Value);
            Assert.False(SqlString.LessThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlString.LessThanOrEqual(_test3, _test2).Value);
            Assert.True(SqlString.LessThanOrEqual(_test2, SqlString.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            Assert.True(SqlString.NotEquals(_test1, _test2).Value);
            Assert.True(SqlString.NotEquals(_test2, _test1).Value);
            Assert.True(SqlString.NotEquals(_test3, _test1).Value);
            Assert.False(SqlString.NotEquals(_test2, _test3).Value);
            Assert.True(SqlString.NotEquals(SqlString.Null, _test3).IsNull);
        }

        [Fact]
        public void Concat()
        {
            _test1 = new SqlString("First TestString");
            _test2 = new SqlString("This is just a test SqlString");
            _test3 = new SqlString("This is just a test SqlString");

            Assert.Equal("First TestStringThis is just a test SqlString", SqlString.Concat(_test1, _test2));
            Assert.Equal(SqlString.Null, SqlString.Concat(_test1, SqlString.Null));
        }

        [Fact]
        public void Clone()
        {
            SqlString testSqlString = _test1.Clone();
            Assert.Equal(_test1, testSqlString);
        }

        [Fact]
        public void CompareOptionsFromSqlCompareOptions()
        {
            Assert.Equal(CompareOptions.IgnoreCase,
                SqlString.CompareOptionsFromSqlCompareOptions(
                SqlCompareOptions.IgnoreCase));
            Assert.Equal(CompareOptions.IgnoreCase,
                SqlString.CompareOptionsFromSqlCompareOptions(
                SqlCompareOptions.IgnoreCase));
            Assert.Throws<ArgumentOutOfRangeException>(() => SqlString.CompareOptionsFromSqlCompareOptions(SqlCompareOptions.BinarySort));
        }

        [Fact]
        public void UnicodeBytes()
        {
            Assert.Equal((byte)105, _test1.GetNonUnicodeBytes()[1]);
            Assert.Equal((byte)32, _test1.GetNonUnicodeBytes()[5]);

            Assert.Equal((byte)70, _test1.GetUnicodeBytes()[0]);
            Assert.Equal((byte)70, _test1.GetNonUnicodeBytes()[0]);
            Assert.Equal((byte)0, _test1.GetUnicodeBytes()[1]);
            Assert.Equal((byte)105, _test1.GetNonUnicodeBytes()[1]);
            Assert.Equal((byte)105, _test1.GetUnicodeBytes()[2]);
            Assert.Equal((byte)114, _test1.GetNonUnicodeBytes()[2]);
            Assert.Equal((byte)0, _test1.GetUnicodeBytes()[3]);
            Assert.Equal((byte)115, _test1.GetNonUnicodeBytes()[3]);
            Assert.Equal((byte)114, _test1.GetUnicodeBytes()[4]);
            Assert.Equal((byte)116, _test1.GetNonUnicodeBytes()[4]);

            Assert.Equal((byte)105, _test1.GetUnicodeBytes()[2]);

            Assert.Throws<IndexOutOfRangeException>(() => _test1.GetUnicodeBytes()[105]);
        }

        [Fact]
        public void ConversionBoolFormatException1()
        {
            Assert.Throws<FormatException>(() =>
            {
                bool test = _test1.ToSqlBoolean().Value;
            });
        }

        [Fact]
        public void ConversionByteFormatException()
        {
            Assert.Throws<FormatException>(() =>
            {
                byte test = _test1.ToSqlByte().Value;
            });
        }

        [Fact]
        public void ConversionDecimalFormatException1()
        {
            Assert.Throws<FormatException>(() =>
            {
                decimal d = _test1.ToSqlDecimal().Value;
            });
        }

        [Fact]
        public void ConversionDecimalFormatException2()
        {
            SqlString String9E300 = new SqlString("9E+300");
            Assert.Throws<FormatException>(() =>
            {
                SqlDecimal test = String9E300.ToSqlDecimal();
            });
        }

        [Fact]
        public void ConversionGuidFormatException()
        {
            SqlString String9E300 = new SqlString("9E+300");
            Assert.Throws<FormatException>(() =>
            {
                SqlGuid test = String9E300.ToSqlGuid();
            });
        }

        [Fact]
        public void ConversionInt16FormatException()
        {
            Assert.Throws<FormatException>(() =>
            {
                SqlString String9E300 = new SqlString("9E+300");
                SqlInt16 test = String9E300.ToSqlInt16().Value;
            });
        }

        [Fact]
        public void ConversionInt32FormatException1()
        {
            Assert.Throws<FormatException>(() =>
            {
                SqlString String9E300 = new SqlString("9E+300");
                SqlInt32 test = String9E300.ToSqlInt32().Value;
            });
        }

        [Fact]
        public void ConversionInt32FormatException2()
        {
            Assert.Throws<FormatException>(() =>
            {
                SqlInt32 test = _test1.ToSqlInt32().Value;
            });
        }

        [Fact]
        public void ConversionInt64FormatException()
        {
            Assert.Throws<FormatException>(() =>
            {
                SqlString String9E300 = new SqlString("9E+300");
                SqlInt64 test = String9E300.ToSqlInt64().Value;
            });
        }

        [Fact]
        public void ConversionIntMoneyFormatException2()
        {
            Assert.Throws<FormatException>(() =>
            {
                SqlString String9E300 = new SqlString("9E+300");
                SqlMoney test = String9E300.ToSqlMoney().Value;
            });
        }

        [Fact]
        public void ConversionByteOverflowException()
        {
            Assert.Throws<OverflowException>(() =>
            {
                SqlByte b = (new SqlString("2500")).ToSqlByte();
            });
        }

        [Fact]
        public void ConversionDoubleOverflowException()
        {
            Assert.Throws<OverflowException>(() =>
            {
                SqlDouble test = (new SqlString("4e400")).ToSqlDouble();
            });
        }

        [Fact]
        public void ConversionSingleOverflowException()
        {
            Assert.Throws<OverflowException>(() =>
            {
                SqlString String9E300 = new SqlString("9E+300");
                SqlSingle test = String9E300.ToSqlSingle().Value;
            });
        }

        [Fact]
        public void Conversions()
        {
            SqlString string250 = new SqlString("250");
            SqlString string9E300 = new SqlString("9E+300");

            // ToSqlBoolean ()
            Assert.True((new SqlString("1")).ToSqlBoolean().Value);
            Assert.False((new SqlString("0")).ToSqlBoolean().Value);
            Assert.True((new SqlString("True")).ToSqlBoolean().Value);
            Assert.False((new SqlString("FALSE")).ToSqlBoolean().Value);
            Assert.True(SqlString.Null.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)250, string250.ToSqlByte().Value);

            // ToSqlDateTime
            Assert.Equal(10, (new SqlString("2002-10-10")).ToSqlDateTime().Value.Day);

            // ToSqlDecimal ()
            Assert.Equal(250, string250.ToSqlDecimal().Value);

            // ToSqlDouble
            Assert.Equal(9E+300, string9E300.ToSqlDouble());

            // ToSqlGuid
            SqlString TestGuid = new SqlString("11111111-1111-1111-1111-111111111111");
            Assert.Equal(new SqlGuid("11111111-1111-1111-1111-111111111111"), TestGuid.ToSqlGuid());

            // ToSqlInt16 ()
            Assert.Equal((short)250, string250.ToSqlInt16().Value);

            // ToSqlInt32 ()
            Assert.Equal(250, string250.ToSqlInt32().Value);

            // ToSqlInt64 ()
            Assert.Equal(250, string250.ToSqlInt64().Value);

            // ToSqlMoney ()
            Assert.Equal(250.0000M, string250.ToSqlMoney().Value);

            // ToSqlSingle ()
            Assert.Equal(250, string250.ToSqlSingle().Value);

            // ToString ()
            Assert.Equal("First TestString", _test1.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlString testString = new SqlString("...Testing...");
            Assert.Equal("First TestString...Testing...", _test1 + testString);
            Assert.Equal(SqlString.Null, _test1 + SqlString.Null);
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            // == -operator
            Assert.True((_test2 == _test3).Value);
            Assert.False((_test1 == _test2).Value);
            Assert.True((_test1 == SqlString.Null).IsNull);

            // != -operator
            Assert.False((_test3 != _test2).Value);
            Assert.False((_test2 != _test3).Value);
            Assert.True((_test1 != _test3).Value);
            Assert.True((_test1 != SqlString.Null).IsNull);

            // > -operator
            Assert.True((_test2 > _test1).Value);
            Assert.False((_test1 > _test3).Value);
            Assert.False((_test2 > _test3).Value);
            Assert.True((_test1 > SqlString.Null).IsNull);

            // >= -operator
            Assert.False((_test1 >= _test3).Value);
            Assert.True((_test3 >= _test1).Value);
            Assert.True((_test2 >= _test3).Value);
            Assert.True((_test1 >= SqlString.Null).IsNull);

            // < -operator
            Assert.True((_test1 < _test2).Value);
            Assert.True((_test1 < _test3).Value);
            Assert.False((_test2 < _test3).Value);
            Assert.True((_test1 < SqlString.Null).IsNull);

            // <= -operator
            Assert.True((_test1 <= _test3).Value);
            Assert.False((_test3 <= _test1).Value);
            Assert.True((_test2 <= _test3).Value);
            Assert.True((_test1 <= SqlString.Null).IsNull);
        }

        [Fact]
        public void SqlBooleanToSqlString()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlBoolean testBoolean2 = new SqlBoolean(false);
            SqlString result;

            result = (SqlString)testBoolean;
            Assert.Equal("True", result.Value);

            result = (SqlString)testBoolean2;
            Assert.Equal("False", result.Value);

            result = (SqlString)SqlBoolean.Null;
            Assert.True(result.IsNull);
        }

        [Fact]
        public void SqlByteToBoolean()
        {
            SqlByte testByte = new SqlByte(250);
            Assert.Equal("250", ((SqlString)testByte).Value);

            Assert.Throws<SqlNullValueException>(() => ((SqlString)SqlByte.Null).Value);
        }

        [Fact]
        public void SqlDateTimeToSqlString()
        {
            SqlDateTime testTime = new SqlDateTime(2002, 10, 22, 9, 52, 30);
            Assert.Equal(testTime.Value.ToString((IFormatProvider)null), ((SqlString)testTime).Value);
        }

        [Fact]
        public void SqlDecimalToSqlString()
        {
            SqlDecimal testDecimal = new SqlDecimal(1000.2345);
            Assert.Equal("1000.2345000000000", ((SqlString)testDecimal).Value);
        }

        [Fact]
        public void SqlDoubleToSqlString()
        {
            SqlDouble testDouble = new SqlDouble(64E+64);
            Assert.Equal(6.4E+65.ToString(), ((SqlString)testDouble).Value);
        }

        [Fact]
        public void SqlGuidToSqlString()
        {
            byte[] b = new byte[16];
            b[0] = 100;
            b[1] = 64;
            SqlGuid testGuid = new SqlGuid(b);

            Assert.Equal("00004064-0000-0000-0000-000000000000", ((SqlString)testGuid).Value);

            Assert.Throws<SqlNullValueException>(() => ((SqlString)SqlGuid.Null).Value);
        }

        [Fact]
        public void SqlInt16ToSqlString()
        {
            SqlInt16 testInt = new SqlInt16(20012);
            Assert.Equal("20012", ((SqlString)testInt).Value);

            Assert.Throws<SqlNullValueException>(() => ((SqlString)SqlInt16.Null).Value);
        }

        [Fact]
        public void SqlInt32ToSqlString()
        {
            SqlInt32 testInt = new SqlInt32(-12456);
            Assert.Equal("-12456", ((SqlString)testInt).Value);

            Assert.Throws<SqlNullValueException>(() => ((SqlString)SqlInt32.Null).Value);
        }

        [Fact]
        public void SqlInt64ToSqlString()
        {
            SqlInt64 testInt = new SqlInt64(10101010);
            Assert.Equal("10101010", ((SqlString)testInt).Value);
        }

        [Fact]
        public void SqlMoneyToSqlString()
        {
            SqlMoney testMoney = new SqlMoney(646464.6464);
            Assert.Equal(646464.6464.ToString(), ((SqlString)testMoney).Value);
        }

        [Fact]
        public void SqlSingleToSqlString()
        {
            SqlSingle TestSingle = new SqlSingle(3E+20);
            Assert.Equal(3E+20.ToString(), ((SqlString)TestSingle).Value);
        }

        [Fact]
        public void SqlStringToString()
        {
            Assert.Equal("First TestString", (string)_test1);
        }

        [Fact]
        public void StringToSqlString()
        {
            string testString = "Test String";
            Assert.Equal("Test String", ((SqlString)testString).Value);
        }

        [Fact]
        public void AddSqlString()
        {
            Assert.Equal("First TestStringThis is just a test SqlString", (string)(SqlString.Add(_test1, _test2)));
            Assert.Equal("First TestStringPlainString", (string)(SqlString.Add(_test1, "PlainString")));
            Assert.True(SqlString.Add(_test1, null).IsNull);
        }

        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlString.GetXsdType(null);
            Assert.Equal("string", qualifiedName.Name);
        }

        internal void ReadWriteXmlTestInternal(string xml,
                               string testval,
                               string unit_test_id)
        {
            SqlString test;
            SqlString test1;
            XmlSerializer ser;
            StringWriter sw;
            XmlTextWriter xw;
            StringReader sr;
            XmlTextReader xr;

            test = new SqlString(testval);
            ser = new XmlSerializer(typeof(SqlString));
            sw = new StringWriter();
            xw = new XmlTextWriter(sw);

            ser.Serialize(xw, test);

            Assert.Equal(xml, sw.ToString());

            sr = new StringReader(xml);
            xr = new XmlTextReader(sr);
            test1 = (SqlString)ser.Deserialize(xr);

            Assert.Equal(testval, test1.Value);
        }

        [Fact]
        public void ReadWriteXmlTest()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><string>This is a test string</string>";
            string xml2 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><string>a</string>";
            string strtest1 = "This is a test string";
            char strtest2 = 'a';

            ReadWriteXmlTestInternal(xml1, strtest1, "BA01");
            ReadWriteXmlTestInternal(xml2, strtest2.ToString(), "BA02");
        }
    }
}
