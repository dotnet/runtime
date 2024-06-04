// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Optional service used to determine if the specified type with the specified service key is available
    /// from the <see cref="IServiceProvider"/>.
    /// </summary>
    public interface IServiceProviderIsKeyedService : IServiceProviderIsService
    {
        /// <summary>
        /// Determines if the specified service type with the specified service key is available from the
        /// <see cref="IServiceProvider"/>.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to test.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <returns>true if the specified service is a available, false if it is not.</returns>
        bool IsKeyedService(Type serviceType, object? serviceKey);
    }
}
