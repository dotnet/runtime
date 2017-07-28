// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class CloneTests
    {
        [Fact]
        public void CloneSqlConnection()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "mybadServer";
            builder.ConnectTimeout = 1;
            builder.InitialCatalog = "northwinddb";
            SqlConnection connection = new SqlConnection(builder.ConnectionString);

            SqlConnection clonedConnection = (connection as ICloneable).Clone() as SqlConnection;
            Assert.Equal(connection.ConnectionString, clonedConnection.ConnectionString);
            Assert.Equal(connection.ConnectionTimeout, clonedConnection.ConnectionTimeout);
            Assert.NotEqual(connection, clonedConnection);
        }

        [Fact]
        public void CloneSqlCommand()
        {
            SqlConnection connection = new SqlConnection();

            SqlCommand command = connection.CreateCommand();
            command.CommandText = "select 1";
            command.CommandTimeout = 45;
            command.ResetCommandTimeout();
            Assert.Equal(command.CommandTimeout, 30);
            command.CommandType = CommandType.Text;

            SqlParameter parameter = command.CreateParameter();
            parameter.Direction = ParameterDirection.Input;
            parameter.DbType = DbType.Currency;
            parameter.Size = 10;
            parameter.Precision = 3;
            parameter.Scale = 5;

            command.Parameters.Add(parameter);

            SqlCommand clonedCommand = command.Clone();
            compareCommands(command, clonedCommand);

            SqlCommand clonedByICloneable = (command as ICloneable).Clone() as SqlCommand;
            compareCommands(command, clonedByICloneable);
        }

        [Fact]
        public void CloneParameters()
        {
            SqlParameter parameter = new SqlParameter();
            parameter.Direction = ParameterDirection.Input;
            parameter.DbType = DbType.Currency;
            parameter.Size = 10;
            parameter.Precision = 3;
            parameter.Scale = 5;

            SqlParameter clonedParameter = (parameter as ICloneable).Clone() as SqlParameter;

            Assert.Equal(parameter.Direction, clonedParameter.Direction);
            Assert.Equal(parameter.DbType, clonedParameter.DbType);
            Assert.Equal(parameter.Size, clonedParameter.Size);
            Assert.Equal(parameter.Precision, clonedParameter.Precision);
            Assert.Equal(parameter.Scale, clonedParameter.Scale);
            Assert.NotEqual(parameter, clonedParameter);
        }

        private void compareCommands(SqlCommand original, SqlCommand cloned)
        {
            Assert.Equal(original.CommandText, cloned.CommandText);
            Assert.Equal(original.CommandTimeout, cloned.CommandTimeout);
            Assert.Equal(original.CommandType, cloned.CommandType);
            Assert.Equal(original.Connection, cloned.Connection);
            Assert.Equal(original.Parameters.Count, cloned.Parameters.Count);
            Assert.NotEqual(original, cloned);
        }
    }
}
