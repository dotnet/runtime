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
using System.Globalization;
using System.Xml.Serialization;
using System.IO;

namespace System.Data.Tests.SqlTypes
{
    public class SqlDecimalTest
    {
        private CultureInfo _originalCulture;
        private SqlDecimal _test1;
        private SqlDecimal _test2;
        private SqlDecimal _test3;
        private SqlDecimal _test4;
        private SqlDecimal _test5;

        public SqlDecimalTest()
        {
            _test1 = new SqlDecimal(6464.6464m);
            _test2 = new SqlDecimal(10000.00m);
            _test3 = new SqlDecimal(10000.00m);
            _test4 = new SqlDecimal(-6m);
            _test5 = new SqlDecimal(decimal.MaxValue);
        }

        // Test constructor
        [Fact]
        public void Create()
        {
            // SqlDecimal (decimal)
            SqlDecimal test = new SqlDecimal(30.3098m);
            Assert.Equal((decimal)30.3098, test.Value);

            // SqlDecimal (double)
            test = new SqlDecimal(1E11d);
            Assert.Equal(1E11m, test.Value);

            Assert.Throws<OverflowException>(() => new SqlDecimal(1E201d));

            // SqlDecimal (int)
            test = new SqlDecimal(-1);
            Assert.Equal(-1m, test.Value);

            // SqlDecimal (long)
            test = new SqlDecimal((long)-99999);
            Assert.Equal(-99999m, test.Value);

            // SqlDecimal (byte, byte, bool. int[]
            test = new SqlDecimal(10, 3, false, new int[] { 200, 1, 0, 0 });
            Assert.Equal(-4294967.496m, test.Value);

            Assert.Throws<SqlTypeException>(() => 
                new SqlDecimal(100, 100, false, new int[4] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue }));

            // SqlDecimal (byte, byte, bool, int, int, int, int)
            test = new SqlDecimal(12, 2, true, 100, 100, 0, 0);
            Assert.Equal(4294967297.00m, test.Value);

