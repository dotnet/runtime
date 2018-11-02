// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// A factory for creating instances of <see cref="IServiceScope"/>, which is used to create
    /// services within a scope.
    /// </summary>
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// Create an <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/> which
        /// contains an <see cref="System.IServiceProvider"/> used to resolve dependencies from a
        /// newly created scope.
        /// </summary>
        /// <returns>
        /// An <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope"/> controlling the
        /// lifetime of the scope. Once this is disposed, any scoped services that have been resolved
        /// from the <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope.ServiceProvider"/>
        /// will also be disposed.
        /// </returns>
        IServiceScope CreateScope();
    }
}
