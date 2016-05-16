using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Data.SqlClient
{
    /// <summary>
    /// Extension methods on the DiagnosticListener class to log SqlCommand data
    /// </summary>
    internal static class SqlClientDiagnosticListenerExtensions
    {
        public const string DiagnosticListenerName = "SqlClientDiagnosticListener";

        private const string SqlClientPrefix = "System.Data.SqlClient.";
        public const string SqlBeforeExecuteCommand = SqlClientPrefix + nameof(WriteCommandBefore);
        public const string SqlAfterExecuteCommand = SqlClientPrefix + nameof(WriteCommandAfter);
        public const string SqlErrorExecuteCommand = SqlClientPrefix + nameof(WriteCommandError);

        public static Guid WriteCommandBefore(this DiagnosticListener @this, SqlCommand sqlCommand, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(SqlBeforeExecuteCommand))
            {
                Guid operationId = Guid.NewGuid();

                @this.Write(
                    SqlBeforeExecuteCommand,
                    new
                    {
                        OperationId = operationId,
                        Operation = operation,
                        Command = sqlCommand
                    });

                return operationId;
            }
            else
                return Guid.Empty;
        }

        public static void WriteCommandAfter(this DiagnosticListener @this, Guid operationId, SqlCommand sqlCommand, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(SqlAfterExecuteCommand))
            {
                @this.Write(
                    SqlAfterExecuteCommand,
                    new
                    {
                        OperationId = operationId,
                        Operation = operation,
                        Command = sqlCommand,
                        Statistics = sqlCommand.Statistics?.GetDictionary(),
                        Timestamp = Stopwatch.GetTimestamp()
                    });
            }
        }

        public static void WriteCommandError(this DiagnosticListener @this, Guid operationId, SqlCommand sqlCommand, Exception ex, [CallerMemberName] string operation = "")
        {
            if (@this.IsEnabled(SqlErrorExecuteCommand))
            {
                @this.Write(
                    SqlErrorExecuteCommand,
                    new
                    {
                        OperationId = operationId,
                        Operation = operation,
                        Command = sqlCommand,
                        Exception = ex,
                        Timestamp = Stopwatch.GetTimestamp()
                    });
            }
        }
    }
}