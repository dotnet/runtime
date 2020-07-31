// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class JsonConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private IDisposable _optionsReloadToken;
        private char[] _singleCharArray = new char[1];

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
                    var timestampFormat = FormatterOptions.TimestampFormat;
                    if (timestampFormat != null)
                    {
                        DateTimeOffset dateTimeOffset = FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                        writer.WriteString("Timestamp", dateTimeOffset.ToString(timestampFormat));
                    }
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

                    if (logEntry.State != null)
                    {
                        writer.WriteStartObject(nameof(logEntry.State));
                        writer.WriteString("Message", logEntry.State.ToString());
                        if (logEntry.State is IReadOnlyCollection<KeyValuePair<string, object>> stateProperties)
                        {
                            foreach (KeyValuePair<string, object> item in stateProperties)
                            {
                                WriteItem(writer, item);
                            }
                        }
                        writer.WriteEndObject();
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
                writer.WriteStartArray("Scopes");
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (scope is IReadOnlyCollection<KeyValuePair<string, object>> scopes)
                    {
                        state.WriteStartObject();
                        state.WriteString("Message", scope.ToString());
                        foreach (KeyValuePair<string, object> item in scopes)
                        {
                            WriteItem(state, item);
                        }
                        state.WriteEndObject();
                    }
                    else
                    {
                        state.WriteStringValue(ToInvariantString(scope));
                    }
                }, writer);
                writer.WriteEndArray();
            }
        }

        private void WriteItem(Utf8JsonWriter writer, KeyValuePair<string, object> item)
        {
            if (item.Value is bool boolValue)
            {
                writer.WriteBoolean(item.Key, boolValue);
            }
            else if (item.Value is byte byteValue)
            {
                writer.WriteNumber(item.Key, byteValue);
            }
            else if (item.Value is sbyte sbyteValue)
            {
                writer.WriteNumber(item.Key, sbyteValue);
            }
            else if (item.Value is char charValue)
            {
                _singleCharArray[0] = charValue;
                writer.WriteString(item.Key, _singleCharArray.AsSpan());
            }
            else if (item.Value is decimal decimalValue)
            {
                writer.WriteNumber(item.Key, decimalValue);
            }
            else if (item.Value is double doubleValue)
            {
                writer.WriteNumber(item.Key, doubleValue);
            }
            else if (item.Value is float floatValue)
            {
                writer.WriteNumber(item.Key, floatValue);
            }
            else if (item.Value is int intValue)
            {
                writer.WriteNumber(item.Key, intValue);
            }
            else if (item.Value is uint uintValue)
            {
                writer.WriteNumber(item.Key, uintValue);
            }
            else if (item.Value is long longValue)
            {
                writer.WriteNumber(item.Key, longValue);
            }
            else if (item.Value is ulong ulongValue)
            {
                writer.WriteNumber(item.Key, ulongValue);
            }
            else if (item.Value is short shortValue)
            {
                writer.WriteNumber(item.Key, shortValue);
            }
            else if (item.Value is ushort ushortValue)
            {
                writer.WriteNumber(item.Key, ushortValue);
            }
            else
            {
                writer.WriteString(item.Key, ToInvariantString(item.Value));
            }
        }

        private static string ToInvariantString(object obj) => Convert.ToString(obj, CultureInfo.InvariantCulture);

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
