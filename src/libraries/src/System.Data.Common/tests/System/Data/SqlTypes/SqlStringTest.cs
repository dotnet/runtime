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

using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.Diagnostics;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

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

        [Theory]
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
            SqlString TestString = new SqlString("Test");
            Assert.Equal("Test", TestString.Value);

            // SqlString (String, int)
            TestString = new SqlString("Test", 2057);
            Assert.Equal(2057, TestString.LCID);

            // SqlString (int, SqlCompareOptions, byte[])
            TestString = new SqlString(2057,
                SqlCompareOptions.BinarySort | SqlCompareOptions.IgnoreCase,
                new byte[2] { 123, 221 });
            Assert.Equal(2057, TestString.CompareInfo.LCID);

            // SqlString(string, int, SqlCompareOptions)
            TestString = new SqlString("Test", 2057, SqlCompareOptions.IgnoreNonSpace);
            Assert.True(!TestString.IsNull);

            // SqlString (int, SqlCompareOptions, byte[], bool)
            TestString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[4] { 100, 100, 200, 45 }, true);
            Assert.Equal((byte)63, TestString.GetNonUnicodeBytes()[0]);
            TestString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[2] { 113, 100 }, false);
            Assert.Equal("qd", TestString.Value);

            // SqlString (int, SqlCompareOptions, byte[], int, int)
            TestString = new SqlString(2057, SqlCompareOptions.BinarySort, new byte[2] { 113, 100 }, 0, 2);
            Assert.True(!TestString.IsNull);

            // SqlString (int, SqlCompareOptions, byte[], int, int, bool)
            TestString = new SqlString(2057, SqlCompareOptions.IgnoreCase, new byte[3] { 100, 111, 50 }, 1, 2, false);
            Assert.Equal("o2", TestString.Value);
            TestString = new SqlString(2057, SqlCompareOptions.IgnoreCase, new byte[3] { 123, 111, 222 }, 1, 2, true);
            Assert.True(!TestString.IsNull);
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
        [Fact]
        public void Properties()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-AU");
                var one = new SqlString("First TestString");

                // CompareInfo
                Assert.Equal(3081, one.CompareInfo.LCID);

                // CultureInfo
                Assert.Equal(3081, one.CultureInfo.LCID);

                // LCID
                Assert.Equal(3081, one.LCID);

                // IsNull
                Assert.True(!one.IsNull);
                Assert.True(SqlString.Null.IsNull);

                // SqlCompareOptions
                Assert.Equal("IgnoreCase, IgnoreKanaType, IgnoreWidth", one.SqlCompareOptions.ToString());

                // Value
                Assert.Equal("First TestString", one.Value);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        // PUBLIC METHODS

        [Fact]
        public void CompareToArgumentException()
        {
            SqlByte Test = new SqlByte(1);
            AssertExtensions.Throws<ArgumentException>(null, () => _test1.CompareTo(Test));
        }

        [Fact]
        public void CompareToSqlTypeException()
        {
            SqlString T1 = new SqlString("test", 2057, SqlCompareOptions.IgnoreCase);
            SqlString T2 = new SqlString("TEST", 2057, SqlCompareOptions.None);
            Assert.Throws<SqlTypeException>(() => T1.CompareTo(T2));
        }

        [Fact]
        public void CompareTo()
        {
            SqlByte Test = new SqlByte(1);

            Assert.True(_test1.CompareTo(_test3) < 0);
            Assert.True(_test2.CompareTo(_test1) > 0);
            Assert.True(_test2.CompareTo(_test3) == 0);
            Assert.True(_test3.CompareTo(SqlString.Null) > 0);

            SqlString T1 = new SqlString("test", 2057, SqlCompareOptions.IgnoreCase);
            SqlString T2 = new SqlString("TEST", 2057, SqlCompareOptions.None);

            // IgnoreCase
            T1 = new SqlString("test", 2057, SqlCompareOptions.IgnoreCase);
            T2 = new SqlString("TEST", 2057, SqlCompareOptions.IgnoreCase);
            Assert.True(T2.CompareTo(T1) == 0);

            T1 = new SqlString("test", 2057);
            T2 = new SqlString("TEST", 2057);
            Assert.True(T2.CompareTo(T1) == 0);

            T1 = new SqlString("test", 2057, SqlCompareOptions.None);
            T2 = new SqlString("TEST", 2057, SqlCompareOptions.None);
            Assert.True(T2.CompareTo(T1) != 0);

            // IgnoreNonSpace
            T1 = new SqlString("TEST\xF1", 2057, SqlCompareOptions.IgnoreNonSpace);
            T2 = new SqlString("TESTn", 2057, SqlCompareOptions.IgnoreNonSpace);
            Assert.True(T2.CompareTo(T1) == 0);

            T1 = new SqlString("TEST\u00F1", 2057, SqlCompareOptions.None);
            T2 = new SqlString("TESTn", 2057, SqlCompareOptions.None);
            Assert.True(T2.CompareTo(T1) != 0);

            // BinarySort
            T1 = new SqlString("01_", 2057, SqlCompareOptions.BinarySort);
            T2 = new SqlString("_01", 2057, SqlCompareOptions.BinarySort);
            Assert.True(T1.CompareTo(T2) < 0);

            T1 = new SqlString("01_", 2057, SqlCompareOptions.None);
            T2 = new SqlString("_01", 2057, SqlCompareOptions.None);
            Assert.True(T1.CompareTo(T2) > 0);
        }

        [Fact]
        public void EqualsMethods()
        {
            Assert.True(!_test1.Equals(_test2));
            Assert.True(!_test3.Equals(_test1));
            Assert.True(!_test2.Equals(new SqlString("TEST")));
            Assert.True(_test2.Equals(_test3));

            // Static Equals()-method
            Assert.True(SqlString.Equals(_test2, _test3).Value);
            Assert.True(!SqlString.Equals(_test1, _test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            // FIXME: Better way to test HashCode
            Assert.Equal(_test1.GetHashCode(), _test1.GetHashCode());
            Assert.True(_test1.GetHashCode() != _test2.GetHashCode());
            Assert.True(_test2.GetHashCode() == _test2.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            Assert.Equal("System.Data.SqlTypes.SqlString", _test1.GetType().ToString());
            Assert.Equal("System.String", _test1.Value.GetType().ToString());
        }

        [Fact]
        public void Greaters()
        {
            // GreateThan ()
            Assert.True(!SqlString.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlString.GreaterThan(_test2, _test1).Value);
            Assert.True(!SqlString.GreaterThan(_test2, _test3).Value);

            // GreaterTharOrEqual ()
            Assert.True(!SqlString.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlString.GreaterThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlString.GreaterThanOrEqual(_test2, _test3).Value);
        }

        [Fact]
        public void Lessers()
        {
            // LessThan()
            Assert.True(!SqlString.LessThan(_test2, _test3).Value);
            Assert.True(!SqlString.LessThan(_test2, _test1).Value);
            Assert.True(SqlString.LessThan(_test1, _test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlString.LessThanOrEqual(_test1, _test2).Value);
            Assert.True(!SqlString.LessThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlString.LessThanOrEqual(_test3, _test2).Value);
            Assert.True(SqlString.LessThanOrEqual(_test2, SqlString.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            Assert.True(SqlString.NotEquals(_test1, _test2).Value);
            Assert.True(SqlString.NotEquals(_test2, _test1).Value);
            Assert.True(SqlString.NotEquals(_test3, _test1).Value);
            Assert.True(!SqlString.NotEquals(_test2, _test3).Value);
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
            SqlString TestSqlString = _test1.Clone();
            Assert.Equal(_test1, TestSqlString);
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

            try
            {
                byte test = _test1.GetUnicodeBytes()[105];
                Assert.False(true);
            }
            catch (Exception e)
            {
                Assert.Equal(typeof(IndexOutOfRangeException), e.GetType());
            }
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
            SqlString String250 = new SqlString("250");
            SqlString String9E300 = new SqlString("9E+300");

            // ToSqlBoolean ()
            Assert.True((new SqlString("1")).ToSqlBoolean().Value);
            Assert.True(!(new SqlString("0")).ToSqlBoolean().Value);
            Assert.True((new SqlString("True")).ToSqlBoolean().Value);
            Assert.True(!(new SqlString("FALSE")).ToSqlBoolean().Value);
            Assert.True(SqlString.Null.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            Assert.Equal((byte)250, String250.ToSqlByte().Value);

            // ToSqlDateTime
            Assert.Equal(10, (new SqlString("2002-10-10")).ToSqlDateTime().Value.Day);

            // ToSqlDecimal ()
            Assert.Equal(250, String250.ToSqlDecimal().Value);

            // ToSqlDouble
            Assert.Equal(9E+300, String9E300.ToSqlDouble());

            // ToSqlGuid
            SqlString TestGuid = new SqlString("11111111-1111-1111-1111-111111111111");
            Assert.Equal(new SqlGuid("11111111-1111-1111-1111-111111111111"), TestGuid.ToSqlGuid());

            // ToSqlInt16 ()
            Assert.Equal((short)250, String250.ToSqlInt16().Value);

            // ToSqlInt32 ()
            Assert.Equal(250, String250.ToSqlInt32().Value);

            // ToSqlInt64 ()
            Assert.Equal(250, String250.ToSqlInt64().Value);

            // ToSqlMoney ()
            Assert.Equal(250.0000M, String250.ToSqlMoney().Value);

            // ToSqlSingle ()
            Assert.Equal(250, String250.ToSqlSingle().Value);

            // ToString ()
            Assert.Equal("First TestString", _test1.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlString TestString = new SqlString("...Testing...");
            Assert.Equal("First TestString...Testing...", _test1 + TestString);
            Assert.Equal(SqlString.Null, _test1 + SqlString.Null);
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            // == -operator
            Assert.True((_test2 == _test3).Value);
            Assert.True(!(_test1 == _test2).Value);
            Assert.True((_test1 == SqlString.Null).IsNull);

            // != -operator
            Assert.True(!(_test3 != _test2).Value);
            Assert.True(!(_test2 != _test3).Value);
            Assert.True((_test1 != _test3).Value);
            Assert.True((_test1 != SqlString.Null).IsNull);

            // > -operator
            Assert.True((_test2 > _test1).Value);
            Assert.True(!(_test1 > _test3).Value);
            Assert.True(!(_test2 > _test3).Value);
            Assert.True((_test1 > SqlString.Null).IsNull);

            // >= -operator
            Assert.True(!(_test1 >= _test3).Value);
            Assert.True((_test3 >= _test1).Value);
            Assert.True((_test2 >= _test3).Value);
            Assert.True((_test1 >= SqlString.Null).IsNull);

            // < -operator
            Assert.True((_test1 < _test2).Value);
            Assert.True((_test1 < _test3).Value);
            Assert.True(!(_test2 < _test3).Value);
            Assert.True((_test1 < SqlString.Null).IsNull);

            // <= -operator
            Assert.True((_test1 <= _test3).Value);
            Assert.True(!(_test3 <= _test1).Value);
            Assert.True((_test2 <= _test3).Value);
            Assert.True((_test1 <= SqlString.Null).IsNull);
        }

        [Fact]
        public void SqlBooleanToSqlString()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);
            SqlBoolean TestBoolean2 = new SqlBoolean(false);
            SqlString Result;

            Result = (SqlString)TestBoolean;
            Assert.Equal("True", Result.Value);

            Result = (SqlString)TestBoolean2;
            Assert.Equal("False", Result.Value);

            Result = (SqlString)SqlBoolean.Null;
            Assert.True(Result.IsNull);
        }

        [Fact]
        public void SqlByteToBoolean()
        {
            SqlByte TestByte = new SqlByte(250);
            Assert.Equal("250", ((SqlString)TestByte).Value);
            try
            {
                SqlString test = ((SqlString)SqlByte.Null).Value;
                Assert.False(true);
            }
            catch (SqlNullValueException e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }
        }

        [Fact]
        public void SqlDateTimeToSqlString()
        {
            SqlDateTime TestTime = new SqlDateTime(2002, 10, 22, 9, 52, 30);
            Assert.Equal(TestTime.Value.ToString((IFormatProvider)null), ((SqlString)TestTime).Value);
        }

        [Fact]
        public void SqlDecimalToSqlString()
        {
            SqlDecimal TestDecimal = new SqlDecimal(1000.2345);
            Assert.Equal("1000.2345000000000", ((SqlString)TestDecimal).Value);
        }

        [Fact]
        public void SqlDoubleToSqlString()
        {
            SqlDouble TestDouble = new SqlDouble(64E+64);
            Assert.Equal(6.4E+65.ToString(), ((SqlString)TestDouble).Value);
        }

        [Fact]
        public void SqlGuidToSqlString()
        {
            byte[] b = new byte[16];
            b[0] = 100;
            b[1] = 64;
            SqlGuid TestGuid = new SqlGuid(b);

            Assert.Equal("00004064-0000-0000-0000-000000000000", ((SqlString)TestGuid).Value);
            try
            {
                SqlString test = ((SqlString)SqlGuid.Null).Value;
                Assert.False(true);
            }
            catch (SqlNullValueException e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt16ToSqlString()
        {
            SqlInt16 TestInt = new SqlInt16(20012);
            Assert.Equal("20012", ((SqlString)TestInt).Value);
            try
            {
                SqlString test = ((SqlString)SqlInt16.Null).Value;
                Assert.False(true);
            }
            catch (SqlNullValueException e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt32ToSqlString()
        {
            SqlInt32 TestInt = new SqlInt32(-12456);
            Assert.Equal("-12456", ((SqlString)TestInt).Value);
            try
            {
                SqlString test = ((SqlString)SqlInt32.Null).Value;
                Assert.False(true);
            }
            catch (SqlNullValueException e)
            {
                Assert.Equal(typeof(SqlNullValueException), e.GetType());
            }
        }

        [Fact]
        public void SqlInt64ToSqlString()
        {
            SqlInt64 TestInt = new SqlInt64(10101010);
            Assert.Equal("10101010", ((SqlString)TestInt).Value);
        }

        [Fact]
        public void SqlMoneyToSqlString()
        {
            SqlMoney TestMoney = new SqlMoney(646464.6464);
            Assert.Equal(646464.6464.ToString(), ((SqlString)TestMoney).Value);
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
            string TestString = "Test String";
            Assert.Equal("Test String", ((SqlString)TestString).Value);
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
