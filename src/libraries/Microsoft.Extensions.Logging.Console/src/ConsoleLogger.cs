// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using Microsoft.Extensions.Logging.Console.Internal;

namespace Microsoft.Extensions.Logging.Console
{
    [Obsolete("This type is obsolete and will be removed in a future version. The recommended alternative is ConsoleLoggerProvider.")]
    public class ConsoleLogger : ILogger
    {
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        // ConsoleColor does not have a value to specify the 'Default' color
        private readonly ConsoleColor? DefaultConsoleColor = null;

        private readonly ConsoleLoggerProcessor _queueProcessor;
        private Func<string, LogLevel, bool> _filter;

        [ThreadStatic]
        private static StringBuilder _logBuilder;

        static ConsoleLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes)
            : this(name, filter, includeScopes ? new LoggerExternalScopeProvider() : null, new ConsoleLoggerProcessor())
        {
        }

        public ConsoleLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider)
            : this(name, filter, scopeProvider, new ConsoleLoggerProcessor())
        {
        }

        internal ConsoleLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider, ConsoleLoggerProcessor loggerProcessor)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Filter = filter ?? ((category, logLevel) => true);
            ScopeProvider = scopeProvider;
            _queueProcessor = loggerProcessor;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console = new WindowsLogConsole();
            }
            else
            {
                Console = new AnsiLogConsole(new AnsiSystemConsole());
            }
        }

        public IConsole Console
        {
            get { return _queueProcessor.Console; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _queueProcessor.Console = value;
            }
        }

        public Func<string, LogLevel, bool> Filter
        {
            get { return _filter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _filter = value;
            }
        }

        public string Name { get; }

        [Obsolete("Changing this property has no effect. Use " + nameof(ConsoleLoggerOptions) + "." + nameof(ConsoleLoggerOptions.IncludeScopes) + " instead")]
        public bool IncludeScopes { get; set; }

        internal IExternalScopeProvider ScopeProvider { get; set; }

        public bool DisableColors { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            var message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, Name, eventId.Id, message, exception);
            }
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
        {
            var logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            var logLevelColors = default(ConsoleColors);
            var logLevelString = string.Empty;

            // Example:
            // INFO: ConsoleApp.Program[10]
            //       Request received

            logLevelColors = GetLogLevelConsoleColors(logLevel);
            logLevelString = GetLogLevelString(logLevel);
            // category and event id
            logBuilder.Append(_loglevelPadding);
            logBuilder.Append(logName);
            logBuilder.Append("[");
            logBuilder.Append(eventId);
            logBuilder.AppendLine("]");

            // scope information
            GetScopeInformation(logBuilder);

            if (!string.IsNullOrEmpty(message))
            {
                // message
                logBuilder.Append(_messagePadding);

                var len = logBuilder.Length;
                logBuilder.AppendLine(message);
                logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                logBuilder.AppendLine(exception.ToString());
            }

            var hasLevel = !string.IsNullOrEmpty(logLevelString);
            // Queue log message
            _queueProcessor.EnqueueMessage(new LogMessageEntry()
            {
                Message = logBuilder.ToString(),
                MessageColor = DefaultConsoleColor,
                LevelString = hasLevel ? logLevelString : null,
                LevelBackground = hasLevel ? logLevelColors.Background : null,
                LevelForeground = hasLevel ? logLevelColors.Foreground : null
            });

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
            {
                return false;
            }

            return Filter(Name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state) ?? NullScope.Instance;

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
            if (DisableColors)
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

        private void GetScopeInformation(StringBuilder stringBuilder)
        {
            var scopeProvider = ScopeProvider;
            if (scopeProvider != null)
            {
                var initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    var (builder, length) = state;
                    var first = length == builder.Length;
                    builder.Append(first ? "=> " : " => ").Append(scope);
                }, (stringBuilder, initialLength));

                if (stringBuilder.Length > initialLength)
                {
                    stringBuilder.Insert(initialLength, _messagePadding);
                    stringBuilder.AppendLine();
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

        private class AnsiSystemConsole : IAnsiSystemConsole
        {
            public void Write(string message)
            {
                System.Console.Write(message);
            }

            public void WriteLine(string message)
            {
                System.Console.WriteLine(message);
            }
        }
    }
}
