// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Indicates that the parameter should be bound using the keyed service registered with the specified key.
    /// </summary>
    /// <seealso cref="ServiceKeyAttribute"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromKeyedServicesAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance.
        /// </summary>
        /// <param name="key">The key of the keyed service to bind to.</param>
        public FromKeyedServicesAttribute(object? key) => Key = key;

        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance with <see cref="Key"/> set to <see cref="FromServiceKey"/>.
        /// </summary>
        public FromKeyedServicesAttribute() => Key = FromServiceKey;

        /// <summary>
        /// The key of the keyed service to bind to.
        /// </summary>
        /// <remarks>A <see langword="null"/> value indicates there is not a key and just the parameter type is used to resolve the service.
        /// This is useful for DI implementations that require an explict way to declare that the parameter should be resolved for unkeyed services.
        /// </remarks>
        public object? Key { get; }

        /// <summary>
        /// Indicates that a parameter represents a service should be resolved from the same key that the current service was resolved with.
        /// </summary>
        public static object FromServiceKey { get; } = new FromServiceKeyObj();

        private sealed class FromServiceKeyObj { }
    }
}
