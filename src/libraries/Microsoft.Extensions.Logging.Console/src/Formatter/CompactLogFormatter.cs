// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace Microsoft.Extensions.Logging.Console
{
    internal class CompactLogFormatter : ILogFormatter
    {
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        // ConsoleColor does not have a value to specify the 'Default' color
        private readonly ConsoleColor? DefaultConsoleColor = null;

        [ThreadStatic]
        private static StringBuilder _logBuilder;

        static CompactLogFormatter()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        public CompactLogFormatter() { }

        public string Name => "Compact";

        public LogMessageEntry Format(LogLevel logLevel, string logName, int eventId, string message, Exception exception, ConsoleLoggerOptions options, IExternalScopeProvider scopeProvider)
        {
            // todo fix later:
            var logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received

            var logLevelColors = GetLogLevelConsoleColors(logLevel, options);
            var logLevelString = GetLogLevelString(logLevel);
            // category and event id
            logBuilder.Append(_loglevelPadding);
            logBuilder.Append(logName);
            logBuilder.Append("[");
            logBuilder.Append(eventId);
            logBuilder.Append("]");
            // logBuilder.AppendLine("]");

            // scope information
            GetScopeInformation(logBuilder, options, scopeProvider);

            if (!string.IsNullOrEmpty(message))
            {
                // message
                logBuilder.Append(_messagePadding);

                var len = logBuilder.Length;
                logBuilder.AppendLine(message);
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                logBuilder.Append(exception.ToString());
                // logBuilder.AppendLine(exception.ToString());
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

            return new LogMessageEntry(
                message: formattedMessage,
                timeStamp: timestamp,
                levelString: logLevelString,
                levelBackground: logLevelColors.Background,
                levelForeground: logLevelColors.Foreground,
                messageColor: DefaultConsoleColor,
                logAsError: logLevel >= options.LogToStandardErrorThreshold,
                writeCallback : console =>
                {
                    if (timestamp != null)
                    {
                        console.Write(timestamp, DefaultConsoleColor, DefaultConsoleColor);
                    }

                    if (logLevelString != null)
                    {
                        console.Write(logLevelString, logLevelColors.Background, logLevelColors.Foreground);
                    }
                    
                    console.Write(formattedMessage, DefaultConsoleColor, DefaultConsoleColor);
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
                    return "compact_trce";
                case LogLevel.Debug:
                    return "compact_dbug";
                case LogLevel.Information:
                    return "compact_info";
                case LogLevel.Warning:
                    return "compact_warn";
                case LogLevel.Error:
                    return "compact_fail";
                case LogLevel.Critical:
                    return "compact_crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel, ConsoleLoggerOptions options)
        {
            if (options.DisableColors)
            {
                return new ConsoleColors(null, null);
            }

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
                default:
                    return new ConsoleColors(DefaultConsoleColor, DefaultConsoleColor);
            }
        }

        private void GetScopeInformation(StringBuilder stringBuilder, ConsoleLoggerOptions options, IExternalScopeProvider scopeProvider)
        {
            if (options.IncludeScopes && scopeProvider != null)
            {
                var initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    var (builder, paddAt) = state;
                    var padd = paddAt == builder.Length;
                    if (padd)
                    {
                        builder.Append(_messagePadding);
                    }
                    builder.Append("=> ");
                    builder.Append(scope);
                    builder.Append(" ");
                }, (stringBuilder, initialLength));

                if (stringBuilder.Length > initialLength)
                {
                    // stringBuilder.AppendLine();
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
