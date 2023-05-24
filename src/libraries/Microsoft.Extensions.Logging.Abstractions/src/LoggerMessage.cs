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
            LogValuesMetadata<T1, T2> metadata = LogValues<T1, T2>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterAttributes);
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

        public delegate void Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(T0 v0, T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6, T7 v7, T8 v8, T9 v9, T10 v10, T11 v11, T12 v12, T13 v13, T14 v14, T15 v15, T16 v16, T17 v17, T18 v18, T19 v19, T20 v20, T21 v21);

        public static Action<ILogger, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, Exception?> Define<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(LogLevel logLevel, EventId eventId, string formatString, LogDefineOptions? options = null)
        {
            LogValuesMetadata<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> metadata =
                LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterAttributes);
            LogEntryPipeline<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, Exception? exception)
            {
                LogEntryPipeline<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>? pipelineSnapshot = pipeline;
                if (pipelineSnapshot != null && pipelineSnapshot.UserState == logger && pipelineSnapshot.IsUpToDate)
                {
                    if (!pipelineSnapshot.IsEnabled ||
                       (pipelineSnapshot.IsDynamicLevelCheckRequired && needFullEnabledCheck && !pipelineSnapshot.IsEnabledDynamic(logLevel)))
                        return;
                    LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> state = new LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(metadata);
                    state._value0 = arg0;
                    state._value1 = arg1;
                    state._value2 = arg2;
                    state._value3 = arg3;
                    state._value4 = arg4;
                    state._value5 = arg5;
                    state._value6 = arg6;
                    state._value7 = arg7;
                    state._value8 = arg8;
                    state._value9 = arg9;
                    state._value10 = arg10;
                    state._value11 = arg11;
                    state._value12 = arg12;
                    state._value13 = arg13;
                    state._value14 = arg14;
                    state._value15 = arg15;
                    state._value16 = arg16;
                    state._value17 = arg17;
                    state._value18 = arg18;
                    state._value19 = arg19;
                    LogEntry<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>> entry =
                        new LogEntry<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>(logLevel, category: null!, eventId, state, exception, null!);
                    pipelineSnapshot.HandleLogEntry(ref entry);
                }
                else
                {
                    LogSlowPath(logger, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16, arg17, arg18, arg19, exception);
                }
            }

            void LogSlowPath(ILogger logger, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14, T15 arg15, T16 arg16, T17 arg17, T18 arg18, T19 arg19, Exception? exception)
            {
                LogEntryPipeline<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>? pipelineSnapshot = null;
                LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> state = new LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(metadata);
                state._value0 = arg0;
                state._value1 = arg1;
                state._value2 = arg2;
                state._value3 = arg3;
                state._value4 = arg4;
                state._value5 = arg5;
                state._value6 = arg6;
                state._value7 = arg7;
                state._value8 = arg8;
                state._value9 = arg9;
                state._value10 = arg10;
                state._value11 = arg11;
                state._value12 = arg12;
                state._value13 = arg13;
                state._value14 = arg14;
                state._value15 = arg15;
                state._value16 = arg16;
                state._value17 = arg17;
                state._value18 = arg18;
                state._value19 = arg19;
                LogEntry<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>> entry =
                    new LogEntry<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>(logLevel, category: null!, eventId, state, exception, null!);
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
                    pipelineSnapshot.HandleLogEntry(ref entry);
                }
                else
                {
                    if (needFullEnabledCheck && logger.IsEnabled(logLevel))
                        return;
                    logger.Log(entry.LogLevel, entry.EventId, entry.State, entry.Exception, LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>.Callback);
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
            public LogValuesMetadata(string format, LogLevel level, EventId eventId, Attribute[]?[]? attributes = null) : base(format, level, eventId, attributes) { }

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
                FormatPropertyAction<T1> formatter0 = propertyFormatterFactory.GetPropertyFormatter<T1>(0, GetPropertyMetadata(0));
                FormatPropertyAction<T2> formatter1 = propertyFormatterFactory.GetPropertyFormatter<T2>(1, GetPropertyMetadata(1));
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

            public static LogValuesMetadata<T0, T1> CreateMetadata(LogLevel level, EventId eventId, string formatString, Attribute[]?[]? parameterAttributes = null)
            {
                var metadata = new LogValuesMetadata<T0, T1>(formatString, level, eventId, parameterAttributes);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 2, metadata.PropertyCount);
                return metadata;
            }
        }

        internal class LogValuesMetadata<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> :
            LogValuesMetadata, ILogMetadata<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>
        {
            public LogValuesMetadata(string format, LogLevel level, EventId eventId, Attribute[]?[]? attributes = null) : base(format, level, eventId, attributes) { }

            public void AppendFormattedMessage(in LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> state, IBufferWriter<char> buffer)
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

            public Action<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>, IBufferWriter<char>> GetMessageFormatter(PropertyCustomFormatter[] customPropertyFormatters) =>
                (state, buffer) => AppendFormattedMessage(state, buffer, customPropertyFormatters);

            private void AppendFormattedMessage(in LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> state, IBufferWriter<char> buffer, PropertyCustomFormatter[] customFormatters)
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

            public FormatPropertyListAction<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>> GetPropertyListFormatter(IPropertyFormatterFactory propertyFormatterFactory)
            {
                FormatPropertyAction<T0> formatter0 = propertyFormatterFactory.GetPropertyFormatter<T0>(0, GetPropertyMetadata(0));
                FormatPropertyAction<T1> formatter1 = propertyFormatterFactory.GetPropertyFormatter<T1>(1, GetPropertyMetadata(1));
                FormatPropertyAction<T2> formatter2 = propertyFormatterFactory.GetPropertyFormatter<T2>(2, GetPropertyMetadata(2));
                FormatPropertyAction<T3> formatter3 = propertyFormatterFactory.GetPropertyFormatter<T3>(3, GetPropertyMetadata(3));
                FormatPropertyAction<T4> formatter4 = propertyFormatterFactory.GetPropertyFormatter<T4>(4, GetPropertyMetadata(4));
                FormatPropertyAction<T5> formatter5 = propertyFormatterFactory.GetPropertyFormatter<T5>(5, GetPropertyMetadata(5));
                FormatPropertyAction<T6> formatter6 = propertyFormatterFactory.GetPropertyFormatter<T6>(6, GetPropertyMetadata(6));
                FormatPropertyAction<T7> formatter7 = propertyFormatterFactory.GetPropertyFormatter<T7>(7, GetPropertyMetadata(7));
                FormatPropertyAction<T8> formatter8 = propertyFormatterFactory.GetPropertyFormatter<T8>(8, GetPropertyMetadata(8));
                FormatPropertyAction<T9> formatter9 = propertyFormatterFactory.GetPropertyFormatter<T9>(9, GetPropertyMetadata(9));
                FormatPropertyAction<T10> formatter10 = propertyFormatterFactory.GetPropertyFormatter<T10>(10, GetPropertyMetadata(10));
                FormatPropertyAction<T11> formatter11 = propertyFormatterFactory.GetPropertyFormatter<T11>(11, GetPropertyMetadata(11));
                FormatPropertyAction<T12> formatter12 = propertyFormatterFactory.GetPropertyFormatter<T12>(12, GetPropertyMetadata(12));
                FormatPropertyAction<T13> formatter13 = propertyFormatterFactory.GetPropertyFormatter<T13>(13, GetPropertyMetadata(13));
                FormatPropertyAction<T14> formatter14 = propertyFormatterFactory.GetPropertyFormatter<T14>(14, GetPropertyMetadata(14));
                FormatPropertyAction<T15> formatter15 = propertyFormatterFactory.GetPropertyFormatter<T15>(15, GetPropertyMetadata(15));
                FormatPropertyAction<T16> formatter16 = propertyFormatterFactory.GetPropertyFormatter<T16>(16, GetPropertyMetadata(16));
                FormatPropertyAction<T17> formatter17 = propertyFormatterFactory.GetPropertyFormatter<T17>(17, GetPropertyMetadata(17));
                FormatPropertyAction<T18> formatter18 = propertyFormatterFactory.GetPropertyFormatter<T18>(18, GetPropertyMetadata(18));
                FormatPropertyAction<T19> formatter19 = propertyFormatterFactory.GetPropertyFormatter<T19>(19, GetPropertyMetadata(19));
                return FormatPropertyList;

                void FormatPropertyList(in LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> tstate, ref BufferWriter<byte> writer)
                {
                    formatter0(tstate._value0, ref writer);
                    formatter1(tstate._value1, ref writer);
                    formatter2(tstate._value2, ref writer);
                    formatter3(tstate._value3, ref writer);
                    formatter4(tstate._value4, ref writer);
                    formatter5(tstate._value5, ref writer);
                    formatter6(tstate._value6, ref writer);
                    formatter7(tstate._value7, ref writer);
                    formatter8(tstate._value8, ref writer);
                    formatter9(tstate._value9, ref writer);
                    formatter10(tstate._value10, ref writer);
                    formatter11(tstate._value11, ref writer);
                    formatter12(tstate._value12, ref writer);
                    formatter13(tstate._value13, ref writer);
                    formatter14(tstate._value14, ref writer);
                    formatter15(tstate._value15, ref writer);
                    formatter16(tstate._value16, ref writer);
                    formatter17(tstate._value17, ref writer);
                    formatter18(tstate._value18, ref writer);
                    formatter19(tstate._value19, ref writer);
                }
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

            public Func<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>, Exception?, string> GetStringMessageFormatter() =>
                LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>.Callback;
        }

        internal struct LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>, Exception?, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            internal T0 _value0;
            internal T1 _value1;
            internal T2 _value2;
            internal T3 _value3;
            internal T4 _value4;
            internal T5 _value5;
            internal T6 _value6;
            internal T7 _value7;
            internal T8 _value8;
            internal T9 _value9;
            internal T10 _value10;
            internal T11 _value11;
            internal T12 _value12;
            internal T13 _value13;
            internal T14 _value14;
            internal T15 _value15;
            internal T16 _value16;
            internal T17 _value17;
            internal T18 _value18;
            internal T19 _value19;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            public LogValues(LogValuesFormatter formatter)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            {
                _formatter = formatter;
            }

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8, T9 value9, T10 value10,
                T11 value11, T12 value12, T13 value13, T14 value14, T15 value15, T16 value16, T17 value17, T18 value18, T19 value19)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
                _value5 = value5;
                _value6 = value6;
                _value7 = value7;
                _value8 = value8;
                _value9 = value9;
                _value10 = value10;
                _value11 = value11;
                _value12 = value12;
                _value13 = value13;
                _value14 = value14;
                _value15 = value15;
                _value16 = value16;
                _value17 = value17;
                _value18 = value18;
                _value19 = value19;
            }

            public ILogMetadata<LogValues<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>? Metadata =>
                _formatter as LogValuesMetadata<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>;

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
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(6), _value6);
                        case 7:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(7), _value7);
                        case 8:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(8), _value8);
                        case 9:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(9), _value9);
                        case 10:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(10), _value10);
                        case 11:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(11), _value11);
                        case 12:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(12), _value12);
                        case 13:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(13), _value13);
                        case 14:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(14), _value14);
                        case 15:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(15), _value15);
                        case 16:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(16), _value16);
                        case 17:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(17), _value17);
                        case 18:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(18), _value18);
                        case 19:
                            return new KeyValuePair<string, object?>(_formatter.GetValueName(19), _value19);
                        case 20:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public int Count => 21;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                for (int i = 0; i < Count; ++i)
                {
                    yield return this[i];
                }
            }

            public override string ToString() => throw new NotImplementedException();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static LogValuesMetadata<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> CreateMetadata(LogLevel level, EventId eventId, string formatString, Attribute[]?[]? parameterAttributes = null)
            {
                var metadata = new LogValuesMetadata<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(formatString, level, eventId, parameterAttributes);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 20, metadata.PropertyCount);
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
