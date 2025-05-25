// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Indicates that the parameter should be bound using the keyed service registered with the specified key.
    /// </summary>
    /// <seealso cref="ServiceKeyAttribute"/>
    /// <seealso cref="ServiceKeyLookupMode"/>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromKeyedServicesAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance.
        /// </summary>
        /// <param name="key">The key of the keyed service to bind to.</param>
        public FromKeyedServicesAttribute(object? key)
        {
            Key = key;
            LookupMode = key == null ? ServiceKeyLookupMode.NullKey : ServiceKeyLookupMode.ExplicitKey;
        }

        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance with <see cref="LookupMode"/> set to <see cref="ServiceKeyLookupMode.InheritKey"/>.
        /// </summary>
        public FromKeyedServicesAttribute()
        {
            Key = null;
            LookupMode = ServiceKeyLookupMode.InheritKey;
        }

        /// <summary>
        /// The key of the keyed service to bind to.
        /// </summary>
        /// <remarks>A <see langword="null"/> value with indicates there is not a key and just the parameter type is used to resolve the service.
        /// This is useful for DI implementations that require an explict way to declare that the parameter should be resolved for unkeyed services.
        /// A <see langword="null"/> value is also used along with <see cref="LookupMode"/> set to <see cref="ServiceKeyLookupMode.InheritKey"/> to indicate that the key should be inherited from the parent scope.
        /// </remarks>
        public object? Key { get; }

        /// <summary>
        /// Gets the mode used to look up the service key.
        /// </summary>
        public ServiceKeyLookupMode LookupMode { get; }
    }
}
