// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration.CommandLine;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for registering <see cref="CommandLineConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class CommandLineConfigurationExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the command line.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="args">The command line args.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder configurationBuilder, string[] args)
        {
            return configurationBuilder.AddCommandLine(args, switchMappings: null);
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the command line using the specified switch mappings.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="args">The command line args.</param>
        /// <param name="switchMappings">The switch mappings.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddCommandLine(
            this IConfigurationBuilder configurationBuilder,
            string[] args,
            IDictionary<string, string> switchMappings)
        {
            configurationBuilder.Add(new CommandLineConfigurationSource { Args = args, SwitchMappings = switchMappings });
            return configurationBuilder;
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from the command line.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">Configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddCommandLine(this IConfigurationBuilder builder, Action<CommandLineConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}
