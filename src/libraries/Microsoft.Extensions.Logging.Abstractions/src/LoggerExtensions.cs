// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// ILogger extension methods for common scenarios.
    /// </summary>
    public static class LoggerExtensions
    {
        private static readonly Func<FormattedLogValues, Exception?, string> _messageFormatter = MessageFormatter;

        //------------------------------------------DEBUG------------------------------------------//

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, exception, message, arg0);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, exception, message, arg0, arg1);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1, T2>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, exception, message, arg0, arg1, arg2);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, exception, message, args);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0>(this ILogger logger, EventId eventId, string? message, T0 arg0)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, message, arg0);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, message, arg0, arg1);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1, T2>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(0, "Processing request from {Address}", address)</example>
        public static void LogDebug(this ILogger logger, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, eventId, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0>(this ILogger logger, Exception? exception, string? message, T0 arg0)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, exception, message, arg0);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, exception, message, arg0, arg1);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1, T2>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug(exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0>(this ILogger logger, string? message, T0 arg0)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, message, arg0);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1>(this ILogger logger, string? message, T0 arg0, T1 arg1)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, message, arg0, arg1);
            }
        }
        
        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogDebug(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogDebug<T0, T1, T2>(this ILogger logger, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if(logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogDebug("Processing request from {Address}", address)</example>
        public static void LogDebug(this ILogger logger, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.Log(LogLevel.Debug, message, args);
            }
        }

        //------------------------------------------TRACE------------------------------------------//

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1, T2>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0>(this ILogger logger, EventId eventId, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1, T2>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(0, "Processing request from {Address}", address)</example>
        public static void LogTrace(this ILogger logger, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, eventId, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0>(this ILogger logger, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1, T2>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace(exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0>(this ILogger logger, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1>(this ILogger logger, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogTrace(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogTrace<T0, T1, T2>(this ILogger logger, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a trace log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogTrace("Processing request from {Address}", address)</example>
        public static void LogTrace(this ILogger logger, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.Log(LogLevel.Trace, message, args);
            }
        }

        //------------------------------------------INFORMATION------------------------------------------//

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1, T2>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0>(this ILogger logger, EventId eventId, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1, T2>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(0, "Processing request from {Address}", address)</example>
        public static void LogInformation(this ILogger logger, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, eventId, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0>(this ILogger logger, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1, T2>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation(exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0>(this ILogger logger, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1>(this ILogger logger, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogInformation(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogInformation<T0, T1, T2>(this ILogger logger, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a informational log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogInformation("Processing request from {Address}", address)</example>
        public static void LogInformation(this ILogger logger, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.Log(LogLevel.Information, message, args);
            }
        }

        //------------------------------------------WARNING------------------------------------------//

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1, T2>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0>(this ILogger logger, EventId eventId, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1, T2>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(0, "Processing request from {Address}", address)</example>
        public static void LogWarning(this ILogger logger, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, eventId, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0>(this ILogger logger, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1, T2>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning(exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0>(this ILogger logger, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1>(this ILogger logger, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogWarning(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogWarning<T0, T1, T2>(this ILogger logger, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a warning log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogWarning("Processing request from {Address}", address)</example>
        public static void LogWarning(this ILogger logger, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.Log(LogLevel.Warning, message, args);
            }
        }

        //------------------------------------------ERROR------------------------------------------//

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1, T2>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0>(this ILogger logger, EventId eventId, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1, T2>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(0, "Processing request from {Address}", address)</example>
        public static void LogError(this ILogger logger, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, eventId, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0>(this ILogger logger, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1, T2>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError(exception, "Error while processing request from {Address}", address)</example>
        public static void LogError(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0>(this ILogger logger, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1>(this ILogger logger, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogError(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogError<T0, T1, T2>(this ILogger logger, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a error log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogError("Processing request from {Address}", address)</example>
        public static void LogError(this ILogger logger, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.Log(LogLevel.Error, message, args);
            }
        }

        //------------------------------------------CRITICAL------------------------------------------//

        // <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1, T2>(this ILogger logger, EventId eventId, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical(this ILogger logger, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0>(this ILogger logger, EventId eventId, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1, T2>(this ILogger logger, EventId eventId, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(0, "Processing request from {Address}", address)</example>
        public static void LogCritical(this ILogger logger, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, eventId, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0>(this ILogger logger, Exception? exception, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, exception, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, exception, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1, T2>(this ILogger logger, Exception? exception, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, exception, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical(exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical(this ILogger logger, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, exception, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0>(this ILogger logger, string? message, T0 arg0)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, message, arg0);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1>(this ILogger logger, string? message, T0 arg0, T1 arg1)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, message, arg0, arg1);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="arg0">A generic type that contains the value to format for the message.</param>
        /// <param name="arg1">A generic type that contains the value to format for the message.</param>
        /// <param name="arg2">A generic type that contains the value to format for the message.</param>
        /// <example>logger.LogCritical(0, exception, "Error while processing request from {Address}", address)</example>
        public static void LogCritical<T0, T1, T2>(this ILogger logger, string? message, T0 arg0, T1 arg1, T2 arg2)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, message, arg0, arg1, arg2);
            }
        }

        /// <summary>
        /// Formats and writes a critical log message.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="message">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <example>logger.LogCritical("Processing request from {Address}", address)</example>
        public static void LogCritical(this ILogger logger, string? message, params object?[] args)
        {
            if (logger.IsEnabled(LogLevel.Critical))
            {
                logger.Log(LogLevel.Critical, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this ILogger logger, LogLevel logLevel, string? message, params object?[] args)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, 0, null, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string? message, params object?[] args)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, eventId, null, message, args);
            }
        }

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this ILogger logger, LogLevel logLevel, Exception? exception, string? message, params object?[] args)
        {
            logger.Log(logLevel, 0, exception, message, args);
        }

        /// <summary>
        /// Formats and writes a log message at the specified log level.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to write to.</param>
        /// <param name="logLevel">Entry will be written on this level.</param>
        /// <param name="eventId">The event id associated with the log.</param>
        /// <param name="exception">The exception to log.</param>
        /// <param name="message">Format string of the log message.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        public static void Log(this ILogger logger!!, LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args)
        {
            if (logger.IsEnabled(logLevel))
            {
                logger.Log(logLevel, eventId, new FormattedLogValues(message, args), exception, _messageFormatter);
            }
        }

        //------------------------------------------Scope------------------------------------------//

        /// <summary>
        /// Formats the message and creates a scope.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to create the scope in.</param>
        /// <param name="messageFormat">Format string of the log message in message template format. Example: <c>"User {User} logged in from {Address}"</c></param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A disposable scope object. Can be null.</returns>
        /// <example>
        /// using(logger.BeginScope("Processing request from {Address}", address))
        /// {
        /// }
        /// </example>
        public static IDisposable BeginScope(
            this ILogger logger!!,
            string messageFormat,
            params object?[] args)
        {
            return logger.BeginScope(new FormattedLogValues(messageFormat, args));
        }

        //------------------------------------------HELPERS------------------------------------------//

        private static string MessageFormatter(FormattedLogValues state, Exception? error)
        {
            return state.ToString();
        }
    }
}
