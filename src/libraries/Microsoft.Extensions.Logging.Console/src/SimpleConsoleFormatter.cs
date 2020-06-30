// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [ThreadStatic]
        private static StringBuilder _logBuilder;
        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        private IDisposable _optionsReloadToken;


        public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
        : base (ConsoleFormatterNames.Simple)
        {
            FormatterOptions = options.CurrentValue;
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
            LogLevel logLevel = logEntry.LogLevel;
            ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
            string logLevelString = GetLogLevelString(logLevel);

            string timestamp = null;
            string timestampFormat = FormatterOptions.TimestampFormat;
            if (timestampFormat != null)
            {
                DateTime dateTime = GetCurrentDateTime();
                timestamp = dateTime.ToString(timestampFormat);
            }
            if (timestamp != null)
            {
                textWriter.Write(FormatterOptions.SingleLine ? timestamp + ' ' : timestamp);
            }

            if (logLevelString != null)
            {
                textWriter.WriteColoredMessage(logLevelString, logLevelColors, FormatterOptions.DisableColors);
            }

            StringBuilder logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            CreateDefaultLogMessage(logBuilder, logEntry, scopeProvider);
            textWriter.Write(logBuilder.ToString());

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

        private void CreateDefaultLogMessage<TState>(StringBuilder logBuilder, in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider)
        {
            bool singleLine = FormatterOptions.SingleLine;
            string category = logEntry.Category;
            int eventId = logEntry.EventId.Id;
            Exception exception = logEntry.Exception;
            string message = logEntry.Formatter(logEntry.State, exception);

            // Example:
            // info: ConsoleApp.Program[10]
            //       Request received

            // category and event id
            logBuilder.Append(LoglevelPadding + category + '[' + eventId + "]");
            if (!singleLine)
            {
                logBuilder.Append(Environment.NewLine);
            }

            // scope information
            GetScopeInformation(logBuilder, scopeProvider, singleLine);
            if (singleLine)
            {
                logBuilder.Append(' ');
            }
            WriteMessage(logBuilder, message, singleLine);

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                WriteMessage(logBuilder, exception.ToString(), singleLine);
            }
            if (singleLine)
            {
                logBuilder.Append(Environment.NewLine);
            }
        }

        private void WriteMessage(StringBuilder stringBuilder, string message, bool singleLine)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (singleLine)
                {
                    WriteReplacing(Environment.NewLine, " ", message, stringBuilder);
                }
                else
                {
                    stringBuilder.Append(_messagePadding);
                    WriteReplacing(Environment.NewLine, _newLineWithMessagePadding, message, stringBuilder);
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            void WriteReplacing(string oldValue, string newValue, string message, StringBuilder builder)
            {
                int len = builder.Length;
                builder.Append(message);
                builder.Replace(oldValue, newValue, len, message.Length);
            }
        }

        private DateTime GetCurrentDateTime()
        {
            return FormatterOptions.UseUtcTimestamp ? DateTime.UtcNow : DateTime.Now;
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            if (!FormatterOptions.DisableColors)
            {
                // We must explicitly set the background color if we are setting the foreground color,
                // since just setting one can look bad on the users console.
                switch (logLevel)
                {
                    case LogLevel.Critical:
                        return new ConsoleColors(ConsoleColor.White, ConsoleColor.Red);
                    case LogLevel.Error:
                        return new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red);
                    case LogLevel.Warning:
                        return new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black);
                    case LogLevel.Information:
                        return new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black);
                    case LogLevel.Debug:
                        return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                    case LogLevel.Trace:
                        return new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black);
                }
            }

            return new ConsoleColors(null, null);
        }

        private void GetScopeInformation(StringBuilder stringBuilder, IExternalScopeProvider scopeProvider, bool singleLine)
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
                }, (stringBuilder, singleLine ? -1 : initialLength));

                if (stringBuilder.Length > initialLength && !singleLine)
                {
                    stringBuilder.AppendLine();
                }
            }
        }
    }

    internal readonly struct ConsoleColors
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
