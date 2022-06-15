// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.Console
{
    internal sealed class SimpleConsoleFormatter : ConsoleFormatter, IDisposable
    {
        /// <summary>Whether to output ansi color strings.</summary>
        private static volatile int s_emitAnsiColorCodes = -1;
        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new string(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        private IDisposable? _optionsReloadToken;

        public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
            : base(ConsoleFormatterNames.Simple)
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        [MemberNotNull(nameof(FormatterOptions))]
        private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
            if (logEntry.Exception == null && message == null)
            {
                return;
            }
            LogLevel logLevel = logEntry.LogLevel;
            ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
            string logLevelString = GetLogLevelString(logLevel);

            string? timestamp = null;
            string? timestampFormat = FormatterOptions.TimestampFormat;
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

        private void CreateDefaultLogMessage<TState>(TextWriter textWriter, in LogEntry<TState> logEntry, string message, IExternalScopeProvider? scopeProvider)
        {
            bool singleLine = FormatterOptions.SingleLine;
            int eventId = logEntry.EventId.Id;
            Exception? exception = logEntry.Exception;

            // Example:
            // info: ConsoleApp.Program[10]
            //       Request received

            // category and event id
            textWriter.Write(LoglevelPadding);
            textWriter.Write(logEntry.Category);
            textWriter.Write('[');

#if NETCOREAPP
            Span<char> span = stackalloc char[10];
            if (eventId.TryFormat(span, out int charsWritten))
                textWriter.Write(span.Slice(0, charsWritten));
            else
#endif
                textWriter.Write(eventId.ToString());

            textWriter.Write(']');
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

        private static void WriteMessage(TextWriter textWriter, string message, bool singleLine)
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

        /// <summary>Get whether to emit ANSI color codes.</summary>
        private static bool EmitAnsiColorCodes
        {
            get
            {
                // The flag starts at -1.  If it's no longer -1, it's 0 or 1 to represent false or true.
                int emitAnsiColorCodes = s_emitAnsiColorCodes;
                if (emitAnsiColorCodes != -1)
                {
                    return Convert.ToBoolean(emitAnsiColorCodes);
                }

                // We've not yet computed whether to emit codes or not.  Do so now.  We may race with
                // other threads, and that's ok; this is idempotent unless someone is currently changing
                // the value of the relevant environment variables, in which case behavior here is undefined.

                // By default, we emit ANSI color codes if output isn't redirected, and suppress them if output is redirected.
                bool enabled = !System.Console.IsOutputRedirected;

                if (enabled)
                {
                    // We subscribe to the informal standard from https://no-color.org/.  If we'd otherwise emit
                    // ANSI color codes but the NO_COLOR environment variable is set, disable emitting them.
                    enabled = Environment.GetEnvironmentVariable("NO_COLOR") is null;
                }
                else
                {
                    // We also support overriding in the other direction.  If we'd otherwise avoid emitting color
                    // codes but the DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION environment variable is
                    // set to 1 or true, enable color.
                    string? envVar = Environment.GetEnvironmentVariable("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION");
                    enabled = envVar is not null && (envVar == "1" || envVar.Equals("true", StringComparison.OrdinalIgnoreCase));
                }

                // Store and return the computed answer.
                s_emitAnsiColorCodes = Convert.ToInt32(enabled);
                return enabled;
            }
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            bool disableColors = (FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled) ||
                (FormatterOptions.ColorBehavior == LoggerColorBehavior.Default && !EmitAnsiColorCodes);
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

        private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider, bool singleLine)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                bool paddingNeeded = !singleLine;
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (paddingNeeded)
                    {
                        paddingNeeded = false;
                        state.Write(_messagePadding);
                        state.Write("=> ");
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
