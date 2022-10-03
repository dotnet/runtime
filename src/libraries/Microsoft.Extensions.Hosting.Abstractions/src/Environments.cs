// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Commonly used environment names.
    /// </summary>
    public static class Environments
    {
        /// <summary>
        /// The development environment can enable features that shouldn't be exposed in production. Because of the performance cost, scope validation and dependency validation only happens in development.
        /// </summary>
        public static readonly string Development = "Development";
        /// <summary>
        /// The staging environment can be used to validate app changes before changing the environment to production.
        /// </summary>
        public static readonly string Staging = "Staging";
        /// <summary>
        /// The production environment should be configured to maximize security, performance, and application robustness.
        /// </summary>
        public static readonly string Production = "Production";
    }
}
