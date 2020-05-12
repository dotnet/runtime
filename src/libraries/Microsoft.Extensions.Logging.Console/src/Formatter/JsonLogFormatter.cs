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
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal sealed class PooledByteBufferWriter : IBufferWriter<byte>, IDisposable
    {
        private byte[] _rentedBuffer;
        private int _index;

        private const int MinimumBufferSize = 256;

        public PooledByteBufferWriter(int initialCapacity)
        {
            Debug.Assert(initialCapacity > 0);

            _rentedBuffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
            _index = 0;
        }

        public ReadOnlyMemory<byte> WrittenMemory
        {
            get
            {
                Debug.Assert(_rentedBuffer != null);
                Debug.Assert(_index <= _rentedBuffer.Length);
                return _rentedBuffer.AsMemory(0, _index);
            }
        }

        public int WrittenCount
        {
            get
            {
                Debug.Assert(_rentedBuffer != null);
                return _index;
            }
        }

        public int Capacity
        {
            get
            {
                Debug.Assert(_rentedBuffer != null);
                return _rentedBuffer.Length;
            }
        }

        public int FreeCapacity
        {
            get
            {
                Debug.Assert(_rentedBuffer != null);
                return _rentedBuffer.Length - _index;
            }
        }

        public void Clear()
        {
            ClearHelper();
        }

        private void ClearHelper()
        {
            Debug.Assert(_rentedBuffer != null);
            Debug.Assert(_index <= _rentedBuffer.Length);

            _rentedBuffer.AsSpan(0, _index).Clear();
            _index = 0;
        }

        // Returns the rented buffer back to the pool
        public void Dispose()
        {
            if (_rentedBuffer == null)
            {
                return;
            }

            ClearHelper();
            ArrayPool<byte>.Shared.Return(_rentedBuffer);
            _rentedBuffer = null!;
        }

        public void Advance(int count)
        {
            Debug.Assert(_rentedBuffer != null);
            Debug.Assert(count >= 0);
            Debug.Assert(_index <= _rentedBuffer.Length - count);

            _index += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _rentedBuffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _rentedBuffer.AsSpan(_index);
        }

#if BUILDING_INBOX_LIBRARY
        internal ValueTask WriteToStreamAsync(Stream destination, CancellationToken cancellationToken)
        {
            return destination.WriteAsync(WrittenMemory, cancellationToken);
        }
#else
        internal Task WriteToStreamAsync(Stream destination, CancellationToken cancellationToken)
        {
            return destination.WriteAsync(_rentedBuffer, 0, _index, cancellationToken);
        }
#endif

        private void CheckAndResizeBuffer(int sizeHint)
        {
            Debug.Assert(_rentedBuffer != null);
            Debug.Assert(sizeHint >= 0);

            if (sizeHint == 0)
            {
                sizeHint = MinimumBufferSize;
            }

            int availableSpace = _rentedBuffer.Length - _index;

            if (sizeHint > availableSpace)
            {
                int currentLength = _rentedBuffer.Length;
                int growBy = Math.Max(sizeHint, currentLength);

                int newSize = currentLength + growBy;

                if ((uint)newSize > int.MaxValue)
                {
                    newSize = currentLength + sizeHint;
                    if ((uint)newSize > int.MaxValue)
                    {
                        throw new OutOfMemoryException("(uint)newSize _BufferMaximumSizeExceeded");
                    }
                }

                byte[] oldBuffer = _rentedBuffer;

                _rentedBuffer = ArrayPool<byte>.Shared.Rent(newSize);

                Debug.Assert(oldBuffer.Length >= _index);
                Debug.Assert(_rentedBuffer.Length >= _index);

                Span<byte> previousBuffer = oldBuffer.AsSpan(0, _index);
                previousBuffer.CopyTo(_rentedBuffer);
                previousBuffer.Clear();
                ArrayPool<byte>.Shared.Return(oldBuffer);
            }

            Debug.Assert(_rentedBuffer.Length - _index > 0);
            Debug.Assert(_rentedBuffer.Length - _index >= sizeHint);
        }
    }

    public class JsonConsoleLogFormatter : ILogFormatter, IDisposable
    {
        private readonly IOptionsMonitor<JsonLogFormatterOptions> _options;
        private IDisposable _optionsReloadToken;

        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        [ThreadStatic]
        private static StringBuilder _logBuilder;
        [ThreadStatic]
        private static IDictionary<string, object> _xBuilder;

        static JsonConsoleLogFormatter()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        public JsonLogFormatterOptions FormatterOptions { get; set; }

        public JsonConsoleLogFormatter(IOptionsMonitor<JsonLogFormatterOptions> options)
        {
            _options = options;
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = _options.OnChange(ReloadLoggerOptions);
        }

        private void ReloadLoggerOptions(JsonLogFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public string Name => "Json";

        internal IDictionary<string, object> Scope { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        /// <inheritdoc />
        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        public LogMessageEntry Format(LogLevel logLevel, string logName, int eventId, string message, Exception exception, ConsoleLoggerOptions options, IExternalScopeProvider scopeProvider)
        {
            var logBuilder = _logBuilder;
            var xBuilder = _xBuilder;
            _logBuilder = null;
            _xBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
                xBuilder = new Dictionary<string, object>();
            }

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received

            var logLevelString = GetLogLevelString(logLevel);

            if (!string.IsNullOrEmpty(message))
            {
                const int DefaultBufferSize = 16384;
                
                using (var output = new PooledByteBufferWriter(DefaultBufferSize))
                {
                    using (var writer = new Utf8JsonWriter(output, options.JsonWriterOptions))
                    {
                        writer.WriteStartObject();
                        // TimeStamp
                        writer.WriteNumber("EventId", eventId);
                        writer.WriteString("LogLevel", logLevelString);
                        writer.WriteString("Category", logName);
                        writer.WriteString("Message", message);

                        GetScopeInformation(writer, options, scopeProvider);

                        writer.WriteEndObject();
                    }
                    logBuilder.AppendLine(Encoding.UTF8.GetString(output.WrittenMemory.Span));
                }
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                // logBuilder.AppendLine(exception.ToString());
                const int DefaultBufferSize = 16384;
                
                using (var output = new PooledByteBufferWriter(DefaultBufferSize))
                {
                    using (var writer = new Utf8JsonWriter(output, FormatterOptions.JsonWriterOptions))
                    {
                        writer.WriteStartObject();
                        writer.WriteString("Message", exception.Message.ToString());
                        writer.WriteString("Type", exception.GetType().ToString());
                        writer.WriteStartArray("StackTrace");
                        foreach (var xx in exception?.StackTrace?.Split(Environment.NewLine))
                        {
                            JsonSerializer.Serialize<string>(writer, xx);
                        }
                        writer.WriteEndArray();
                        writer.WriteNumber("HResult", exception.HResult);
                        writer.WriteEndObject();
                    }

                    logBuilder.AppendLine(Encoding.UTF8.GetString(output.WrittenMemory.Span));
                }
            }

            string timestamp = null;
            var timestampFormat = options.TimestampFormat;
            if (timestampFormat != null)
            {
                var dateTime = GetCurrentDateTime(options);
                timestamp = dateTime.ToString(timestampFormat);
            }

            var formattedMessage = logBuilder.ToString();
            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
            _xBuilder = xBuilder;

            return new LogMessageEntry(
                message: formattedMessage,
                timeStamp: timestamp,
                levelString: string.Empty,
                levelBackground: null,
                levelForeground: null,
                messageColor: null,
                logAsError: logLevel >= options.LogToStandardErrorThreshold,
                writeCallback : console =>
                {
                    if (timestamp != null)
                    {
                        console.Write(timestamp, null, null);
                    }
                    
                    console.Write(formattedMessage, null, null);
                    console.Flush();
                }
            );
        }

        private DateTime GetCurrentDateTime(ConsoleLoggerOptions options)
        {
            return options.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "json_trce";
                case LogLevel.Debug:
                    return "json_dbug";
                case LogLevel.Information:
                    return "json_info";
                case LogLevel.Warning:
                    return "json_warn";
                case LogLevel.Error:
                    return "json_fail";
                case LogLevel.Critical:
                    return "json_crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private void GetScopeInformation(Utf8JsonWriter writer, ConsoleLoggerOptions options, IExternalScopeProvider scopeProvider)
        {
            try
            {
                if (options.IncludeScopes && scopeProvider != null)
                {
                    writer.WriteStartArray("Scopes");
                    scopeProvider.ForEachScope((scope, state) =>
                    {
                        if (scope is IReadOnlyList<KeyValuePair<string, object>>)// kvps)
                        {
                            //foreach (var kvp in kvps)
                            //{
                            //    //state.WritePropertyName(kvp.Key);
                            //    JsonSerializer.Serialize(kvp.Value);
                            //}
                            //state is the writer
                            JsonSerializer.Serialize(state, scope);
                        }
                    }, (writer));
                    writer.WriteEndArray();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Something went wrong" + ex.Message);
            }
        }

        private readonly struct ConsoleColors
        {
            public ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
            {
                Foreground = foreground;
                Background = background;
            }

            public ConsoleColor? Foreground { get; }

            public ConsoleColor? Background { get; }
        }
    }
}
