// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.IO;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class SqlFileStreamTest
    {
        private static bool IsFileStreamEnvironmentSet() => DataTestUtility.IsFileStreamSetup();
        private static bool AreConnectionStringsSetup() => DataTestUtility.AreConnStringsSetup();
        private static bool IsIntegratedSecurityEnvironmentSet() => DataTestUtility.IsIntegratedSecuritySetup();

        private static int[] s_insertedValues = { 11 , 22 };

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(nameof(IsFileStreamEnvironmentSet), nameof(IsIntegratedSecurityEnvironmentSet), nameof(AreConnectionStringsSetup))]
        public static void ReadFilestream()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                string tempTable = SetupTable(connection);
                int nRow = 0;
                byte[] retrievedValue;
                SqlCommand command = new SqlCommand($"SELECT Photo.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT(),EmployeeId FROM {tempTable} ORDER BY EmployeeId", connection);

                SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                command.Transaction = transaction;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Get the pointer for the file.
                        string path = reader.GetString(0);
                        byte[] transactionContext = reader.GetSqlBytes(1).Buffer;
                        
                        // Create the SqlFileStream  
                        using (Stream fileStream = new SqlFileStream(path, transactionContext, FileAccess.Read, FileOptions.SequentialScan, allocationSize: 0))
                        {
                            // Read the contents as bytes.
                            retrievedValue = new byte[fileStream.Length];
                            fileStream.Read(retrievedValue,0,(int)(fileStream.Length));

                            // Reverse the byte array, if the system architecture is little-endian.
                            if (BitConverter.IsLittleEndian)
                                Array.Reverse(retrievedValue);

                            // Compare inserted and retrieved values.
                            Assert.Equal(s_insertedValues[nRow], BitConverter.ToInt32(retrievedValue,0));
                        }
                        nRow++;
                    }
                    
                }
                transaction.Commit();

                // Drop Table
                ExecuteNonQueryCommand($"DROP TABLE {tempTable}", connection);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(nameof(IsFileStreamEnvironmentSet), nameof(IsIntegratedSecurityEnvironmentSet), nameof(AreConnectionStringsSetup))]
        public static void OverwriteFilestream()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                string tempTable = SetupTable(connection);
                byte[] insertedValue = BitConverter.GetBytes(3);

                // Reverse the byte array, if the system architecture is little-endian.
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(insertedValue);

                SqlCommand command = new SqlCommand($"SELECT TOP(1) Photo.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT(),EmployeeId FROM {tempTable} ORDER BY EmployeeId", connection);

                SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                command.Transaction = transaction;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Get the pointer for file   
                        string path = reader.GetString(0);
                        byte[] transactionContext = reader.GetSqlBytes(1).Buffer;

                        // Create the SqlFileStream  
                        using (Stream fileStream = new SqlFileStream(path, transactionContext, FileAccess.Write, FileOptions.SequentialScan, allocationSize: 0))
                        {
                            // Overwrite the first row in the table
                            fileStream.Write((insertedValue), 0, 4);
                        }
                    }
                }
                transaction.Commit();

                // Compare inserted and retrieved value
                byte[] retrievedValue = RetrieveData(tempTable, connection, insertedValue.Length);
                Assert.Equal(insertedValue, retrievedValue);
                
                // Drop Table
                ExecuteNonQueryCommand($"DROP TABLE {tempTable}", connection);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows)]
        [ConditionalFact(nameof(IsFileStreamEnvironmentSet), nameof(IsIntegratedSecurityEnvironmentSet), nameof(AreConnectionStringsSetup))]
        public static void AppendFilestream()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                string tempTable = SetupTable(connection);

                byte[] insertedValue = BitConverter.GetBytes(s_insertedValues[0]);
                byte appendedByte = 0x04;
                insertedValue = AddByteToArray(insertedValue, appendedByte);

                // Reverse the byte array, if the system architecture is little-endian.
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(insertedValue);

                SqlCommand command = new SqlCommand($"SELECT TOP(1) Photo.PathName(), GET_FILESTREAM_TRANSACTION_CONTEXT(),EmployeeId FROM {tempTable} ORDER BY EmployeeId", connection);

                SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
                command.Transaction = transaction;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Get the pointer for file  
                        string path = reader.GetString(0);
                        byte[] transactionContext = reader.GetSqlBytes(1).Buffer;

                        using (Stream fileStream = new SqlFileStream(path, transactionContext, FileAccess.ReadWrite, FileOptions.SequentialScan, allocationSize: 0))
                        {
                            // Seek to the end of the file  
                            fileStream.Seek(0, SeekOrigin.End);

                            // Append a single byte   
                            fileStream.WriteByte(appendedByte);
                        }
                    }
                }
                transaction.Commit();

                // Compare inserted and retrieved value
                byte[] retrievedValue = RetrieveData(tempTable, connection, insertedValue.Length);
                Assert.Equal(insertedValue, retrievedValue);

                // Drop Table
                ExecuteNonQueryCommand($"DROP TABLE {tempTable}", connection);
            }
        }
        #region Private helper methods
        private static string SetupTable(SqlConnection conn)
        {
            // Generate random table name
            string tempTable = "fs_" + Guid.NewGuid().ToString().Replace('-', '_');

            // Create table
            string createTable = $"CREATE TABLE {tempTable} (EmployeeId INT  NOT NULL  PRIMARY KEY, Photo VARBINARY(MAX) FILESTREAM  NULL, RowGuid UNIQUEIDENTIFIER NOT NULL ROWGUIDCOL UNIQUE DEFAULT NEWID() ) ";
            ExecuteNonQueryCommand(createTable, conn);

            // Insert data into created table
            for (int i = 0; i < s_insertedValues.Length; i++)
            {
                string prepTable = $"INSERT INTO {tempTable} VALUES ({i + 1}, {s_insertedValues[i]} , default)";
                ExecuteNonQueryCommand(prepTable, conn);
            }

            return tempTable;
        }

        private static void ExecuteNonQueryCommand(string cmdText, SqlConnection conn)
        {
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.ExecuteNonQuery();
            }
        }

        private static byte[] RetrieveData(string tempTable, SqlConnection conn, int len)
        {
            SqlCommand command = new SqlCommand($"SELECT TOP(1) Photo FROM {tempTable}", conn);
            byte[] bArray = new byte[len];
            using (SqlDataReader reader = command.ExecuteReader())
            {
                reader.Read();
                reader.GetBytes(0, 0, bArray, 0, len);
            }
            return bArray;
        }

        public static byte[] AddByteToArray(byte[] oldArray, byte newByte)
        {
            byte[] newArray = new byte[oldArray.Length + 1];
            oldArray.CopyTo(newArray, 1);
            newArray[0] = newByte;
            return newArray;
        }
        #endregion
    }
}
