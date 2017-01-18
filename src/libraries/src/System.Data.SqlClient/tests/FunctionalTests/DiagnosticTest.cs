// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Xml;
using System.Threading.Tasks;
using Microsoft.SqlServer.TDS.Servers;
using Microsoft.SqlServer.TDS;
using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.SQLBatch;
using Microsoft.SqlServer.TDS.Error;

namespace System.Data.SqlClient.Tests
{
    public class DiagnosticTest : IDisposable
    {
        private string _connectionString;
        private string _badConnectionString = "data source = bad; initial catalog = bad; uid = bad; password = bad; connection timeout = 1;";
        
        private TestTdsServer _server;
        private static readonly string s_tcpConnStr = Environment.GetEnvironmentVariable("TEST_TCP_CONN_STR");

        public static bool IsConnectionStringConfigured() => !string.IsNullOrEmpty(s_tcpConnStr);

        public DiagnosticTest()
        {
            QueryEngine engine = new DiagnosticsQueryEngine();
            _server = TestTdsServer.StartServerWithQueryEngine(engine);
            _connectionString = _server.ConnectionString;
        }

        [Fact]
        public void ExecuteScalarTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    var output = cmd.ExecuteScalar();
                }
            });
        }

        [Fact]
        public void ExecuteScalarErrorTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select 1 / 0;";

                    conn.Open();

                    try { var output = cmd.ExecuteScalar(); }
                    catch { }
                }
            });
        }

        [Fact]
        public void ExecuteNonQueryTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    var output = cmd.ExecuteNonQuery();
                }
            });
        }

        [Fact]
        public void ExecuteNonQueryErrorTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select 1 / 0;";

                    conn.Open();
                    try { var output = cmd.ExecuteNonQuery(); }
                    catch { }
                }
            });
        }

        [Fact]
        public void ExecuteReaderTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read()) { }
                }
            });
        }

        [Fact]
        public void ExecuteReaderErrorTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select 1 / 0;";

                    try
                    {
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read()) { }
                    }
                    catch { }
                }
            });
        }

        [Fact]
        public void ExecuteReaderWithCommandBehaviorTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.Default);
                    while (reader.Read()) { }
                }
            });
        }

        [ConditionalFact(nameof(IsConnectionStringConfigured))]
        public void ExecuteXmlReaderTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(s_tcpConnStr))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select * from sys.objects for xml auto, xmldata;";

                    conn.Open();
                    XmlReader reader = cmd.ExecuteXmlReader();
                    while (reader.Read()) { }
                }
            });
        }

        [Fact]
        public void ExecuteXmlReaderErrorTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select *, baddata = 1 / 0 from sys.objects for xml auto, xmldata;";

                    try
                    {
                        XmlReader reader = cmd.ExecuteXmlReader();
                        while (reader.Read()) { }
                    }
                    catch { }
                }
            });
        }

        [Fact]
        public void ExecuteScalarAsyncTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    var output = await cmd.ExecuteScalarAsync();
                }
            });
        }

        [Fact]
        public void ExecuteScalarAsyncErrorTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select 1 / 0;";

                    conn.Open();

                    try { var output = await cmd.ExecuteScalarAsync(); }
                    catch { }
                }
            });
        }

        [Fact]
        public void ExecuteNonQueryAsyncTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    var output = await cmd.ExecuteNonQueryAsync();
                }
            });
        }

        [Fact]
        public void ExecuteNonQueryAsyncErrorTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select 1 / 0;";

                    conn.Open();
                    try { var output = await cmd.ExecuteNonQueryAsync(); }
                    catch { }
                }
            });
        }

        [Fact]
        public void ExecuteReaderAsyncTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                    conn.Open();
                    SqlDataReader reader = await cmd.ExecuteReaderAsync();
                    while (reader.Read()) { }
                }
            });
        }

        [Fact]
        public void ExecuteReaderAsyncErrorTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select 1 / 0;";

                    try
                    {
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();
                        while (reader.Read()) { }
                    }
                    catch { }
                }
            });
        }

        [ConditionalFact(nameof(IsConnectionStringConfigured))]
        public void ExecuteXmlReaderAsyncTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(s_tcpConnStr))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select * from sys.objects for xml auto, xmldata;";

                    conn.Open();
                    XmlReader reader = await cmd.ExecuteXmlReaderAsync();
                    while (reader.Read()) { }
                }
            });
        }

        [Fact]
        public void ExecuteXmlReaderAsyncErrorTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "select *, baddata = 1 / 0 from sys.objects for xml auto, xmldata;";

                    try
                    {
                        XmlReader reader = await cmd.ExecuteXmlReaderAsync();
                        while (reader.Read()) { }
                    }
                    catch { }
                }
            });
        }

        [Fact]
        public void ConnectionOpenTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
                {
                    sqlConnection.Open();
                }
            });
        }

        [Fact]
        public void ConnectionOpenErrorTest()
        {
            CollectStatisticsDiagnostics(() =>
            {
                using (SqlConnection sqlConnection = new SqlConnection(_badConnectionString))
                {
                    try { sqlConnection.Open(); } catch { }
                }
            });
        }

        [Fact]
        public void ConnectionOpenAsyncTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
                {
                    await sqlConnection.OpenAsync();
                }
            });
        }

        [Fact]
        public void ConnectionOpenAsyncErrorTest()
        {
            CollectStatisticsDiagnosticsAsync(async () =>
            {
                using (SqlConnection sqlConnection = new SqlConnection(_badConnectionString))
                {
                    try { await sqlConnection.OpenAsync(); } catch { }
                }
            });
        }
        
        private void CollectStatisticsDiagnostics(Action sqlOperation)
        {
            bool statsLogged = false;
            bool operationHasError = false;
            Guid beginOperationId = Guid.Empty;
            
            FakeDiagnosticListenerObserver diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
                {
                    IDictionary statistics;

                    if (kvp.Key.Equals("System.Data.SqlClient.WriteCommandBefore"))
                    {
                        Assert.NotNull(kvp.Value);

                        Guid retrievedOperationId = GetPropertyValueFromType<Guid>(kvp.Value, "OperationId");
                        Assert.NotEqual(retrievedOperationId, Guid.Empty);

                        SqlCommand sqlCommand = GetPropertyValueFromType<SqlCommand>(kvp.Value, "Command");
                        Assert.NotNull(sqlCommand);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        if (sqlCommand.Connection.State == ConnectionState.Open)
                        { 
                            Assert.NotEqual(connectionId, Guid.Empty);
                        }

                        beginOperationId = retrievedOperationId;
                                                                        
                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteCommandAfter"))
                    {
                        Assert.NotNull(kvp.Value);

                        Guid retrievedOperationId = GetPropertyValueFromType<Guid>(kvp.Value, "OperationId");
                        Assert.NotEqual(retrievedOperationId, Guid.Empty);

                        SqlCommand sqlCommand = GetPropertyValueFromType<SqlCommand>(kvp.Value, "Command");
                        Assert.NotNull(sqlCommand);

                        statistics = GetPropertyValueFromType<IDictionary>(kvp.Value, "Statistics");
                        if (!operationHasError)
                            Assert.NotNull(statistics);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        if (sqlCommand.Connection.State == ConnectionState.Open)
                        {
                            Assert.NotEqual(connectionId, Guid.Empty);
                        }

                        // if we get to this point, then statistics exist and this must be the "end" 
                        // event, so we need to make sure the operation IDs match
                        Assert.Equal(retrievedOperationId, beginOperationId);
                        beginOperationId = Guid.Empty;

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteCommandError"))
                    {
                        operationHasError = true;
                        Assert.NotNull(kvp.Value);

                        SqlCommand sqlCommand = GetPropertyValueFromType<SqlCommand>(kvp.Value, "Command");
                        Assert.NotNull(sqlCommand);

                        Exception ex = GetPropertyValueFromType<Exception>(kvp.Value, "Exception");
                        Assert.NotNull(ex);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        if (sqlCommand.Connection.State == ConnectionState.Open)
                        {
                            Assert.NotEqual(connectionId, Guid.Empty);
                        }

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionOpenBefore"))
                    {
                        Assert.NotNull(kvp.Value);

                        SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                        Assert.NotNull(sqlConnection);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionOpenAfter"))
                    {
                        Assert.NotNull(kvp.Value);

                        SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                        Assert.NotNull(sqlConnection);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        statistics = GetPropertyValueFromType<IDictionary>(kvp.Value, "Statistics");
                        Assert.NotNull(statistics);

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        Assert.NotEqual(connectionId, Guid.Empty);

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionOpenError"))
                    {
                        Assert.NotNull(kvp.Value);

                        SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                        Assert.NotNull(sqlConnection);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        Exception ex = GetPropertyValueFromType<Exception>(kvp.Value, "Exception");
                        Assert.NotNull(ex);

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionCloseBefore"))
                    {
                        Assert.NotNull(kvp.Value);

                        SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                        Assert.NotNull(sqlConnection);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        Assert.NotEqual(connectionId, Guid.Empty);

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionCloseAfter"))
                    {
                        Assert.NotNull(kvp.Value);

                        SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                        Assert.NotNull(sqlConnection);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        statistics = GetPropertyValueFromType<IDictionary>(kvp.Value, "Statistics");

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        Assert.NotEqual(connectionId, Guid.Empty);

                        statsLogged = true;
                    }
                    else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionCloseError"))
                    {
                        Assert.NotNull(kvp.Value);

                        SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                        Assert.NotNull(sqlConnection);

                        string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                        Assert.False(string.IsNullOrWhiteSpace(operation));

                        Exception ex = GetPropertyValueFromType<Exception>(kvp.Value, "Exception");
                        Assert.NotNull(ex);

                        Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                        Assert.NotEqual(connectionId, Guid.Empty);

                        statsLogged = true;
                    }
                });

            diagnosticListenerObserver.Enable();
            using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
            { 
                sqlOperation();

                Assert.True(statsLogged);

                diagnosticListenerObserver.Disable();
            }
        }
        private async void CollectStatisticsDiagnosticsAsync(Func<Task> sqlOperation)
        {
            bool statsLogged = false;
            bool operationHasError = false;
            Guid beginOperationId = Guid.Empty;

            FakeDiagnosticListenerObserver diagnosticListenerObserver = new FakeDiagnosticListenerObserver(kvp =>
            {
                IDictionary statistics;

                if (kvp.Key.Equals("System.Data.SqlClient.WriteCommandBefore"))
                {
                    Assert.NotNull(kvp.Value);

                    Guid retrievedOperationId = GetPropertyValueFromType<Guid>(kvp.Value, "OperationId");
                    Assert.NotEqual(retrievedOperationId, Guid.Empty);

                    SqlCommand sqlCommand = GetPropertyValueFromType<SqlCommand>(kvp.Value, "Command");
                    Assert.NotNull(sqlCommand);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    beginOperationId = retrievedOperationId;

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteCommandAfter"))
                {
                    Assert.NotNull(kvp.Value);

                    Guid retrievedOperationId = GetPropertyValueFromType<Guid>(kvp.Value, "OperationId");
                    Assert.NotEqual(retrievedOperationId, Guid.Empty);

                    SqlCommand sqlCommand = GetPropertyValueFromType<SqlCommand>(kvp.Value, "Command");
                    Assert.NotNull(sqlCommand);

                    statistics = GetPropertyValueFromType<IDictionary>(kvp.Value, "Statistics");
                    if (!operationHasError)
                        Assert.NotNull(statistics);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    // if we get to this point, then statistics exist and this must be the "end" 
                    // event, so we need to make sure the operation IDs match
                    Assert.Equal(retrievedOperationId, beginOperationId);
                    beginOperationId = Guid.Empty;

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteCommandError"))
                {
                    operationHasError = true;
                    Assert.NotNull(kvp.Value);

                    SqlCommand sqlCommand = GetPropertyValueFromType<SqlCommand>(kvp.Value, "Command");
                    Assert.NotNull(sqlCommand);

                    Exception ex = GetPropertyValueFromType<Exception>(kvp.Value, "Exception");
                    Assert.NotNull(ex);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionOpenBefore"))
                {
                    Assert.NotNull(kvp.Value);

                    SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                    Assert.NotNull(sqlConnection);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionOpenAfter"))
                {
                    Assert.NotNull(kvp.Value);

                    SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                    Assert.NotNull(sqlConnection);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    statistics = GetPropertyValueFromType<IDictionary>(kvp.Value, "Statistics");
                    Assert.NotNull(statistics);

                    Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                    if (sqlConnection.State == ConnectionState.Open)
                    { 
                        Assert.NotEqual(connectionId, Guid.Empty);
                    }

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionOpenError"))
                {
                    Assert.NotNull(kvp.Value);

                    SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                    Assert.NotNull(sqlConnection);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    Exception ex = GetPropertyValueFromType<Exception>(kvp.Value, "Exception");
                    Assert.NotNull(ex);

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionCloseBefore"))
                {
                    Assert.NotNull(kvp.Value);

                    SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                    Assert.NotNull(sqlConnection);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                    Assert.NotEqual(connectionId, Guid.Empty);

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionCloseAfter"))
                {
                    Assert.NotNull(kvp.Value);

                    SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                    Assert.NotNull(sqlConnection);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    statistics = GetPropertyValueFromType<IDictionary>(kvp.Value, "Statistics");

                    Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                    Assert.NotEqual(connectionId, Guid.Empty);

                    statsLogged = true;
                }
                else if (kvp.Key.Equals("System.Data.SqlClient.WriteConnectionCloseError"))
                {
                    Assert.NotNull(kvp.Value);

                    SqlConnection sqlConnection = GetPropertyValueFromType<SqlConnection>(kvp.Value, "Connection");
                    Assert.NotNull(sqlConnection);

                    string operation = GetPropertyValueFromType<string>(kvp.Value, "Operation");
                    Assert.False(string.IsNullOrWhiteSpace(operation));

                    Exception ex = GetPropertyValueFromType<Exception>(kvp.Value, "Exception");
                    Assert.NotNull(ex);

                    Guid connectionId = GetPropertyValueFromType<Guid>(kvp.Value, "ConnectionId");
                    Assert.NotEqual(connectionId, Guid.Empty);

                    statsLogged = true;
                }
            });

            diagnosticListenerObserver.Enable();
            using (DiagnosticListener.AllListeners.Subscribe(diagnosticListenerObserver))
            { 
                await sqlOperation();

                Assert.True(statsLogged);

                diagnosticListenerObserver.Disable();
            }
        }
        
        private T GetPropertyValueFromType<T>(object obj, string propName)
        {
            Type type = obj.GetType();
            PropertyInfo pi = type.GetRuntimeProperty(propName);

            var propertyValue = pi.GetValue(obj);
            return (T)propertyValue;
        }

        public void Dispose() => _server?.Dispose();

    }

    public class DiagnosticsQueryEngine : QueryEngine
    {
        public DiagnosticsQueryEngine() : base(new TDSServerArguments())
        {
        }

        protected override TDSMessageCollection CreateQueryResponse(ITDSServerSession session, TDSSQLBatchToken batchRequest)
        {
            string lowerBatchText = batchRequest.Text.ToLowerInvariant();
            
            if (lowerBatchText.Contains("1 / 0")) // SELECT 1/0 
            {
                TDSErrorToken errorToken = new TDSErrorToken(8134, 1, 1, "Divide by zero error encountered.");
                TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken);
                return new TDSMessageCollection(responseMessage);
            }
            else
            {
                return base.CreateQueryResponse(session, batchRequest);
            }
        }
    }
}