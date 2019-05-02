// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace System.Data.OleDb.Tests
{
    public class OleDbCommandTests : OleDbTestBase
    {
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