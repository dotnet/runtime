// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Optional contract used by <see cref="ServiceProviderServiceExtensions.GetRequiredService{T}(IServiceProvider)"/>
    /// to resolve services if supported by <see cref="IServiceProvider"/>.
    /// </summary>
    public interface ISupportRequiredService
    {
        /// <summary>
        /// Gets service of type <paramref name="serviceType"/> from the <see cref="IServiceProvider"/> implementing
        /// this interface.
        /// </summary>
        /// <param name="serviceType">An object that specifies the type of service object to get.</param>
        /// <returns>A service object of type <paramref name="serviceType"/>.
        /// Throws an exception if the <see cref="IServiceProvider"/> cannot create the object.</returns>
        object GetRequiredService(Type serviceType);
    }
}
