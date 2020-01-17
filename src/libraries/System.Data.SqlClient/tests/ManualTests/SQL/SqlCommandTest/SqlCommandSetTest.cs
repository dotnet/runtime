using System;
using System.Data.SqlTypes;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SqlCommandSetTest
    {
        private static Assembly mds = Assembly.GetAssembly(typeof(SqlConnection));

        [ConditionalFact(typeof(DataTestUtility), nameof(DataTestUtility.AreConnStringsSetup))]
        public void TestByteArrayParameters()
        {
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("CMD");
            string procName = DataTestUtility.GetUniqueNameForSqlServer("CMD");
            byte[] bArray = new byte[] { 1, 2, 3 };

            using (var connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                try
                {
                    using (var cmd = new SqlCommand(procName, connection))
                    {
                        setupByteArrayArtifacts(connection, tableName, procName);

                        // Insert with SqlCommand
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        SqlCommandBuilder.DeriveParameters(cmd);
                        cmd.Parameters["@array"].Value = bArray;

                        cmd.ExecuteNonQuery();

                        //Insert with command Set
                        var commandSetType = mds.GetType("Microsoft.Data.SqlClient.SqlCommandSet");
                        var cmdSet = Activator.CreateInstance(commandSetType, true);
                        commandSetType.GetMethod("Append", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(cmdSet, new object[] { cmd });
                        commandSetType.GetProperty("Connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetSetMethod(true).Invoke(cmdSet, new object[] { connection });
                        commandSetType.GetMethod("ExecuteNonQuery", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(cmdSet, new object[] { });

                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.CommandText = $"SELECT * FROM {tableName}";
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                SqlBytes byteArray = reader.GetSqlBytes(0);
                                Assert.Equal(byteArray.Length, bArray.Length);

                                for (int i = 0; i < bArray.Length; i++)
                                {
                                    Assert.Equal(bArray[i], byteArray[i]);
                                }
                            }

                        }
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                finally
                {
                    dropByteArrayArtifacts(connection, tableName, procName);
                }
            }
        }

        private void dropByteArrayArtifacts(SqlConnection connection, string tableName, string procName)
        {
            using (SqlCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"DROP TABLE IF EXISTS {tableName}";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $"DROP PROCEDURE IF EXISTS {procName}";
                cmd.ExecuteNonQuery();
            }
        }

        private void setupByteArrayArtifacts(SqlConnection connection, string tableName, string procName)
        {
            using (SqlCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"CREATE TABLE {tableName} (ByteArrayColumn varbinary(max))";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $"CREATE PROCEDURE {procName} @array varbinary(max) AS BEGIN SET NOCOUNT ON; " +
                    $"insert into {tableName}(ByteArrayColumn) values(@array) END";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
