// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// IKeyedServiceProvider is a service provider that can be used to retrieve services using a key in addition
    /// to a type.
    /// </summary>
    public interface IKeyedServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns> A service object of type serviceType. -or- null if there is no service object of type serviceType.</returns>
        object? GetKeyedService(Type serviceType, object? serviceKey);

        /// <summary>
        /// Gets service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/> implementing
        /// this interface.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">The <see cref="ServiceDescriptor.ServiceKey"/> of the service.</param>
        /// <returns>A service object of type <paramref name="serviceType"/>.
        /// Throws an exception if the <see cref="IServiceProvider"/> cannot create the object.</returns>
        object GetRequiredKeyedService(Type serviceType, object? serviceKey);
    }

    /// <summary>
    /// Statics for use with <see cref="IKeyedServiceProvider"/>.
    /// </summary>
    public static class KeyedService
    {
        /// <summary>
        /// Represents a key that matches any key.
        /// </summary>
        public static object AnyKey { get; } = new AnyKeyObj();

        private sealed class AnyKeyObj
        {
            public override string? ToString() => "*";
        }
    }
}
