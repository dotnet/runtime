// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal sealed class SystemdConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private readonly IDisposable? _optionsReloadToken;

        public SystemdConsoleFormatter(IOptionsMonitor<ConsoleFormatterOptions> options)
            : base(ConsoleFormatterNames.Systemd)
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        [MemberNotNull(nameof(FormatterOptions))]
        private void ReloadLoggerOptions(ConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal ConsoleFormatterOptions FormatterOptions { get; set; }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            if (logEntry.State is BufferedLogRecord bufferedRecord)
            {
                string message = bufferedRecord.FormattedMessage ?? string.Empty;
                if (bufferedRecord.Exception == null && message == null)
                {
                    return;
                }

                WriteInternal(scopeProvider, textWriter, message, bufferedRecord.LogLevel, logEntry.Category, bufferedRecord.EventId.Id, bufferedRecord.Exception, bufferedRecord.Timestamp);
            }
            else
            {
                string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
                if (logEntry.Exception == null && message == null)
                {
                    return;
                }

                // We extract most of the work into a non-generic method to save code size. If this was left in the generic
                // method, we'd get generic specialization for all TState parameters, but that's unnecessary.
                WriteInternal(scopeProvider, textWriter, message, logEntry.LogLevel, logEntry.Category, logEntry.EventId.Id, logEntry.Exception?.ToString(), GetCurrentDateTime());
            }
        }

        private void WriteInternal(IExternalScopeProvider? scopeProvider, TextWriter textWriter, string message, LogLevel logLevel, string category,
            int eventId, string? exception, DateTimeOffset stamp)
        {
            // systemd reads messages from standard out line-by-line in a '<pri>message' format.
            // newline characters are treated as message delimiters, so we must replace them.
            // Messages longer than the journal LineMax setting (default: 48KB) are cropped.
            // Example:
            // <6>ConsoleApp.Program[10] Request received

            // loglevel
            string logLevelString = GetSyslogSeverityString(logLevel);
            textWriter.Write(logLevelString);

            // timestamp
            string? timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                textWriter.Write(stamp.ToString(timestampFormat));
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
                WriteReplacingNewLine(textWriter, exception);
            }

            // newline delimiter
            textWriter.Write(Environment.NewLine);

            static void WriteReplacingNewLine(TextWriter writer, string message)
            {
                string newMessage = message.Replace(Environment.NewLine, " ");
                writer.Write(newMessage);
            }
        }

        private DateTimeOffset GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
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

        private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider)
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
