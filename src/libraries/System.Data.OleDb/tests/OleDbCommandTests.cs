// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Data.OleDb.Tests
{
    [Collection("System.Data.OleDb")] // not let tests run in parallel
    public class OleDbCommandTests : OleDbTestBase
    {
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void UpdatedRowSource_SetInvalidValue_Throws()
        {
            const int InvalidValue = 50;
            using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
            {
                Assert.Equal(UpdateRowSource.Both, cmd.UpdatedRowSource);
                cmd.UpdatedRowSource = UpdateRowSource.FirstReturnedRecord;
                Assert.Equal(UpdateRowSource.FirstReturnedRecord, cmd.UpdatedRowSource);
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    () => cmd.UpdatedRowSource = (UpdateRowSource)InvalidValue, 
                    $"The {nameof(UpdateRowSource)} enumeration value, {InvalidValue}, is invalid.\r\nParameter name: {nameof(UpdateRowSource)}"
                );
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void CommandTimeout_SetInvalidValue_Throws()
        {
            const int InvalidValue = -1;
            using (var cmd = new OleDbCommand(default, connection, transaction))
            {
                AssertExtensions.Throws<ArgumentException>(
                    () => cmd.CommandTimeout = InvalidValue, 
                    $"Invalid CommandTimeout value {InvalidValue}; the value must be >= 0.\r\nParameter name: {nameof(cmd.CommandTimeout)}"
                );
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ResetCommandTimeout_ResetsToDefault()
        {
            using (var cmd = new OleDbCommand(default, connection, transaction))
            {
                const int DefaultValue = 30;
                Assert.Equal(DefaultValue, cmd.CommandTimeout);
                cmd.CommandTimeout = DefaultValue + 50;
                Assert.Equal(DefaultValue + 50, cmd.CommandTimeout);
                cmd.ResetCommandTimeout();
                Assert.Equal(DefaultValue, cmd.CommandTimeout);
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void CommandType_SetInvalidValue_Throws()
        {
            const int InvalidValue = 0;
            using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>(
                    () => cmd.CommandType = (CommandType)InvalidValue, 
                    $"The CommandType enumeration value, {InvalidValue}, is invalid.\r\nParameter name: {nameof(cmd.CommandType)}"
                );
            }
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Prepare_ClosedConnection_Throws()
        {
            RunTest((command, tableName) => {
                command.CommandText = @"SELECT * FROM " + tableName;
                connection.Close();
                AssertExtensions.Throws<InvalidOperationException>(
                    () => command.Prepare(), 
                    $"{nameof(command.Prepare)} requires an open and available Connection. The connection's current state is closed."
                );
                connection.Open(); // reopen when done
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Prepare_MultipleCases_ThrowsForInvalidQuery()
        {
            RunTest((command, tableName) => {
                Assert.Equal(ConnectionState.Open, connection.State);
                command.CommandText = "INVALID_STATEMENT";
                AssertExtensions.Throws<OleDbException>(
                    () => command.Prepare(), 
                    "Invalid SQL statement; expected 'DELETE', 'INSERT', 'PROCEDURE', 'SELECT', or 'UPDATE'."
                );
                command.CommandText = @"UPDATE " + tableName + " SET NumPlants ? WHERE Firstname = ?";
                AssertExtensions.Throws<OleDbException>(
                    () => command.Prepare(), 
                    $"Syntax error in UPDATE statement."
                );
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Prepare_InsertMultipleItems_UseTableDirectToVerify()
        {
            RunTest((command, tableName) => {
                command.CommandText = @"INSERT INTO " + tableName + " (Firstname, NumPlants) VALUES (?, ?)";
                command.Prepare(); // Good to use when command used multiple times

                command.Parameters.Add(command.CreateParameter());
                command.Parameters.Add(command.CreateParameter());
                
                object[] newItems = new object[] {
                    new { Firstname = "John", NumPlants = 7 },
                    new { Firstname = "Mark", NumPlants = 12 },
                    new { Firstname = "Nick", NumPlants = 6 }
                };
                foreach (dynamic item in newItems)
                {    
                    command.Parameters[0].Value = item.Firstname;
                    command.Parameters[1].Value = item.NumPlants;
                    command.ExecuteNonQuery();
                }
                var currentCommandType = command.CommandType;
                command.CommandType = CommandType.TableDirect;
                command.CommandText = tableName;
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read(), "skip existing row");
                    Assert.True(reader.Read(), "skip existing row");
                    foreach (dynamic item in newItems)
                    {
                        Assert.True(reader.Read(), "validate new row");
                        Assert.Equal(item.Firstname, reader["Firstname"]);
                        Assert.Equal(item.NumPlants, reader["NumPlants"]);
                    }
                    object x;
                    AssertExtensions.Throws<IndexOutOfRangeException>(() => x = reader["MissingColumn"], "MissingColumn");
                }
                command.CommandType = currentCommandType;
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Parameters_AddNullParameter_Throws()
        {
            RunTest((command, tableName) => {
                AssertExtensions.Throws<ArgumentNullException>(
                    () => command.Parameters.Add(null), 
                    $"The {nameof(OleDbParameterCollection)} only accepts non-null {nameof(OleDbParameter)} type objects.\r\nParameter name: value"
                );
                command.CommandText = "SELECT * FROM " + tableName + " WHERE NumPlants = ?";
                command.Parameters.Add(new OleDbParameter("@p1", 7));
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.Equal("John", reader["Firstname"]);
                    Assert.Equal(7, reader["NumPlants"]);
                    Assert.False(reader.Read(), "Expected to find only one item");
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ExecuteNonQuery_NullConnection_Throws()
        {
            RunTest((command, tableName) => {
                command.CommandText = @"SELECT * FROM " + tableName;
                var currentConnection = command.Connection;
                command.Connection = null;
                AssertExtensions.Throws<InvalidOperationException>(
                    () => command.ExecuteNonQuery(), 
                    $"{nameof(command.ExecuteNonQuery)}: {nameof(command.Connection)} property has not been initialized."
                );
                command.Connection = currentConnection;
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void CommandType_InvalidType_Throws()
        {
            RunTest((command, tableName) => {
                using (var innerCommand = new OleDbCommand(cmdText: @"SELECT * FROM " + tableName))
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => innerCommand.CommandType = (CommandType)0);
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ExecuteScalar_Select_ComputesSumAndCount()
        {
            RunTest((command, tableName) => {
                command.CommandText = @"SELECT Count(*) FROM " + tableName;  
                Assert.Equal(2, Convert.ToInt32(command.ExecuteScalar()));
                
                command.CommandText = @"SELECT Sum(NumPlants) FROM " + tableName;  
                Assert.Equal(13, Convert.ToInt32(command.ExecuteScalar()));
            });
        }

        private void RunTest(Action<OleDbCommand, string> testAction, [CallerMemberName] string memberName = null)
        {
            string tableName = Helpers.GetTableName(memberName);
            Assert.False(File.Exists(Path.Combine(TestDirectory, tableName)));
            command.CommandText =
                @"CREATE TABLE " + tableName + @" (
                    Firstname NVARCHAR(5),
                    NumPlants INT)";
            command.ExecuteNonQuery();
            Assert.True(File.Exists(Path.Combine(TestDirectory, tableName)));

            command.CommandText =
                @"INSERT INTO " + tableName + @" ( 
                    Firstname,
                    NumPlants)
                VALUES ( 'John', 7 );";
            command.ExecuteNonQuery();

            command.CommandText =
                @"INSERT INTO " + tableName + @" ( 
                    Firstname,
                    NumPlants)
                VALUES ( 'Sam', 6 );";
            command.ExecuteNonQuery();

            testAction(command, tableName);

            command.CommandText = @"DROP TABLE " + tableName;
            command.ExecuteNonQuery();
        }
    }
}
