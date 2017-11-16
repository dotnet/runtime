// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.Common;
using System.Data.SqlTypes;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ParametersTest
    {
        private static string s_connString = DataTestUtility.TcpConnStr;

        [CheckConnStrSetupFact]
        public static void CodeCoverageSqlClient()
        {
            SqlParameterCollection opc = new SqlCommand().Parameters;

            Assert.True(opc.Count == 0, string.Format("FAILED: Expected count: {0}. Actual count: {1}.", 0, opc.Count));
            Assert.False(((IList)opc).IsReadOnly, "FAILED: Expected collection to NOT be read only.");
            Assert.False(((IList)opc).IsFixedSize, "FAILED: Expected collection to NOT be fixed size.");
            Assert.False(((IList)opc).IsSynchronized, "FAILED: Expected collection to NOT be synchronized.");
            DataTestUtility.AssertEqualsWithDescription("Object", ((IList)opc).SyncRoot.GetType().Name, "FAILED: Incorrect SyncRoot Name");

            {
                string failValue;
                DataTestUtility.AssertThrowsWrapper<IndexOutOfRangeException>(() => failValue = opc[0].ParameterName, "Invalid index 0 for this SqlParameterCollection with Count=0.");

                DataTestUtility.AssertThrowsWrapper<IndexOutOfRangeException>(() => failValue = opc["@p1"].ParameterName, "An SqlParameter with ParameterName '@p1' is not contained by this SqlParameterCollection.");
            }

            DataTestUtility.AssertThrowsWrapper<ArgumentNullException>(() => opc.Add(null), "The SqlParameterCollection only accepts non-null SqlParameter type objects.");

            opc.Add((object)new SqlParameter());
            IEnumerator enm = opc.GetEnumerator();
            Assert.True(enm.MoveNext(), "FAILED: Expected MoveNext to be true");
            DataTestUtility.AssertEqualsWithDescription("Parameter1", ((SqlParameter)enm.Current).ParameterName, "FAILED: Incorrect ParameterName");

            opc.Add(new SqlParameter());
            DataTestUtility.AssertEqualsWithDescription("Parameter2", opc[1].ParameterName, "FAILED: Incorrect ParameterName");

            opc.Add(new SqlParameter(null, null));
            opc.Add(null, SqlDbType.Int, 0, null);
            DataTestUtility.AssertEqualsWithDescription("Parameter4", opc["Parameter4"].ParameterName, "FAILED: Incorrect ParameterName");

            opc.Add(new SqlParameter("Parameter5", SqlDbType.NVarChar, 20));
            opc.Add(new SqlParameter(null, SqlDbType.NVarChar, 20, "a"));
            opc.RemoveAt(opc[3].ParameterName);
            DataTestUtility.AssertEqualsWithDescription(-1, opc.IndexOf(null), "FAILED: Incorrect index for null value");

            SqlParameter p = opc[0];

            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => opc.Add((object)p), "The SqlParameter is already contained by another SqlParameterCollection.");

            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => new SqlCommand().Parameters.Add(p), "The SqlParameter is already contained by another SqlParameterCollection.");

            DataTestUtility.AssertThrowsWrapper<ArgumentNullException>(() => opc.Remove(null), "The SqlParameterCollection only accepts non-null SqlParameter type objects.");

            string pname = p.ParameterName;
            p.ParameterName = pname;
            p.ParameterName = pname.ToUpper();
            p.ParameterName = pname.ToLower();
            p.ParameterName = "@p1";
            p.ParameterName = pname;

            opc.Clear();
            opc.Add(p);

            opc.Clear();
            opc.AddWithValue("@p1", null);

            DataTestUtility.AssertEqualsWithDescription(-1, opc.IndexOf(p.ParameterName), "FAILED: Incorrect index for parameter name");

            opc[0] = p;
            DataTestUtility.AssertEqualsWithDescription(0, opc.IndexOf(p.ParameterName), "FAILED: Incorrect index for parameter name");

            Assert.True(opc.Contains(p.ParameterName), "FAILED: Expected collection to contain provided parameter.");
            Assert.True(opc.Contains(opc[0]), "FAILED: Expected collection to contain provided parameter.");

            opc[0] = p;
            opc[p.ParameterName] = new SqlParameter(p.ParameterName, null);
            opc[p.ParameterName] = new SqlParameter();
            opc.RemoveAt(0);

            new SqlCommand().Parameters.Clear();
            new SqlCommand().Parameters.CopyTo(new object[0], 0);
            Assert.False(new SqlCommand().Parameters.GetEnumerator().MoveNext(), "FAILED: Expected MoveNext to be false");

            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => new SqlCommand().Parameters.Add(0), "The SqlParameterCollection only accepts non-null SqlParameter type objects, not Int32 objects.");

            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => new SqlCommand().Parameters.Insert(0, 0), "The SqlParameterCollection only accepts non-null SqlParameter type objects, not Int32 objects.");

            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => new SqlCommand().Parameters.Remove(0), "The SqlParameterCollection only accepts non-null SqlParameter type objects, not Int32 objects.");

            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => new SqlCommand().Parameters.Remove(new SqlParameter()), "Attempted to remove an SqlParameter that is not contained by this SqlParameterCollection.");
        }

        [CheckConnStrSetupFact]
        public static void Test_SqlParameter_Constructor()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                var dataTable = new DataTable();
                var adapter = new SqlDataAdapter();

                adapter.SelectCommand = new SqlCommand("SELECT CustomerID, ContactTitle FROM dbo.Customers WHERE ContactTitle = @ContactTitle", conn);
                var selectParam = new SqlParameter("@ContactTitle", SqlDbType.NVarChar, 30, ParameterDirection.Input, true, 0, 0, "ContactTitle", DataRowVersion.Current, "Owner");
                adapter.SelectCommand.Parameters.Add(selectParam);

                adapter.UpdateCommand = new SqlCommand("UPDATE dbo.Customers SET ContactTitle = @ContactTitle WHERE CustomerID = @CustomerID", conn);
                var titleParam = new SqlParameter("@ContactTitle", SqlDbType.NVarChar, 30, ParameterDirection.Input, true, 0, 0, "ContactTitle", DataRowVersion.Current, null);
                var idParam = new SqlParameter("@CustomerID", SqlDbType.NChar, 5, ParameterDirection.Input, false, 0, 0, "CustomerID", DataRowVersion.Current, null);
                adapter.UpdateCommand.Parameters.Add(titleParam);
                adapter.UpdateCommand.Parameters.Add(idParam);

                adapter.Fill(dataTable);
                object titleData = dataTable.Rows[0]["ContactTitle"];
                Assert.Equal("Owner", (string)titleData);

                titleData = "Test Data";
                adapter.Update(dataTable);
                adapter.Fill(dataTable);
                Assert.Equal("Test Data", (string)titleData);

                titleData = "Owner";
                adapter.Update(dataTable);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithEnumValue_ShouldInferToUnderlyingType()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var cmd = new SqlCommand("select @input", conn);
                cmd.Parameters.AddWithValue("@input", MyEnum.B);
                object value = cmd.ExecuteScalar();
                Assert.Equal((MyEnum)value, MyEnum.B);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithOutputEnumParameter_ShouldReturnEnum()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var cmd = new SqlCommand("set @output = @input", conn);
                cmd.Parameters.AddWithValue("@input", MyEnum.B);

                var outputParam = cmd.CreateParameter();
                outputParam.ParameterName = "@output";
                outputParam.DbType = DbType.Int32;
                outputParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outputParam);

                cmd.ExecuteNonQuery();

                Assert.Equal((MyEnum)outputParam.Value, MyEnum.B);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithDecimalValue_ShouldReturnDecimal()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var cmd = new SqlCommand("select @foo", conn);
                cmd.Parameters.AddWithValue("@foo", new SqlDecimal(0.5));
                var result = (decimal)cmd.ExecuteScalar();
                Assert.Equal(result, (decimal)0.5);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithGuidValue_ShouldReturnGuid()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var expectedGuid = Guid.NewGuid();
                var cmd = new SqlCommand("select @input", conn);
                cmd.Parameters.AddWithValue("@input", expectedGuid);
                var result = cmd.ExecuteScalar();
                Assert.Equal(expectedGuid, (Guid)result);
            }
        }

        [CheckConnStrSetupFact]
        public static void TestParametersWithDatatablesTVPInsert()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr);
            builder.InitialCatalog = "tempdb";
            int x = 4, y = 5;

            DataTable table = new DataTable { Columns = { { "x", typeof(int) }, { "y", typeof(int) } }, Rows = { { x, y } } };

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                ExecuteSqlIgnoreExceptions(connection, "drop proc dbo.UpdatePoint");
                ExecuteSqlIgnoreExceptions(connection, "drop table dbo.PointTable");
                ExecuteSqlIgnoreExceptions(connection, "drop type dbo.PointTableType");
                ExecuteSqlIgnoreExceptions(connection, "CREATE TYPE dbo.PointTableType AS TABLE (x INT, y INT)");
                ExecuteSqlIgnoreExceptions(connection, "CREATE TABLE dbo.PointTable (x INT, y INT)");
                ExecuteSqlIgnoreExceptions(connection, "CREATE PROCEDURE dbo.UpdatePoint @TVP dbo.PointTableType READONLY AS SET NOCOUNT ON INSERT INTO dbo.PointTable(x, y) SELECT * FROM  @TVP");

                using (SqlCommand cmd = connection.CreateCommand())
                {
                    // Update Data Using TVPs
                    cmd.CommandText = "dbo.UpdatePoint";
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter parameter = cmd.Parameters.AddWithValue("@TVP", table);
                    parameter.TypeName = "dbo.PointTableType";

                    cmd.ExecuteNonQuery();

                    // Verify if the data was updated 
                    cmd.CommandText = "select * from dbo.PointTable";
                    cmd.CommandType = CommandType.Text;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        DataTable dbData = new DataTable();
                        dbData.Load(reader);
                        Assert.Equal(1, dbData.Rows.Count);
                        Assert.Equal(x, dbData.Rows[0][0]);
                        Assert.Equal(y, dbData.Rows[0][1]);
                    }
                }
            }
        }

        private static void ExecuteSqlIgnoreExceptions(DbConnection connection, string query)
        {
            using (DbCommand cmd = connection.CreateCommand())
            {
                try
                {
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                catch { /* Ignore exception if the command execution fails*/ }
            }
        }

        private enum MyEnum
        {
            A = 1,
            B = 2
        }

    }
}
