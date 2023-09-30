// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Settings for a <see cref="ConsoleLogger"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("This type is retained only for compatibility. The recommended alternative is ConsoleLoggerOptions.")]
    public class ConfigurationConsoleLoggerSettings : IConsoleLoggerSettings
    {
        internal readonly IConfiguration _configuration;

        /// <summary>
        /// Creates a new instance of <see cref="ConfigurationConsoleLoggerSettings"/>.
        /// </summary>
        /// <param name="configuration">provides access to configuration values.</param>
        public ConfigurationConsoleLoggerSettings(IConfiguration configuration)
        {
            _configuration = configuration;
            ChangeToken = configuration.GetReloadToken();
        }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> propagates notifications that a change has occurred.
        /// </summary>
        public IChangeToken? ChangeToken { get; private set; }

        /// <summary>
        /// Gets a value indicating whether scopes should be included in the message.
        /// </summary>
        public bool IncludeScopes
        {
            get
            {
                bool includeScopes;
                var value = _configuration["IncludeScopes"];
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }
                else if (bool.TryParse(value, out includeScopes))
                {
                    return includeScopes;
                }
                else
                {
                    var message = $"Configuration value '{value}' for setting '{nameof(IncludeScopes)}' is not supported.";
                    throw new InvalidOperationException(message);
                }
            }
        }

        /// <summary>
        /// Reload the settings from the configuration.
        /// </summary>
        /// <returns>The reloaded settings.</returns>
        public IConsoleLoggerSettings Reload()
        {
            ChangeToken = null!;
            return new ConfigurationConsoleLoggerSettings(_configuration);
        }

        /// <summary>
        /// Gets the log level for the specified switch.
        /// </summary>
        /// <param name="name">The name of the switch to look up</param>
        /// <param name="level">An out parameter that will be set to the value of the switch if it is found. If the switch is not found, the method returns false and sets the value of level to LogLevel.None</param>
        /// <returns>True if the switch was found, otherwise false.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool TryGetSwitch(string name, out LogLevel level)
        {
            var switches = _configuration.GetSection("LogLevel");
            if (switches == null)
            {
                level = LogLevel.None;
                return false;
            }

            var value = switches[name];
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse<LogLevel>(value, true, out level))
            {
                return true;
            }
            else
            {
                var message = $"Configuration value '{value}' for category '{name}' is not supported.";
                throw new InvalidOperationException(message);
            }
        }
    }
}
