// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Data.OleDb.Tests
{
    public class OleDbDataAdapterTests : OleDbTestBase
    {
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Fill_NullDataTable_Throws()
        {
            var adapter = (OleDbDataAdapter)OleDbFactory.Instance.CreateDataAdapter();
            Assert.Throws<ArgumentNullException>(() => adapter.Fill(null, new object()));
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Fill_NoSelectCommand_Throws()
        {
            RunTest((command, tableName) => {
                var adapter = new OleDbDataAdapter();
                Assert.Throws<InvalidOperationException>(() => adapter.Fill(new DataSet()));
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void DefaultCommandValues()
        {
            RunTest((command, tableName) => {
                string commandText = @"SELECT * FROM " + tableName;
                var dataTable = new DataTable();
                var adapter = new OleDbDataAdapter(commandText, connection);
                Assert.Null(adapter.InsertCommand);
                Assert.Null(adapter.UpdateCommand);
                Assert.Null(adapter.DeleteCommand);
                Assert.NotNull(adapter.SelectCommand);
                Assert.Equal(commandText, adapter.SelectCommand.CommandText);

                adapter.SelectCommand.Dispose();
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Fill_Select_Success()
        {
            RunTest((command, tableName) => {
                OleDbDataAdapter adapter = new OleDbDataAdapter(@"SELECT * FROM " + tableName, ConnectionString);

                DataSet ds = new DataSet();
                adapter.Fill(ds);
                Assert.Equal(1, ds.Tables.Count);
                Assert.Equal(1, ds.Tables[0].Rows.Count);

                string[] expectedValues = { "Foo", "Bar", "John" };
                for (int i = 0; i < expectedValues.Length; i++)
                {
                    Assert.Equal(expectedValues[i], ds.Tables[0].Rows[0][i]);
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Fill_Select_NullDataTable_Throws()
        {
            RunTest((command, tableName) => {
                var adapter = new OleDbDataAdapter(@"SELECT * FROM " + tableName, connection);

                Assert.Throws<ArgumentNullException>(() => adapter.Fill(null));
                Assert.Throws<ArgumentNullException>(() => adapter.Fill(new DataTable(), null));
                Assert.Throws<ArgumentNullException>(() => adapter.Fill(null, null, null));
                Assert.Throws<ArgumentNullException>(() => adapter.Fill(new DataSet(), null, null));

                adapter.SelectCommand.Dispose();
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Update_Success()
        {
            RunTest((command, tableName) => {
                var dataTable = new DataTable();
                var adapter = new OleDbDataAdapter();

                adapter.SelectCommand = new OleDbCommand(@"SELECT * FROM " + tableName + @" WHERE Nickname = @Nickname", connection);
                var selectParam = new OleDbParameter("@Nickname", OleDbType.VarWChar, 30, ParameterDirection.Input, true, 0, 0, "Nickname", DataRowVersion.Current, "John");
                adapter.SelectCommand.Parameters.Add(selectParam);
                adapter.SelectCommand.Transaction = transaction;

                adapter.UpdateCommand = new OleDbCommand(@"UPDATE " + tableName + @" SET Nickname = @Nickname WHERE Firstname = @Firstname", connection);
                OleDbParameter[] parameters = new OleDbParameter[] {
                    new OleDbParameter("@Nickname", OleDbType.WChar, 30, ParameterDirection.Input, true, 0, 0, "Nickname", DataRowVersion.Current, null),
                    new OleDbParameter("@Firstname", OleDbType.WChar, 5, ParameterDirection.Input, false, 0, 0, "Firstname", DataRowVersion.Current, null)
                };
                adapter.UpdateCommand.Parameters.AddRange(parameters);
                adapter.UpdateCommand.Transaction = transaction;

                adapter.Fill(dataTable);
                object titleData = dataTable.Rows[0]["Nickname"];
                Assert.Equal("John", (string)titleData);

                titleData = "Sam";
                adapter.Update(dataTable);
                adapter.Fill(dataTable);
                Assert.Equal("Sam", (string)titleData);

                titleData = "John";
                adapter.Update(dataTable);

                adapter.SelectCommand.Dispose();
                adapter.UpdateCommand.Dispose();
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Fill_OpenDataReader_Throws()
        {
            RunTest((command, tableName) => {
                command.CommandText = @"SELECT * FROM " + tableName;  
                Action<bool> FillShouldThrow = (shouldFail) => {
                    DataSet ds = new DataSet();
                    OleDbDataAdapter adapter = new OleDbDataAdapter(command);
                    if (shouldFail)
                    {
                        AssertExtensions.Throws<InvalidOperationException>(
                            () => adapter.Fill(ds, tableName), 
                            "There is already an open DataReader associated with this Command which must be closed first.");
                    }
                    else
                    {
                        Assert.NotNull(adapter.Fill(ds, tableName));
                    }
                };
                using (var reader = command.ExecuteReader())
                {
                    FillShouldThrow(true);
                }
                using (var reader = command.ExecuteReader()) { }
                FillShouldThrow(false);
            });
        }

        private void RunTest(Action<OleDbCommand, string> testAction, [CallerMemberName] string memberName = null)
        {
            string tableName = Helpers.GetTableName(memberName);
            Assert.False(File.Exists(Path.Combine(TestDirectory, tableName)));
            command.CommandText =
                @"CREATE TABLE " + tableName + @" (
                    Firstname NVARCHAR(5),
                    Lastname NVARCHAR(40), 
                    Nickname NVARCHAR(30))";
            command.ExecuteNonQuery();
            Assert.True(File.Exists(Path.Combine(TestDirectory, tableName)));

            command.CommandText =
                @"INSERT INTO " + tableName + @" ( 
                    Firstname,
                    Lastname,
                    Nickname)
                VALUES ( 'Foo', 'Bar', 'John' );";
            command.ExecuteNonQuery();

            testAction(command, tableName);

            command.CommandText = @"DROP TABLE " + tableName;
            command.ExecuteNonQuery();
        }
    }
}
