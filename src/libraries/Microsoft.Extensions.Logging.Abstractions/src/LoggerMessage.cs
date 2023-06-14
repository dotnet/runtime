// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
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
            LogValuesMetadata metadata = LogValues.CreateMetadata(LogLevel.None, default, formatString);

            return logger => logger.BeginScope(new LogValues(metadata));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, IDisposable?> DefineScope<T1>(string formatString)
        {
            LogValuesMetadata<T1> metadata = LogValues<T1>.CreateMetadata(LogLevel.None, default, formatString);

            return (logger, arg1) => logger.BeginScope(new LogValues<T1>(metadata, arg1));
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
            LogValuesMetadata<T1, T2> metadata = LogValues<T1, T2>.CreateMetadata(LogLevel.None, default, formatString);

            return (logger, arg1, arg2) => logger.BeginScope(new LogValues<T1, T2>(metadata, arg1, arg2));
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
            LogValuesMetadata<T1, T2, T3> metadata = LogValues<T1, T2, T3>.CreateMetadata(LogLevel.None, default, formatString);

            return (logger, arg1, arg2, arg3) => logger.BeginScope(new LogValues<T1, T2, T3>(metadata, arg1, arg2, arg3));
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
            LogValuesMetadata<T1, T2, T3, T4> metadata = LogValues<T1, T2, T3, T4>.CreateMetadata(LogLevel.None, default, formatString);

            return (logger, arg1, arg2, arg3, arg4) => logger.BeginScope(new LogValues<T1, T2, T3, T4>(metadata, arg1, arg2, arg3, arg4));
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
            LogValuesMetadata<T1, T2, T3, T4, T5> metadata = LogValues<T1, T2, T3, T4, T5>.CreateMetadata(LogLevel.None, default, formatString);

            return (logger, arg1, arg2, arg3, arg4, arg5) => logger.BeginScope(new LogValues<T1, T2, T3, T4, T5>(metadata, arg1, arg2, arg3, arg4, arg5));
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
            LogValuesMetadata<T1, T2, T3, T4, T5, T6> metadata = LogValues<T1, T2, T3, T4, T5, T6>.CreateMetadata(LogLevel.None, default, formatString);

            return (logger, arg1, arg2, arg3, arg4, arg5, arg6) => logger.BeginScope(new LogValues<T1, T2, T3, T4, T5, T6>(metadata, arg1, arg2, arg3, arg4, arg5, arg6));
        }

        public delegate void Log<TState>(ILogger logger, ref TState state, Exception? exception);

        private static void LogCore<TState>(ref LogEntryPipeline<TState>? cachedPipeline, ILogger logger, ILogMetadata<TState>? metadata, ref LogEntry<TState> entry, bool needFullEnabledCheck)
        {
            if (cachedPipeline == null || cachedPipeline.UserState != logger || cachedPipeline.CancelToken.IsCancellationRequested)
            {
                cachedPipeline = GetLogEntryPipeline(metadata, logger);
            }

            if (!cachedPipeline.IsEnabled ||
                   (cachedPipeline.IsDynamicLevelCheckRequired && needFullEnabledCheck && !cachedPipeline.IsEnabledDynamic(entry.LogLevel)))
            {
                return;
            }

            cachedPipeline.HandleLogEntry(ref entry);
        }

        private static LogEntryPipeline<TState> GetLogEntryPipeline<TState>(ILogMetadata<TState>? metadata, ILogger logger)
        {
            if (logger is ILogEntryProcessorFactory)
            {
                ProcessorContext context = ((ILogEntryProcessorFactory)logger).GetProcessor();
                LogEntryHandler<TState> handler = context.Processor.GetLogEntryHandler(metadata, out bool enabled, out bool dynamicEnableCheckRequired);
                return new LogEntryPipeline<TState>(handler, logger, enabled, dynamicEnableCheckRequired, context.CancellationToken);
            }
            else
            {
                return new LogEntryPipeline<TState>(new InvokeLoggerLogHandler<TState>(logger), logger, true, true, CancellationToken.None);
            }
        }

        public static Log<TState> Define<TState>(ILogMetadata<TState> metadata, LogDefineOptions? options = null)
        {
            LogEntryPipeline<TState>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, ref TState state, Exception? exception)
            {
                LogEntry<TState> entry = new LogEntry<TState>(metadata.LogLevel, category: null!, metadata.EventId, state, exception, null!);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
            }
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
            LogValuesMetadata metadata = LogValues.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, Exception? exception)
            {
                LogValues state = new LogValues(metadata);
                LogEntry<LogValues> entry = new LogEntry<LogValues>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
            }
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
            LogValuesMetadata<T1> metadata = LogValues<T1>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues<T1>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T1 arg1, Exception? exception)
            {
                LogValues<T1> state = new LogValues<T1>(metadata, arg1);
                LogEntry<LogValues<T1>> entry = new LogEntry<LogValues<T1>>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
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
                LogValues<T1, T2> state = new LogValues<T1, T2>(metadata, arg1, arg2);
                LogEntry<LogValues<T1, T2>> entry = new LogEntry<LogValues<T1, T2>>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
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
            LogValuesMetadata<T1, T2, T3> metadata = LogValues<T1, T2, T3>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues<T1, T2, T3>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, Exception? exception)
            {
                LogValues<T1, T2, T3> state = new LogValues<T1, T2, T3>(metadata, arg1, arg2, arg3);
                LogEntry<LogValues<T1, T2, T3>> entry = new LogEntry<LogValues<T1, T2, T3>>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
            }
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
            LogValuesMetadata<T1, T2, T3, T4> metadata = LogValues<T1, T2, T3, T4>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues<T1, T2, T3, T4>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception? exception)
            {
                LogValues<T1, T2, T3, T4> state = new LogValues<T1, T2, T3, T4>(metadata, arg1, arg2, arg3, arg4);
                LogEntry<LogValues<T1, T2, T3, T4>> entry = new LogEntry<LogValues<T1, T2, T3, T4>>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
            }
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
            LogValuesMetadata<T1, T2, T3, T4, T5> metadata = LogValues<T1, T2, T3, T4, T5>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues<T1, T2, T3, T4, T5>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception? exception)
            {
                LogValues<T1, T2, T3, T4, T5> state = new LogValues<T1, T2, T3, T4, T5>(metadata, arg1, arg2, arg3, arg4, arg5);
                LogEntry<LogValues<T1, T2, T3, T4, T5>> entry = new LogEntry<LogValues<T1, T2, T3, T4, T5>>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
            }
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
            LogValuesMetadata<T1, T2, T3, T4, T5, T6> metadata = LogValues<T1, T2, T3, T4, T5, T6>.CreateMetadata(logLevel, eventId, formatString, options?.ParameterMetadata);
            LogEntryPipeline<LogValues<T1, T2, T3, T4, T5, T6>>? pipeline = null;
            bool needFullEnabledCheck = (options == null || !options.SkipEnabledCheck);
            return Log;

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Exception? exception)
            {
                LogValues<T1, T2, T3, T4, T5, T6> state = new LogValues<T1, T2, T3, T4, T5, T6>(metadata, arg1, arg2, arg3, arg4, arg5, arg6);
                LogEntry<LogValues<T1, T2, T3, T4, T5, T6>> entry = new LogEntry<LogValues<T1, T2, T3, T4, T5, T6>>(logLevel, category: null!, eventId, state, exception, metadata.MessageFormatter);
                LogCore(ref pipeline, logger, metadata, ref entry, needFullEnabledCheck);
            }
        }

        private static void ValidateFormatStringParameterCount(string formatString, int expectedNamedParameterCount, int actualCount)
        {
            if (actualCount != expectedNamedParameterCount)
            {
                throw new ArgumentException(
                    SR.Format(SR.UnexpectedNumberOfNamedParameters, formatString, expectedNamedParameterCount, actualCount));
            }
        }

        internal readonly struct LogValues : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata _metadata;

            public LogValues(LogValuesMetadata metadata)
            {
                _metadata = metadata;
            }

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object?>("{OriginalFormat}", _metadata.OriginalFormat);
                    }
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public int Count => 1;

            public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            {
                yield return this[0];
            }

            public override string ToString() => _metadata.MessageFormatter(this, null);

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static LogValuesMetadata CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 0, metadata.PropertyCount);
                return metadata;
            }
        }

        internal readonly struct LogValues<T0> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata<T0> _metadata;
            internal readonly T0 _value0;

            public LogValues(LogValuesMetadata<T0> metadata, T0 value0)
            {
                _metadata = metadata;
                _value0 = value0;
            }

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _metadata.OriginalFormat);
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

            public override string ToString() => _metadata.MessageFormatter(this, null);

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public static LogValuesMetadata<T0> CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata<T0>(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 1, metadata.PropertyCount);
                return metadata;
            }
        }

        internal readonly struct LogValues<T0, T1> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata<T0, T1> _formatter;
            internal readonly T0 _value0;
            internal readonly T1 _value1;

            public LogValues(LogValuesMetadata<T0, T1> formatter, T0 value0, T1 value1)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
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

            public override string ToString() => _formatter.MessageFormatter(this, null);

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

        internal readonly struct LogValues<T0, T1, T2> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata<T0, T1, T2> _metadata;
            internal readonly T0 _value0;
            internal readonly T1 _value1;
            internal readonly T2 _value2;

            public LogValues(LogValuesMetadata<T0, T1, T2> metadata, T0 value0, T1 value1, T2 value2)
            {
                _metadata = metadata;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
            }

            public int Count => 4;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _metadata.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public override string ToString() => _metadata.MessageFormatter(this, null);

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

            public static LogValuesMetadata<T0, T1, T2> CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata<T0, T1, T2>(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 3, metadata.PropertyCount);
                return metadata;
            }
        }

        internal readonly struct LogValues<T0, T1, T2, T3> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata<T0, T1, T2, T3> _metadata;
            internal readonly T0 _value0;
            internal readonly T1 _value1;
            internal readonly T2 _value2;
            internal readonly T3 _value3;

            public int Count => 5;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(3), _value3);
                        case 4:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _metadata.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public LogValues(LogValuesMetadata<T0, T1, T2, T3> metadata, T0 value0, T1 value1, T2 value2, T3 value3)
            {
                _metadata = metadata;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
            }

            public override string ToString() => _metadata.MessageFormatter(this, null);

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

            public static LogValuesMetadata<T0, T1, T2, T3> CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata<T0, T1, T2, T3>(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 4, metadata.PropertyCount);
                return metadata;
            }
        }

        internal readonly struct LogValues<T0, T1, T2, T3, T4> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata<T0, T1, T2, T3, T4> metadata;
            internal readonly T0 _value0;
            internal readonly T1 _value1;
            internal readonly T2 _value2;
            internal readonly T3 _value3;
            internal readonly T4 _value4;

            public LogValues(LogValuesMetadata<T0, T1, T2, T3, T4> metadata, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4)
            {
                this.metadata = metadata;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
            }

            public int Count => 6;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(metadata.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(metadata.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(metadata.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>(metadata.GetValueName(3), _value3);
                        case 4:
                            return new KeyValuePair<string, object?>(metadata.GetValueName(4), _value4);
                        case 5:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", metadata.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public override string ToString() => metadata.MessageFormatter(this, null);

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

            public static LogValuesMetadata<T0, T1, T2, T3, T4> CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata<T0, T1, T2, T3, T4>(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 5, metadata.PropertyCount);
                return metadata;
            }
        }

        internal readonly struct LogValues<T0, T1, T2, T3, T4, T5> : IReadOnlyList<KeyValuePair<string, object?>>
        {
            private readonly LogValuesMetadata<T0, T1, T2, T3, T4, T5> _metadata;
            internal readonly T0 _value0;
            internal readonly T1 _value1;
            internal readonly T2 _value2;
            internal readonly T3 _value3;
            internal readonly T4 _value4;
            internal readonly T5 _value5;

            public LogValues(LogValuesMetadata<T0, T1, T2, T3, T4, T5> metadata, T0 value0, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
            {
                _metadata = metadata;
                _value0 = value0;
                _value1 = value1;
                _value2 = value2;
                _value3 = value3;
                _value4 = value4;
                _value5 = value5;
            }

            public int Count => 7;

            public KeyValuePair<string, object?> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(0), _value0);
                        case 1:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(1), _value1);
                        case 2:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(2), _value2);
                        case 3:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(3), _value3);
                        case 4:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(4), _value4);
                        case 5:
                            return new KeyValuePair<string, object?>(_metadata.GetValueName(5), _value5);
                        case 6:
                            return new KeyValuePair<string, object?>("{OriginalFormat}", _metadata.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public override string ToString() => _metadata.MessageFormatter(this, null);

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

            public static LogValuesMetadata<T0, T1, T2, T3, T4, T5> CreateMetadata(LogLevel level, EventId eventId, string formatString, object[]?[]? parameterMetadata = null)
            {
                var metadata = new LogValuesMetadata<T0, T1, T2, T3, T4, T5>(formatString, level, eventId, parameterMetadata);
                ValidateFormatStringParameterCount(formatString, expectedNamedParameterCount: 6, metadata.PropertyCount);
                return metadata;
            }
        }
    }
}
