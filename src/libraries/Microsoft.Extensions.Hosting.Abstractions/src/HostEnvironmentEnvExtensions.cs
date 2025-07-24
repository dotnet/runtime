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
        /// Checks if the current host environment name is <see cref="Environments.Development"/>.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <returns><see langword="true"/> if the environment name is <see cref="Environments.Development"/>, otherwise <see langword="false"/>.</returns>
        public static bool IsDevelopment(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(Environments.Development);
        }

        /// <summary>
        /// Checks if the current host environment name is <see cref="Environments.Staging"/>.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <returns><see langword="true"/> if the environment name is <see cref="Environments.Staging"/>, otherwise <see langword="false"/>.</returns>
        public static bool IsStaging(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(Environments.Staging);
        }

        /// <summary>
        /// Checks if the current host environment name is <see cref="Environments.Production"/>.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <returns><see langword="true"/> if the environment name is <see cref="Environments.Production"/>, otherwise <see langword="false"/>.</returns>
        public static bool IsProduction(this IHostEnvironment hostEnvironment)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return hostEnvironment.IsEnvironment(Environments.Production);
        }

        /// <summary>
        /// Compares the current host environment name against the specified value.
        /// </summary>
        /// <param name="hostEnvironment">An instance of <see cref="IHostEnvironment"/>.</param>
        /// <param name="environmentName">Environment name to validate against.</param>
        /// <returns><see langword="true"/> if the specified name is the same as the current environment, otherwise <see langword="false"/>.</returns>
        public static bool IsEnvironment(
            this IHostEnvironment hostEnvironment,
            string environmentName)
        {
            ArgumentNullException.ThrowIfNull(hostEnvironment);

            return string.Equals(
                hostEnvironment.EnvironmentName,
                environmentName,
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
