// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Odbc.Tests
{
    public class SmokeTest : IntegrationTestBase
    {
        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void CreateInsertSelectTest()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeByte TINYINT,
                    SomeBoolean BIT,
                    SomeDate DATE,
                    SomeDateTime DATETIME,
                    SomeDecimal DECIMAL(10,5),
                    SomeDouble FLOAT,
                    SomeFloat REAL,
                    SomeGuid UNIQUEIDENTIFIER,
                    SomeInt32 INT,
                    SomeInt64 BIGINT,
                    SomeString NVARCHAR(100))";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeByte,
                    SomeBoolean,
                    SomeDate,
                    SomeDateTime,
                    SomeDecimal,
                    SomeDouble,
                    SomeFloat,
                    SomeGuid,
                    SomeInt32,
                    SomeInt64,
                    SomeString)
                VALUES (
                    7,
                    1,
                    '2010-12-13',
                    '2016-02-29 22:33:44',
                    12345.12002,
                    1.0 + 0.00000001,
                    1.0 + 0.00000001,
                    '9b7c0b33-d38b-4d89-a3b2-0202c55ce6e5',
                    32767532,
                    21474836479999,
                    'SomeString')";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeByte,
                    SomeBoolean,
                    SomeDate,
                    SomeDateTime,
                    SomeDecimal,
                    SomeDouble,
                    SomeFloat,
                    SomeGuid,
                    SomeInt32,
                    SomeInt64,
                    SomeString
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                Assert.Equal((byte)7, reader.GetByte(0));
                Assert.Equal(true, reader.GetBoolean(1));
                Assert.Equal(new DateTime(2010, 12, 13), reader.GetDate(2));
                Assert.Equal(new DateTime(2016, 2, 29, 22, 33, 44), reader.GetDateTime(3));
                Assert.Equal(12345.12002m, reader.GetDecimal(4));
                Assert.Equal(1.00000001d, reader.GetDouble(5));
                Assert.Equal(1f, reader.GetFloat(6));
                // SQLite Driver does not support this parameter
                //Assert.Equal(new Guid("9b7c0b33-d38b-4d89-a3b2-0202c55ce6e5"), reader.GetGuid(7));
                Assert.Equal(32767532, reader.GetInt32(8));
                Assert.Equal(21474836479999, reader.GetInt64(9));
                Assert.Equal("SomeString", reader.GetString(10));
            }
        }
    }
}
