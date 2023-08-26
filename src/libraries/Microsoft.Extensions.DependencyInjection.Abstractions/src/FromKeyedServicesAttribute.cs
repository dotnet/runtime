// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Indicates that the parameter should be bound using the keyed service registered with the specified key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class FromKeyedServicesAttribute : Attribute
    {
        /// <summary>
        /// Creates a new <see cref="FromKeyedServicesAttribute"/> instance.
        /// </summary>
        /// <param name="key">The key of the keyed service to bind to.</param>
        public FromKeyedServicesAttribute(object key) => Key = key;

        /// <summary>
        /// The key of the keyed service to bind to.
        /// </summary>
        public object Key { get; }
    }
}
