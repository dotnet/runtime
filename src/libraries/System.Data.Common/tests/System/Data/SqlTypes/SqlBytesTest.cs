// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace System.Data.Tests.SqlTypes
{
    public class SqlBytesTest
    {
        // Test constructor
        [Fact]
        public void SqlBytesItem()
        {
            SqlBytes bytes = new SqlBytes();
            Assert.Throws<SqlNullValueException>(() => bytes[0]);

            byte[] b = null;
            bytes = new SqlBytes(b);
            Assert.Throws<SqlNullValueException>(() => bytes[0]);

            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(0, bytes[0]);
            Assert.Throws<ArgumentOutOfRangeException>(() => bytes[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => bytes[10]);
        }
        [Fact]
        public void SqlBytesLength()
        {
            byte[] b = null;
            SqlBytes bytes = new SqlBytes();
            Assert.Throws<SqlNullValueException>(() => bytes.Length);

            bytes = new SqlBytes(b);
            Assert.Throws<SqlNullValueException>(() => bytes.Length);

            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(10, bytes.Length);
        }
        [Fact]
        public void SqlBytesMaxLength()
        {
            byte[] b = null;
            SqlBytes bytes = new SqlBytes();
            Assert.Equal(-1, bytes.MaxLength);
            bytes = new SqlBytes(b);
            Assert.Equal(-1, bytes.MaxLength);
            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(10, bytes.MaxLength);
        }
        [Fact]
        public void SqlBytesNull()
        {
            SqlBytes bytes = SqlBytes.Null;
            Assert.True(bytes.IsNull);
        }
        [Fact]
        public void SqlBytesStorage()
        {
            byte[] b = null;
            SqlBytes bytes = new SqlBytes();
            Assert.Throws<SqlNullValueException>(() => bytes.Storage);

            bytes = new SqlBytes(b);
            Assert.Throws<SqlNullValueException>(() => bytes.Storage);

            b = new byte[10];
            bytes = new SqlBytes(b);
            Assert.Equal(StorageState.Buffer, bytes.Storage);

            FileStream fs = null;
            bytes = new SqlBytes(fs);
            Assert.Throws<SqlNullValueException>(() => bytes.Storage);
        }
        [Fact]
        public void SqlBytesValue()
        {
            byte[] b1 = new byte[10];
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = bytes.Value;
            Assert.Equal(b1[0], b2[0]);
            b2[0] = 10;
            Assert.Equal(0, b1[0]);
            Assert.Equal(10, b2[0]);
        }
        [Fact]
        public void SqlBytesSetLength()
        {
            byte[] b1 = new byte[10];
            SqlBytes bytes = new SqlBytes();
            Assert.Throws<SqlTypeException>(() => bytes.SetLength(20));

            bytes = new SqlBytes(b1);
            Assert.Equal(10, bytes.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.SetLength(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.SetLength(11));

            bytes.SetLength(2);
            Assert.Equal(2, bytes.Length);
        }
        [Fact]
        public void SqlBytesSetNull()
        {
            byte[] b1 = new byte[10];
            SqlBytes bytes = new SqlBytes(b1);
            Assert.Equal(10, bytes.Length);
            bytes.SetNull();
            Assert.Throws<SqlNullValueException>(() => bytes.Length);
            Assert.True(bytes.IsNull);
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

            long read = bytes.Read(0, b2, 0, (int)bytes.Length);
            Assert.Equal(bytes.Length, read);
            Assert.Equal(bytes.Value[5], b2[5]);

            Span<byte> b2Span = b2.AsSpan();
            b2Span.Clear();

            using Stream stream = bytes.Stream;
            read = stream.Read(b2, 0, (int) bytes.Length);
            Assert.Equal(bytes.Length, read);
            Assert.Equal(bytes.Value[5], b2[5]);

            b2Span.Clear();
            stream.Position = 0;

            read = stream.Read(b2Span.Slice(0, (int) bytes.Length));
            Assert.Equal(bytes.Length, read);
            Assert.Equal(bytes.Value[5], b2Span[5]);
        }

        [Fact]
        public void Read_NullBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = null;

            Assert.Throws<ArgumentNullException>(() => bytes.Read(0, b2, 0, 10));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentNullException>(() => stream.Read(b2, 0, 10));
        }

        [Fact]
        public void Read_InvalidCountTest1()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, 0, 10));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(b2, 0, 10));
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

            Assert.Throws<SqlNullValueException>(() => bytes.Read(0, b2, 0, 1));

            using Stream stream = bytes.Stream;
            Assert.Throws<SqlNullValueException>(() => stream.Read(b2, 0, 1));
        }

        [Fact]
        public void Read_SuccessTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[10];

            const long offset = 5;
            long read = bytes.Read(offset, b2, 0, 10);
            byte[] currentValue = bytes.Value;
            Assert.Equal(b1.Length - offset, read);
            Assert.Equal(currentValue[5], b2[0]);
            Assert.Equal(currentValue[9], b2[4]);

            Span<byte> b2Span = b2.AsSpan();
            b2Span.Clear();

            using Stream stream = bytes.Stream;
            read = stream.Read(b2, 0, 10);
            Assert.Equal(10, read);
            currentValue = bytes.Value;
            Assert.Equal(currentValue[5], b2[5]);
            Assert.Equal(currentValue[9], b2[9]);

            b2Span.Clear();
            stream.Position = 0;

            read = stream.Read(b2Span.Slice(0, 10));
            Assert.Equal(10, read);
            currentValue = bytes.Value;
            Assert.Equal(currentValue[5], b2Span[5]);
            Assert.Equal(currentValue[9], b2Span[9]);
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

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(b2, 0, -1));
        }

        [Fact]
        public void Read_InvalidCountTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes(b1);
            byte[] b2 = new byte[5];

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Read(0, b2, 3, 4));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Read(b2, 3, 4));
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

            Span<byte> b2Span = b2.AsSpan();
            b2Span.Clear();

            using Stream stream = bytes.Stream;
            stream.Write(b1, 0, b1.Length);
            Assert.Equal(bytes.Value[0], b1[0]);

            b2Span.Clear();
            stream.Position = 0;

            stream.Write(b1.AsSpan(0, b1.Length));
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

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(b1, -1, b1.Length));
        }

        [Fact]
        public void Write_InvalidOffsetInBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);
            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, b1.Length + 5, b1.Length));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(b1, b1.Length + 5, b1.Length));
        }

        [Fact]
        public void Write_InvalidCountTest1()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, 0, b1.Length + 5));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(b1, 0, b1.Length + 5));
        }

        [Fact]
        public void Write_InvalidCountTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<SqlTypeException>(() => bytes.Write(8, b1, 0, b1.Length));

            using Stream stream = bytes.Stream;
            const int offsetEmulation = 8;
            stream.Position = offsetEmulation;

            Assert.Throws<SqlTypeException>(() => stream.Write(b1, 0, b1.Length));
        }

        [Fact]
        public void Write_NullBufferTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = null;
            SqlBytes bytes = new SqlBytes(b1);

            Assert.Throws<ArgumentNullException>(() => bytes.Write(0, b2, 0, 10));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentNullException>(() => stream.Write(b2, 0, 10));
        }

        [Fact]
        public void Write_NullInstanceValueTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            SqlBytes bytes = new SqlBytes();

            Assert.Throws<SqlTypeException>(() => bytes.Write(0, b1, 0, 10));

            using Stream stream = bytes.Stream;
            Assert.Throws<SqlTypeException>(() => stream.Write(b1, 0, 10));
            Assert.Throws<SqlTypeException>(() => stream.Write(b1.AsSpan(0, 10)));
        }

        [Fact]
        public void Write_NullBufferAndInstanceValueTest()
        {
            byte[] b1 = null;
            SqlBytes bytes = new SqlBytes();

            Assert.Throws<ArgumentNullException>(() => bytes.Write(0, b1, 0, 10));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentNullException>(() => stream.Write(b1, 0, 10));
        }

        [Fact]
        public void Write_SuccessTest2()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[20];
            SqlBytes bytes = new SqlBytes(b2);

            bytes.Write(8, b1, 0, 10);
            byte[] currentValue = bytes.Value;
            Assert.Equal(currentValue[8], b1[0]);
            Assert.Equal(currentValue[17], b1[9]);

            Span<byte> b2Span = b2.AsSpan();
            b2Span.Clear();

            const int offsetEmulate = 8;
            using Stream stream = bytes.Stream;
            stream.Position = offsetEmulate;

            stream.Write(b1, 0, 10);
            currentValue = bytes.Value;
            Assert.Equal(currentValue[8], b1[0]);
            Assert.Equal(currentValue[17], b1[9]);

            b2Span.Clear();
            stream.Position = offsetEmulate;

            stream.Write(b1.AsSpan(0, 10));
            currentValue = bytes.Value;
            Assert.Equal(currentValue[8], b1[0]);
            Assert.Equal(currentValue[17], b1[9]);
        }

        [Fact]
        public void Write_NegativeCountTest()
        {
            byte[] b1 = { 33, 34, 35, 36, 37, 38, 39, 40, 41, 42 };
            byte[] b2 = new byte[10];
            SqlBytes bytes = new SqlBytes(b2);

            Assert.Throws<ArgumentOutOfRangeException>(() => bytes.Write(0, b1, 0, -1));

            using Stream stream = bytes.Stream;
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Write(b1, 0, -1));
        }

        [Fact]
        public void SqlBytes_FromStream_BufferContainsExpectedData()
        {
            var bytes = new SqlBytes(new TrickleStream(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.Equal("01-02-03-04-05", BitConverter.ToString(bytes.Buffer));
        }

        [Fact]
        public void SqlBytes_FromStream_ValueContainsExpectedData()
        {
            var bytes = new SqlBytes(new TrickleStream(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.Equal("01-02-03-04-05", BitConverter.ToString(bytes.Value));
        }

        [Fact]
        public void SqlBytes_FromStream_ReadReturnsExpectedCount()
        {
            var bytes = new SqlBytes(new TrickleStream(new byte[] { 1, 2, 3, 4, 5 }));
            byte[] buffer = new byte[5];
            long bytesRead = bytes.Read(0, buffer, 0, buffer.Length);
            Assert.Equal(1, bytesRead);
        }

        private sealed class TrickleStream : MemoryStream
        {
            public TrickleStream(byte[] bytes) : base(bytes) { }
            public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
        }
    }
}
