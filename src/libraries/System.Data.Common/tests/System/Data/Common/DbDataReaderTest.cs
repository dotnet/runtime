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

using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Data.Common.Tests
{
    public class DbDataReaderTest
    {
        private readonly DbDataReaderMock _dataReader;

        public DbDataReaderTest()
        {
            // Setup test data table
            DataTable testData = new DataTable();
            testData.Columns.Add("text_col", typeof(string));
            testData.Columns.Add("binary_col", typeof(byte[]));
            testData.Columns.Add("boolean_col", typeof(bool));
            testData.Columns.Add("byte_col", typeof(byte));
            testData.Columns.Add("char_col", typeof(char));
            testData.Columns.Add("datetime_col", typeof(DateTime));
            testData.Columns.Add("decimal_col", typeof(decimal));
            testData.Columns.Add("double_col", typeof(double));
            testData.Columns.Add("float_col", typeof(float));
            testData.Columns.Add("guid_col", typeof(Guid));
            testData.Columns.Add("short_col", typeof(short));
            testData.Columns.Add("int_col", typeof(int));
            testData.Columns.Add("long_col", typeof(long));
            testData.Columns.Add("dbnull_col", typeof(object));

            var values = new object[14];
            values[0] = ".NET";
            values[1] = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            values[2] = true;
            values[3] = 0x00;
            values[4] = 'E';
            values[5] = new DateTime(2016, 6, 27);
            values[6] = 810.72m;
            values[7] = Math.PI;
            values[8] = 776.90f;
            values[9] = Guid.Parse("893e4fe8-299a-465a-a600-3cd4ad91629a");
            values[10] = 12345;
            values[11] = 1234567890;
            values[12] = 1234567890123456789;
            values[13] = DBNull.Value;

            testData.Rows.Add("row_1", new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
            testData.Rows.Add("row_2", DBNull.Value);
            testData.Rows.Add("row_3", new byte[] { 0x00 });
            testData.Rows.Add(values);

            _dataReader = new DbDataReaderMock(testData);

            Assert.Equal(4, testData.Rows.Count);
        }

        [Fact]
        public void GetFieldValueTest()
        {
            // First row
            _dataReader.Read();

            Assert.Equal("row_1", _dataReader.GetFieldValue<string>(0));

            byte[] expected_data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            byte[] actual_data = _dataReader.GetFieldValue<byte[]>(1);
            Assert.Equal(expected_data.Length, actual_data.Length);
            for (int i = 0; i < expected_data.Length; i++)
            {
                Assert.Equal(expected_data[i], actual_data[i]);
            }

            // Second row where data row column value is DBNull
            _dataReader.Read();
            Assert.Equal("row_2", _dataReader.GetFieldValue<string>(0));
            Assert.Throws<InvalidCastException>(() => _dataReader.GetFieldValue<byte[]>(1));

            // Third row
            _dataReader.Read();
            Assert.Equal("row_3", _dataReader.GetFieldValue<string>(0));

            expected_data = new byte[] { 0x00 };
            actual_data = _dataReader.GetFieldValue<byte[]>(1);
            Assert.Equal(expected_data.Length, actual_data.Length);
            Assert.Equal(expected_data[0], actual_data[0]);
        }

        [Fact]
        public void GetStreamTest()
        {
            int testColOrdinal = 1;
            byte[] buffer = new byte[1024];

            _dataReader.Read();
            Stream stream = _dataReader.GetStream(testColOrdinal);
            Assert.NotNull(stream);

            // Read stream content to byte buffer
            int data_length = stream.Read(buffer, 0, buffer.Length);

            // Verify that content is expected
            byte[] expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            Assert.Equal(expected.Length, data_length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], buffer[i]);
            }

            // Get DBNull value stream
            Assert.True(_dataReader.Read());
            stream = _dataReader.GetStream(testColOrdinal);
            Assert.Equal(0, stream.Length);

            // Get single byte value stream
            Assert.True(_dataReader.Read());
            stream = _dataReader.GetStream(testColOrdinal);
            expected = new byte[] { 0x00 };
            Assert.Equal(expected.Length, stream.Length);
            Assert.Equal(expected[0], stream.ReadByte());
        }

        [Fact]
        public void GetTextReader()
        {
            int testColOrdinal = 0;

            // Read first row
            _dataReader.Read();
            TextReader textReader = _dataReader.GetTextReader(testColOrdinal);
            Assert.NotNull(textReader);

            string txt = textReader.ReadToEnd();
            Assert.Equal("row_1", txt);

            // Move to second row
            Assert.True(_dataReader.Read());
            textReader = _dataReader.GetTextReader(testColOrdinal);
            txt = textReader.ReadToEnd();
            Assert.Equal("row_2", txt);

            // Move to third row
            Assert.True(_dataReader.Read());
            textReader = _dataReader.GetTextReader(testColOrdinal);
            txt = textReader.ReadToEnd();
            Assert.Equal("row_3", txt);

            // Move to fourth row
            Assert.True(_dataReader.Read());
            textReader = _dataReader.GetTextReader(testColOrdinal);
            txt = textReader.ReadToEnd();
            Assert.Equal(".NET", txt);

            Assert.False(_dataReader.Read());
        }

        [Fact]
        public void GetBooleanByColumnNameTest()
        {
            SkipRows(3);

            var expected = true;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetBoolean("boolean_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetByteByColumnNameTest()
        {
            SkipRows(3);

            var expected = (byte)0x00;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetByte("byte_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetBytesByColumnNameTest()
        {
            SkipRows(3);

            var expected = new byte[] { 0xAD, 0xBE };

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = new byte[1024];
            var length = _dataReader.GetBytes("binary_col", 1, actual, 0, 2);

            Assert.Equal(expected.Length, length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void GetCharByColumnNameTest()
        {
            SkipRows(3);

            var expected = 'E';

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetChar("char_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetCharsByColumnNameTest()
        {
            SkipRows(3);

            var expected = new char[] { 'N', 'E', 'T' };

            // The row after rowsToSkip
            _dataReader.Read();

            const int dataLength = 1024;

            var actual = new char[dataLength];
            var length = _dataReader.GetChars("text_col", 1, actual, 0, dataLength);

            Assert.Equal(expected.Length, length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void GetDateTimeByColumnNameTest()
        {
            SkipRows(3);

            var expected = new DateTime(2016, 6, 27);

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetDateTime("datetime_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetDecimalByColumnNameTest()
        {
            SkipRows(3);

            var expected = 810.72m;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetDecimal("decimal_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetDoubleByColumnNameTest()
        {
            SkipRows(3);

            var expected = Math.PI;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetDouble("double_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetFieldValueByColumnNameTest()
        {
            SkipRows(3);

            var expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetFieldValue<byte[]>("binary_col");

            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void GetFloatByColumnNameTest()
        {
            SkipRows(3);

            var expected = 776.90f;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetFloat("float_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetGuidByColumnNameTest()
        {
            SkipRows(3);

            var expected = Guid.Parse("893e4fe8-299a-465a-a600-3cd4ad91629a");

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetGuid("guid_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetInt16ByColumnNameTest()
        {
            SkipRows(3);

            short expected = 12345;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetInt16("short_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetInt32ByColumnNameTest()
        {
            SkipRows(3);

            var expected = 1234567890;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetInt32("int_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetInt64ByColumnNameTest()
        {
            SkipRows(3);

            var expected = 1234567890123456789;

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetInt64("long_col");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetStreamByColumnNameTest()
        {
            SkipRows(3);

            var expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            // The row after rowsToSkip
            _dataReader.Read();

            var stream = _dataReader.GetStream("binary_col");
            Assert.NotNull(stream);

            var actual = new byte[1024];
            var readLength = stream.Read(actual, 0, actual.Length);

            Assert.Equal(expected.Length, readLength);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        [Fact]
        public void GetStringByColumnNameTest()
        {
            SkipRows(3);

            var expected = ".NET";

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetString("text_col");

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void GetTextReaderByColumnNameTest()
        {
            SkipRows(3);

            var expected = ".NET";

            // The row after rowsToSkip
            _dataReader.Read();

            var textReader = _dataReader.GetTextReader("text_col");
            Assert.NotNull(textReader);

            var actual = textReader.ReadToEnd();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetValueByColumnNameTest()
        {
            SkipRows(3);

            var expected = ".NET";

            // The row after rowsToSkip
            _dataReader.Read();

            var actual = _dataReader.GetValue("text_col") as string;

            Assert.NotNull(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void IsDBNullByColumnNameTest()
        {
            SkipRows(3);

            // The row after rowsToSkip
            _dataReader.Read();

            Assert.False(_dataReader.IsDBNull("text_col"));
            Assert.True(_dataReader.IsDBNull("dbnull_col"));
        }

        [Fact]
        public Task GetValueAsyncByColumnNameCanceledTest()
        {
            return Assert.ThrowsAsync<TaskCanceledException>(() => _dataReader.GetFieldValueAsync<string>("text_col", new CancellationToken(true)));
        }

        [Fact]
        public Task IsDbNullAsyncByColumnNameCanceledTest()
        {
            return Assert.ThrowsAsync<TaskCanceledException>(() => _dataReader.IsDBNullAsync("dbnull_col", new CancellationToken(true)));
        }

        [Fact]
        public void GetSchemaTableAsync_with_cancelled_token()
            => Assert.ThrowsAsync<TaskCanceledException>(async () => await new DbDataReaderMock().GetSchemaTableAsync(new CancellationToken(true)));

        [Fact]
        public void GetSchemaTableAsync_with_exception()
            => Assert.ThrowsAsync<NotSupportedException>(async () => await new DbDataReaderMock().GetSchemaTableAsync());

        [Fact]
        public async Task GetSchemaTableAsync_calls_GetSchemaTable()
        {
            var readerTable = new DataTable();
            readerTable.Columns.Add("text_col", typeof(string));

            var table = (await new SchemaDbDataReaderMock(readerTable).GetSchemaTableAsync())!;

            var textColRow = table.Rows.Cast<DataRow>().Single();
            Assert.Equal("text_col", textColRow["ColumnName"]);
            Assert.Same(typeof(string), textColRow["DataType"]);
        }

        [Fact]
        public void GetColumnSchemaAsync_with_cancelled_token()
            => Assert.ThrowsAsync<TaskCanceledException>(async () => await new DbDataReaderMock().GetColumnSchemaAsync(new CancellationToken(true)));

        [Fact]
        public void GetColumnSchemaAsync_with_exception()
            => Assert.ThrowsAsync<NotSupportedException>(async () => await new DbDataReaderMock().GetColumnSchemaAsync());

        [Fact]
        public async Task GetColumnSchemaAsync_calls_GetSchemaTable()
        {
            var readerTable = new DataTable();
            readerTable.Columns.Add("text_col", typeof(string));

            var column = (await new SchemaDbDataReaderMock(readerTable).GetColumnSchemaAsync()).Single();
            Assert.Equal("text_col", column.ColumnName);
            Assert.Same(typeof(string), column.DataType);
        }

        private void SkipRows(int rowsToSkip)
        {
            var i = 0;

            do
            {
                _dataReader.Read();
            } while (++i < rowsToSkip);
        }
    }
}
