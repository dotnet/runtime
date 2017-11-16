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

namespace System.Data.Tests.SqlTypes
{
    public class SqlBytesTest
    {
        // Test constructor
        [Fact]
        public void SqlBytesItem()
        {
            SqlBytes bytes = new SqlBytes();
            try
            {
                Assert.Equal(bytes[0], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            byte[] b = null;
            bytes = new SqlBytes(b);
            try
            {
                Assert.Equal(bytes[0], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(bytes[0], 0);
            try
            {
                Assert.Equal(bytes[-1], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
            try
            {
                Assert.Equal(bytes[10], 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
        }
        [Fact]
        public void SqlBytesLength()
        {
            byte[] b = null;
            SqlBytes bytes = new SqlBytes();
            try
            {
                Assert.Equal(bytes.Length, 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            bytes = new SqlBytes(b);
            try
            {
                Assert.Equal(bytes.Length, 0);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(bytes.Length, 10);
        }
        [Fact]
        public void SqlBytesMaxLength()
        {
            byte[] b = null;
            SqlBytes bytes = new SqlBytes();
            Assert.Equal(bytes.MaxLength, -1);
            bytes = new SqlBytes(b);
            Assert.Equal(bytes.MaxLength, -1);
            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(bytes.MaxLength, 10);
        }
        [Fact]
        public void SqlBytesNull()
        {
            byte[] b = null;
            SqlBytes bytes = SqlBytes.Null;
            Assert.Equal(bytes.IsNull, true);
        }
        [Fact]
        public void SqlBytesStorage()
        {
            byte[] b = null;
            SqlBytes bytes = new SqlBytes();
            try
            {
                Assert.Equal(bytes.Storage, StorageState.Buffer);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            bytes = new SqlBytes(b);
            try
            {
                Assert.Equal(bytes.Storage, StorageState.Buffer);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(bytes.Storage, StorageState.Buffer);
            FileStream fs = null;
            bytes = new SqlBytes(fs);
            try
            {
                Assert.Equal(bytes.Storage, StorageState.Buffer);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
        }
        [Fact]
        public void SqlBytesValue()
        {
            byte[] b1 = new byte[10];
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = bytes.Value;
            Assert.Equal(b1[0], b2[0]);
            b2[0] = 10;
            Assert.Equal(b1[0], 0);
            Assert.Equal(b2[0], 10);
        }
        [Fact]
        public void SqlBytesSetLength()
        {
            byte[] b1 = new byte[10];
            SqlBytes bytes = new SqlBytes();
            try
            {
                bytes.SetLength(20);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlTypeException), ex.GetType());
            }
            bytes = new SqlBytes(b1);
            Assert.Equal(bytes.Length, 10);
            try
            {
                bytes.SetLength(-1);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
            try
            {
                bytes.SetLength(11);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
            }
            bytes.SetLength(2);
            Assert.Equal(bytes.Length, 2);
        }
        [Fact]
        public void SqlBytesSetNull()
        {
            byte[] b1 = new byte[10];
            SqlBytes bytes = new SqlBytes(b1);
            Assert.Equal(bytes.Length, 10);
            bytes.SetNull();
            try
            {
                Assert.Equal(bytes.Length, 10);
                Assert.False(true);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(SqlNullValueException), ex.GetType());
            }
            Assert.Equal(true, bytes.IsNull);
        }
        [Fact]
        public void GetXsdTypeTest()
        {
            XmlQualifiedName qualifiedName = SqlBytes.GetXsdType(null);
            Assert.Equal("base64Binary", qualifiedName.Name);
        }

        /* Read tests */
        [Fact]
        public void Read_SuccessTest1()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[10];

            bytes.Read(0, b2, 0, (int)bytes.Length);
            Assert.Equal(bytes.Value[5], b2[5]);
        }

        [Fact]
        public void Read_NullBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = null;

            Assert.Throws<ArgumentNullException>(() => bytes.Read(0, b2, 0, 10));
        }

        [Fact]
        public void Read_InvalidCountTest1()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, 0, 10));
        }

        [Fact]
        public void Read_NegativeOffsetTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(-1, b2, 0, 4));
        }

        [Fact]
        public void Read_NegativeOffsetInBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, -1, 4));
        }

        [Fact]
        public void Read_InvalidOffsetInBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, 8, 4));
        }

        [Fact]
        public void Read_NullInstanceValueTest()
        {
            byte[] b2 = new byte[5];
            SqlBytes bytes = new SqlBytes();

            Assert.Throws<SqlNullValueException>(() => bytes.Read(0, b2, 8, 4));
        }

        [Fact]
        public void Read_SuccessTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[10];

            bytes.Read(5, b2, 0, 10);
            Assert.Equal(bytes.Value[5], b2[0]);
            Assert.Equal(bytes.Value[9], b2[4]);
        }

        [Fact]
        public void Read_NullBufferAndInstanceValueTest()
        {
            byte[] b2 = null;
            SqlBytes bytes = new SqlBytes();

            Assert.Throws<SqlNullValueException>(() => bytes.Read(0, b2, 8, 4));
        }

        [Fact]
        public void Read_NegativeCountTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, 0, -1));
        }

        [Fact]
        public void Read_InvalidCountTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, 3, 4));
        }

        /* Write Tests */
        [Fact]
        public void Write_SuccessTest1()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            bytes.Write(0, b1, 0, b1.Length);
            Assert.Equal(bytes.Value[0], b1[0]);
        }

        [Fact]
        public void Write_NegativeOffsetTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(-1, b1, 0, b1.Length));
        }

        [Fact]
        public void Write_InvalidOffsetTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<SqlTypeException>(() => bytes.Write(bytes.Length + 5, b1, 0, b1.Length));
        }

        [Fact]
        public void Write_NegativeOffsetInBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, -1, b1.Length));
        }

        [Fact]
        public void Write_InvalidOffsetInBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, b1.Length + 5, b1.Length));
        }

        [Fact]
        public void Write_InvalidCountTest1()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, 0, b1.Length + 5));
        }

        [Fact]
        public void Write_InvalidCountTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<SqlTypeException>(() => bytes.Write(8, b1, 0, b1.Length));
        }

        [Fact]
        public void Write_NullBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = null;
            SqlBytes bytes = new SqlBytes(b1);

            Assert.Throws<ArgumentNullException>(() => bytes.Write(0, b2, 0, 10));
        }

        [Fact]
        public void Write_NullInstanceValueTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes();

            Assert.Throws<SqlTypeException>(() => bytes.Write(0, b1, 0, 10));
        }

        [Fact]
        public void Write_NullBufferAndInstanceValueTest()
        {
            byte[] b1 = null;
            SqlBytes bytes = new SqlBytes();

            Assert.Throws<ArgumentNullException>(() => bytes.Write(0, b1, 0, 10));
        }

        [Fact]
        public void Write_SuccessTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[20];
            SqlBytes bytes = new SqlBytes(b2);

            bytes.Write(8, b1, 0, 10);
            Assert.Equal(bytes.Value[8], b1[0]);
            Assert.Equal(bytes.Value[17], b1[9]);
        }

        [Fact]
        public void Write_NegativeCountTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, 0, -1));
        }
    }
}
