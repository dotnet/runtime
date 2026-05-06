// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Retrieves services using a key and a type.
    /// </summary>
    public interface IKeyedServiceProvider : IServiceProvider
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns><para>A service object of type <paramref name="serviceType"/>.</para>
        /// <para>-or-</para>
        /// <para><see langword="null"/> if there is no service object of type <paramref name="serviceType"/>.</para></returns>
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
    /// Provides static APIs for use with <see cref="IKeyedServiceProvider"/>.
    /// </summary>
    public static class KeyedService
    {
        /// <summary>
        /// Gets a key that matches any key.
        /// </summary>
        public static object AnyKey { get; } = new AnyKeyObj();

        private sealed class AnyKeyObj
        {
            public override string? ToString() => "*";
        }
    }
}
