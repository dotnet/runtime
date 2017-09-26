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

using System.IO;
using Xunit;

namespace System.Data.Tests.Common
{
    public class DbDataReaderTest
    {
        private DbDataReaderMock _dataReader;

        public DbDataReaderTest()
        {
            //Setup test data table
            DataTable testData = new DataTable();
            testData.Columns.Add("text_col", typeof(string));
            testData.Columns.Add("binary_col", typeof(byte[]));

            testData.Rows.Add("row_1", new byte[] { 0xde, 0xad, 0xbe, 0xef });
            testData.Rows.Add("row_2", DBNull.Value);
            testData.Rows.Add("row_3", new byte[] { 0x00 });

            _dataReader = new DbDataReaderMock(testData);

            Assert.Equal(3, testData.Rows.Count);
        }

		[Fact]
		public void GetFieldValueTest()
		{
			//First row
			 _dataReader.Read();
			Assert.Equal("row_1", _dataReader.GetFieldValue<string>(0));
			byte[] expected_data = new byte[] { 0xde, 0xad, 0xbe, 0xef };
			byte[] actual_data = _dataReader.GetFieldValue<byte[]>(1);
			Assert.Equal(expected_data.Length, actual_data.Length);
			for (int i = 0; i < expected_data.Length; i++)
			{
				Assert.Equal(expected_data [i], actual_data [i]);
			}

			//Second row where data row column value is DBNull
			 _dataReader.Read();
			Assert.Equal("row_2", _dataReader.GetFieldValue<string>(0));
			Assert.Throws<InvalidCastException>(() => _dataReader.GetFieldValue<byte[]>(1));

			//Third row
			 _dataReader.Read();
			Assert.Equal("row_3", _dataReader.GetFieldValue<string>(0));
			expected_data = new byte[] { 0x00 };
			actual_data = _dataReader.GetFieldValue<byte[]>(1);
			Assert.Equal(expected_data.Length, actual_data.Length);
			Assert.Equal(expected_data [0], actual_data [0]);
		}

		[Fact]
		public void GetStreamTest()
		{
			int testColOrdinal = 1;
			byte[] buffer = new byte[1024];

			 _dataReader.Read();
			Stream stream = _dataReader.GetStream(testColOrdinal);
			Assert.NotNull(stream);

			//Read stream content to byte buffer
			int data_length = stream.Read(buffer, 0, buffer.Length);

			//Verify that content is expected
			byte[] expected = new byte[] { 0xde, 0xad, 0xbe, 0xef };
			Assert.Equal(expected.Length, data_length);
			for (int i = 0; i < expected.Length; i++)
			{
				Assert.Equal(expected [i], buffer [i]);
			}

			//Get DBNull value stream
			Assert.True(_dataReader.Read());
			stream = _dataReader.GetStream(testColOrdinal);
			Assert.Equal(0, stream.Length);

			//Get single byte value stream
			Assert.True(_dataReader.Read());
			stream = _dataReader.GetStream(testColOrdinal);
			expected = new byte[] { 0x00 };
			Assert.Equal(expected.Length, stream.Length);
			Assert.Equal(expected [0], stream.ReadByte());
		}

		[Fact]
		public void GetTextReader()
		{
			int testColOrdinal = 0;

			//Read first row
			 _dataReader.Read();
			TextReader textReader = _dataReader.GetTextReader(testColOrdinal);
			Assert.NotNull(textReader);

			string txt = textReader.ReadToEnd();
			Assert.Equal("row_1", txt);

			//Move to second row
			Assert.True(_dataReader.Read());
			textReader = _dataReader.GetTextReader(testColOrdinal);
			txt = textReader.ReadToEnd();
			Assert.Equal("row_2", txt);

			//Move to third row
			Assert.True(_dataReader.Read());
			textReader = _dataReader.GetTextReader(testColOrdinal);
			txt = textReader.ReadToEnd();
			Assert.Equal("row_3", txt);

			Assert.False(_dataReader.Read());
		}
    }
}

