// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides known service keys for a service type.
    /// </summary>
    public interface IServiceKeysProvider
    {
        /// <summary>
        /// Gets known service keys for the specified <paramref name="serviceType"/>.
        /// </summary>
        /// <param name="serviceType">The type of service to get known keys for.</param>
        /// <returns>An enumeration of known service keys for <paramref name="serviceType"/>.</returns>
        IEnumerable<object?> GetServiceKeys(Type serviceType);
    }
}
