// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// (C) Tim Coleman
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

using System.Data.SqlTypes;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Xunit;

namespace System.Data.Tests.SqlTypes
{
    public class SqlInt32Test
    {
        [Fact]
        public void Create()
        {
            SqlInt32 foo = new SqlInt32(5);
            Assert.Equal(5, (int)foo);
        }

        [Fact]
        public void Add()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x;
            SqlInt32 y;
            SqlInt32 z;

            x = new SqlInt32(a);
            y = new SqlInt32(b);
            z = x + y;
            Assert.Equal(a + b, z.Value);
            z = SqlInt32.Add(x, y);
            Assert.Equal(a + b, z.Value);
        }

        [Fact]
        public void BitwiseAnd()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x & y;
            Assert.Equal(a & b, z.Value);
            z = SqlInt32.BitwiseAnd(x, y);
            Assert.Equal(a & b, z.Value);
        }

        [Fact]
        public void BitwiseOr()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x | y;
            Assert.Equal(a | b, z.Value);
            z = SqlInt32.BitwiseOr(x, y);
            Assert.Equal(a | b, z.Value);
        }

        [Fact]
        public void Divide()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x / y;
            Assert.Equal(a / b, z.Value);
            z = SqlInt32.Divide(x, y);
            Assert.Equal(a / b, z.Value);
        }

        [Fact]
        public void EqualsTest()
        {
            SqlInt32 x;
            SqlInt32 y;

            // Case 1: either is SqlInt32.Null
            x = SqlInt32.Null;
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.Null, x == y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.Equals(x, y));

            // Case 2: both are SqlInt32.Null
            y = SqlInt32.Null;
            Assert.Equal(SqlBoolean.Null, x == y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.Equals(x, y));

            // Case 3: both are equal
            x = new SqlInt32(5);
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.True, x == y);
            Assert.Equal(SqlBoolean.True, SqlInt32.Equals(x, y));

            // Case 4: inequality
            x = new SqlInt32(5);
            y = new SqlInt32(6);
            Assert.Equal(SqlBoolean.False, x == y);
            Assert.Equal(SqlBoolean.False, SqlInt32.Equals(x, y));
        }

        [Fact]
        public void GreaterThan()
        {
            SqlInt32 x;
            SqlInt32 y;

            // Case 1: either is SqlInt32.Null
            x = SqlInt32.Null;
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.Null, x > y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.GreaterThan(x, y));

            // Case 2: both are SqlInt32.Null
            y = SqlInt32.Null;
            Assert.Equal(SqlBoolean.Null, x > y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.GreaterThan(x, y));

            // Case 3: x > y
            x = new SqlInt32(5);
            y = new SqlInt32(4);
            Assert.Equal(SqlBoolean.True, x > y);
            Assert.Equal(SqlBoolean.True, SqlInt32.GreaterThan(x, y));

            // Case 4: x < y
            x = new SqlInt32(5);
            y = new SqlInt32(6);
            Assert.Equal(SqlBoolean.False, x > y);
            Assert.Equal(SqlBoolean.False, SqlInt32.GreaterThan(x, y));
        }

        [Fact]
        public void GreaterThanOrEqual()
        {
            SqlInt32 x;
            SqlInt32 y;

            // Case 1: either is SqlInt32.Null
            x = SqlInt32.Null;
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.Null, x >= y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.GreaterThanOrEqual(x, y));

            // Case 2: both are SqlInt32.Null
            y = SqlInt32.Null;
            Assert.Equal(SqlBoolean.Null, x >= y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.GreaterThanOrEqual(x, y));

            // Case 3: x > y
            x = new SqlInt32(5);
            y = new SqlInt32(4);
            Assert.Equal(SqlBoolean.True, x >= y);
            Assert.Equal(SqlBoolean.True, SqlInt32.GreaterThanOrEqual(x, y));

            // Case 4: x < y
            x = new SqlInt32(5);
            y = new SqlInt32(6);
            Assert.Equal(SqlBoolean.False, x >= y);
            Assert.Equal(SqlBoolean.False, SqlInt32.GreaterThanOrEqual(x, y));

            // Case 5: x == y
            x = new SqlInt32(5);
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.True, x >= y);
            Assert.Equal(SqlBoolean.True, SqlInt32.GreaterThanOrEqual(x, y));
        }

        [Fact]
        public void LessThan()
        {
            SqlInt32 x;
            SqlInt32 y;

            // Case 1: either is SqlInt32.Null
            x = SqlInt32.Null;
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.Null, x < y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.LessThan(x, y));

            // Case 2: both are SqlInt32.Null
            y = SqlInt32.Null;
            Assert.Equal(SqlBoolean.Null, x < y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.LessThan(x, y));

            // Case 3: x > y
            x = new SqlInt32(5);
            y = new SqlInt32(4);
            Assert.Equal(SqlBoolean.False, x < y);
            Assert.Equal(SqlBoolean.False, SqlInt32.LessThan(x, y));

            // Case 4: x < y
            x = new SqlInt32(5);
            y = new SqlInt32(6);
            Assert.Equal(SqlBoolean.True, x < y);
            Assert.Equal(SqlBoolean.True, SqlInt32.LessThan(x, y));
        }

        [Fact]
        public void LessThanOrEqual()
        {
            SqlInt32 x;
            SqlInt32 y;

            // Case 1: either is SqlInt32.Null
            x = SqlInt32.Null;
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.Null, x <= y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.LessThanOrEqual(x, y));

            // Case 2: both are SqlInt32.Null
            y = SqlInt32.Null;
            Assert.Equal(SqlBoolean.Null, x <= y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.LessThanOrEqual(x, y));

            // Case 3: x > y
            x = new SqlInt32(5);
            y = new SqlInt32(4);
            Assert.Equal(SqlBoolean.False, x <= y);
            Assert.Equal(SqlBoolean.False, SqlInt32.LessThanOrEqual(x, y));

            // Case 4: x < y
            x = new SqlInt32(5);
            y = new SqlInt32(6);
            Assert.Equal(SqlBoolean.True, x <= y);
            Assert.Equal(SqlBoolean.True, SqlInt32.LessThanOrEqual(x, y));

            // Case 5: x == y
            x = new SqlInt32(5);
            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.True, x <= y);
            Assert.Equal(SqlBoolean.True, SqlInt32.LessThanOrEqual(x, y));
        }

        [Fact]
        public void Mod()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x % y;
            Assert.Equal(a % b, z.Value);
            z = SqlInt32.Mod(x, y);
            Assert.Equal(a % b, z.Value);
        }

        [Fact]
        public void Modulus()
        {
            int a = 50;
            int b = 7;
            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x % y;
            Assert.Equal(a % b, z.Value);
            z = SqlInt32.Modulus(x, y);
            Assert.Equal(a % b, z.Value);
        }

        [Fact]
        public void Multiply()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x * y;
            Assert.Equal(a * b, z.Value);
            z = SqlInt32.Multiply(x, y);
            Assert.Equal(a * b, z.Value);
        }

        [Fact]
        public void NotEquals()
        {
            SqlInt32 x;
            SqlInt32 y;

            x = new SqlInt32(5);
            y = SqlInt32.Null;

            Assert.Equal(SqlBoolean.Null, x != y);
            Assert.Equal(SqlBoolean.Null, SqlInt32.NotEquals(x, y));

            y = new SqlInt32(5);
            Assert.Equal(SqlBoolean.False, x != y);
            Assert.Equal(SqlBoolean.False, SqlInt32.NotEquals(x, y));

            y = new SqlInt32(6);
            Assert.Equal(SqlBoolean.True, x != y);
            Assert.Equal(SqlBoolean.True, SqlInt32.NotEquals(x, y));
        }

        [Fact]
        public void OnesComplement()
        {
            int a = 5;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 z = ~x;
            Assert.Equal(~a, z.Value);
            z = SqlInt32.OnesComplement(x);
            Assert.Equal(~a, z.Value);
        }

        [Fact]
        public void IsNullProperty()
        {
            SqlInt32 n = SqlInt32.Null;
            Assert.True(n.IsNull);
        }

        [Fact]
        public void Subtract()
        {
            int a = 7;
            int b = 5;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x - y;
            Assert.Equal(a - b, z.Value);
            z = SqlInt32.Subtract(x, y);
            Assert.Equal(a - b, z.Value);
        }

        [Fact]
        public void ConversionMethods()
        {
            SqlInt32 x;

            // Case 1: SqlInt32.Null -> SqlBoolean == SqlBoolean.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlBoolean.Null, x.ToSqlBoolean());

            // Case 2: SqlInt32.Zero -> SqlBoolean == False
            x = SqlInt32.Zero;
            Assert.Equal(SqlBoolean.False, x.ToSqlBoolean());

            // Case 3: SqlInt32(nonzero) -> SqlBoolean == True
            x = new SqlInt32(27);
            Assert.Equal(SqlBoolean.True, x.ToSqlBoolean());

            // Case 4: SqlInt32.Null -> SqlByte == SqlByte.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlByte.Null, x.ToSqlByte());

            // Case 5: Test non-null conversion to SqlByte
            x = new SqlInt32(27);
            Assert.Equal((byte)27, x.ToSqlByte().Value);

            // Case 6: SqlInt32.Null -> SqlDecimal == SqlDecimal.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlDecimal.Null, x.ToSqlDecimal());

            // Case 7: Test non-null conversion to SqlDecimal
            x = new SqlInt32(27);
            Assert.Equal(27, x.ToSqlDecimal().Value);

            // Case 8: SqlInt32.Null -> SqlDouble == SqlDouble.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlDouble.Null, x.ToSqlDouble());

            // Case 9: Test non-null conversion to SqlDouble
            x = new SqlInt32(27);
            Assert.Equal(27, x.ToSqlDouble().Value);

            // Case 10: SqlInt32.Null -> SqlInt16 == SqlInt16.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlInt16.Null, x.ToSqlInt16());

            // Case 11: Test non-null conversion to SqlInt16
            x = new SqlInt32(27);
            Assert.Equal((short)27, x.ToSqlInt16().Value);

            // Case 12: SqlInt32.Null -> SqlInt64 == SqlInt64.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlInt64.Null, x.ToSqlInt64());

            // Case 13: Test non-null conversion to SqlInt64
            x = new SqlInt32(27);
            Assert.Equal(27, x.ToSqlInt64().Value);

            // Case 14: SqlInt32.Null -> SqlMoney == SqlMoney.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlMoney.Null, x.ToSqlMoney());

            // Case 15: Test non-null conversion to SqlMoney
            x = new SqlInt32(27);
            Assert.Equal(27.0000M, x.ToSqlMoney().Value);

            // Case 16: SqlInt32.Null -> SqlSingle == SqlSingle.Null
            x = SqlInt32.Null;
            Assert.Equal(SqlSingle.Null, x.ToSqlSingle());

            // Case 17: Test non-null conversion to SqlSingle
            x = new SqlInt32(27);
            Assert.Equal(27, x.ToSqlSingle().Value);
        }

        [Fact]
        public void Xor()
        {
            int a = 5;
            int b = 7;

            SqlInt32 x = new SqlInt32(a);
            SqlInt32 y = new SqlInt32(b);
            SqlInt32 z = x ^ y;
            Assert.Equal(a ^ b, z.Value);
            z = SqlInt32.Xor(x, y);
            Assert.Equal(a ^ b, z.Value);
        }

        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlInt32.GetXsdType(null);
            Assert.Equal("int", qualifiedName.Name);
        }

        internal void ReadWriteXmlTestInternal(string xml,
                               int testval,
                               string unit_test_id)
        {
            SqlInt32 test;
            SqlInt32 test1;
            XmlSerializer ser;
            StringWriter sw;
            XmlTextWriter xw;
            StringReader sr;
            XmlTextReader xr;

            test = new SqlInt32(testval);
            ser = new XmlSerializer(typeof(SqlInt32));
            sw = new StringWriter();
            xw = new XmlTextWriter(sw);

            ser.Serialize(xw, test);

            // Assert.Equal (xml, sw.ToString ());

            sr = new StringReader(xml);
            xr = new XmlTextReader(sr);
            test1 = (SqlInt32)ser.Deserialize(xr);

            Assert.Equal(testval, test1.Value);
        }

        [Fact]
        //[Category ("MobileNotWorking")]
        public void ReadWriteXmlTest()
        {
            string xml1 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><int>4556</int>";
            string xml2 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><int>-6445</int>";
            string xml3 = "<?xml version=\"1.0\" encoding=\"utf-16\"?><int>0x455687AB3E4D56F</int>";
            int test1 = 4556;
            int test2 = -6445;
            int test3 = 0x4F56;

            ReadWriteXmlTestInternal(xml1, test1, "BA01");
            ReadWriteXmlTestInternal(xml2, test2, "BA02");

            InvalidOperationException ex =
                Assert.Throws<InvalidOperationException>(() => ReadWriteXmlTestInternal(xml3, test3, "#BA03"));
            Assert.Equal(typeof(FormatException), ex.InnerException.GetType());
        }
    }
}
