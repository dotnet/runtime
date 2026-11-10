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
        /// Gets or sets a value that indicates whether the <see cref="HostApplicationBuilder"/> instance is configured with pre-configured defaults.
        /// </summary>
        /// <value>
        /// <see langword="false"/> if the <see cref="HostApplicationBuilder"/> instance is configured with pre-configured defaults.
        /// This has a similar effect to calling <see cref="HostingHostBuilderExtensions.ConfigureDefaults(IHostBuilder, string[])"/>.
        /// </value>
        /// <remarks>
        ///   The following defaults are applied to the <see cref="IHostBuilder"/>:
        ///   <list type="bullet">
        ///     <item>set the <see cref="IHostEnvironment.ContentRootPath"/> to the result of <see cref="Directory.GetCurrentDirectory()"/></item>
        ///     <item>load <see cref="IConfiguration"/> from "DOTNET_" prefixed environment variables</item>
        ///     <item>load <see cref="IConfiguration"/> from 'appsettings.json' and 'appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json'</item>
        ///     <item>load <see cref="IConfiguration"/> from User Secrets when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development' using the entry assembly</item>
        ///     <item>load <see cref="IConfiguration"/> from environment variables</item>
        ///     <item>load <see cref="IConfiguration"/> from supplied command line args</item>
        ///     <item>configure the <see cref="ILoggerFactory"/> to log to the console, debug, and event source output</item>
        ///     <item>enables scope validation on the dependency injection container when <see cref="IHostEnvironment.EnvironmentName"/> is 'Development'</item>
        ///   </list>
        /// </remarks>
        public bool DisableDefaults { get; set; }

        /// <summary>
        /// Gets or sets the command-line arguments to add to the <see cref="HostApplicationBuilder.Configuration"/>.
        /// </summary>
        public string[]? Args { get; set; }

        /// <summary>
        /// Gets or sets the initial configuration sources to be added to the <see cref="HostApplicationBuilder.Configuration"/>. These sources can influence
        /// the <see cref="HostApplicationBuilder.Environment"/> through the use of <see cref="HostDefaults"/> keys. Disposing the built
        /// <see cref="IHost"/> disposes the <see cref="ConfigurationManager"/>.
        /// </summary>
        public ConfigurationManager? Configuration { get; set; }

        /// <summary>
        /// Gets or sets the environment name.
        /// </summary>
        public string? EnvironmentName { get; set; }

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string? ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the content root path.
        /// </summary>
        public string? ContentRootPath { get; set; }
    }
}
