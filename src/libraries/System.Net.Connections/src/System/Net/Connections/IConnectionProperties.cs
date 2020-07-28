// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Net.Connections
{
    /// <summary>
    /// A container for connection properties.
    /// </summary>
    public interface IConnectionProperties
    {
        /// <summary>
        /// Retrieves a connection property, if it exists.
        /// </summary>
        /// <param name="propertyKey">The key of the property to retrieve.</param>
        /// <param name="property">If the property was found, retrieves the property. Otherwise, null.</param>
        /// <returns>If the property was found, true. Otherwise, false.</returns>
        bool TryGet(Type propertyKey, [NotNullWhen(true)] out object? property);
    }
}
