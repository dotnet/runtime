// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class JsonConsoleLogFormatter : ConsoleFormatter, IDisposable
    {
        private IDisposable _optionsReloadToken;

        public JsonConsoleLogFormatter(IOptionsMonitor<JsonConsoleFormatterOptions> options)
            : base (ConsoleFormatterNames.Json)
        {
            FormatterOptions = options.CurrentValue;
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            LogLevel logLevel = logEntry.LogLevel;
            string category = logEntry.Category;
            int eventId = logEntry.EventId.Id;
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            Exception exception = logEntry.Exception;
            const int DefaultBufferSize = 1024;
            var output = new ArrayBufferWriter<byte>(DefaultBufferSize);
            using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
            {
                writer.WriteStartObject();
                string timestamp = null;
                var timestampFormat = FormatterOptions.TimestampFormat;
                if (timestampFormat != null)
                {
                    var dateTime = FormatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
                    timestamp = dateTime.ToString(timestampFormat);
                }
                writer.WriteString("Timestamp", timestamp);
                writer.WriteNumber(nameof(logEntry.EventId), eventId);
                writer.WriteString(nameof(logEntry.LogLevel), GetLogLevelString(logLevel));
                writer.WriteString(nameof(logEntry.Category), category);
                writer.WriteString("Message", message);

                if (exception != null)
                {
                    writer.WriteStartObject(nameof(Exception));
                    writer.WriteString(nameof(exception.Message), exception.Message.ToString());
                    writer.WriteString("Type", exception.GetType().ToString());
                    writer.WriteStartArray(nameof(exception.StackTrace));
                    string stackTrace = exception?.StackTrace;
                    if (stackTrace != null)
                    {
                        foreach (var stackTraceLines in stackTrace?.Split(Environment.NewLine))
                        {
                            writer.WriteStringValue(stackTraceLines);
                        }
                    }
                    writer.WriteEndArray();
                    writer.WriteNumber(nameof(exception.HResult), exception.HResult);
                    writer.WriteEndObject();
                }

                GetScopeInformation(writer, scopeProvider);
                writer.WriteEndObject();
                writer.Flush();
            }
            textWriter.Write(Encoding.UTF8.GetString(output.WrittenMemory.Span));
            textWriter.Write(Environment.NewLine);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "Trace";
                case LogLevel.Debug:
                    return "Debug";
                case LogLevel.Information:
                    return "Information";
                case LogLevel.Warning:
                    return "Warning";
                case LogLevel.Error:
                    return "Error";
                case LogLevel.Critical:
                    return "Critical";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private void GetScopeInformation(Utf8JsonWriter writer, IExternalScopeProvider scopeProvider)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                int numScopes = 1;
                writer.WriteStartObject("Scopes");
                scopeProvider.ForEachScope((scope, state) =>
                {
                    state.WriteString("Scope_" + numScopes++, scope.ToString());
                }, (writer));
                writer.WriteEndObject();
            }
        }

        internal JsonConsoleFormatterOptions FormatterOptions { get; set; }

        private void ReloadLoggerOptions(JsonConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }
    }
}
