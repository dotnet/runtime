// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Settings for constructing an <see cref="HostApplicationBuilder"/>.
    /// </summary>
    public sealed class HostApplicationBuilderSettings
    {
        /// <summary>
        /// Initializes an instance of the <see cref="HostApplicationBuilderSettings"/> class.
        /// </summary>
        public HostApplicationBuilderSettings()
        {
        }

        /// <summary>
        /// If <see langword="false"/>, configures the <see cref="HostApplicationBuilder"/> instance with pre-configured defaults.
        /// This has a similar effect to calling <see cref="HostingHostBuilderExtensions.ConfigureDefaults(IHostBuilder, string[])"/>.
        /// </summary>
        /// <remarks>
        ///   The following defaults are applied to the <see cref="IHostBuilder"/>:
        ///     * set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/>
        ///     * load <see cref="IConfiguration"/> from "DOTNET_" prefixed environment variables
        ///     * load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json'
        ///     * load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly
        ///     * load <see cref="IConfiguration"/> from environment variables
        ///     * load <see cref="IConfiguration"/> from supplied command line args
        ///     * configure the <see cref="ILoggerFactory"/> to log to the console, debug, and event source output
        ///     * enables scope validation on the dependency injection container when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development'
        /// </remarks>
        public bool DisableDefaults { get; set; }

        /// <summary>
        /// The command line arguments. This is unused if <see cref="DisableDefaults"/> is <see langword="true"/>.
        /// </summary>
        public string[]? Args { get; set; }

        /// <summary>
        /// Initial configuration sources to be added to the <see cref="HostApplicationBuilder.Configuration"/>. These sources can influence
        /// the <see cref="HostApplicationBuilder.Environment"/> through the use of <see cref="HostDefaults"/> keys. Disposing the built
        /// <see cref="IHost"/> disposes the <see cref="ConfigurationManager"/>.
        /// </summary>
        public ConfigurationManager? Configuration { get; set; }

        /// <summary>
        /// The environment name.
        /// </summary>
        public string? EnvironmentName { get; set; }

        /// <summary>
        /// The application name.
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// The content root path.
        /// </summary>
        public string? ContentRootPath { get; set; }
    }
}
