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
            if (_configuration["DisableColors"] is string disableColors)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.DisableColors = bool.Parse(disableColors);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (_configuration["Format"] is string format)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.Format = ParseEnum<ConsoleLoggerFormat>(format);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (_configuration["FormatterName"] is string formatterName)
            {
                options.FormatterName = formatterName;
            }

            if (_configuration["IncludeScopes"] is string includeScopes)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.IncludeScopes = bool.Parse(includeScopes);
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (_configuration["LogToStandardErrorThreshold"] is string logToStandardErrorThreshold)
            {
                options.LogToStandardErrorThreshold = ParseEnum<LogLevel>(logToStandardErrorThreshold);
            }

            if (_configuration["MaxQueueLength"] is string maxQueueLength)
            {
                options.MaxQueueLength = int.Parse(maxQueueLength, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);
            }

            if (_configuration["QueueFullMode"] is string queueFullMode)
            {
                options.QueueFullMode = ParseEnum<ConsoleLoggerQueueFullMode>(queueFullMode);
            }

            if (_configuration["TimestampFormat"] is string timestampFormat)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.TimestampFormat = timestampFormat;
#pragma warning restore CS0618 // Type or member is obsolete
            }

            if (_configuration["UseUtcTimestamp"] is string useUtcTimestamp)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                options.UseUtcTimestamp = bool.Parse(useUtcTimestamp);
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        public static T ParseEnum<T>(string value) where T : struct =>
#if NETSTANDARD || NETFRAMEWORK
            (T)Enum.Parse(typeof(T), value, ignoreCase: true);
#else
            Enum.Parse<T>(value, ignoreCase: true);
#endif
    }
}
