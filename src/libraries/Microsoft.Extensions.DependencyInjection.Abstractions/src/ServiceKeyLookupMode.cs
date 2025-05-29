// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Specifies how to look up the service key for a parameter.
    /// </summary>
    public enum ServiceKeyLookupMode
    {
        /// <summary>
        /// The key is inherited from the parent service.
        /// </summary>
        InheritKey,

        /// <summary>
        /// A <see langword="null" /> key indicates that the parameter should be resolved from unkeyed services.
        /// This is useful for DI implementations that require an explicit way to declare that the parameter should be resolved from unkeyed services.
        /// </summary>
        NullKey,

        /// <summary>
        /// The key is explicitly specified.
        /// </summary>
        ExplicitKey
    }
}
