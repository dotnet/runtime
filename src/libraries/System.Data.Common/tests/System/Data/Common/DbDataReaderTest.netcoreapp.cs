// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Tests.Common
{
    public partial class DbDataReaderTest
    {
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
