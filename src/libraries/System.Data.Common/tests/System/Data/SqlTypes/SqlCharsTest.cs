// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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
using System.IO;
using System.Xml;
using System.Data.SqlTypes;
using System.Globalization;
using System.Xml.Serialization;

namespace System.Data.Tests.SqlTypes
{
    public class SqlCharsTest
    {
        private CultureInfo _originalCulture;

        // Test constructor
        [Fact]
        public void SqlCharsItem()
        {
            SqlChars chars = new SqlChars();
            try
            {
                Assert.Equal(chars[0], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            char[] b = null;
            chars = new SqlChars(b);
            try
            {
                Assert.Equal(chars[0], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            b = new char[10];
            chars = new SqlChars(b);
            Assert.Equal(chars[0], 0);
            try
            {
                Assert.Equal(chars[-1], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
            try
            {
                Assert.Equal(chars[10], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
        }

        [Fact]
        public void SqlCharsLength()
        {
            char[] b = null;
            SqlChars chars = new SqlChars();
            try
            {
                Assert.Equal(chars.Length, 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            chars = new SqlChars(b);
            try
            {
                Assert.Equal(chars.Length, 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            b = new char[10];
            chars = new SqlChars(b);
            Assert.Equal(chars.Length, 10);
        }

        [Fact]
        public void SqlCharsMaxLength()
        {
            char[] b = null;
            SqlChars chars = new SqlChars();
            Assert.Equal(chars.MaxLength, -1);
            chars = new SqlChars(b);
            Assert.Equal(chars.MaxLength, -1);
            b = new char[10];
            chars = new SqlChars(b);
            Assert.Equal(chars.MaxLength, 10);
        }

        [Fact]
        public void SqlCharsNull()
        {
            char[] b = null;
            SqlChars chars = SqlChars.Null;
            Assert.Equal(chars.IsNull, true);
        }

        [Fact]
        public void SqlCharsStorage()
        {
            char[] b = null;
            SqlChars chars = new SqlChars();
            try
            {
                Assert.Equal(chars.Storage, StorageState.Buffer);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            chars = new SqlChars(b);
            try
            {
                Assert.Equal(chars.Storage, StorageState.Buffer);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            b = new char[10];
            chars = new SqlChars(b);
            Assert.Equal(chars.Storage, StorageState.Buffer);
        }

        [Fact]
        public void SqlCharsValue()
        {
            char[] b1 = new char[10];
            SqlChars chars = new SqlChars(b1);
            char[] b2 = chars.Value;
            Assert.Equal(b1[0], b2[0]);
            b2[0] = '1';
            Assert.Equal(b1[0], 0);
            Assert.Equal(b2[0], '1');
        }

        [Fact]
        public void SqlCharsSetLength()
        {
            char[] b1 = new char[10];
            SqlChars chars = new SqlChars();
            try
            {
                chars.SetLength(20);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlTypeException), ex.GetType());
            }
            chars = new SqlChars(b1);
            Assert.Equal(chars.Length, 10);
            try
            {
                chars.SetLength(-1);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
            try
            {
                chars.SetLength(11);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
            chars.SetLength(2);
            Assert.Equal(chars.Length, 2);
        }

        [Fact]
        public void SqlCharsSetNull()
        {
            char[] b1 = new char[10];
            SqlChars chars = new SqlChars(b1);
            Assert.Equal(chars.Length, 10);
            chars.SetNull();
            try
            {
                Assert.Equal(chars.Length, 10);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            Assert.Equal(true, chars.IsNull);
        }

        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlChars.GetXsdType(null);
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

        /* Read tests */
        [Fact]
        public void Read_SuccessTest1()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[10];

            chars.Read(0, c2, 0, (int)chars.Length);
            Assert.Equal(chars.Value[5], c2[5]);
        }

        [Fact]
        public void Read_NullBufferTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = null;

            Assert.Throws<ArgumentNullException>(() => chars.Read(0, c2, 0, 10));
        }

        [Fact]
        public void Read_InvalidCountTest1()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Read(0, c2, 0, 10));
        }

        [Fact]
        public void Read_NegativeOffsetTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Read(-1, c2, 0, 4));
        }

        [Fact]
        public void Read_NegativeOffsetInBufferTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Read(0, c2, -1, 4));
        }

        [Fact]
        public void Read_InvalidOffsetInBufferTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Read(0, c2, 8, 4));
        }

        [Fact]
        public void Read_NullInstanceValueTest()
        {
            char[] c2 = new char[5];
            SqlChars chars = new SqlChars();

            Assert.Throws<SqlNullValueException>(() => chars.Read(0, c2, 8, 4));
        }

        [Fact]
        public void Read_SuccessTest2()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[10];

            chars.Read(5, c2, 0, 10);
            Assert.Equal(chars.Value[5], c2[0]);
            Assert.Equal(chars.Value[9], c2[4]);
        }

        [Fact]
        public void Read_NullBufferAndInstanceValueTest()
        {
            char[] c2 = null;
            SqlChars chars = new SqlChars();

            Assert.Throws<SqlNullValueException>(() => chars.Read(0, c2, 8, 4));
        }

        [Fact]
        public void Read_NegativeCountTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Read(0, c2, 0, -1));
        }

        [Fact]
        public void Read_InvalidCountTest2()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars(c1);
            char[] c2 = new char[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Read(0, c2, 3, 4));
        }

        [Fact]
        public void Write_SuccessTest1()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            chars.Write(0, c1, 0, c1.Length);
            Assert.Equal(chars.Value[0], c1[0]);
        }

        [Fact]
        public void Write_NegativeOffsetTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Write(-1, c1, 0, c1.Length));
        }

        [Fact]
        public void Write_InvalidOffsetTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<SqlTypeException>(() => chars.Write(chars.Length + 5, c1, 0, c1.Length));
        }

        [Fact]
        public void Write_NegativeOffsetInBufferTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Write(0, c1, -1, c1.Length));
        }

        [Fact]
        public void Write_InvalidOffsetInBufferTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Write(0, c1, c1.Length + 5, c1.Length));
        }

        [Fact]
        public void Write_InvalidCountTest1()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Write(0, c1, 0, c1.Length + 5));
        }

        [Fact]
        public void Write_InvalidCountTest2()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<SqlTypeException>(() => chars.Write(8, c1, 0, c1.Length));
        }

        [Fact]
        public void Write_NullBufferTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = null;
            SqlChars chars = new SqlChars(c1);

            Assert.Throws<ArgumentNullException>(() => chars.Write(0, c2, 0, 10));
        }

        [Fact]
        public void Write_NullInstanceValueTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            SqlChars chars = new SqlChars();

            Assert.Throws<SqlTypeException>(() => chars.Write(0, c1, 0, 10));
        }

        [Fact]
        public void Write_NullBufferAndInstanceValueTest()
        {
            char[] c1 = null;
            SqlChars chars = new SqlChars();

            Assert.Throws<ArgumentNullException>(() => chars.Write(0, c1, 0, 10));
        }

        [Fact]
        public void Write_SuccessTest2()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[20];
            SqlChars chars = new SqlChars(c2);

            chars.Write(8, c1, 0, 10);
            Assert.Equal(chars.Value[8], c1[0]);
            Assert.Equal(chars.Value[17], c1[9]);
        }

        [Fact]
        public void Write_NegativeCountTest()
        {
            char[] c1 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j' };
            char[] c2 = new char[10];
            SqlChars chars = new SqlChars(c2);

            Assert.Throws<ArgumentOutOfRangeException>(() => chars.Write(0, c1, 0, -1));
        }
    }
}

