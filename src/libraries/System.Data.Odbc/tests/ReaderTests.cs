// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Odbc.Tests
{
    public class ReaderTests : IntegrationTestBase
    {
        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void EmptyReader()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeString NVARCHAR(100))";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeString
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                Assert.False(reader.HasRows);

                var exception = Record.Exception(() => reader.GetString(1));
                Assert.NotNull(exception);
                Assert.IsType<InvalidOperationException>(exception);
                Assert.Equal(
                    "No data exists for the row/column.",
                    exception.Message);

                var values = new object[1];
                exception = Record.Exception(() => reader.GetValues(values));
                Assert.NotNull(exception);
                Assert.IsType<InvalidOperationException>(exception);
                Assert.Equal(
                    "No data exists for the row/column.",
                    exception.Message);
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void GetValues()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeInt32 INT,
                    SomeString NVARCHAR(100))";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeInt32,
                    SomeString)
                VALUES (
                    2147483647,
                    'SomeString')";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeInt32,
                    SomeString
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                Assert.Equal(2147483647, values[0]);
                Assert.Equal("SomeString", values[1]);
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void GetValueFailsWithBigIntWithBackwardsCompatibility()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeInt64 BIGINT)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeInt64)
                VALUES (
                    2147499983647)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeInt64
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                var values = new object[reader.FieldCount];
                var exception = Record.Exception(() => reader.GetValue(0));
                Assert.NotNull(exception);
                Assert.IsType<ArgumentException>(exception);
                Assert.Equal(
                    "Unknown SQL type - -25.",
                    exception.Message);

                Assert.Equal(2147499983647, reader.GetInt64(0));
                Assert.Equal(2147499983647, reader.GetValue(0));
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void GetDataTypeName()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeInt64 BIGINT)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeInt64)
                VALUES (
                    2147499983647)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeInt64
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                Assert.Equal("BIGINT", reader.GetDataTypeName(0));
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void GetFieldTypeIsNotSupportedInSqlite()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeInt64 BIGINT)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeInt64)
                VALUES (
                    2147499983647)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeInt64
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                var exception = Record.Exception(() => reader.GetFieldType(0));
                Assert.NotNull(exception);
                Assert.IsType<ArgumentException>(exception);
                Assert.Equal(
                    "Unknown SQL type - -25.",
                    exception.Message);
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void IsDbNullIsNotSupportedInSqlite()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeInt64 BIGINT)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeInt64)
                VALUES (
                    2147499983647)";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeInt64
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                var exception = Record.Exception(() => reader.IsDBNull(0));
                Assert.NotNull(exception);
                Assert.IsType<ArgumentException>(exception);
                Assert.Equal(
                    "Unknown SQL type - -25.",
                    exception.Message);
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void InvalidRowIndex()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeString NVARCHAR(100))";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeString)
                VALUES (
                    'SomeString')";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeString
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                Assert.True(reader.HasRows);
                var exception = Record.Exception(() => reader.GetString(2));
                Assert.NotNull(exception);
                Assert.IsType<IndexOutOfRangeException>(exception);
                Assert.Equal(
                    "Index was outside the bounds of the array.",
                    exception.Message);
            }
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void InvalidRowName()
        {
            command.CommandText =
                @"CREATE TABLE SomeTable (
                    SomeString NVARCHAR(100))";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO SomeTable (
                    SomeString)
                VALUES (
                    'SomeString')";
            command.ExecuteNonQuery();

            command.CommandText =
                @"SELECT 
                    SomeString
                FROM SomeTable";
            using (var reader = command.ExecuteReader())
            {
                reader.Read();
                Assert.True(reader.HasRows);
                var exception = Record.Exception(() => reader["SomeOtherString"]);
                Assert.NotNull(exception);
                Assert.IsType<IndexOutOfRangeException>(exception);
                Assert.Equal(
                    "SomeOtherString",
                    exception.Message);
            }
        }
    }
}
