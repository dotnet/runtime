// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class SqlDataRecordTest
    {

        [Fact]
        public void SqlRecordFillTest()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.Bit),
                new SqlMetaData("col2", SqlDbType.TinyInt),
                new SqlMetaData("col3", SqlDbType.VarBinary, 1000),
                new SqlMetaData("col4", SqlDbType.NVarChar, 1000),
                new SqlMetaData("col5", SqlDbType.DateTime),
                new SqlMetaData("col6", SqlDbType.Float),
                new SqlMetaData("col7", SqlDbType.UniqueIdentifier),
                new SqlMetaData("col8", SqlDbType.SmallInt),
                new SqlMetaData("col9", SqlDbType.Int),
                new SqlMetaData("col10", SqlDbType.BigInt),
                new SqlMetaData("col11", SqlDbType.Real),
                new SqlMetaData("col12", SqlDbType.Decimal),
                new SqlMetaData("col13", SqlDbType.Money),
                new SqlMetaData("col14", SqlDbType.Variant)
            };

            SqlDataRecord record = new SqlDataRecord(metaData);

            for (int i = 0; i < record.FieldCount; i++)
            {
                Assert.Equal($"col{i + 1}", record.GetName(i));
            }

            record.SetBoolean(0, true);
            Assert.Equal(true, record.GetBoolean(0));

            record.SetByte(1, 1);
            Assert.Equal(1, record.GetByte(1));

            byte[] bb1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] bb2 = new byte[5];
            record.SetSqlBinary(2, new SqlBinary(new byte[0]));
            record.SetBytes(2, 0, bb1, 0, 3);
            record.SetBytes(2, 2, bb1, 6, 3);

            // Verify the length of the byte array
            Assert.Equal(5, record.GetBytes(2, 0, bb2, 0, 5));

            Assert.Equal(5, record.GetBytes(2, 0, null, 0, 0));

            byte[] expected = new byte[] { 1, 2, 7, 8, 9 };
            Assert.Equal<byte>(expected, bb2);

            char[] cb1 = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g' };
            char[] cb2 = new char[5];
            record.SetChars(3, 0, cb1, 0, 3);
            record.SetChars(3, 2, cb1, 4, 3);

            char[] expectedValue = new char[] { 'a', 'b', 'e', 'f', 'g' };
            Assert.Equal(expectedValue.Length, record.GetChars(3, 0, cb2, 0, 5));
            Assert.Equal<char>(expectedValue, new string(cb2, 0, (int)record.GetChars(3, 0, null, 0, 0)));

            record.SetString(3, "");
            string xyz = "xyz";
            record.SetString(3, "xyz");
            Assert.Equal(xyz, record.GetString(3));
            Assert.Equal(xyz.Length, record.GetChars(3, 0, cb2, 0, 5));
            Assert.Equal(xyz, new string(cb2, 0, (int)record.GetChars(3, 0, null, 0, 0)));

            record.SetChars(3, 2, cb1, 4, 3);
            Assert.Equal(5, record.GetChars(3, 0, cb2, 0, 5));

            string interleavedResult = "xyefg";
            Assert.Equal(interleavedResult, new string(cb2, 0, (int)record.GetChars(3, 0, null, 0, 0)));
            Assert.Equal(interleavedResult, record.GetString(3));

            record.SetSqlDateTime(4, SqlDateTime.MaxValue);
            Assert.Equal(SqlDateTime.MaxValue, record.GetSqlDateTime(4));

            record.SetSqlDouble(5, SqlDouble.MaxValue);
            Assert.Equal(SqlDouble.MaxValue, record.GetSqlDouble(5));

            SqlGuid guid = new SqlGuid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            record.SetSqlGuid(6, guid);
            Assert.Equal(guid, record.GetSqlGuid(6));

            record.SetSqlInt16(7, SqlInt16.MaxValue);
            Assert.Equal(SqlInt16.MaxValue, record.GetSqlInt16(7));

            record.SetSqlInt32(8, SqlInt32.MaxValue);
            Assert.Equal(SqlInt32.MaxValue, record.GetSqlInt32(8));

            record.SetSqlInt64(9, SqlInt64.MaxValue);
            Assert.Equal(SqlInt64.MaxValue, record.GetSqlInt64(9));

            record.SetSqlSingle(10, SqlSingle.MinValue);
            Assert.Equal(SqlSingle.MinValue, record.GetSqlSingle(10));

            record.SetSqlDecimal(11, SqlDecimal.Null);
            record.SetSqlDecimal(11, SqlDecimal.MaxValue);
            Assert.Equal(SqlDecimal.MaxValue, record.GetSqlDecimal(11));

            record.SetSqlMoney(12, SqlMoney.MaxValue);
            Assert.Equal(SqlMoney.MaxValue, record.GetSqlMoney(12));


            // Try adding different values to SqlVariant type
            for (int i = 0; i < record.FieldCount - 1; ++i)
            {
                object valueToSet = record.GetSqlValue(i);
                record.SetValue(record.FieldCount - 1, valueToSet);
                object o = record.GetSqlValue(record.FieldCount - 1);

                if (o is SqlBinary)
                {
                    Assert.Equal<byte>(((SqlBinary)valueToSet).Value, ((SqlBinary)o).Value);
                }
                else
                {
                    Assert.Equal(valueToSet, o);
                }

                record.SetDBNull(record.FieldCount - 1);
                Assert.Equal(DBNull.Value, record.GetSqlValue(record.FieldCount - 1));

                record.SetDBNull(i);
                Assert.Equal(DBNull.Value, record.GetValue(i));
            }
        }
        [Fact]
        public void GetDataTypeName_ReturnsMetaDataTypeIfUdtType()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.Udt, typeof(TestUdt), "sql_TestUdt")
            };

            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Equal("System.Data.SqlClient.Tests.TestUdt", record.GetDataTypeName(0));
        }

        [Fact]
        public void GetDataTypeName_ReturnsTypeFromMetaTypeIfNotUdt()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50)
            };

            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Equal("nvarchar", record.GetDataTypeName(0));
        }
        [Fact]
        public void GetFieldType_ReturnMetaTypeClassType()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50)
            };

            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Equal(typeof(string), record.GetFieldType(0));
        }

        [Fact]
        public void GetValues_ThrowsIfNull()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50)
            };

            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Throws<ArgumentNullException>(() => record.GetValues(null));
        }

        [Fact]
        public void GetValues_IfValuesBiggerThanColumnCount_LastArrayItemKeptEmpty()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);
            record.SetString(0, "test");
            record.SetSqlInt32(1, 2);

            object[] values = new object[5];
            int columnCount = record.GetValues(values);

            for (int i = 2; i < 5; i++)
            {
                Assert.Null(values[i]);
            }
            Assert.Equal(2, columnCount);
        }

        [Fact]
        public void GetValues_IfValuesShorterThanColumnCount_FillOnlyFirstColumn()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);
            record.SetString(0, "test");
            record.SetSqlInt32(1, 2);

            object[] values = new object[1];
            int columnCount = record.GetValues(values);

            Assert.Equal("test", values[0]);
            Assert.Equal(1, columnCount);
        }

        [Fact]
        public void GetValues_FillsArrayAndRespectColumnOrder()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);
            record.SetString(0, "test");
            record.SetSqlInt32(1, 2);

            object[] values = new object[2];
            int columnCount = record.GetValues(values);

            Assert.Equal("test", values[0]);
            Assert.Equal(2, values[1]);
            Assert.Equal(2, columnCount);
        }

        [Fact]
        public void GetOrdinal_ThrowsAgumentNull_IfNameIsNull()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
           {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
           };
            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Throws<ArgumentNullException>(() => record.GetOrdinal(null));
        }

        [Fact]
        public void GetOrdinal_ThrowsOutOfRange_IfNameIsNotAColumn()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);


            Assert.Throws<IndexOutOfRangeException>(() => record.GetOrdinal("outofrange"));

            Assert.Throws<IndexOutOfRangeException>(() => record.GetOrdinal("col1 "));

        }

        [Fact]
        public void GetOrdinal_ReturnsIndexOfColumn()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Equal(1, record.GetOrdinal("col2"));
        }
        [Fact]
        public void GetOrdinal_ReturnsIndexOfColumn_CaseInsensitive()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 50),
                new SqlMetaData("col2", SqlDbType.Int)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);

            Assert.Equal(1, record.GetOrdinal("Col2"));
        }

        [Fact]
        public void GetChar_ThrowsNotSupported()
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.Char, 100)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);
            record.SetValue(0, 'c');
            Assert.Throws<NotSupportedException>(() => record.GetChar(0));
        }

        [Theory]
        [ClassData(typeof(GetXXXBadTypeTestData))]
        public void GetXXX_ThrowsIfBadType(Func<SqlDataRecord, object> getXXX)
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", SqlDbType.NVarChar, 1)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);
            record.SetValue(0, "a");
            Assert.Throws<InvalidCastException>(() => getXXX(record));

        }

        [Theory]
        [ClassData(typeof(GetXXXCheckValueTestData))]
        public void GetXXX_ReturnValue(SqlDbType dbType, object value, Func<SqlDataRecord, object> getXXX)
        {
            SqlMetaData[] metaData = new SqlMetaData[]
            {
                new SqlMetaData("col1", dbType)
            };
            SqlDataRecord record = new SqlDataRecord(metaData);
            record.SetValue(0, value);
            Assert.Equal(value, getXXX(record));

        }
    }

    public class GetXXXBadTypeTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetGuid(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetInt16(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetInt32(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetInt64(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetFloat(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetDouble(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetDecimal(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetDateTime(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetDateTimeOffset(0)) };
            yield return new object[] { new Func<SqlDataRecord, object>(r => r.GetTimeSpan(0)) };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class GetXXXCheckValueTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { SqlDbType.UniqueIdentifier, Guid.NewGuid(), new Func<SqlDataRecord, object>(r => r.GetGuid(0)) };
            yield return new object[] { SqlDbType.SmallInt, (short)123, new Func<SqlDataRecord, object>(r => r.GetInt16(0)) };
            yield return new object[] { SqlDbType.Int, 123456, new Func<SqlDataRecord, object>(r => r.GetInt32(0)) };
            yield return new object[] { SqlDbType.BigInt, (long)123456789, new Func<SqlDataRecord, object>(r => r.GetInt64(0)) };
            yield return new object[] { SqlDbType.Float, (double)1.2, new Func<SqlDataRecord, object>(r => r.GetDouble(0)) };
            yield return new object[] { SqlDbType.Real, (float)1.2, new Func<SqlDataRecord, object>(r => r.GetFloat(0)) };
            yield return new object[] { SqlDbType.Decimal, 1.2m, new Func<SqlDataRecord, object>(r => r.GetDecimal(0)) };
            yield return new object[] { SqlDbType.DateTime, DateTime.Now, new Func<SqlDataRecord, object>(r => r.GetDateTime(0)) };
            yield return new object[] { SqlDbType.DateTimeOffset, new DateTimeOffset(DateTime.Now), new Func<SqlDataRecord, object>(r => r.GetDateTimeOffset(0)) };
            yield return new object[] { SqlDbType.Time, TimeSpan.FromHours(1), new Func<SqlDataRecord, object>(r => r.GetTimeSpan(0)) };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    [SqlUserDefinedType(Format.UserDefined)]
    public class TestUdt
    {

    }
}
