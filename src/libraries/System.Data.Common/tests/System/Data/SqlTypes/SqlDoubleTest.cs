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
            SqlDouble Test = new SqlDouble(34.87);
            Assert.Equal(34.87D, Test.Value);

            Test = new SqlDouble(-9000.6543);
            Assert.Equal(-9000.6543D, Test.Value);
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
            SqlDouble Test5443 = new SqlDouble(5443e12);
            SqlDouble Test1 = new SqlDouble(1);

            Assert.True(SqlDouble.Null.IsNull);
            Assert.Equal(5443e12, Test5443.Value);
            Assert.Equal(1, Test1.Value);
        }

        // PUBLIC METHODS

        [Fact]
        public void ArithmeticMethods()
        {
            SqlDouble Test0 = new SqlDouble(0);
            SqlDouble Test1 = new SqlDouble(15E+108);
            SqlDouble Test2 = new SqlDouble(-65E+64);
            SqlDouble Test3 = new SqlDouble(5E+64);
            SqlDouble Test4 = new SqlDouble(5E+108);
            SqlDouble TestMax = new SqlDouble(SqlDouble.MaxValue.Value);

            // Add()
            Assert.Equal(15E+108, SqlDouble.Add(Test1, Test0).Value);
            Assert.Equal(1.5E+109, SqlDouble.Add(Test1, Test2).Value);

            try
            {
                SqlDouble test = SqlDouble.Add(SqlDouble.MaxValue, SqlDouble.MaxValue);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // Divide()
            Assert.Equal(3, SqlDouble.Divide(Test1, Test4));
            Assert.Equal(-13d, SqlDouble.Divide(Test2, Test3).Value);

            try
            {
                SqlDouble test = SqlDouble.Divide(Test1, Test0).Value;
                Assert.False(true);
            }
            catch (DivideByZeroException e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // Multiply()
            Assert.Equal(75E+216, SqlDouble.Multiply(Test1, Test4).Value);
            Assert.Equal(0, SqlDouble.Multiply(Test1, Test0).Value);

            try
            {
                SqlDouble test = SqlDouble.Multiply(TestMax, Test1);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }


            // Subtract()
            Assert.Equal(1.5E+109, SqlDouble.Subtract(Test1, Test3).Value);

            try
            {
                SqlDouble test = SqlDouble.Subtract(SqlDouble.MinValue, SqlDouble.MaxValue);
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void CompareTo()
        {
            SqlDouble Test1 = new SqlDouble(4e64);
            SqlDouble Test11 = new SqlDouble(4e64);
            SqlDouble Test2 = new SqlDouble(-9e34);
            SqlDouble Test3 = new SqlDouble(10000);
            SqlString TestString = new SqlString("This is a test");

            Assert.True(Test1.CompareTo(Test3) > 0);
            Assert.True(Test2.CompareTo(Test3) < 0);
            Assert.True(Test1.CompareTo(Test11) == 0);
            Assert.True(Test11.CompareTo(SqlDouble.Null) > 0);

            try
            {
                Test1.CompareTo(TestString);
                Assert.False(true);
            }
            catch (ArgumentException e)
            {
                Assert.Equal(typeof(ArgumentException), e.GetType());
            }
        }

        [Fact]
        public void EqualsMethods()
        {
            SqlDouble Test0 = new SqlDouble(0);
            SqlDouble Test1 = new SqlDouble(1.58e30);
            SqlDouble Test2 = new SqlDouble(1.8e180);
            SqlDouble Test22 = new SqlDouble(1.8e180);

            Assert.True(!Test0.Equals(Test1));
            Assert.True(!Test1.Equals(Test2));
            Assert.True(!Test2.Equals(new SqlString("TEST")));
            Assert.True(Test2.Equals(Test22));

            // Static Equals()-method
            Assert.True(SqlDouble.Equals(Test2, Test22).Value);
            Assert.True(!SqlDouble.Equals(Test1, Test2).Value);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            SqlDouble Test15 = new SqlDouble(15);

            // FIXME: Better way to test HashCode
            Assert.Equal(Test15.GetHashCode(), Test15.GetHashCode());
        }

        [Fact]
        public void GetTypeTest()
        {
            SqlDouble Test = new SqlDouble(84);
            Assert.Equal("System.Data.SqlTypes.SqlDouble", Test.GetType().ToString());
            Assert.Equal("System.Double", Test.Value.GetType().ToString());
        }

        [Fact]
        public void Greaters()
        {
            SqlDouble Test1 = new SqlDouble(1e100);
            SqlDouble Test11 = new SqlDouble(1e100);
            SqlDouble Test2 = new SqlDouble(64e164);

            // GreateThan ()
            Assert.True(!SqlDouble.GreaterThan(Test1, Test2).Value);
            Assert.True(SqlDouble.GreaterThan(Test2, Test1).Value);
            Assert.True(!SqlDouble.GreaterThan(Test1, Test11).Value);

            // GreaterTharOrEqual ()
            Assert.True(!SqlDouble.GreaterThanOrEqual(Test1, Test2).Value);
            Assert.True(SqlDouble.GreaterThanOrEqual(Test2, Test1).Value);
            Assert.True(SqlDouble.GreaterThanOrEqual(Test1, Test11).Value);
        }

        [Fact]
        public void Lessers()
        {
            SqlDouble Test1 = new SqlDouble(1.8e100);
            SqlDouble Test11 = new SqlDouble(1.8e100);
            SqlDouble Test2 = new SqlDouble(64e164);

            // LessThan()
            Assert.True(!SqlDouble.LessThan(Test1, Test11).Value);
            Assert.True(!SqlDouble.LessThan(Test2, Test1).Value);
            Assert.True(SqlDouble.LessThan(Test11, Test2).Value);

            // LessThanOrEqual ()
            Assert.True(SqlDouble.LessThanOrEqual(Test1, Test2).Value);
            Assert.True(!SqlDouble.LessThanOrEqual(Test2, Test1).Value);
            Assert.True(SqlDouble.LessThanOrEqual(Test11, Test1).Value);
            Assert.True(SqlDouble.LessThanOrEqual(Test11, SqlDouble.Null).IsNull);
        }

        [Fact]
        public void NotEquals()
        {
            SqlDouble Test1 = new SqlDouble(1280000000001);
            SqlDouble Test2 = new SqlDouble(128e10);
            SqlDouble Test22 = new SqlDouble(128e10);

            Assert.True(SqlDouble.NotEquals(Test1, Test2).Value);
            Assert.True(SqlDouble.NotEquals(Test2, Test1).Value);
            Assert.True(SqlDouble.NotEquals(Test22, Test1).Value);
            Assert.True(!SqlDouble.NotEquals(Test22, Test2).Value);
            Assert.True(!SqlDouble.NotEquals(Test2, Test22).Value);
            Assert.True(SqlDouble.NotEquals(SqlDouble.Null, Test22).IsNull);
            Assert.True(SqlDouble.NotEquals(SqlDouble.Null, Test22).IsNull);
        }

        [Fact]
        public void Parse()
        {
            try
            {
                SqlDouble.Parse(null);
                Assert.False(true);
            }
            catch (ArgumentNullException e)
            {
                Assert.Equal(typeof(ArgumentNullException), e.GetType());
            }

            try
            {
                SqlDouble.Parse("not-a-number");
                Assert.False(true);
            }
            catch (FormatException e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }

            try
            {
                SqlDouble.Parse("9e400");
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            Assert.Equal(150, SqlDouble.Parse("150").Value);
        }

        [Fact]
        public void Conversions()
        {
            SqlDouble Test0 = new SqlDouble(0);
            SqlDouble Test1 = new SqlDouble(250);
            SqlDouble Test2 = new SqlDouble(64e64);
            SqlDouble Test3 = new SqlDouble(64e164);
            SqlDouble TestNull = SqlDouble.Null;

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
            catch (OverflowException e)
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
            catch (OverflowException e)
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
            catch (OverflowException e)
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
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlInt64 ()
            Assert.Equal(250, Test1.ToSqlInt64().Value);
            Assert.Equal(0, Test0.ToSqlInt64().Value);

            try
            {
                SqlInt64 test = Test2.ToSqlInt64().Value;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlMoney ()
            Assert.Equal(250.0000M, Test1.ToSqlMoney().Value);
            Assert.Equal(0, Test0.ToSqlMoney().Value);

            try
            {
                SqlMoney test = Test2.ToSqlMoney().Value;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlSingle ()
            Assert.Equal(250, Test1.ToSqlSingle().Value);
            Assert.Equal(0, Test0.ToSqlSingle().Value);

            try
            {
                SqlSingle test = Test2.ToSqlSingle().Value;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // ToSqlString ()
            Assert.Equal("250", Test1.ToSqlString().Value);
            Assert.Equal("0", Test0.ToSqlString().Value);
            Assert.Equal("6.4E+65", Test2.ToSqlString().Value);

            // ToString ()
            Assert.Equal("250", Test1.ToString());
            Assert.Equal("0", Test0.ToString());
            Assert.Equal("6.4E+65", Test2.ToString());
        }

        // OPERATORS

        [Fact]
        public void ArithmeticOperators()
        {
            SqlDouble Test0 = new SqlDouble(0);
            SqlDouble Test1 = new SqlDouble(24E+100);
            SqlDouble Test2 = new SqlDouble(64E+164);
            SqlDouble Test3 = new SqlDouble(12E+100);
            SqlDouble Test4 = new SqlDouble(1E+10);
            SqlDouble Test5 = new SqlDouble(2E+10);

            // "+"-operator
            Assert.Equal(3E+10, Test4 + Test5);

            try
            {
                SqlDouble test = SqlDouble.MaxValue + SqlDouble.MaxValue;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "/"-operator
            Assert.Equal(2, Test1 / Test3);

            try
            {
                SqlDouble test = Test3 / Test0;
                Assert.False(true);
            }
            catch (DivideByZeroException e)
            {
                Assert.Equal(typeof(DivideByZeroException), e.GetType());
            }

            // "*"-operator
            Assert.Equal(2e20, Test4 * Test5);

            try
            {
                SqlDouble test = SqlDouble.MaxValue * Test1;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }

            // "-"-operator
            Assert.Equal(12e100, Test1 - Test3);

            try
            {
                SqlDouble test = SqlDouble.MinValue - SqlDouble.MaxValue;
                Assert.False(true);
            }
            catch (OverflowException e)
            {
                Assert.Equal(typeof(OverflowException), e.GetType());
            }
        }

        [Fact]
        public void ThanOrEqualOperators()
        {
            SqlDouble Test1 = new SqlDouble(1E+164);
            SqlDouble Test2 = new SqlDouble(9.7E+100);
            SqlDouble Test22 = new SqlDouble(9.7E+100);
            SqlDouble Test3 = new SqlDouble(2E+200);

            // == -operator
            Assert.True((Test2 == Test22).Value);
            Assert.True(!(Test1 == Test2).Value);
            Assert.True((Test1 == SqlDouble.Null).IsNull);

            // != -operator
            Assert.True(!(Test2 != Test22).Value);
            Assert.True((Test2 != Test3).Value);
            Assert.True((Test1 != Test3).Value);
            Assert.True((Test1 != SqlDouble.Null).IsNull);

            // > -operator
            Assert.True((Test1 > Test2).Value);
            Assert.True(!(Test1 > Test3).Value);
            Assert.True(!(Test2 > Test22).Value);
            Assert.True((Test1 > SqlDouble.Null).IsNull);

            // >=  -operator
            Assert.True(!(Test1 >= Test3).Value);
            Assert.True((Test3 >= Test1).Value);
            Assert.True((Test2 >= Test22).Value);
            Assert.True((Test1 >= SqlDouble.Null).IsNull);

            // < -operator
            Assert.True(!(Test1 < Test2).Value);
            Assert.True((Test1 < Test3).Value);
            Assert.True(!(Test2 < Test22).Value);
            Assert.True((Test1 < SqlDouble.Null).IsNull);

            // <= -operator
            Assert.True((Test1 <= Test3).Value);
            Assert.True(!(Test3 <= Test1).Value);
            Assert.True((Test2 <= Test22).Value);
            Assert.True((Test1 <= SqlDouble.Null).IsNull);
        }

        [Fact]
        public void UnaryNegation()
        {
            SqlDouble Test = new SqlDouble(2000000001);
            SqlDouble TestNeg = new SqlDouble(-3000);

            SqlDouble Result = -Test;
            Assert.Equal(-2000000001, Result.Value);

            Result = -TestNeg;
            Assert.Equal(3000, Result.Value);
        }

        [Fact]
        public void SqlBooleanToSqlDouble()
        {
            SqlBoolean TestBoolean = new SqlBoolean(true);
            SqlDouble Result;

            Result = (SqlDouble)TestBoolean;

            Assert.Equal(1, Result.Value);

            Result = (SqlDouble)SqlBoolean.Null;
            Assert.True(Result.IsNull);
        }

        [Fact]
        public void SqlDoubleToDouble()
        {
            SqlDouble Test = new SqlDouble(12e12);
            double Result = (double)Test;
            Assert.Equal(12e12, Result);
        }

        [Fact]
        public void SqlStringToSqlDouble()
        {
            SqlString TestString = new SqlString("Test string");
            SqlString TestString100 = new SqlString("100");

            Assert.Equal(100, ((SqlDouble)TestString100).Value);

            try
            {
                SqlDouble test = (SqlDouble)TestString;
                Assert.False(true);
            }
            catch (FormatException e)
            {
                Assert.Equal(typeof(FormatException), e.GetType());
            }
        }

        [Fact]
        public void DoubleToSqlDouble()
        {
            double Test1 = 5e64;
            SqlDouble Result = Test1;
            Assert.Equal(5e64, Result.Value);
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
