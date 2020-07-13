// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class SystemdConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private IDisposable _optionsReloadToken;

        public SystemdConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
            : base(ConsoleFormatterNames.Systemd)
        {
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
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            string category = logEntry.Category;
            int eventId = logEntry.EventId.Id;
            Exception exception = logEntry.Exception;
            // systemd reads messages from standard out line-by-line in a '<pri>message' format.
            // newline characters are treated as message delimiters, so we must replace them.
            // Messages longer than the journal LineMax setting (default: 48KB) are cropped.
            // Example:
            // <6>ConsoleApp.Program[10] Request received

            // loglevel
            string logLevelString = GetSyslogSeverityString(logLevel);
            textWriter.Write(logLevelString);

            // timestamp
            string timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTime dateTime = GetCurrentDateTime();
                textWriter.Write(dateTime.ToString(timestampFormat));
            }

            // category and event id
            textWriter.Write(category);
            textWriter.Write('[');
            textWriter.Write(eventId);
            textWriter.Write(']');

            // scope information
            WriteScopeInformation(textWriter, scopeProvider);

            // message
            if (!string.IsNullOrEmpty(message))
            {
                textWriter.Write(' ');
                // message
                WriteReplacingNewLine(textWriter, message);
            }

            // exception
            // System.InvalidOperationException at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                textWriter.Write(' ');
                WriteReplacingNewLine(textWriter, exception.ToString());
            }

            // newline delimiter
            textWriter.Write(Environment.NewLine);

            static void WriteReplacingNewLine(TextWriter writer, string message)
            {
                string newMessage = message.Replace(Environment.NewLine, " ");
                writer.Write(newMessage);
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

        private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider scopeProvider)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                scopeProvider.ForEachScope((scope, state) =>
                {
                    state.Write(" => ");
                    state.Write(scope);
                }, textWriter);
            }
        }
    }
}
