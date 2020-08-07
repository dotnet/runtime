// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal class SimpleConsoleFormatter : ConsoleFormatter, IDisposable
    {
        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        private IDisposable _optionsReloadToken;

        public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
            : base (ConsoleFormatterNames.Simple)
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
            string logLevelString = GetLogLevelString(logLevel);

            string timestamp = null;
            string timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTimeOffset dateTimeOffset = GetCurrentDateTime();
                timestamp = dateTimeOffset.ToString(timestampFormat);
            }
            if (timestamp != null)
            {
                textWriter.Write(timestamp);
            }
            if (logLevelString != null)
            {
                textWriter.WriteColoredMessage(logLevelString, logLevelColors.Background, logLevelColors.Foreground);
            }
            CreateDefaultLogMessage(textWriter, logEntry, message, scopeProvider);
        }

        private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, IExternalScopeProvider scopeProvider)
        {
            bool singleLine = FormatterOptions.SingleLine;
            int eventId = logEntry.EventId.Id;
            Exception exception = logEntry.Exception;

            // Example:
            // info: ConsoleApp.Program[10]
            //       Request received

            // category and event id
            textWriter.Write(LoglevelPadding + logEntry.Category + '[' + eventId + "]");
            if (!singleLine)
            {
                textWriter.Write(Environment.NewLine);
            }

            // scope information
            WriteScopeInformation(textWriter, scopeProvider, singleLine);
            WriteMessage(textWriter, message, singleLine);

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                WriteMessage(textWriter, exception.ToString(), singleLine);
            }
            if (singleLine)
            {
                textWriter.Write(Environment.NewLine);
            }
        }

        private void WriteMessage(TextWriter textWriter, string message, bool singleLine)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (singleLine)
                {
                    textWriter.Write(' ');
                    WriteReplacing(textWriter, Environment.NewLine, " ", message);
                }
                else
                {
                    textWriter.Write(_messagePadding);
                    WriteReplacing(textWriter, Environment.NewLine, _newLineWithMessagePadding, message);
                    textWriter.Write(Environment.NewLine);
                }
            }

            static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
            {
                string newMessage = message.Replace(oldValue, newValue);
                writer.Write(newMessage);
            }
        }

        private DateTimeOffset GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            bool disableColors = (FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled) ||
                (FormatterOptions.ColorBehavior == LoggerColorBehavior.Default && System.Console.IsOutputRedirected);
            if (disableColors)
            {
                return new ConsoleColors(null, null);
            }
            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            return logLevel switch
            {
                LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
                LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
                LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
                LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
                LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
                LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
                _ => new ConsoleColors(null, null)
            };
        }

        private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider scopeProvider, bool singleLine)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                bool paddingNeeded = !singleLine;
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (paddingNeeded)
                    {
                        paddingNeeded = false;
                        state.Write(_messagePadding + "=> ");
                    }
                    else
                    {
                        state.Write(" => ");
                    }
                    state.Write(scope);
                }, textWriter);

                if (!paddingNeeded && !singleLine)
                {
                    textWriter.Write(Environment.NewLine);
                }
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
