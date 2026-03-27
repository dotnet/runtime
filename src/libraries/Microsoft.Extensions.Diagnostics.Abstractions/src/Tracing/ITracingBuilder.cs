// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Configures the tracing system by registering <see cref="IActivityListener"/> implementations and using
    /// rules to determine which <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Activity"/>
    /// instances are enabled.
    /// </summary>
    public interface ITracingBuilder
    {
        /// <summary>
        /// Gets the application service collection that's used by extension methods to register services.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
