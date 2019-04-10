// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;

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
        public static Func<ILogger, IDisposable> DefineScope(string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 0);

            var logValues = new LogValues(formatter);

            return logger => logger.BeginScope(logValues);
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, IDisposable> DefineScope<T1>(string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 1);

            return (logger, arg1) => logger.BeginScope(new LogValues<T1>(formatter, arg1));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, IDisposable> DefineScope<T1, T2>(string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 2);

            return (logger, arg1, arg2) => logger.BeginScope(new LogValues<T1, T2>(formatter, arg1, arg2));
        }

        /// <summary>
        /// Creates a delegate which can be invoked to create a log scope.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter passed to the named format string.</typeparam>
        /// <typeparam name="T2">The type of the second parameter passed to the named format string.</typeparam>
        /// <typeparam name="T3">The type of the third parameter passed to the named format string.</typeparam>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log scope.</returns>
        public static Func<ILogger, T1, T2, T3, IDisposable> DefineScope<T1, T2, T3>(string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 3);

            return (logger, arg1, arg2, arg3) => logger.BeginScope(new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3));
        }

        /// <summary>
        /// Creates a delegate which can be invoked for logging a message.
        /// </summary>
        /// <param name="logLevel">The <see cref="LogLevel"/></param>
        /// <param name="eventId">The event id</param>
        /// <param name="formatString">The named format string</param>
        /// <returns>A delegate which when invoked creates a log message.</returns>
        public static Action<ILogger, Exception> Define(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 0);

            return (logger, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues(formatter), exception, LogValues.Callback);
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
        public static Action<ILogger, T1, Exception> Define<T1>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 1);

            void Log(ILogger logger, T1 arg1, Exception exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1>(formatter, arg1), exception, LogValues<T1>.Callback);
            }

            return (logger, arg1, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, exception);
                }
            };
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
        public static Action<ILogger, T1, T2, Exception> Define<T1, T2>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 2);

            void Log(ILogger logger, T1 arg1, T2 arg2, Exception exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2>(formatter, arg1, arg2), exception, LogValues<T1, T2>.Callback);
            }

            return (logger, arg1, arg2, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    Log(logger, arg1, arg2, exception);
                }
            };
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
        public static Action<ILogger, T1, T2, T3, Exception> Define<T1, T2, T3>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 3);

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, Exception exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2, T3>(formatter, arg1, arg2, arg3), exception, LogValues<T1, T2, T3>.Callback);
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
        public static Action<ILogger, T1, T2, T3, T4, Exception> Define<T1, T2, T3, T4>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 4);

            void Log(ILogger logger, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception exception)
            {
                logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4>(formatter, arg1, arg2, arg3, arg4), exception, LogValues<T1, T2, T3, T4>.Callback);
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
        public static Action<ILogger, T1, T2, T3, T4, T5, Exception> Define<T1, T2, T3, T4, T5>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 5);

            return (logger, arg1, arg2, arg3, arg4, arg5, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4, T5>(formatter, arg1, arg2, arg3, arg4, arg5), exception, LogValues<T1, T2, T3, T4, T5>.Callback);
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
        public static Action<ILogger, T1, T2, T3, T4, T5, T6, Exception> Define<T1, T2, T3, T4, T5, T6>(LogLevel logLevel, EventId eventId, string formatString)
        {
            var formatter = CreateLogValuesFormatter(formatString, expectedNamedParameterCount: 6);

            return (logger, arg1, arg2, arg3, arg4, arg5, arg6, exception) =>
            {
                if (logger.IsEnabled(logLevel))
                {
                    logger.Log(logLevel, eventId, new LogValues<T1, T2, T3, T4, T5, T6>(formatter, arg1, arg2, arg3, arg4, arg5, arg6), exception, LogValues<T1, T2, T3, T4, T5, T6>.Callback);
                }
            };
        }

        private static LogValuesFormatter CreateLogValuesFormatter(string formatString, int expectedNamedParameterCount)
        {
            var logValuesFormatter = new LogValuesFormatter(formatString);

            var actualCount = logValuesFormatter.ValueNames.Count;
            if (actualCount != expectedNamedParameterCount)
            {
                throw new ArgumentException(
                    Resource.FormatUnexpectedNumberOfNamedParameters(formatString, expectedNamedParameterCount, actualCount));
            }

            return logValuesFormatter;
        }

        private readonly struct LogValues : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;

            public LogValues(LogValuesFormatter formatter)
            {
                _formatter = formatter;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    if (index == 0)
                    {
                        return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
                    }
                    throw new IndexOutOfRangeException(nameof(index));
                }
            }

            public int Count => 1;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return this[0];
            }

            public override string ToString() => _formatter.Format();

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private readonly struct LogValues<T0> : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues<T0>, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;

            public LogValues(LogValuesFormatter formatter, T0 value0)
            {
                _formatter = formatter;
                _value0 = value0;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1:
                            return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public int Count => 2;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
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

        private readonly struct LogValues<T0, T1> : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues<T0, T1>, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;

            public LogValues(LogValuesFormatter formatter, T0 value0, T1 value1)
            {
                _formatter = formatter;
                _value0 = value0;
                _value1 = value1;
            }

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2:
                            return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
                        default:
                            throw new IndexOutOfRangeException(nameof(index));
                    }
                }
            }

            public int Count => 3;

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
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
        }

        private readonly struct LogValues<T0, T1, T2> : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues<T0, T1, T2>, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;

            public int Count => 4;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2);
                        case 3:
                            return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
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

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
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

        private readonly struct LogValues<T0, T1, T2, T3> : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3>, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;
            private readonly T3 _value3;

            public int Count => 5;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2);
                        case 3:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3);
                        case 4:
                            return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
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

            private object[] ToArray() => new object[] { _value0, _value1, _value2, _value3 };

            public override string ToString() => _formatter.Format(ToArray());

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
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

        private readonly struct LogValues<T0, T1, T2, T3, T4> : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3, T4>, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;
            private readonly T3 _value3;
            private readonly T4 _value4;

            public int Count => 6;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2);
                        case 3:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3);
                        case 4:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[4], _value4);
                        case 5:
                            return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
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

            private object[] ToArray() => new object[] { _value0, _value1, _value2, _value3, _value4 };

            public override string ToString() => _formatter.Format(ToArray());

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
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

        private readonly struct LogValues<T0, T1, T2, T3, T4, T5> : IReadOnlyList<KeyValuePair<string, object>>
        {
            public static readonly Func<LogValues<T0, T1, T2, T3, T4, T5>, Exception, string> Callback = (state, exception) => state.ToString();

            private readonly LogValuesFormatter _formatter;
            private readonly T0 _value0;
            private readonly T1 _value1;
            private readonly T2 _value2;
            private readonly T3 _value3;
            private readonly T4 _value4;
            private readonly T5 _value5;

            public int Count => 7;

            public KeyValuePair<string, object> this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[0], _value0);
                        case 1:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[1], _value1);
                        case 2:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[2], _value2);
                        case 3:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[3], _value3);
                        case 4:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[4], _value4);
                        case 5:
                            return new KeyValuePair<string, object>(_formatter.ValueNames[5], _value5);
                        case 6:
                            return new KeyValuePair<string, object>("{OriginalFormat}", _formatter.OriginalFormat);
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

            private object[] ToArray() => new object[] { _value0, _value1, _value2, _value3, _value4, _value5 };

            public override string ToString() => _formatter.Format(ToArray());

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
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
