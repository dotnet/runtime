// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// An interface for configuring logging providers.
    /// </summary>
    public interface ILoggingBuilder
    {
        /// <summary>
        /// Gets the <see cref="IServiceCollection"/> where Logging services are configured.
        /// </summary>
        IServiceCollection Services { get; }
    }
}
