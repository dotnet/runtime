// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data.Odbc;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Data.Odbc.Tests
{
    public class ReaderTests : IntegrationTestBase
    {
        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
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

        [ConditionalFact]
        public void GetString_ShouldNotLoopIndefinitely_WhenDriverReturnsLessDataFollowedByNoData()
        {
            // This test simulates a driver that:
            // - Returns a full length string indication (strLenOrInd)
            // - First returns a partial chunk of data
            // - Then returns a smaller chunk than expected
            // - Finally returns SQL_NO_DATA_FOUND

            command.CommandText = @"CREATE TABLE T (Val TEXT);
                                    INSERT INTO T VALUES ('" + new string('X', 8000) + "');";
            command.ExecuteNonQuery();

            command.CommandText = "SELECT Val FROM T;";
            using (var reader = command.ExecuteReader())
            {
                Assert.True(reader.Read());
                string result = reader.GetString(0);

                // The string should match the inserted string (even if fetched in chunks).
                string expected = new string('X', 8000);
                Assert.Equal(expected, result);
            }
        }
    }
}
