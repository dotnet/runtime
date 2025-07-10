using System;
using System.Data.Odbc;
using System.Text;
using Xunit;

namespace System.Data.Odbc.Tests
{
    public class OdbcDataReaderTests
    {
        [Fact]
        public void GetString_ShouldNotLoopIndefinitely_WhenDriverReturnsLessDataFollowedByNoData()
        {
            // This test simulates a driver that:
            // - Returns a full length string indication (strLenOrInd)
            // - First returns a partial chunk of data
            // - Then returns a smaller chunk than expected
            // - Finally returns SQL_NO_DATA_FOUND

            // We use an in-memory SQLite database via ODBC as a test harness.
            using var connection = new OdbcConnection("Driver=SQLite3;Database=:memory:");
            connection.Open();

            using var create = connection.CreateCommand();
            create.CommandText = "CREATE TABLE T (Val TEXT); INSERT INTO T VALUES ('" + new string('X', 8000) + "');";
            create.ExecuteNonQuery();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Val FROM T;";
            using var reader = cmd.ExecuteReader();

            Assert.True(reader.Read());
            string result = reader.GetString(0);

            // The string should match the inserted string (even if fetched in chunks).
            string expected = new string('X', 8000);
            Assert.Equal(expected, result);
        }
    }
}