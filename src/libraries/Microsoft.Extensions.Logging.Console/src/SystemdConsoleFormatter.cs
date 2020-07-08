// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class SystemdConsoleFormatter : ConsoleFormatter, IDisposable
    {
        [ThreadStatic]
        private static StringBuilder _logBuilder;
        private IDisposable _optionsReloadToken;

        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new string(' ', GetSyslogSeverityString(LogLevel.Information).Length + LoglevelPadding.Length);

        public SystemdConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
            : base(ConsoleFormatterNames.Systemd)
        {
            FormatterOptions = options.CurrentValue;
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        private void ReloadLoggerOptions(ConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal ConsoleFormatterOptions FormatterOptions { get; set; }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            LogLevel logLevel = logEntry.LogLevel;
            string category = logEntry.Category;
            int eventId = logEntry.EventId.Id;
            Exception exception = logEntry.Exception;
            string message = logEntry.Formatter(logEntry.State, exception);

            StringBuilder logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }
            CreateSystemdLogMessage(logBuilder, logLevel, category, eventId, message, exception, scopeProvider);
            textWriter.Write(logBuilder.ToString());

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

        private void CreateSystemdLogMessage(StringBuilder logBuilder, LogLevel logLevel, string logName, int eventId, string message, Exception exception, IExternalScopeProvider scopeProvider)
        {
            // systemd reads messages from standard out line-by-line in a '<pri>message' format.
            // newline characters are treated as message delimiters, so we must replace them.
            // Messages longer than the journal LineMax setting (default: 48KB) are cropped.
            // Example:
            // <6>ConsoleApp.Program[10] Request received

            // loglevel
            string logLevelString = GetSyslogSeverityString(logLevel);
            logBuilder.Append(logLevelString);

            // timestamp
            string timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTime dateTime = GetCurrentDateTime();
                logBuilder.Append(dateTime.ToString(timestampFormat));
            }

            // category and event id
            logBuilder.Append(logName);
            logBuilder.Append('[');
            logBuilder.Append(eventId);
            logBuilder.Append(']');

            // scope information
            GetScopeInformation(logBuilder, scopeProvider);

            // message
            if (!string.IsNullOrEmpty(message))
            {
                logBuilder.Append(' ');
                // message
                AppendAndReplaceNewLine(logBuilder, message);
            }

            // exception
            // System.InvalidOperationException at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                logBuilder.Append(' ');
                AppendAndReplaceNewLine(logBuilder, exception.ToString());
            }

            // newline delimiter
            logBuilder.Append(Environment.NewLine);

            static void AppendAndReplaceNewLine(StringBuilder sb, string message)
            {
                int len = sb.Length;
                sb.Append(message);
                sb.Replace(Environment.NewLine, " ", len, message.Length);
            }
        }

        private DateTime GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
        }

        private static string GetSyslogSeverityString(LogLevel logLevel)
        {
            // 'Syslog Message Severities' from https://tools.ietf.org/html/rfc5424.
            return logLevel switch
            {
                LogLevel.Trace => "<7>",
                LogLevel.Debug => "<7>",        // debug-level messages
                LogLevel.Information => "<6>",  // informational messages
                LogLevel.Warning => "<4>",     // warning conditions
                LogLevel.Error => "<3>",       // error conditions
                LogLevel.Critical => "<2>",    // critical conditions
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private void GetScopeInformation(StringBuilder stringBuilder, IExternalScopeProvider scopeProvider)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                int initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    (StringBuilder builder, int paddAt) = state;
                    bool padd = paddAt == builder.Length;
                    if (padd)
                    {
                        builder.Append(_messagePadding);
                        builder.Append("=> ");
                    }
                    else
                    {
                        builder.Append(" => ");
                    }
                    builder.Append(scope);
                }, (stringBuilder, -1));
            }
        }
    }
}
