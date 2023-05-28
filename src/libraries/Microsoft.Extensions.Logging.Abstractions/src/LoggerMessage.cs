// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using static Microsoft.Extensions.Logging.LoggerMessage;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Creates delegates which can be later cached to log messages in a performant way.
    /// </summary>
    public static class LoggerMessage
    {
        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, IDisposable?> DefineScope(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 0);

            var logValues = new LogValues(formatter);

            return logger => logger.BeginScope(logValues);
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, IDisposable?> DefineScope<T1>(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 1);

            return (logger, arg1) => logger.BeginScope(new LogValues<T1>(formatter, arg1));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, IDisposable?> DefineScope<T1, T2>(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 2);

            return (logger, arg1, arg2) =>
            {
                return logger.BeginScope(new LogValues<T1, T2>(formatter, arg1, arg2));
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, T3, IDisposable?> DefineScope<T1, T2, T3>(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 3);

            return (logger, arg1, arg2, arg3) => logger.BeginScope(new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, T3, T4, IDisposable?> DefineScope<T1, T2, T3, T4>(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 4);

            return (logger, arg1, arg2, arg3, arg4) => logger.BeginScope(new LogValues<T1, T2, T3, T4>(formatter, arg1, arg2, arg3, arg4));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, T3, T4, T5, IDisposable?> DefineScope<T1, T2, T3, T4, T5>(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 5);

            return (logger, arg1, arg2, arg3, arg4, arg5) => logger.BeginScope(new LogValues<T1, T2, T3, T4, T5>(formatter, arg1, arg2, arg3, arg4, arg5));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, T3, T4, T5, T6, IDisposable?> DefineScope<T1, T2, T3, T4, T5, T6>(string formatString)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 6);

            return (logger, arg1, arg2, arg3, arg4, arg5, arg6) => logger.BeginScope(new LogValues<T1, T2, T3, T4, T5, T6>(formatter, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, Exception?> Define(LogLevel logLevel, EventId eventId, string formatString)
            => Define(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, Exception?> Define(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 0);

            void Log(ILogger logger, Exception? exception)
            {
                logger.Log(logLevel, eventId, new LogValues(formatter), exception, LogValues.Callback);
            }

            if (options != null && options.SkipEnabledCheck)
            {
                return Log;
            }

            return (logger, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, exception);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, Exception?> Define<T1>(LogLevel logLevel, EventId eventId, string formatString)
            => Define<T1>(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, Exception?> Define<T1>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 1);

            void Log(ILogger logger, T1 arg1, Exception? exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1>(formatter, arg1), exception, LogValues<T1>.Callback);
            }

            if (options != null && options.SkipEnabledCheck)
            {
                return Log;
            }

            return (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, exception);
                }
            };
        }

        public delegate void Log<TState>(ILogger logger, ref TState state, Exception? exception);

        public static Log<TState> Define<TState>(ILogMetadata<TState> metadata, LogDefineOptions? options = null)
        {
            LogEntryPipeline<TState>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, ref TState state, Exception? exception)
            {
                LogEntryPipeline<TState>? pipelineSnapshot = pipeline;
                if (pipelineSnapshot != null && pipelineSnapshot.UserState == logger && pipelineSnapshot.IsUpToDate)
                {
                    if (!pipelineSnapshot.IsEnabled ||
                       (pipelineSnapshot.IsDynamicLevelCheckRequired && needFullEnabledCheck && !pipelineSnapshot.IsEnabledDynamic(metadata.LogLevel)))
                        return;
                    LogEntry<TState> entry = new LogEntry<TState>(metadata.LogLevel, category: null!, metadata.EventId, state, exception, null!);
                    pipelineSnapshot.HandleLogEntry(ref entry);
                }
                else
                {
                    LogSlowPath(logger, ref state, exception);
                }
            }

            void LogSlowPath(ILogger logger, ref TState state, Exception? exception)
            {
                LogEntryPipeline<TState>? pipelineSnapshot = null;
                LogEntry<TState> entry = new LogEntry<TState>(metadata.LogLevel, category: null!, metadata.EventId, state, exception, null!);
                if (logger is ILogEntryPipelineFactory)
                {
                    pipelineSnapshot = ((ILogEntryPipelineFactory)logger).GetLoggingPipeline(metadata, logger);
                    pipeline = pipelineSnapshot;
                }
                if (pipelineSnapshot != null)
                {
                    if (!pipelineSnapshot.IsEnabled ||
                       (pipelineSnapshot.IsDynamicLevelCheckRequired && needFullEnabledCheck && !pipelineSnapshot.IsEnabledDynamic(metadata.LogLevel)))
                        return;
                    pipelineSnapshot.HandleLogEntry(ref entry);
                }
                else
                {
                    if (needFullEnabledCheck && logger.IsEnabled(metadata.LogLevel))
                        return;
                    logger.Log(entry.LogLevel, entry.EventId, entry.State, entry.Exception, metadata.GetStringMessageFormatter());
                }
            }
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, Exception?> Define<T1, T2>(LogLevel logLevel, EventId eventId, string formatString)
            => Define<T1, T2>(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, Exception?> Define<T1, T2>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesMetadata<T1, T2> metadata = LogValues<T1, T2>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues<T1, T2>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T1 arg1, T2 arg2, Exception? exception)
            {
                LogEntryPipeline<LogValues<T1, T2>>? pipelineSnapshot = pipeline;
                if (pipelineSnapshot != null && pipelineSnapshot.UserState == logger && pipelineSnapshot.IsUpToDate)
                {
                    if (!pipelineSnapshot.IsEnabled ||
                       (pipelineSnapshot.IsDynamicLevelCheckRequired && needFullEnabledCheck && !pipelineSnapshot.IsEnabledDynamic(logLevel)))
                        return;
                    LogValues<T1, T2> state = new LogValues<T1, T2>(metadata, arg1, arg2);
                    LogEntry<LogValues<T1, T2>> entry = new LogEntry<LogValues<T1, T2>>(logLevel, category: null!, eventId, state, exception, LogValues<T1, T2>.Callback);
                    pipelineSnapshot.HandleLogEntry(ref entry);
                }
                else
                {
                    LogSlowPath(logger, arg1, arg2, exception);
                }
            }

            void LogSlowPath(ILogger logger, T1 arg1, T2 arg2, Exception? exception)
            {
                LogEntryPipeline<LogValues<T1, T2>>? pipelineSnapshot = null;
                if (logger is ILogEntryPipelineFactory)
                {
                    pipelineSnapshot = ((ILogEntryPipelineFactory)logger).GetLoggingPipeline(metadata, logger);
                    pipeline = pipelineSnapshot;
                }
                if (pipelineSnapshot != null)
                {
                    if (!pipelineSnapshot.IsEnabled ||
                       (pipelineSnapshot.IsDynamicLevelCheckRequired && needFullEnabledCheck && !pipelineSnapshot.IsEnabledDynamic(logLevel)))
                        return;
                    LogValues<T1, T2> state = new LogValues<T1, T2>(metadata, arg1, arg2);
                    LogEntry<LogValues<T1, T2>> entry = new LogEntry<LogValues<T1, T2>>(logLevel, category: null!, eventId, state, exception, LogValues<T1, T2>.Callback);
                    pipelineSnapshot.HandleLogEntry(ref entry);
                }
                else
                {
                    if (needFullEnabledCheck && !logger.IsEnabled(logLevel))
                        return;
                    LogValues<T1, T2> state = new LogValues<T1, T2>(metadata, arg1, arg2);
                    logger.Log(logLevel, eventId, state, exception, LogValues<T1, T2>.Callback);
                }
            }
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, Exception?> Define<T1, T2, T3>(LogLevel logLevel, EventId eventId, string formatString)
            => Define<T1, T2, T3>(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, Exception?> Define<T1, T2, T3>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 3);

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, Exception? exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3), exception, LogValues<T1, T2, T3>.Callback);
            }

            if (options != null && options.SkipEnabledCheck)
            {
                return Log;
            }

            return (logger, arg1, arg2, arg3, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, arg2, arg3, exception);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, Exception?> Define<T1, T2, T3, T4>(LogLevel logLevel, EventId eventId, string formatString)
            => Define<T1, T2, T3, T4>(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, Exception?> Define<T1, T2, T3, T4>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 4);

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4>(formatter, arg1, arg2, arg3, arg4), exception, LogValues<T1, T2, T3, T4>.Callback);
            }

            if (options != null && options.SkipEnabledCheck)
            {
                return Log;
            }

            return (logger, arg1, arg2, arg3, arg4, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, arg2, arg3, arg4, exception);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, T5, Exception?> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, EventId eventId, string formatString)
            => Define<T1, T2, T3, T4, T5>(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, T5, Exception?> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 5);

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4, T5>(formatter, arg1, arg2, arg3, arg4, arg5), exception, LogValues<T1, T2, T3, T4, T5>.Callback);
            }

            if (options != null && options.SkipEnabledCheck)
            {
                return Log;
            }

            return (logger, arg1, arg2, arg3, arg4, arg5, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, arg2, arg3, arg4, arg5, exception);
                }
            };
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, T5, T6, Exception?> Define<T1, T2, T3, T4, T5, T6>(LogLevel logLevel, EventId eventId, string formatString)
            => Define<T1, T2, T3, T4, T5, T6>(logLevel, eventId, formatString, options: null);

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter passed to the named format string.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter passed to the named format string.</typeparam>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <param name="options">The <see cref="LogDefineOptions"/></param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, T1, T2, T3, T4, T5, T6, Exception?> Define<T1, T2, T3, T4, T5, T6>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options)
        {
            LogValuesFormatter formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 6);

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Exception? exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4, T5, T6>(formatter, arg1, arg2, arg3, arg4, arg5, arg6), exception, LogValues<T1, T2, T3, T4, T5, T6>.Callback);
            }

            if (options != null && options.SkipEnabledCheck)
            {
                return Log;
            }

            return (logger, arg1, arg2, arg3, arg4, arg5, arg6, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, arg2, arg3, arg4, arg5, arg6, exception);
                }
            };
        }

        private static LogValuesFormatter CreateLogValuesFormatter(string formatString, int expectedNamedParameterCount)
        {
            var logValuesFormatter = new LogValuesFormatter(formatString);

            ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount, logValuesFormatter.PropertyCount);

            return logValuesFormatter;
        }

        private static void ValidateFormatStringParameterCount(string formatString, int expectedNamedParameterCount, int actualCount)
        {
            if (actualCount != expectedNamedParameterCount)
            {
                throw new ArgumentException(
                    SR.Format(SR.UnexpectedNumberOfNamedParameters, formatString, expectedNamedParameterCount, actualCount));
            }
        }

        private readonly struct LogValues : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;

            public LogValues(LogValuesFormatter formatter)
            {
                _formatter = formatter;
            }

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                    }
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public int Count => 1;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return this[0];
            }

            public override string ToString() => _formatter.Format();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly struct LogValues<T0> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;

            public LogValues(LogValuesFormatter formatter, T0 value0)
            {
                _formatter = formatter;
                _value0 = value0;
            }

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public int Count => 2;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }


            public override string ToString() => _formatter.Format(_value0);

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class LogValuesMetadata<T1, T2> : LogValuesMetadata, ILogMetadata<LogValues<T1, T2>>
        {
            public LogValuesMetadata(string format, LogLevel level, EventId eventId, object[]?[]? metadata = null) : base(format, level, eventId, metadata) { }

            public void AppendFormattedMessage(in LogValues<T1, T2> state, IBufferWriter<char> buffer)
            {
                BufferWriter<char> writer = new BufferWriter<char>(buffer);
                foreach ((string? Literal, int ArgIndex, int Alignment, string? Format) segment in CompositeFormat._segments)
                {
                    int index = segment.ArgIndex;
                    switch (index)
                    {
                        case 0:
                            AppendFormattedPropertyValue(state._value0, ref writer, segment.Alignment, segment.Format);
                            break;
                        case 1:
                            AppendFormattedPropertyValue(state._value1, ref writer, segment.Alignment, segment.Format);
                            break;
                        default:
                            writer.Write(segment.Literal.AsSpan());
                            break;
                    }
                }
                writer.Flush();
            }

            public FormatPropertyListAction<LogValues<T1, T2>> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory)
            {
                FormatPropertyAction<T1> formatter0 = propertyFormatterFactory.GetPropertyFormatter<T1>(0, GetPropertyInfo(0));
                FormatPropertyAction<T2> formatter1 = propertyFormatterFactory.GetPropertyFormatter<T2>(1, GetPropertyInfo(1));
                return FormatPropertyList;

                void FormatPropertyList(in LogValues<T1, T2> tstate, ref BufferWriter<byte> writer)
                {
                    formatter0(tstate._value0, ref writer);
                    formatter1(tstate._value1, ref writer);
                }
            }

            public Action<LogValues<T1, T2>, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customPropertyFormatters) =>
                (state, buffer) => AppendFormattedMessage(state, buffer, customPropertyFormatters);

            private void AppendFormattedMessage(in LogValues<T1, T2> state, IBufferWriter<char> buffer, PropertyCustomFormatter[] customFormatters)
            {
                BufferWriter<char> writer = new BufferWriter<char>(buffer);
                foreach ((string? Literal, int ArgIndex, int Alignment, string? Format) segment in CompositeFormat._segments)
                {
                    int index = segment.ArgIndex;
                    switch (index)
                    {
                        case 0:
                            AppendCustomFormattedProperty(index, state._value0, ref writer, segment.Alignment, segment.Format, customFormatters[index]);
                            break;
                        case 1:
                            AppendCustomFormattedProperty(index, state._value1, ref writer, segment.Alignment, segment.Format, customFormatters[index]);
                            break;
                        default:
                            writer.Write(segment.Literal.AsSpan());
                            break;
                    }
                }
                writer.Flush();
            }

            private static void AppendCustomFormattedProperty<T>(int index, T value, ref BufferWriter<char> writer, int alignment, string? format, PropertyCustomFormatter? formatter)
            {
                if (formatter == null)
                {
                    AppendFormattedPropertyValue(value, ref writer, alignment, format);
                }
                else
                {
                    writer.Flush();
                    if (value is string strVal)
                    {
                        formatter.AppendFormatted(index, strVal, writer.Writer);
                    }
                    else if (value is int intVal)
                    {
                        formatter.AppendFormatted(index, intVal, writer.Writer);
                    }
                    else
                    {
                        formatter.AppendFormatted(index, value, writer.Writer);
                    }
                }
            }

            public Func<LogValues<T1, T2>, Exception?, string> GetStringMessageFormatter() => LogValues<T1, T2>.Callback;
        }

        internal readonly struct LogValues<T0, T1> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0, T1>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            internal readonly T0 _value0;
            internal readonly T1 _value1;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
            }

            public ILogMetadata<LogValues<T0, T1>>? Metadata => _formatter as LogValuesMetadata<T0, T1>;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public int Count => 3;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            public override string ToString() => _formatter.Format(_value0, _value1);

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static LogValuesMetadata<T0, T1> CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata<T0, T1>(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 2, metadata.PropertyCount);
                return metadata;
            }
        }

        private readonly struct LogValues<T0, T1, T2> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0, T1, T2>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;

            public int Count => 4;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
            }

            public override string ToString() => _formatter.Format(_value0, _value1, _value2);

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly struct LogValues<T0, T1, T2, T3> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;
            private readonly T3 _value3;

            public int Count => 5;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(3), _value3);
                        case 4:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
            }

            private object?[] ToArray() => new object?[] { _value0, _value1, _value2, _value3 };

            public override string ToString() => _formatter.FormatWithOverwrite(ToArray());

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly struct LogValues<T0, T1, T2, T3, T4> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3, T4>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;
            private readonly T3 _value3;
            private readonly T4 _value4;

            public int Count => 6;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(3), _value3);
                        case 4:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(4), _value4);
                        case 5:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
            }

            private object?[] ToArray() => new object?[] { _value0, _value1, _value2, _value3, _value4 };

            public override string ToString() => _formatter.FormatWithOverwrite(ToArray());

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly struct LogValues<T0, T1, T2, T3, T4, T5> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3, T4, T5>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;
            private readonly T3 _value3;
            private readonly T4 _value4;
            private readonly T5 _value5;

            public int Count => 7;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(3), _value3);
                        case 4:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(4), _value4);
                        case 5:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(5), _value5);
                        case 6:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
                _value5 = value5;
            }

            private object?[] ToArray() => new object?[] { _value0, _value1, _value2, _value3, _value4, _value5 };

            public override string ToString() => _formatter.FormatWithOverwrite(ToArray());

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
