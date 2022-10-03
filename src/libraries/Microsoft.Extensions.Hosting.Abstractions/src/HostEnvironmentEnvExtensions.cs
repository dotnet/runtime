// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for <see cref="IHostEnvironment"/>.
    /// </summary>
    public static class HostEnvironmentEnvExtensions
    {
        /// <summary>
        /// Checks if the current host environment name is <see cref="EnvironmentName.Development"/>.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <returns>True if the environment name is <see cref="EnvironmentName.Development"/>, otherwise false.</returns>
        public static bool IsDevelopment(this IHostEnvironment hostEnvironment)
        {
            ThrowHelper.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(Environments.Development);
        }

        /// <summary>
        /// Checks if the current host environment name is <see cref="EnvironmentName.Staging"/>.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <returns>True if the environment name is <see cref="EnvironmentName.Staging"/>, otherwise false.</returns>
        public static bool IsStaging(this IHostEnvironment hostEnvironment)
        {
            ThrowHelper.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(Environments.Staging);
        }

        /// <summary>
        /// Checks if the current host environment name is <see cref="EnvironmentName.Production"/>.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <returns>True if the environment name is <see cref="EnvironmentName.Production"/>, otherwise false.</returns>
        public static bool IsProduction(this IHostEnvironment hostEnvironment)
        {
            ThrowHelper.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(Environments.Production);
        }

        /// <summary>
        /// Compares the current host environment name against the specified value.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <param name="environmentName">Environment name to validate against.</param>
        /// <returns>True if the specified name is the same as the current environment, otherwise false.</returns>
        public static bool IsEnvironment(
            this IHostEnvironment hostEnvironment,
            string environmentName)
        {
            ThrowHelper.ThrowIfNull(hostEnvironment);

            return string.Equals(
                hostEnvironment.EnvironmentName,
                environmentName,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
