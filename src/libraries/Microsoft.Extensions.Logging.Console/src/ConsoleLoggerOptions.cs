// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for a <see cref="ConsoleLogger"/>.
    /// </summary>
    public class ConsoleLoggerOptions
    {
        /// <summary>
        /// Disables colors when <see langword="true" />.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.DisableColors has been deprecated. Use SimpleConsoleFormatterOptions.ColorBehavior instead.")]
        public bool DisableColors { get; set; }

#pragma warning disable CS0618
        private ConsoleLoggerFormat _format = ConsoleLoggerFormat.Default;
        /// <summary>
        /// Gets or sets log message format. Defaults to <see cref="ConsoleLoggerFormat.Default" />.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.Format has been deprecated. Use ConsoleLoggerOptions.FormatterName instead.")]
        public ConsoleLoggerFormat Format
        {
            get => _format;
            set
            {
                if (value < ConsoleLoggerFormat.Default || value > ConsoleLoggerFormat.Systemd)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _format = value;
            }
#pragma warning restore CS0618
        }

        /// <summary>
        /// Name of the log message formatter to use. Defaults to <c>simple</c>.
        /// </summary>
        public string? FormatterName { get; set; }

        /// <summary>
        /// Includes scopes when <see langword="true" />.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.IncludeScopes has been deprecated. Use ConsoleFormatterOptions.IncludeScopes instead.")]
        public bool IncludeScopes { get; set; }

        /// <summary>
        /// Gets or sets value indicating the minimum level of messages that would get written to <c>Console.Error</c>.
        /// </summary>
        public LogLevel LogToStandardErrorThreshold { get; set; } = LogLevel.None;

        /// <summary>
        /// Gets or sets format string used to format timestamp in logging messages. Defaults to <c>null</c>.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.TimestampFormat has been deprecated. Use ConsoleFormatterOptions.TimestampFormat instead.")]
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets indication whether or not UTC timezone should be used to format timestamps in logging messages. Defaults to <c>false</c>.
        /// </summary>
        [System.ObsoleteAttribute("ConsoleLoggerOptions.UseUtcTimestamp has been deprecated. Use ConsoleFormatterOptions.UseUtcTimestamp instead.")]
        public bool UseUtcTimestamp { get; set; }

        private ConsoleLoggerQueueFullMode _queueFullMode = ConsoleLoggerQueueFullMode.Wait;
        /// <summary>
        /// Gets or sets the desired console logger behavior when the queue becomes full. Defaults to <c>Wait</c>.
        /// </summary>
        public ConsoleLoggerQueueFullMode QueueFullMode
        {
            get => _queueFullMode;
            set
            {
                if (value != ConsoleLoggerQueueFullMode.Wait && value != ConsoleLoggerQueueFullMode.DropWrite)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.QueueModeNotSupported, nameof(value)));
                }
                _queueFullMode = value;
            }
        }

        internal const int DefaultMaxQueueLengthValue = 2500;
        private int _maxQueuedMessages = DefaultMaxQueueLengthValue;

        /// <summary>
        /// Gets or sets the maximum number of enqueued messages. Defaults to 2500.
        /// </summary>
        public int MaxQueueLength
        {
            get => _maxQueuedMessages;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.MaxQueueLengthBadValue, nameof(value)));
                }

                _maxQueuedMessages = value;
            }
        }

        internal void Configure(IConfiguration configuration)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (ParseBool(configuration, nameof(DisableColors), out bool disableColors))
            {
                DisableColors = disableColors;
            }
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
            if (ParseEnum(configuration, nameof(Format), out ConsoleLoggerFormat format))
            {
                Format = format;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (configuration[nameof(FormatterName)] is string formatterName)
            {
                FormatterName = formatterName;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (ParseBool(configuration, nameof(IncludeScopes), out bool includeScopes))
            {
                IncludeScopes = includeScopes;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (ParseEnum(configuration, nameof(LogToStandardErrorThreshold), out LogLevel logToStandardErrorThreshold))
            {
                LogToStandardErrorThreshold = logToStandardErrorThreshold;
            }

            if (ParseInt(configuration, nameof(MaxQueueLength), out int maxQueueLength))
            {
                MaxQueueLength = maxQueueLength;
            }

            if (ParseEnum(configuration, nameof(QueueFullMode), out ConsoleLoggerQueueFullMode queueFullMode))
            {
                QueueFullMode = queueFullMode;
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (configuration[nameof(TimestampFormat)] is string timestampFormat)
            {
                TimestampFormat = timestampFormat;
            }
#pragma warning restore CS0618 // Type or member is obsolete

#pragma warning disable CS0618 // Type or member is obsolete
            if (ParseBool(configuration, nameof(UseUtcTimestamp), out bool useUtcTimestamp))
            {
                UseUtcTimestamp = useUtcTimestamp;
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        /// <summary>
        /// Parses the configuration value at the specified key into a bool.
        /// </summary>
        /// <returns>true if the value was successfully found and parsed. false if the key wasn't found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when invalid data was found at the specified configuration key.</exception>
        internal static bool ParseBool(IConfiguration configuration, string key, out bool value)
        {
            if (configuration[key] is string valueString)
            {
                try
                {
                    value = bool.Parse(valueString);
                    return true;
                }
                catch (Exception e)
                {
                    ThrowInvalidConfigurationException(configuration, key, typeof(bool), e);
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Parses the configuration value at the specified key into an enum.
        /// </summary>
        /// <returns>true if the value was successfully found and parsed. false if the key wasn't found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when invalid data was found at the specified configuration key.</exception>
        internal static bool ParseEnum<T>(IConfiguration configuration, string key, out T value) where T : struct
        {
            if (configuration[key] is string valueString)
            {
                try
                {
                    value =
#if NETFRAMEWORK || NETSTANDARD2_0
                        (T)Enum.Parse(typeof(T), valueString, ignoreCase: true);
#else
                        Enum.Parse<T>(valueString, ignoreCase: true);
#endif
                    return true;
                }
                catch (Exception e)
                {
                    ThrowInvalidConfigurationException(configuration, key, typeof(T), e);
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Parses the configuration value at the specified key into an int.
        /// </summary>
        /// <returns>true if the value was successfully found and parsed. false if the key wasn't found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when invalid data was found at the specified configuration key.</exception>
        internal static bool ParseInt(IConfiguration configuration, string key, out int value)
        {
            if (configuration[key] is string valueString)
            {
                try
                {
                    value = int.Parse(valueString, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
                    return true;
                }
                catch (Exception e)
                {
                    ThrowInvalidConfigurationException(configuration, key, typeof(int), e);
                }
            }

            value = default;
            return false;
        }

        private static void ThrowInvalidConfigurationException(IConfiguration configuration, string key, Type valueType, Exception innerException)
        {
            IConfigurationSection section = configuration.GetSection(key);
            throw new InvalidOperationException(SR.Format(SR.InvalidConfigurationData, section.Path, valueType), innerException);
        }
    }
}
