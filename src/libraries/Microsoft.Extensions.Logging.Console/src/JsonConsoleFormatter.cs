// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class JsonConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private IDisposable _optionsReloadToken;

        public JsonConsoleFormatter(IOptionsMonitor<JsonConsoleFormatterOptions> options)
            : base (ConsoleFormatterNames.Json)
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

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
            const int DefaultBufferSize = 1024;
            using (var output = new PooledByteBufferWriter(DefaultBufferSize))
            {
                using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
                {
                    writer.WriteStartObject();
                    string timestamp = null;
                    var timestampFormat = FormatterOptions.TimestampFormat;
                    if (timestampFormat != null)
                    {
                        DateTimeOffset dateTimeOffset = FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                        timestamp = dateTimeOffset.ToString(timestampFormat);
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
#if NETCOREAPP
                            foreach (var stackTraceLines in stackTrace?.Split(Environment.NewLine))
#else
                            foreach (var stackTraceLines in stackTrace?.Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
#endif
                            {
                                writer.WriteStringValue(stackTraceLines);
                            }
                        }
                        writer.WriteEndArray();
                        writer.WriteNumber(nameof(exception.HResult), exception.HResult);
                        writer.WriteEndObject();
                    }

                    if (logEntry.State is IReadOnlyCollection<KeyValuePair<string, object>> stateDictionary)
                    {
                        foreach (KeyValuePair<string, object> item in stateDictionary)
                        {
                            writer.WriteString(item.Key, Convert.ToString(item.Value, CultureInfo.InvariantCulture));
                        }
                    }
                    WriteScopeInformation(writer, scopeProvider);
                    writer.WriteEndObject();
                    writer.Flush();
                }
#if NETCOREAPP
                textWriter.Write(Encoding.UTF8.GetString(output.WrittenMemory.Span));
#else
                textWriter.Write(Encoding.UTF8.GetString(output.WrittenMemory.Span.ToArray()));
#endif
            }
            textWriter.Write(Environment.NewLine);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "Trace",
                LogLevel.Debug => "Debug",
                LogLevel.Information => "Information",
                LogLevel.Warning => "Warning",
                LogLevel.Error => "Error",
                LogLevel.Critical => "Critical",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private void WriteScopeInformation(Utf8JsonWriter writer, IExternalScopeProvider scopeProvider)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                int numScopes = 0;
                writer.WriteStartObject("Scopes");
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (scope is IReadOnlyCollection<KeyValuePair<string, object>> scopeDictionary)
                    {
                        foreach (KeyValuePair<string, object> item in scopeDictionary)
                        {
                            state.WriteString(item.Key, Convert.ToString(item.Value, CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        state.WriteString(numScopes++.ToString(), scope.ToString());
                    }
                }, writer);
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
