// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public interface ISupportKeyedService
    {
        /// <summary>
        /// Gets the service object of the specified type.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <param name="serviceKey">An object that specifies the key of service object to get.</param>
        /// <returns> A service object of type serviceType. -or- null if there is no service object of type serviceType.</returns>
        object? GetKeyedService(Type serviceType, object serviceKey);
    }

    public static class KeyedService
    {
        public static object AnyKey { get; } = new object();
    }
}
