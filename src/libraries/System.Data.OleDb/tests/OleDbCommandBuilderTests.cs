// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Data.OleDb.Tests
{
    public class OleDbCommandBuilderTests : OleDbTestBase
    {
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void DeriveParameters_NullCommand_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => OleDbCommandBuilder.DeriveParameters(null));
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [InlineData(CommandType.Text)]
        [InlineData(CommandType.TableDirect)]
        public void DeriveParameters_NullCommand_Throws(CommandType commandType)
        {
            using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
            {
                cmd.CommandType = commandType;
                AssertExtensions.Throws<InvalidOperationException>(
                    () => OleDbCommandBuilder.DeriveParameters(cmd), 
                    $"{nameof(OleDbCommand)} DeriveParameters only supports CommandType.StoredProcedure, not CommandType.{cmd.CommandType.ToString()}.");
            }
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void DeriveParameters_NulllCommandText_Throws()
        {
            using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = null;
                AssertExtensions.Throws<InvalidOperationException>(
                    () => OleDbCommandBuilder.DeriveParameters(cmd), 
                    $"{nameof(OleDbCommandBuilder.DeriveParameters)}: {nameof(cmd.CommandText)} property has not been initialized");
            }
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void DeriveParameters_NullConnection_Throws()
        {
            RunTest((command, tableName) => {
                using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = @"SELECT * FROM " + tableName;  
                    cmd.Connection = null;
                    
                    AssertExtensions.Throws<InvalidOperationException>(
                        () => OleDbCommandBuilder.DeriveParameters(cmd), 
                        $"{nameof(OleDbCommandBuilder.DeriveParameters)}: {nameof(cmd.Connection)} property has not been initialized.");
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void DeriveParameters_ClosedConnection_Throws()
        {
            RunTest((command, tableName) => {
                using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = @"SELECT * FROM " + tableName;  
                    cmd.Connection = (OleDbConnection)OleDbFactory.Instance.CreateConnection();
                    cmd.Connection.Close();
                    var exception = Record.Exception(() => OleDbCommandBuilder.DeriveParameters(cmd));
                    Assert.NotNull(exception);
                    Assert.IsType<InvalidOperationException>(exception);
                    Assert.Contains(
                        $"{nameof(OleDbCommandBuilder.DeriveParameters)} requires an open and available Connection.",
                        exception.Message);
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void QuoteIdentifier_Null_Throws()
        {
            RunTest((command, tableName) => {
                using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = @"SELECT * FROM " + tableName;  
                    cmd.Connection = (OleDbConnection)OleDbFactory.Instance.CreateConnection();
                    cmd.Transaction = transaction;
                    using (var builder = (OleDbCommandBuilder)OleDbFactory.Instance.CreateCommandBuilder())
                    {
                        AssertExtensions.Throws<ArgumentNullException>(
                            () => builder.QuoteIdentifier(null, cmd.Connection), 
                            $"Value cannot be null.\r\nParameter name: unquotedIdentifier");
                    }
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void UnquoteIdentifier_Null_Throws()
        {
            RunTest((command, tableName) => {
                using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = @"SELECT * FROM " + tableName;  
                    cmd.Connection = (OleDbConnection)OleDbFactory.Instance.CreateConnection();
                    cmd.Transaction = transaction;
                    using (var builder = (OleDbCommandBuilder)OleDbFactory.Instance.CreateCommandBuilder())
                    {
                        AssertExtensions.Throws<ArgumentNullException>(
                            () => builder.UnquoteIdentifier(null, cmd.Connection), 
                            $"Value cannot be null.\r\nParameter name: quotedIdentifier");
                    }
                }
            });
        }

        [OuterLoop]
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void QuoteUnquote_CustomPrefixSuffix_Success()
        {
            RunTest((command, tableName) => {
                using (var cmd = (OleDbCommand)OleDbFactory.Instance.CreateCommand())
                {
                    cmd.Transaction = transaction;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = @"SELECT * FROM " + tableName;  
                    cmd.Connection = (OleDbConnection)OleDbFactory.Instance.CreateConnection();
                    cmd.Connection.ConnectionString = connection.ConnectionString;
                    
                    using (var adapter = new OleDbDataAdapter(cmd.CommandText, connection))
                    using (var builder = new OleDbCommandBuilder(adapter))
                    {
                        // Custom prefix & suffix
                        builder.QuotePrefix = "'";
                        builder.QuoteSuffix = "'";

                        Assert.Equal(adapter, builder.DataAdapter);
                        Assert.Equal("'Test'", builder.QuoteIdentifier("Test", connection));
                        Assert.Equal("'Te''st'", builder.QuoteIdentifier("Te'st", connection));
                        Assert.Equal("Test", builder.UnquoteIdentifier("'Test'", connection));
                        Assert.Equal("Te'st", builder.UnquoteIdentifier("'Te''st'", connection));
                        
                        // Ensure we don't need active connection:
                        Assert.Equal("'Test'", builder.QuoteIdentifier("Test", null));
                        Assert.Equal("Test", builder.UnquoteIdentifier("'Test'", null));

                        builder.QuotePrefix = string.Empty;
                        string quoteErrMsg = $"{nameof(builder.QuoteIdentifier)} requires open connection when the quote prefix has not been set.";
                        string unquoteErrMsg = $"{nameof(builder.UnquoteIdentifier)} requires open connection when the quote prefix has not been set.";

                        Assert.Equal("`Test`", builder.QuoteIdentifier("Test", connection));
                        Assert.Equal("Test", builder.UnquoteIdentifier("`Test`", connection));

                        Assert.NotNull(adapter.SelectCommand.Connection);
                        Assert.Equal("`Test`", builder.QuoteIdentifier("Test"));
                        Assert.Equal("Test", builder.UnquoteIdentifier("`Test`"));

                        adapter.SelectCommand.Connection = null;
                        AssertExtensions.Throws<InvalidOperationException>(() => builder.QuoteIdentifier("Test"), quoteErrMsg);
                        AssertExtensions.Throws<InvalidOperationException>(() => builder.UnquoteIdentifier("'Test'"), unquoteErrMsg);
                    }
                }
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
