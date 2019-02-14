// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SqlAdapterUpdateBatch
    {
        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void SqlAdapterTest()
        {
            string tableName = "BatchDemoTable";
            try
            {
                var createTableQuery = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BatchDemoTable' AND xtype='U')" +
                    " CREATE TABLE [dbo].[" + tableName + "]([TransactionNumber][int] IDENTITY(1, 1) NOT NULL,[Level] [nvarchar] (50) NOT NULL," +
                    "[Message] [nvarchar] (500) NOT NULL,[EventTime] [datetime]NOT NULL,CONSTRAINT[PK_BatchDemoTable] " +
                    "PRIMARY KEY CLUSTERED([TransactionNumber] ASC)WITH(PAD_INDEX = OFF,STATISTICS_NORECOMPUTE = OFF, " +
                    "IGNORE_DUP_KEY = OFF,ALLOW_ROW_LOCKS = ON,ALLOW_PAGE_LOCKS = ON,FILLFACTOR = 90) ON[PRIMARY]) ON[PRIMARY]";

                using (var connection = new SqlConnection(DataTestUtility.TcpConnStr))
                using (var cmd = new SqlCommand(createTableQuery, connection))
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
                ExecuteNonQueries();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                var dropTableQuery = "DROP TABLE IF EXISTS " + tableName;
                using (var connection = new SqlConnection(DataTestUtility.TcpConnStr))
                using (var cmd = new SqlCommand(dropTableQuery, connection))
                {
                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }


        public class EventInfo
        {
            public string Level { get; set; }
            public string Message { get; set; }
            public DateTime EventTime { get; set; }

            public EventInfo()
            {
                EventTime = DateTime.Now;
            }
        }

        public static void ExecuteNonQueries()
        {
            List<EventInfo> entities = new List<EventInfo>
            {
                new EventInfo {Level = "L1", Message = "Message 1"},
                new EventInfo {Level = "L2", Message = "Message 2"},
                new EventInfo {Level = "L3", Message = "Message 3"},
                new EventInfo {Level = "L4", Message = "Message 4"},
            };

            var sql = "INSERT INTO BatchDemoTable(Level, Message, EventTime)  VALUES(@Level, @Message, @EventTime)";
            using (var connection = new SqlConnection(DataTestUtility.TcpConnStr))
            using (var adapter = new SqlDataAdapter())
            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.Add(new SqlParameter("@Level", System.Data.SqlDbType.NVarChar, 50, "Level"));
                cmd.Parameters.Add(new SqlParameter("@Message", SqlDbType.NVarChar, 500, "Message"));
                cmd.Parameters.Add(new SqlParameter("@EventTime", SqlDbType.DateTime, 0, "EventTime"));
                cmd.UpdatedRowSource = UpdateRowSource.None;

                adapter.InsertCommand = cmd;
                adapter.UpdateBatchSize = 2;

                adapter.Update(ConvertToTable(entities));
            }
        }
        private static DataTable ConvertToTable(List<EventInfo> entities)
        {
            var table = new DataTable(typeof(EventInfo).Name);

            table.Columns.Add("Level", typeof(string));
            table.Columns.Add("Message", typeof(string));
            table.Columns.Add("EventTime", typeof(DateTime));

            foreach (var entity in entities)
            {
                var row = table.NewRow();
                row["Level"] = entity.Level;
                row["Message"] = entity.Message;
                row["EventTime"] = entity.EventTime;
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
