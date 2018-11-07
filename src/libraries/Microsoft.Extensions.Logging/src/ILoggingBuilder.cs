// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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