            Assert.Throws<SqlTypeException>(() =>
                new SqlDecimal(100, 100, false, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
        }

        // Test public fields
        [Fact]
        public void PublicFields()
        {
            Assert.Equal((byte)38, SqlDecimal.MaxPrecision);
            Assert.Equal((byte)38, SqlDecimal.MaxScale);

            // FIXME: on windows: Conversion overflow
            Assert.Equal(1262177448, SqlDecimal.MaxValue.Data[3]);

            Assert.Equal(1262177448, SqlDecimal.MinValue.Data[3]);
            Assert.True(SqlDecimal.Null.IsNull);
            Assert.False(_test1.IsNull);
        }

        // Test properties
        [Fact]
        public void Properties()
        {
            byte[] b = _test1.BinData;
            Assert.Equal((byte)64, b[0]);

            int[] i = _test1.Data;
            Assert.Equal(64646464, i[0]);

            Assert.True(SqlDecimal.Null.IsNull);
            Assert.True(_test1.IsPositive);
            Assert.False(_test4.IsPositive);
            Assert.Equal((byte)8, _test1.Precision);
            Assert.Equal((byte)2, _test2.Scale);
            Assert.Equal(6464.6464m, _test1.Value);
            Assert.Equal((byte)4, _test1.Scale);
            Assert.Equal((byte)7, _test2.Precision);
            Assert.Equal((byte)1, _test4.Precision);
        }

        // PUBLIC METHODS
        [Fact]
        public void ArithmeticMethods()
        {
            // Abs
            Assert.Equal(6m, SqlDecimal.Abs(_test4));
            Assert.Equal(new SqlDecimal(6464.6464m).Value, SqlDecimal.Abs(_test1).Value);

            Assert.Equal(SqlDecimal.Null, SqlDecimal.Abs(SqlDecimal.Null));

            // Add()
            SqlDecimal test2 = new SqlDecimal(-2000m);
            Assert.Equal(16464.6464m, SqlDecimal.Add(_test1, _test2).Value);
            Assert.Equal("158456325028528675187087900670", SqlDecimal.Add(_test5, _test5).ToString());
            Assert.Equal(9994.00m, SqlDecimal.Add(_test3, _test4));
            Assert.Equal(-2006m, SqlDecimal.Add(_test4, test2));
            Assert.Equal(8000.00m, SqlDecimal.Add(test2, _test3));

            Assert.Throws<OverflowException>(() => SqlDecimal.Add(SqlDecimal.MaxValue, SqlDecimal.MaxValue));

            Assert.Equal(6465m, SqlDecimal.Ceiling(_test1));
            Assert.Equal(SqlDecimal.Null, SqlDecimal.Ceiling(SqlDecimal.Null));

            // Divide
            Assert.Equal(-1077.441066m, SqlDecimal.Divide(_test1, _test4));
            Assert.Equal(1.54687501546m, SqlDecimal.Divide(_test2, _test1).Value, 9);

            Assert.Throws<DivideByZeroException>(() => SqlDecimal.Divide(_test1, new SqlDecimal(0)));

            Assert.Equal(6464m, SqlDecimal.Floor(_test1));

            // Multiply()
            SqlDecimal Test;
            SqlDecimal test1 = new SqlDecimal(2m);
            Assert.Equal(64646464m, SqlDecimal.Multiply(_test1, _test2).Value);
            Assert.Equal(-38787.8784m, SqlDecimal.Multiply(_test1, _test4).Value);
            Test = SqlDecimal.Multiply(_test5, test1);
            Assert.Equal("158456325028528675187087900670", Test.ToString());

            Assert.Throws<OverflowException>(() => SqlDecimal.Multiply(SqlDecimal.MaxValue, _test1));

            // Power
            Assert.Equal(41791653.0770m, SqlDecimal.Power(_test1, 2));

            // Round
            Assert.Equal(6464.65m, SqlDecimal.Round(_test1, 2));

            // Subtract()
            Assert.Equal(-3535.3536m, SqlDecimal.Subtract(_test1, _test3).Value);
            Assert.Equal(10006.00m, SqlDecimal.Subtract(_test3, _test4).Value);
            Assert.Equal("99999999920771837485735662406456049664", SqlDecimal.Subtract(SqlDecimal.MaxValue, decimal.MaxValue).ToString());

            Assert.Throws<OverflowException>(() => SqlDecimal.Subtract(SqlDecimal.MinValue, SqlDecimal.MaxValue));

            Assert.Equal(1, SqlDecimal.Sign(_test1));
            Assert.Equal(new SqlInt32(-1), SqlDecimal.Sign(_test4));
        }

        [Fact]
        public void AdjustScale()
        {
            Assert.Equal(6464.646400m.ToString(), SqlDecimal.AdjustScale(_test1, 2, false).Value.ToString());
            Assert.Equal(6464.65.ToString(), SqlDecimal.AdjustScale(_test1, -2, true).Value.ToString());
            Assert.Equal(6464.64.ToString(), SqlDecimal.AdjustScale(_test1, -2, false).Value.ToString());
            Assert.Equal(10000.000000000000m.ToString(), SqlDecimal.AdjustScale(_test2, 10, false).Value.ToString());
            Assert.Equal("79228162514264337593543950335.00", SqlDecimal.AdjustScale(_test5, 2, false).ToString());

            Assert.Throws<SqlTruncateException>(() => SqlDecimal.AdjustScale(_test1, -5, false));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/95195", typeof(PlatformDetection), nameof(PlatformDetection.IsHybridGlobalizationOnApplePlatform))]
        public void ConvertToPrecScale()
        {
            Assert.Equal(new SqlDecimal(6464.6m).Value, SqlDecimal.ConvertToPrecScale(_test1, 5, 1).Value);

            Assert.Throws<SqlTruncateException>(() => SqlDecimal.ConvertToPrecScale(_test1, 6, 4));

            Assert.Equal("10000.00", SqlDecimal.ConvertToPrecScale(_test2, 7, 2).ToSqlString());

            SqlDecimal tmp = new SqlDecimal(38, 4, true, 64646464, 0, 0, 0);
            Assert.Equal("6465", SqlDecimal.ConvertToPrecScale(tmp, 4, 0).ToString());
        }

        [Fact]
        public void CompareTo()
        {
            SqlString TestString = new SqlString("This is a test");

            Assert.True(_test1.CompareTo(_test3) < 0);
            Assert.True(_test2.CompareTo(_test1) > 0);
            Assert.Equal(0, _test2.CompareTo(_test3));
            Assert.True(_test4.CompareTo(SqlDecimal.Null) > 0);

            Assert.Throws<ArgumentException>(() => _test1.CompareTo(TestString));
        }

        [Fact]
        public void EqualsMethods()
        {
            Assert.False(_test1.Equals(_test2));
            Assert.False(_test1.Equals((object)_test2));
            Assert.False(_test2.Equals(new SqlString("TEST")));
            Assert.True(_test2.Equals(_test3));
            Assert.True(_test2.Equals((object)_test3));

            // Static Equals()-method
            Assert.True(SqlDecimal.Equals(_test2, _test2).Value);
            Assert.False(SqlDecimal.Equals(_test1, _test2).Value);

            // NotEquals
            Assert.True(SqlDecimal.NotEquals(_test1, _test2).Value);
            Assert.True(SqlDecimal.NotEquals(_test4, _test1).Value);
            Assert.False(SqlDecimal.NotEquals(_test2, _test3).Value);
            Assert.True(SqlDecimal.NotEquals(SqlDecimal.Null, _test3).IsNull);
        }

        [Fact]
        public void Greaters()
        {
            // GreaterThan ()
            Assert.False(SqlDecimal.GreaterThan(_test1, _test2).Value);
            Assert.True(SqlDecimal.GreaterThan(_test2, _test1).Value);
            Assert.False(SqlDecimal.GreaterThan(_test2, _test3).Value);

            // GreaterThanOrEqual ()
            Assert.False(SqlDecimal.GreaterThanOrEqual(_test1, _test2).Value);
            Assert.True(SqlDecimal.GreaterThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlDecimal.GreaterThanOrEqual(_test2, _test3).Value);
        }

        [Fact]
        public void Lessers()
        {
            // LessThan()
            Assert.False(SqlDecimal.LessThan(_test3, _test2).Value);
            Assert.False(SqlDecimal.LessThan(_test2, _test1).Value);
            Assert.True(SqlDecimal.LessThan(_test1, _test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlDecimal.LessThanOrEqual(_test1, _test2).Value);
            Assert.False(SqlDecimal.LessThanOrEqual(_test2, _test1).Value);
            Assert.True(SqlDecimal.LessThanOrEqual(_test2, _test3).Value);
            Assert.True(SqlDecimal.LessThanOrEqual(_test1, SqlDecimal.Null).IsNull);
        }

        [Fact]
        public void Conversions()
        {
            // ToDouble
            Assert.Equal(6464.6464, _test1.ToDouble());

            // ToSqlBoolean ()
            Assert.Equal(new SqlBoolean(1), _test1.ToSqlBoolean());

            SqlDecimal test = new SqlDecimal(0);
            Assert.False(test.ToSqlBoolean().Value);

            test = new SqlDecimal(0);
            Assert.False(test.ToSqlBoolean().Value);
            Assert.True(SqlDecimal.Null.ToSqlBoolean().IsNull);

            // ToSqlByte ()
            test = new SqlDecimal(250);
            Assert.Equal((byte)250, test.ToSqlByte().Value);

            Assert.Throws<OverflowException>(() => (byte)_test2.ToSqlByte());

            // ToSqlDouble ()
            Assert.Equal(6464.6464, _test1.ToSqlDouble());

            // ToSqlInt16 ()
            Assert.Equal((short)1, new SqlDecimal(1).ToSqlInt16().Value);

            Assert.Throws<OverflowException>(() => SqlDecimal.MaxValue.ToSqlInt16().Value);
            // ToSqlInt32 () 
            // 6464.6464 --> 64646464 ??? with windows
            // MS.NET seems to return the first 32 bit integer (i.e.
            // Data [0]) but we don't have to follow such stupidity.
            //            Assert.Equal ((int)64646464, Test1.ToSqlInt32 ().Value);
            //            Assert.Equal ((int)1212, new SqlDecimal(12.12m).ToSqlInt32 ().Value);

            Assert.Throws<OverflowException>(() => SqlDecimal.MaxValue.ToSqlInt32().Value);

            // ToSqlInt64 ()
            Assert.Equal(6464, _test1.ToSqlInt64().Value);

            // ToSqlMoney ()
            Assert.Equal((decimal)6464.6464, _test1.ToSqlMoney().Value);

            Assert.Throws<OverflowException>(() => SqlDecimal.MaxValue.ToSqlMoney().Value);

            // ToSqlSingle ()
            Assert.Equal((float)6464.6464, _test1.ToSqlSingle().Value);

            // ToSqlString ()
            Assert.Equal("6464.6464", _test1.ToSqlString().Value);

            // ToString ()
            Assert.Equal("6464.6464", _test1.ToString());
            // NOT WORKING
            Assert.Equal("792281625142643375935439503350000.00", SqlDecimal.Multiply(_test5, _test2).ToString());
            Assert.Equal(1E+38, SqlDecimal.MaxValue.ToSqlDouble());
        }

        [Fact]
        public void Truncate()
        {
            Assert.Equal(new SqlDecimal(6464.6400m).Value, SqlDecimal.Truncate(_test1, 2).Value);
            Assert.Equal(6464.6400m, SqlDecimal.Truncate(_test1, 2).Value);
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            // "+"-operator
            Assert.Equal(new SqlDecimal(16464.6464m), _test1 + _test2);
            Assert.Equal("79228162514264337593543960335.00", (_test5 + _test3).ToString());

            SqlDecimal test2 = new SqlDecimal(-2000m);
            Assert.Equal(8000.00m, _test3 + test2);
            Assert.Equal(-2006m, _test4 + test2);
            Assert.Equal(8000.00m, test2 + _test3);

            Assert.Throws<OverflowException>(() => SqlDecimal.MaxValue + SqlDecimal.MaxValue);

            // "/"-operator => NotWorking
            //Assert.Equal ((SqlDecimal)1.54687501546m, Test2 / Test1);

            Assert.Throws<DivideByZeroException>(() => SqlDecimal.MaxValue / new SqlDecimal(0));

            // "*"-operator
            Assert.Equal(64646464.000000m, _test1 * _test2);

            SqlDecimal Test = _test5 * (new SqlDecimal(2m));
            Assert.Equal("158456325028528675187087900670", Test.ToString());

            Assert.Throws<OverflowException>(() => SqlDecimal.MaxValue * _test1);

            // "-"-operator
            Assert.Equal(3535.3536m, _test2 - _test1);
            Assert.Equal(-10006.00m, _test4 - _test3);

            Assert.Throws<OverflowException>(() => SqlDecimal.MinValue - SqlDecimal.MaxValue);

            Assert.Equal(SqlDecimal.Null, SqlDecimal.Null + _test1);
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlDecimal pval = new SqlDecimal(10m);
            SqlDecimal nval = new SqlDecimal(-10m);
            SqlDecimal val = new SqlDecimal(5m);

            // == -operator
            Assert.True((_test2 == _test3).Value);
            Assert.False((_test1 == _test2).Value);
            Assert.True((_test1 == SqlDecimal.Null).IsNull);
            Assert.False((pval == nval).Value);

            // != -operator
            Assert.False((_test2 != _test3).Value);
            Assert.True((_test1 != _test3).Value);
            Assert.True((_test4 != _test3).Value);
            Assert.True((_test1 != SqlDecimal.Null).IsNull);
            Assert.True((pval != nval).Value);

            // > -operator
            Assert.True((_test2 > _test1).Value);
            Assert.False((_test1 > _test3).Value);
            Assert.False((_test2 > _test3).Value);
            Assert.True((_test1 > SqlDecimal.Null).IsNull);
            Assert.False((nval > val).Value);

            // >=  -operator
            Assert.False((_test1 >= _test3).Value);
            Assert.True((_test3 >= _test1).Value);
            Assert.True((_test2 >= _test3).Value);
            Assert.True((_test1 >= SqlDecimal.Null).IsNull);
            Assert.False((nval > val).Value);

            // < -operator
            Assert.False((_test2 < _test1).Value);
            Assert.True((_test1 < _test3).Value);
            Assert.False((_test2 < _test3).Value);
            Assert.True((_test1 < SqlDecimal.Null).IsNull);
            Assert.False((val < nval).Value);

            // <= -operator
            Assert.True((_test1 <= _test3).Value);
            Assert.False((_test3 <= _test1).Value);
            Assert.True((_test2 <= _test3).Value);
            Assert.True((_test1 <= SqlDecimal.Null).IsNull);
            Assert.False((val <= nval).Value);
        }

        [Fact]
        public void UnaryNegation()
        {
            Assert.Equal(6m, -_test4.Value);
            Assert.Equal(-6464.6464m, -_test1.Value);
            Assert.Equal(SqlDecimal.Null, SqlDecimal.Null);
        }

        [Fact]
        public void SqlBooleanToSqlDecimal()
        {
            SqlBoolean testBoolean = new SqlBoolean(true);
            SqlDecimal result;

            result = (SqlDecimal)testBoolean;

            Assert.Equal(1m, result.Value);

            result = (SqlDecimal)SqlBoolean.Null;
            Assert.True(result.IsNull);
            Assert.Equal(SqlDecimal.Null, (SqlDecimal)SqlBoolean.Null);
        }

        [Fact]
        public void SqlDecimalToDecimal()
        {
            Assert.Equal(6464.6464m, (decimal)_test1);
        }

        [Fact]
        public void SqlDoubleToSqlDecimal()
        {
            SqlDouble test = new SqlDouble(12E+10);
            Assert.Equal(120000000000m, ((SqlDecimal)test).Value);
        }

        [Fact]
        public void SqlSingleToSqlDecimal()
        {
            SqlSingle test = new SqlSingle(1E+9);
            Assert.Equal(1000000000m, ((SqlDecimal)test).Value);

            Assert.Throws<OverflowException>(() => (SqlDecimal)SqlSingle.MaxValue);
        }

        [Fact]
        public void SqlStringToSqlDecimal()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");

            Assert.Equal(100m, ((SqlDecimal)TestString100).Value);

            Assert.Throws<FormatException>(() => (SqlDecimal)TestString);

            Assert.Throws<FormatException>(() => (SqlDecimal)new SqlString("9E+100"));
        }

        [Fact]
        public void DecimalToSqlDecimal()
        {
            decimal d = 1000.1m;
            Assert.Equal(1000.1m, (SqlDecimal)d);
        }

        [Fact]
        public void ByteToSqlDecimal()
        {
            Assert.Equal(255m, ((SqlDecimal)SqlByte.MaxValue).Value);
        }

        [Fact]
        public void SqlIntToSqlDouble()
        {
            SqlInt16 Test64 = new SqlInt16(64);
            SqlInt32 Test640 = new SqlInt32(640);
            SqlInt64 Test64000 = new SqlInt64(64000);
            Assert.Equal(64m, ((SqlDecimal)Test64).Value);
            Assert.Equal(640m, ((SqlDecimal)Test640).Value);
            Assert.Equal(64000m, ((SqlDecimal)Test64000).Value);
        }

        [Fact]
        public void SqlMoneyToSqlDecimal()
        {
            SqlMoney TestMoney64 = new SqlMoney(64);
            Assert.Equal(64.0000M, ((SqlDecimal)TestMoney64).Value);
        }

        [Fact]
        public void ToStringTest()
        {
            Assert.Equal("Null", SqlDecimal.Null.ToString());
            Assert.Equal("-99999999999999999999999999999999999999", SqlDecimal.MinValue.ToString());
            Assert.Equal("99999999999999999999999999999999999999", SqlDecimal.MaxValue.ToString());
        }

        [Fact]
        public void Value()
        {
            decimal d = decimal.Parse("9999999999999999999999999999");
            Assert.Equal(9999999999999999999999999999m, d);
        }

        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlDecimal.GetXsdType(null);
            Assert.Equal("decimal", qualifiedName.Name);
        }

        internal void ReadWriteXmlTestInternal(string xml,
                               decimal testval,
                               string unit_test_id)
        {
            SqlDecimal test;
            SqlDecimal test1;
            XmlSerializer ser;
            StringWriter sw;
            XmlTextWriter xw;
            StringReader sr;
            XmlTextReader xr;

            test = new SqlDecimal(testval);
            ser = new XmlSerializer(typeof(SqlDecimal));
            sw = new StringWriter();
            xw = new XmlTextWriter(sw);

            ser.Serialize(xw, test);

            // Assert.Equal (xml, sw.ToString ());

            sr = new StringReader(xml);
            xr = new XmlTextReader(sr);
            test1 = (SqlDecimal)ser.Deserialize(xr);

            Assert.Equal(testval, test1.Value);
        }

        [Fact]
        //[Category ("MobileNotWorking")]
        public void ReadWriteXmlTest()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><decimal>4556.89756</decimal>";
            string xml2 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><decimal>-6445.9999</decimal>";
            string xml3 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><decimal>0x455687AB3E4D56F</decimal>";
            decimal test1 = new decimal(4556.89756);
            // This one fails because of a possible conversion bug
            //decimal test2 = new Decimal (-6445.999999999999999999999);
            decimal test2 = new decimal(-6445.9999);
            decimal test3 = new decimal(0x455687AB3E4D56F);

            ReadWriteXmlTestInternal(xml1, test1, "BA01");
            ReadWriteXmlTestInternal(xml2, test2, "BA02");

            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => ReadWriteXmlTestInternal(xml3, test3, "BA03"));
            Assert.Equal(typeof(FormatException), ex.InnerException.GetType());
        }

        [Fact]
        public void WriteTdsValue()
        {
            uint[] array = new uint[5];
            Span<uint> span = array;
            Array.Clear(array, 0, array.Length);

            int count = SqlDecimal.MaxValue.WriteTdsValue(span);
            Assert.Equal(4, count);
            Assert.Equal(0xFFFFFFFFu, array[0]);
            Assert.Equal(0x098a223fu, array[1]);
            Assert.Equal(0x5a86c47au, array[2]);
            Assert.Equal(0x4b3b4ca8u, array[3]);

            Assert.Equal(0u, array[4]);

            array = new uint[3];
            Assert.Throws<ArgumentOutOfRangeException>( () => { _ = SqlDecimal.MaxValue.WriteTdsValue(array); } );

        }
    }
}
