// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Defines a disposable service scope.
    /// </summary>
    /// <remarks>
    /// The <see cref="System.IDisposable.Dispose"/> method ends the scope lifetime. Once Dispose
    /// is called, any scoped services that have been resolved from
    /// <see cref="Microsoft.Extensions.DependencyInjection.IServiceScope.ServiceProvider"/> will be
    /// disposed.
    /// </remarks>
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="System.IServiceProvider"/> used to resolve dependencies from the scope.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
