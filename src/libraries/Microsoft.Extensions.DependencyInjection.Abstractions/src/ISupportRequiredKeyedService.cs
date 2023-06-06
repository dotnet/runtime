// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Optional contract used by <see cref="ServiceProviderServiceExtensions.GetRequiredService{T}(IServiceProvider)"/>
    /// to resolve services if supported by <see cref="IServiceProvider"/>.
    /// </summary>
    public interface ISupportRequiredKeyedService
    {
        /// <summary>
        /// Gets service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/> implementing
        /// this interface.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <returns>A service object of type <paramref name="serviceType"/>.
        /// Throws an exception if the <see cref="IServiceProvider"/> cannot create the object.</returns>
        object GetRequiredKeyedService(Type serviceType, object serviceKey);
    }
}
