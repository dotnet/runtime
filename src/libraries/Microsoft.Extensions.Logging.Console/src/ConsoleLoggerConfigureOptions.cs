// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Configures a ConsoleLoggerOptions object from an IConfiguration.
    /// </summary>
    /// <remarks>
    /// Doesn't use ConfigurationBinder in order to allow ConfigurationBinder, and all its dependencies,
    /// to be trimmed. This improves app size and startup.
    /// </remarks>
    [UnsupportedOSPlatform("browser")]
    internal sealed class ConsoleLoggerConfigureOptions : IConfigureOptions<ConsoleLoggerOptions>
    {
        private readonly IConfiguration _configuration;

        public ConsoleLoggerConfigureOptions(ILoggerProviderConfiguration<ConsoleLoggerProvider> providerConfiguration)
        {
            _configuration = providerConfiguration.Configuration;
        }

        public void Configure(ConsoleLoggerOptions options)
        {
            if (ParseBool(_configuration, "DisableColors", out bool disableColors))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.DisableColors = disableColors;
#pragma warning restore CS0618 // Type or member is obsolete
            }

#pragma warning disable CS0618 // Type or member is obsolete
            if (ParseEnum(_configuration, "Format", out ConsoleLoggerFormat format))
            {
                options.Format = format;
            }
#pragma warning restore CS0618 // Type or member is obsolete

            if (_configuration["FormatterName"] is string formatterName)
            {
                options.FormatterName = formatterName;
            }

            if (ParseBool(_configuration, "IncludeScopes", out bool includeScopes))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.IncludeScopes = includeScopes;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (ParseEnum(_configuration, "LogToStandardErrorThreshold", out LogLevel logToStandardErrorThreshold))
            {
                options.LogToStandardErrorThreshold = logToStandardErrorThreshold;
            }

            if (ParseInt(_configuration, "MaxQueueLength", out int maxQueueLength))
            {
                options.MaxQueueLength = maxQueueLength;
            }

            if (ParseEnum(_configuration, "QueueFullMode", out ConsoleLoggerQueueFullMode queueFullMode))
            {
                options.QueueFullMode = queueFullMode;
            }

            if (_configuration["TimestampFormat"] is string timestampFormat)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.TimestampFormat = timestampFormat;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (ParseBool(_configuration, "UseUtcTimestamp", out bool useUtcTimestamp))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.UseUtcTimestamp = useUtcTimestamp;
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        /// <summary>
        /// Parses the configuration value at the specified key into a bool.
        /// </summary>
        /// <returns>true if the value was successfully found and parsed. false if the key wasn't found.</returns>
        /// <exception cref="InvalidOperationException">Thrown when invalid data was found at the specified configuration key.</exception>
        public static bool ParseBool(IConfiguration configuration, string key, out bool value)
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
        public static bool ParseEnum<T>(IConfiguration configuration, string key, out T value) where T : struct
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
        public static bool ParseInt(IConfiguration configuration, string key, out int value)
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
