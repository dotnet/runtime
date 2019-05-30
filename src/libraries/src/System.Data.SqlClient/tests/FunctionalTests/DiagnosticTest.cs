// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.SqlServer.TDS;
using Microsoft.SqlServer.TDS.Done;
using Microsoft.SqlServer.TDS.EndPoint;
using Microsoft.SqlServer.TDS.Error;
using Microsoft.SqlServer.TDS.Servers;
using Microsoft.SqlServer.TDS.SQLBatch;
using Xunit;

namespace System.Data.SqlClient.Tests
{
    public class DiagnosticTest
    {
        private const string BadConnectionString = "data source = bad; initial catalog = bad; uid = bad; password = bad; connection timeout = 1;";
        private static readonly string s_tcpConnStr = Environment.GetEnvironmentVariable("TEST_TCP_CONN_STR") ?? string.Empty;
        
        public static bool IsConnectionStringConfigured() => s_tcpConnStr != string.Empty;

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteScalarTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        var output = cmd.ExecuteScalar();
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteScalarErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "select 1 / 0;";

                        conn.Open();

                        try { var output = cmd.ExecuteScalar(); }
                        catch { }
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteNonQueryTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        var output = cmd.ExecuteNonQuery();
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteNonQueryErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand())
                        {
                            cmd.Connection = conn;
                            cmd.CommandText = "select 1 / 0;";
                            
                            // Limiting the command timeout to 3 seconds. This should be lower than the Process timeout.
                            cmd.CommandTimeout = 3;
                            conn.Open();
                            Console.WriteLine("SqlClient.DiagnosticTest.ExecuteNonQueryErrorTest Connection Open Successful");

                            try
                            {
                                var output = cmd.ExecuteNonQuery();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("SqlClient.DiagnosticTest.ExecuteNonQueryErrorTest " + e.Message);
                            }
                            Console.WriteLine("SqlClient.DiagnosticTest.ExecuteNonQueryErrorTest Command Executed");
                        }
                        Console.WriteLine("SqlClient.DiagnosticTest.ExecuteNonQueryErrorTest Command Disposed");
                    }
                    Console.WriteLine("SqlClient.DiagnosticTest.ExecuteNonQueryErrorTest Connection Disposed");
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteReaderTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read()) { }
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteReaderErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteReaderWithCommandBehaviorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.Default);
                        while (reader.Read()) { }
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [ConditionalFact(nameof(IsConnectionStringConfigured))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteXmlReaderTest()
        {
            RemoteExecutor.Invoke(cs =>
            {
                CollectStatisticsDiagnostics(_ =>
                {
                    using (SqlConnection conn = new SqlConnection(cs))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "select * from sys.objects for xml auto, xmldata;";

                        conn.Open();
                        XmlReader reader = cmd.ExecuteXmlReader();
                        while (reader.Read()) { }
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }, s_tcpConnStr).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteXmlReaderErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
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
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteScalarAsyncTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        var output = await cmd.ExecuteScalarAsync();
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteScalarAsyncErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "select 1 / 0;";

                        conn.Open();

                        try { var output = await cmd.ExecuteScalarAsync(); }
                        catch { }
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteNonQueryAsyncTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        var output = await cmd.ExecuteNonQueryAsync();
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteNonQueryAsyncErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "select 1 / 0;";

                        conn.Open();
                        try { var output = await cmd.ExecuteNonQueryAsync(); }
                        catch { }
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteReaderAsyncTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "SELECT [name], [state] FROM [sys].[databases] WHERE [name] = db_name();";

                        conn.Open();
                        SqlDataReader reader = await cmd.ExecuteReaderAsync();
                        while (reader.Read()) { }
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteReaderAsyncErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection conn = new SqlConnection(connectionString))
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
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [ConditionalFact(nameof(IsConnectionStringConfigured))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteXmlReaderAsyncTest()
        {
            RemoteExecutor.Invoke(cs =>
            {
                CollectStatisticsDiagnosticsAsync(async _ =>
                {
                    using (SqlConnection conn = new SqlConnection(cs))
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "select * from sys.objects for xml auto, xmldata;";

                        conn.Open();
                        XmlReader reader = await cmd.ExecuteXmlReaderAsync();
                        while (reader.Read()) { }
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }, s_tcpConnStr).Dispose();
        }

        [ConditionalFact(nameof(IsConnectionStringConfigured))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ExecuteXmlReaderAsyncErrorTest()
        {
            RemoteExecutor.Invoke(cs =>
            {
                CollectStatisticsDiagnosticsAsync(async _ =>
                {
                    using (SqlConnection conn = new SqlConnection(cs))
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
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }, s_tcpConnStr).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ConnectionOpenTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(connectionString =>
                {
                    using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                    {
                        sqlConnection.Open();
                        Console.WriteLine("SqlClient.DiagnosticsTest.ConnectionOpenTest:: Connection Opened ");
                    }
                    Console.WriteLine("SqlClient.DiagnosticsTest.ConnectionOpenTest:: Connection Should Be Disposed");
                }, true);

                Console.WriteLine("SqlClient.DiagnosticsTest.ConnectionOpenTest:: Done with Diagnostics collection");
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ConnectionOpenErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnostics(_ =>
                {
                    using (SqlConnection sqlConnection = new SqlConnection(BadConnectionString))
                    {
                        try { sqlConnection.Open(); } catch { }
                    }
                });
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ConnectionOpenAsyncTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async connectionString =>
                {
                    using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                    {
                        await sqlConnection.OpenAsync();
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsSubsystemForLinux))] // [ActiveIssue(11057)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework,  "Feature not available on Framework")]
        public void ConnectionOpenAsyncErrorTest()
        {
            RemoteExecutor.Invoke(() =>
            {
                CollectStatisticsDiagnosticsAsync(async _ =>
                {
                    using (SqlConnection sqlConnection = new SqlConnection(BadConnectionString))
                    {
                        try { await sqlConnection.OpenAsync(); } catch { }
                    }
                }).GetAwaiter().GetResult();
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        private static void CollectStatisticsDiagnostics(Action<string> sqlOperation, bool enableServerLogging = false, [CallerMemberName] string methodName = "")
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

                Console.WriteLine(string.Format("Test: {0} Enabled Listeners", methodName));
                using (var server = TestTdsServer.StartServerWithQueryEngine(new DiagnosticsQueryEngine(), enableLog:enableServerLogging, methodName: methodName))
                {
                    Console.WriteLine(string.Format("Test: {0} Started Server", methodName));
                    sqlOperation(server.ConnectionString);

                    Console.WriteLine(string.Format("Test: {0} SqlOperation Successful", methodName));
                    
                    Assert.True(statsLogged);

                    diagnosticListenerObserver.Disable();

                    Console.WriteLine(string.Format("Test: {0} Listeners Disabled", methodName));
                }
                Console.WriteLine(string.Format("Test: {0} Server Disposed", methodName));
            }
            Console.WriteLine(string.Format("Test: {0} Listeners Disposed Successfully", methodName));
        }

        private static async Task CollectStatisticsDiagnosticsAsync(Func<string, Task> sqlOperation, [CallerMemberName] string methodName = "")
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
                Console.WriteLine(string.Format("Test: {0} Enabled Listeners", methodName));
                using (var server = TestTdsServer.StartServerWithQueryEngine(new DiagnosticsQueryEngine(), methodName: methodName))
                {
                    Console.WriteLine(string.Format("Test: {0} Started Server", methodName));

                    await sqlOperation(server.ConnectionString);

                    Console.WriteLine(string.Format("Test: {0} SqlOperation Successful", methodName));

                    Assert.True(statsLogged);

                    diagnosticListenerObserver.Disable();

                    Console.WriteLine(string.Format("Test: {0} Listeners Disabled", methodName));
                }
                Console.WriteLine(string.Format("Test: {0} Server Disposed", methodName));
            }
            Console.WriteLine(string.Format("Test: {0} Listeners Disposed Successfully", methodName));
        }
        
        private static T GetPropertyValueFromType<T>(object obj, string propName)
        {
            Type type = obj.GetType();
            PropertyInfo pi = type.GetRuntimeProperty(propName);

            var propertyValue = pi.GetValue(obj);
            return (T)propertyValue;
        }
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
                TDSErrorToken errorToken = new TDSErrorToken(8134, 1, 16, "Divide by zero error encountered.");
                TDSDoneToken doneToken = new TDSDoneToken(TDSDoneTokenStatusType.Final | TDSDoneTokenStatusType.Count, TDSDoneTokenCommandType.Select, 1);
                TDSMessage responseMessage = new TDSMessage(TDSMessageType.Response, errorToken, doneToken);
                return new TDSMessageCollection(responseMessage);
            }
            else
            {
                return base.CreateQueryResponse(session, batchRequest);
            }
        }
    }
}
