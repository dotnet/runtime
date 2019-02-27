// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
// Copyright (C) 2014 Mika Aalto
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
//


using System.Data.Common;

namespace System.Data.Tests.Common
{
    internal class DbDataReaderMock : DbDataReader
    {
        private int _currentRowIndex = -1;
        private DataTable _testDataTable;

        public DbDataReaderMock()
        {
            _testDataTable = new DataTable();
        }

        public DbDataReaderMock(DataTable testData)
        {
            if (testData == null)
            {
                throw new ArgumentNullException(nameof(testData));
            }

            _testDataTable = testData;
        }

        public override void Close()
        {
            _testDataTable.Clear();
        }

        public override int Depth
        {
            get { throw new NotImplementedException(); }
        }

        public override int FieldCount
        {
            get { throw new NotImplementedException(); }
        }

        public override bool GetBoolean(int ordinal)
        {
            return (bool)GetValue(ordinal);
        }

        public override byte GetByte(int ordinal)
        {
            return (byte)GetValue(ordinal);
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            object value = GetValue(ordinal);
            if (value == DBNull.Value)
            {
                return 0;
            }

            byte[] data = (byte[])value;
            long bytesToRead = Math.Min(data.Length - dataOffset, length);
            Buffer.BlockCopy(data, (int)dataOffset, buffer, bufferOffset, (int)bytesToRead);
            return bytesToRead;
        }

        public override char GetChar(int ordinal)
        {
            return (char)GetValue(ordinal);
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            object value = GetValue(ordinal);
            if (value == DBNull.Value)
            {
                return 0;
            }

            char[] data = value.ToString().ToCharArray();
            long bytesToRead = Math.Min(data.Length - dataOffset, length);
            Array.Copy(data, dataOffset, buffer, bufferOffset, bytesToRead);
            return bytesToRead;
        }

        public override string GetDataTypeName(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return (DateTime)GetValue(ordinal);
        }

        public override decimal GetDecimal(int ordinal)
        {
            return (decimal)GetValue(ordinal);
        }

        public override double GetDouble(int ordinal)
        {
            return (double)GetValue(ordinal);
        }

        public override global::System.Collections.IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override Type GetFieldType(int ordinal)
        {
            throw new NotImplementedException();
        }

        public override float GetFloat(int ordinal)
        {
            return (float)GetValue(ordinal);
        }

        public override Guid GetGuid(int ordinal)
        {
            return (Guid)GetValue(ordinal);
        }

        public override short GetInt16(int ordinal)
        {
            return (short)GetValue(ordinal);
        }

        public override int GetInt32(int ordinal)
        {
            return (int)GetValue(ordinal);
        }

        public override long GetInt64(int ordinal)
        {
            return (long)GetValue(ordinal);
        }

        public override string GetName(int ordinal)
        {
            return _testDataTable.Columns[ordinal].ColumnName;
        }

        public override int GetOrdinal(string name)
        {
            // TODO: not efficient; needs to cache the columns
            for (var i = 0; i < _testDataTable.Columns.Count; ++i)
            {
                var columnName = _testDataTable.Columns[i].ColumnName;

                if (columnName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public override DataTable GetSchemaTable()
        {
            throw new NotImplementedException();
        }

        public override string GetString(int ordinal)
        {
            return (string)_testDataTable.Rows[_currentRowIndex][ordinal];
        }

        public override object GetValue(int ordinal)
        {
            return _testDataTable.Rows[_currentRowIndex][ordinal];
        }

        public override int GetValues(object[] values)
        {
            throw new NotImplementedException();
        }

        public override bool HasRows
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsClosed
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsDBNull(int ordinal)
        {
            return _testDataTable.Rows[_currentRowIndex][ordinal] == DBNull.Value;
        }

        public override bool NextResult()
        {
            throw new NotImplementedException();
        }

        public override bool Read()
        {
            _currentRowIndex++;
            return _currentRowIndex < _testDataTable.Rows.Count;
        }

        public override int RecordsAffected
        {
            get { throw new NotImplementedException(); }
        }

        public override object this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public override object this[int ordinal]
        {
            get { throw new NotImplementedException(); }
        }
    }
}

