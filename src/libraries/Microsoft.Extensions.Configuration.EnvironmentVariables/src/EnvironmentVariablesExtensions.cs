// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Provides extension methods for registering <see cref="EnvironmentVariablesConfigurationProvider"/> with <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class EnvironmentVariablesExtensions
    {
        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Add(new EnvironmentVariablesConfigurationSource());
            return configurationBuilder;
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables
        /// with a specified prefix.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="prefix">The prefix that environment variable names must start with. The prefix will be removed from the environment variable names.
        /// The prefix is transformed by <see cref="EnvironmentVariablesConfigurationSource.DefaultTransformation"/> and then matched against transformed environment variable name, so it should be specified in the pre-transformation
        /// form (for example <c>Logging__</c> for <c>Logging:</c>).</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddEnvironmentVariables(
            this IConfigurationBuilder configurationBuilder,
            string? prefix)
        {
            configurationBuilder.Add(new EnvironmentVariablesConfigurationSource { Prefix = prefix });
            return configurationBuilder;
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables
        /// with a specified prefix and variable name transformation.
        /// </summary>
        /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="prefix">The prefix that environment variable names must start with. The prefix will be removed from the environment variable names.
        /// The prefix is transformed by <paramref name="variableNameTransformation"/> and then matched against transformed environment variable name, so it should be specified in the pre-transformation
        /// form.</param>
        /// <param name="variableNameTransformation">A function that transforms environment variable names. When set, this
        /// completely replaces the default behavior. When <see langword="null"/>,
        /// <see cref="EnvironmentVariablesConfigurationSource.DefaultTransformation"/> is used.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddEnvironmentVariables(
            this IConfigurationBuilder configurationBuilder,
            string? prefix,
            Func<string, string>? variableNameTransformation)
        {
            configurationBuilder.Add(new EnvironmentVariablesConfigurationSource
            {
                Prefix = prefix,
                VariableNameTransformation = variableNameTransformation
            });
            return configurationBuilder;
        }

        /// <summary>
        /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values from environment variables.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/> to add to.</param>
        /// <param name="configureSource">The action that configures the source.</param>
        /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
        public static IConfigurationBuilder AddEnvironmentVariables(this IConfigurationBuilder builder, Action<EnvironmentVariablesConfigurationSource>? configureSource)
            => builder.Add(configureSource);
    }
}
